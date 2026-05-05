//! Embedded HTTP + WebSocket server for bot monitoring.
//!
//! Serves a single-page app at `/` and provides API endpoints:
//!   GET /api/status     — current bot state as JSON
//!   GET /api/settings   — current settings as JSON
//!   GET /ws             — WebSocket for real-time updates
//!
//! Uses Winsock2 directly (ws2_32) since we're Windows-only.

const std = @import("std");
const log = @import("log");
const win = @import("winapi");
const bot_mod = @import("bot.zig");
const config_mod = @import("config.zig");

// ---------------------------------------------------------------------------
// Embedded SPA files (index.html + app.js compiled into the binary)
// ---------------------------------------------------------------------------

const INDEX_HTML = @embedFile("web/index.html");
const APP_JS = @embedFile("web/app.js");

// ---------------------------------------------------------------------------
// BotStatus — JSON-serializable snapshot of bot state
// ---------------------------------------------------------------------------

pub const BotStatus = struct {
    running: bool,
    state: []const u8,
    tick: u32,
    hotspot_zone: []const u8,
    units_count: usize,
    players_count: usize,

    pub fn fromContext(ctx: *bot_mod.BotContext) BotStatus {
        const current = ctx.currentState();
        return .{
            .running = ctx.running,
            .state = if (current) |s| s.stateName() else "idle",
            .tick = ctx.tick,
            .hotspot_zone = ctx.hotspot.zone,
            .units_count = 0, // TODO: obj_mgr.allUnits().len,
            .players_count = 0, // TODO: obj_mgr.allPlayers().len,
        };
    }

    pub fn toJsonString(self: BotStatus, allocator: std.mem.Allocator) ![]const u8 {
        return std.json.Stringify.valueAlloc(allocator, self, .{ .whitespace = .indent_2 });
    }
};

// ---------------------------------------------------------------------------
// WebServer
// ---------------------------------------------------------------------------

