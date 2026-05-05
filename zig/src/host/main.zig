//! bloog-host — out-of-process bot host.
//!
//! Connects to bloog-stub via named pipe, enumerates game objects,
//! and runs bot logic. This is the main entry point for the host process.
//!
//! Modes:
//!   --poll     Just enumerate objects and print counts (default).
//!   --bot      Run the full bot state machine (grind/combat/loot/rest).
//!
//! Flags:
//!   --web      Enable web UI (HTTP + WebSocket on port 8080).
//!   --config   Path to botSettings.json (default: ./botSettings.json).

const std = @import("std");
const builtin = @import("builtin");
const log = @import("log");
const win = @import("winapi");
const stub_client = @import("stub_client.zig");
const obj_mgr = @import("object_manager.zig");
const game = @import("game_objects.zig");
const bot_mod = @import("bot.zig");
const nav = @import("navigation.zig");
const hotspot_mod = @import("hotspot.zig");
const config_mod = @import("config.zig");
const web_mod = @import("web.zig");

comptime {
    if (builtin.os.tag != .windows) {
        @compileError("bloog-host only supports Windows targets (x86-windows-gnu).");
    }
}

const default_hotspot = hotspot_mod.Hotspot{
    .id = 0,
    .zone = "Elwynn Forest",
    .faction = "Alliance",
    .min_level = 1,
    .waypoints = &[_]game.Position{
        .{ .x = -9466.0, .y = -9.0, .z = 49.0 },
        .{ .x = -9430.0, .y = 65.0, .z = 56.0 },
        .{ .x = -9380.0, .y = 20.0, .z = 60.0 },
    },
    .safe_for_grinding = true,
};

const HostMode = enum { poll, bot };

const CliFlags = struct {
    mode: HostMode = .poll,
    web_enabled: bool = false,
    config_path: []const u8 = "botSettings.json",
    wow_path: ?[]const u8 = null,
    stub_path: ?[]const u8 = null,
};

pub fn main(init: std.process.Init) !void {
    const allocator = init.arena.allocator();

    log.info("[bloog-host] starting...", .{});

    const flags = parseArgs(init);

    const config_path_z: [:0]const u8 = if (std.mem.indexOfScalar(u8, flags.config_path, 0) == null)
        blk: {
            const duped = allocator.dupeZ(u8, flags.config_path) catch "botSettings.json";
            break :blk duped;
        }
    else
        flags.config_path[0..0 :0];

    const loaded = config_mod.loadFromFile(allocator, config_path_z) catch |err| {
        log.warn("[bloog-host] config load failed ({s}), using defaults", .{@errorName(err)});
        return err;
    };
    var settings = loaded.settings;
    _ = &settings;

    if (settings.use_verbose_logging) {
        log.info("[bloog-host] verbose logging enabled", .{});
    }

    obj_mgr.init(allocator);
    nav.init();

    var client = stub_client.connect() catch |err| {
        log.err("[bloog-host] failed to connect to stub: {s}", .{@errorName(err)});
        log.err("[bloog-host] make sure bloog-stub.dll is injected into the target process", .{});
        return err;
    };
    defer client.disconnect();

    client.ping(allocator) catch |err| {
        log.err("[bloog-host] ping failed: {s}", .{@errorName(err)});
        return err;
    };
    log.info("[bloog-host] ping OK — stub is alive", .{});

    var web_server: ?web_mod.WebServer = null;
    if (flags.web_enabled) {
        web_server = web_mod.WebServer.init(allocator, settings.web_port) catch |err| blk: {
            log.warn("[bloog-host] web server failed to start: {s}", .{@errorName(err)});
            break :blk null;
        };
        if (web_server) |*ws| {
            ws.startBackground();
        }
    }
    defer {
        if (web_server) |*ws| ws.deinit();
    }

    switch (flags.mode) {
        .poll => runPollMode(allocator, &client),
        .bot => runBotMode(allocator, &client, &web_server, &settings),
    }
}

fn parseArgs(init: std.process.Init) CliFlags {
    var flags = CliFlags{};

    const args = init.minimal.args.toSlice(init.arena.allocator()) catch return flags;

    for (args) |arg| {
        if (std.mem.eql(u8, arg, "--bot")) {
            flags.mode = .bot;
        } else if (std.mem.eql(u8, arg, "--web")) {
            flags.web_enabled = true;
        } else if (std.mem.startsWith(u8, arg, "--config=")) {
            flags.config_path = arg[9..];
        } else if (std.mem.startsWith(u8, arg, "--port=")) {
            const port_str = arg[7..];
            flags.web_enabled = true;
            _ = port_str;
        }
    }
    return flags;
}

fn runPollMode(allocator: std.mem.Allocator, client: *stub_client) void {
    log.info("[bloog-host] running in poll mode (Ctrl+C to exit)", .{});

    var iteration: u32 = 0;
    while (true) {
        iteration += 1;

        obj_mgr.poll(client, allocator) catch |err| {
            log.warn("[bloog-host] poll #{d} failed: {s}", .{ iteration, @errorName(err) });
            win.Sleep(1000);
            continue;
        };

        const all_objects = obj_mgr.allObjects();
        const all_units = obj_mgr.allUnits();
        const all_players = obj_mgr.allPlayers();

        if (iteration % 10 == 0) {
            log.info("[bloog-host] objects={d} units={d} players={d}", .{
                all_objects.len,
                all_units.len,
                all_players.len,
            });
        }

        win.Sleep(500);
    }
}

fn runBotMode(allocator: std.mem.Allocator, client: *stub_client, web_server_opt: *?web_mod.WebServer, settings: *config_mod.BotSettings) void {
    log.info("[bloog-host] running in bot mode", .{});

    const state_stack = std.array_list.AlignedManaged(bot_mod.BotState, null).init(allocator);

    var ctx = bot_mod.BotContext{
        .client = client,
        .allocator = allocator,
        .state_stack = state_stack,
        .hotspot = &default_hotspot,
        .tick = 0,
        .state_start_ms = 0,
        .running = true,
    };

        if (web_server_opt.*) |*ws| {
            if (ws.bot_ctx == null) ws.setBotContext(&ctx);
            if (ws.settings == null) ws.setSettings(settings);
        }

    bot_mod.run(&ctx);
}