pub const WebServer = struct {
    allocator: std.mem.Allocator,
    port: u16,
    listen_sock: win.SOCKET,
    bot_ctx: ?*bot_mod.BotContext,
    settings: ?*config_mod.BotSettings,
    running: bool,

    pub fn init(allocator: std.mem.Allocator, port: u16) !WebServer {
        // Initialize Winsock2.
        var wsa_data: win.WSADATA = undefined;
        const rc = win.WSAStartup(0x0202, &wsa_data); // version 2.2
        if (rc != 0) {
            log.err("[web] WSAStartup failed: {d}", .{win.WSAGetLastError()});
            return error.WSAStartupFailed;
        }

        const sock = win.socket(win.AF_INET, win.SOCK_STREAM, win.IPPROTO_TCP);
        if (sock == win.INVALID_SOCKET) {
            log.err("[web] socket() failed: {d}", .{win.WSAGetLastError()});
            return error.SocketFailed;
        }

        // Allow address reuse.
        var reuse: c_int = 1;
        _ = win.setsockopt(sock, win.SOL_SOCKET, win.SO_REUSEADDR, &reuse, @sizeOf(c_int));

        // Bind to port.
        const addr = win.sockaddr_in{
            .sin_family = win.AF_INET,
            .sin_port = win.htons(port),
            .sin_addr = win.INADDR_ANY,
            .sin_zero = .{0} ** 8,
        };

        if (win.bind(sock, &addr, @sizeOf(win.sockaddr_in)) == win.SOCKET_ERROR) {
            log.err("[web] bind() failed: {d}", .{win.WSAGetLastError()});
            _ = win.closesocket(sock);
            return error.BindFailed;
        }

        if (win.listen(sock, 4) == win.SOCKET_ERROR) {
            log.err("[web] listen() failed: {d}", .{win.WSAGetLastError()});
            _ = win.closesocket(sock);
            return error.ListenFailed;
        }

        log.info("[web] HTTP server listening on http://127.0.0.1:{d}/", .{port});

        return .{
            .allocator = allocator,
            .port = port,
            .listen_sock = sock,
            .bot_ctx = null,
            .settings = null,
            .running = true,
        };
    }

    pub fn deinit(self: *WebServer) void {
        self.running = false;
        _ = win.closesocket(self.listen_sock);
        _ = win.WSACleanup();
    }

    pub fn setBotContext(self: *WebServer, ctx: *bot_mod.BotContext) void {
        self.bot_ctx = ctx;
    }

    pub fn setSettings(self: *WebServer, settings: *config_mod.BotSettings) void {
        self.settings = settings;
    }

    /// Start the web server in a background thread.
    pub fn startBackground(self: *WebServer) void {
        const thread = win.CreateThread(null, 0, webServerThread, @ptrCast(@alignCast(self)), 0, null);
        if (thread) |h| {
            _ = win.CloseHandle(h);
        }
    }

    fn webServerThread(param: win.LPVOID) callconv(.winapi) win.DWORD {
        const self: *WebServer = @ptrCast(@alignCast(param orelse return 1));
        self.runLoop();
        return 0;
    }

    /// Main accept loop — runs in background thread.
    pub fn runLoop(self: *WebServer) void {
        while (self.running) {
            var client_addr: win.sockaddr_in = undefined;
            var client_addr_len: c_int = @sizeOf(win.sockaddr_in);

            const client_sock = win.accept(self.listen_sock, &client_addr, &client_addr_len);
            if (client_sock == win.INVALID_SOCKET) {
                if (!self.running) break;
                continue;
            }

            self.handleClient(client_sock) catch {};
            _ = win.closesocket(client_sock);
        }
    }

    fn handleClient(self: *WebServer, sock: win.SOCKET) !void {
        // Read request.
        var buf: [4096]u8 = undefined;
        const n = win.recv(sock, &buf, @as(c_int, @intCast(buf.len)), 0);
        if (n <= 0) return;

        const request = buf[0..@as(usize, @intCast(n))];

        // Parse HTTP method + path.
        const line_end = std.mem.indexOf(u8, request, "\r\n") orelse return;
        const request_line = request[0..line_end];

        // "GET /path HTTP/1.1"
        const space1 = std.mem.indexOf(u8, request_line, " ") orelse return;
        const space2 = std.mem.indexOfPos(u8, request_line, space1 + 1, " ") orelse return;
        const path = request_line[space1 + 1 .. space2];

        // Route.
        if (std.mem.eql(u8, path, "/")) {
            try self.sendResponse(sock, "200 OK", "text/html", INDEX_HTML);
        } else if (std.mem.eql(u8, path, "/app.js")) {
            try self.sendResponse(sock, "200 OK", "application/javascript", APP_JS);
        } else if (std.mem.eql(u8, path, "/api/status")) {
            try self.handleApiStatus(sock);
        } else if (std.mem.eql(u8, path, "/api/settings")) {
            try self.handleApiSettings(sock);
        } else if (std.mem.startsWith(u8, path, "/ws")) {
            try self.handleWebSocket(sock, request);
        } else {
            try self.sendResponse(sock, "404 Not Found", "text/plain", "Not Found");
        }
    }

    fn sendResponse(self: *WebServer, sock: win.SOCKET, status: []const u8, content_type: []const u8, body: []const u8) !void {
        _ = self;
        var header_buf: [512]u8 = undefined;
        const header = std.fmt.bufPrint(&header_buf,
            "HTTP/1.1 {s}\r\n" ++
            "Content-Type: {s}\r\n" ++
            "Content-Length: {d}\r\n" ++
            "Connection: close\r\n" ++
            "Access-Control-Allow-Origin: *\r\n" ++
            "\r\n", .{ status, content_type, body.len }) catch return;

        _ = win.send(sock, header.ptr, @as(c_int, @intCast(header.len)), 0);
        _ = win.send(sock, body.ptr, @as(c_int, @intCast(body.len)), 0);
    }

    fn handleApiStatus(self: *WebServer, sock: win.SOCKET) !void {
        const status = if (self.bot_ctx) |ctx|
            BotStatus.fromContext(ctx)
        else
            BotStatus{
                .running = false,
                .state = "not_started",
                .tick = 0,
                .hotspot_zone = "",
                .units_count = 0,
                .players_count = 0,
            };

        const json_str = try status.toJsonString(self.allocator);
        defer self.allocator.free(json_str);

        try self.sendResponse(sock, "200 OK", "application/json", json_str);
    }

    fn handleApiSettings(self: *WebServer, sock: win.SOCKET) !void {
        const json_str = if (self.settings) |settings|
            try config_mod.toJson(self.allocator, settings)
        else
            try self.allocator.dupe(u8, "{}");
        defer self.allocator.free(json_str);

        try self.sendResponse(sock, "200 OK", "application/json", json_str);
    }

    fn handleWebSocket(self: *WebServer, sock: win.SOCKET, request: []const u8) !void {
        // Minimal WebSocket handshake.
        // Find the Sec-WebSocket-Key header.
        const key_header = "Sec-WebSocket-Key: ";
        const key_start = std.mem.indexOf(u8, request, key_header) orelse {
            try self.sendResponse(sock, "400 Bad Request", "text/plain", "Missing WebSocket key");
            return;
        };
        const key_value_start = key_start + key_header.len;
        const key_end = std.mem.indexOfPos(u8, request, key_value_start, "\r\n") orelse return;
        const client_key = request[key_value_start..key_end];

        // Compute accept key: SHA1(client_key + "258EAFA5-E914-47DA-95CA-5AB5DC65BDB1") then base64.
        const ws_guid = "258EAFA5-E914-47DA-95CA-5AB5DC65BDB1";
        var combined_buf: [256]u8 = undefined;
        const combined = std.fmt.bufPrint(&combined_buf, "{s}{s}", .{ client_key, ws_guid }) catch return;

        var sha1_hash: [20]u8 = undefined;
        std.crypto.hash.Sha1.hash(combined, &sha1_hash, .{});

        var accept_key: [28]u8 = undefined;
        _ = std.base64.standard.Encoder.encode(&accept_key, &sha1_hash);

        // Send handshake response.
        var response_buf: [512]u8 = undefined;
        const response = std.fmt.bufPrint(&response_buf,
            "HTTP/1.1 101 Switching Protocols\r\n" ++
            "Upgrade: websocket\r\n" ++
            "Connection: Upgrade\r\n" ++
            "Sec-WebSocket-Accept: {s}\r\n" ++
            "\r\n", .{accept_key}) catch return;

        _ = win.send(sock, response.ptr, @as(c_int, @intCast(response.len)), 0);

        // WebSocket connected — send periodic status updates.
        var update_count: u32 = 0;
        while (self.running and update_count < 200) : (update_count += 1) {
            const status = if (self.bot_ctx) |ctx|
                BotStatus.fromContext(ctx)
            else
                BotStatus{
                    .running = false,
                    .state = "not_started",
                    .tick = 0,
                    .hotspot_zone = "",
                    .units_count = 0,
                    .players_count = 0,
                };

            const json_str = status.toJsonString(self.allocator) catch continue;
            defer self.allocator.free(json_str);

            // Send WebSocket text frame.
            self.sendWsTextFrame(sock, json_str) catch break;

            win.Sleep(1000); // 1s update interval
        }
    }

    fn sendWsTextFrame(self: *WebServer, sock: win.SOCKET, payload: []const u8) !void {
        _ = self;
        // WebSocket text frame: FIN=1, opcode=1 (text).
        var header: [10]u8 = undefined;
        var header_len: usize = 2;

        header[0] = 0x81; // FIN + text opcode

        if (payload.len <= 125) {
            header[1] = @as(u8, @intCast(payload.len));
        } else if (payload.len <= 65535) {
            header[1] = 126;
            header[2] = @as(u8, @truncate(payload.len >> 8));
            header[3] = @as(u8, @truncate(payload.len));
            header_len = 4;
        } else {
            header[1] = 127;
            // For simplicity, assume payload < 2^64
            var len = payload.len;
            var i: usize = 0;
            while (i < 8) : (i += 1) {
                header[9 - i] = @as(u8, @truncate(len));
                len >>= 8;
            }
            header_len = 10;
        }

        _ = win.send(sock, &header, @as(c_int, @intCast(header_len)), 0);
        _ = win.send(sock, payload.ptr, @as(c_int, @intCast(payload.len)), 0);
    }
};
