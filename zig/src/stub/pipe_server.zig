//! Named pipe RPC server for bloog-stub.
//!
//! Listens on \\.\pipe\bloogbot. Accepts one client at a time.
//! When the client disconnects (host crash / restart), re-listens.
//! This provides host crash resilience — the stub stays alive inside
//! wow.exe and accepts a new connection whenever the host restarts.

const std = @import("std");
const win = @import("winapi");
const log = @import("log");
const rpc = @import("rpc.zig");
const memory = @import("memory.zig");
const fastcall = @import("fastcall.zig");
const enumeration = @import("enumeration.zig");
const wndproc = @import("wndproc.zig");

const pipe_name = std.unicode.utf8ToUtf16LeStringLiteral("\\\\.\\pipe\\bloogbot");

// Pipe buffer sizes. 64KB is larger than the default 4KB, needed for
// object enumeration results which can be large.
const PIPE_BUFFER_SIZE: win.DWORD = 65536;
const PIPE_TIMEOUT: win.DWORD = 0; // default timeout for WaitNamedPipe (not used here)

// ---------------------------------------------------------------------------
// Server loop
// ---------------------------------------------------------------------------

pub fn run() void {
    log.info("[pipe-server] starting on \\{{.\\pipe\\bloogbot", .{});

    while (true) {
        // Create a new pipe instance for each connection.
        const pipe = win.CreateNamedPipeW(
            pipe_name,
            win.PIPE_ACCESS_DUPLEX,
            win.PIPE_TYPE_BYTE | win.PIPE_READMODE_BYTE | win.PIPE_WAIT,
            win.PIPE_UNLIMITED_INSTANCES,
            PIPE_BUFFER_SIZE,
            PIPE_BUFFER_SIZE,
            PIPE_TIMEOUT,
            null,
        ) orelse {
            log.err("[pipe-server] CreateNamedPipeW failed: GLE={d}", .{win.GetLastError()});
            win.Sleep(1000);
            continue;
        };

        log.info("[pipe-server] waiting for client connection...", .{});

        // Block until a client connects.
        if (win.ConnectNamedPipe(pipe, null) == win.FALSE) {
            const gle = win.GetLastError();
            if (gle != win.ERROR_PIPE_CONNECTED) {
                log.err("[pipe-server] ConnectNamedPipe failed: GLE={d}", .{gle});
                _ = win.CloseHandle(pipe);
                win.Sleep(1000);
                continue;
            }
            // ERROR_PIPE_CONNECTED means a client already connected before
            // we called ConnectNamedPipe — that's fine.
        }

        log.info("[pipe-server] client connected", .{});

        // Process requests from this client until it disconnects.
        serveClient(pipe);

        // Client disconnected (or error). Clean up and re-listen.
        _ = win.FlushFileBuffers(pipe);
        _ = win.DisconnectNamedPipe(pipe);
        _ = win.CloseHandle(pipe);

        log.info("[pipe-server] client disconnected, re-listening", .{});
    }
}

// ---------------------------------------------------------------------------
// Client request loop
// ---------------------------------------------------------------------------

fn serveClient(pipe: win.HANDLE) void {
    // Stack-allocated read buffer. The stub runs inside wow.exe, so we
    // want to avoid heap allocations where possible.
    var recv_buf: [PIPE_BUFFER_SIZE]u8 = undefined;

    while (true) {
        // Read a message header (5 bytes: 1 opcode + 4 length).
        var header_bytes: [rpc.HEADER_SIZE]u8 = undefined;
        var total_read: win.DWORD = 0;

        while (total_read < rpc.HEADER_SIZE) {
            var bytes_read: win.DWORD = 0;
            const ok = win.ReadFile(
                pipe,
                @ptrCast(&header_bytes[total_read]),
                @intCast(rpc.HEADER_SIZE - total_read),
                &bytes_read,
                null,
            );
            if (ok == win.FALSE or bytes_read == 0) {
                // Client disconnected or error.
                return;
            }
            total_read += bytes_read;
        }

        // Parse header.
        const opcode = rpc.parseOpcode(&header_bytes);
        const payload_len: usize = @intCast(rpc.parsePayloadLen(&header_bytes));

        // Read payload if present.
        if (payload_len > 0 and payload_len <= recv_buf.len) {
            total_read = 0;
            while (total_read < payload_len) {
                var bytes_read: win.DWORD = 0;
                const ok = win.ReadFile(
                    pipe,
                    @ptrCast(&recv_buf[total_read]),
                    @intCast(payload_len - total_read),
                    &bytes_read,
                    null,
                );
                if (ok == win.FALSE or bytes_read == 0) return;
                total_read += bytes_read;
            }
        }

        // Dispatch request.
        const payload = recv_buf[0..payload_len];
        handleRequest(pipe, opcode, payload);
    }
}

// ---------------------------------------------------------------------------
// Request dispatcher
// ---------------------------------------------------------------------------

fn handleRequest(pipe: win.HANDLE, opcode: rpc.Opcode, payload: []const u8) void {
    switch (opcode) {
        // Memory read operations.
        .read_u8 => handleReadU8(pipe, payload),
        .read_u16 => handleReadU16(pipe, payload),
        .read_u32 => handleReadU32(pipe, payload),
        .read_u64 => handleReadU64(pipe, payload),
        .read_f32 => handleReadF32(pipe, payload),
        .read_bytes => handleReadBytes(pipe, payload),
        .read_string => handleReadString(pipe, payload),

        // Memory write operations.
        .write_bytes => handleWriteBytes(pipe, payload),
        .write_u8 => handleWriteU8(pipe, payload),
        .write_u32 => handleWriteU32(pipe, payload),

        // Object enumeration.
        .enumerate_objects => handleEnumerateObjects(pipe, payload),

        // Thread synchronization.
        .run_on_main_thread => handleRunOnMainThread(pipe, payload),

        // Fastcall shims.
        .fastcall_enumerate_visible_objects => handleFastcall2(pipe, payload),
        .fastcall_lua_call => handleFastcallLuaCall(pipe, payload),
        .fastcall_loot_slot => handleFastcall2(pipe, payload),
        .fastcall_get_text => handleFastcallGetText(pipe, payload),
        .fastcall_intersect => handleFastcallIntersect(pipe, payload),
        .fastcall_intersect2 => handleFastcallIntersect2(pipe, payload),
        .fastcall_sell_item_by_guid => handleFastcallSellItem(pipe, payload),
        .fastcall_buy_vendor_item => handleFastcallBuyItem(pipe, payload),
        .fastcall_get_object_ptr => handleFastcallGetObjPtr(pipe, payload),

        // Detour management.
        .install_detour => handleInstallDetour(pipe, payload),
        .remove_detour => handleRemoveDetour(pipe, payload),

        // Lifecycle.
        .ping => {
            log.debug("[pipe-server] ping", .{});
            sendResponse(pipe, .ok, &.{});
        },
        .shutdown => {
            log.info("[pipe-server] shutdown requested", .{});
            sendResponse(pipe, .ok, &.{});
        },
        else => {
            log.warn("[pipe-server] unknown opcode: 0x{x}", .{@intFromEnum(opcode)});
            sendResponse(pipe, .error_invalid_opcode, &.{});
        },
    }
}

// ---------------------------------------------------------------------------
// Read handlers
// ---------------------------------------------------------------------------

fn handleReadU8(pipe: win.HANDLE, payload: []const u8) void {
    if (payload.len < @sizeOf(rpc.ReadRequest)) {
        sendResponse(pipe, .error_read_failed, &.{});
        return;
    }
    const req: *align(1) const rpc.ReadRequest = @ptrCast(payload);
    const value = memory.readU8(req.address);
    sendResponse(pipe, .ok, &.{value});
}

fn handleReadU16(pipe: win.HANDLE, payload: []const u8) void {
    if (payload.len < @sizeOf(rpc.ReadRequest)) {
        sendResponse(pipe, .error_read_failed, &.{});
        return;
    }
    const req: *align(1) const rpc.ReadRequest = @ptrCast(payload);
    const value = memory.readU16(req.address);
    var buf: [2]u8 = undefined;
    std.mem.writeInt(u16, &buf, value, .little);
    sendResponse(pipe, .ok, &buf);
}

fn handleReadU32(pipe: win.HANDLE, payload: []const u8) void {
    if (payload.len < @sizeOf(rpc.ReadRequest)) {
        sendResponse(pipe, .error_read_failed, &.{});
        return;
    }
    const req: *align(1) const rpc.ReadRequest = @ptrCast(payload);
    const value = memory.readU32(req.address);
    var buf: [4]u8 = undefined;
    std.mem.writeInt(u32, &buf, value, .little);
    sendResponse(pipe, .ok, &buf);
}

fn handleReadU64(pipe: win.HANDLE, payload: []const u8) void {
    if (payload.len < @sizeOf(rpc.ReadRequest)) {
        sendResponse(pipe, .error_read_failed, &.{});
        return;
    }
    const req: *align(1) const rpc.ReadRequest = @ptrCast(payload);
    const value = memory.readU64(req.address);
    var buf: [8]u8 = undefined;
    std.mem.writeInt(u64, &buf, value, .little);
    sendResponse(pipe, .ok, &buf);
}

fn handleReadF32(pipe: win.HANDLE, payload: []const u8) void {
    if (payload.len < @sizeOf(rpc.ReadRequest)) {
        sendResponse(pipe, .error_read_failed, &.{});
        return;
    }
    const req: *align(1) const rpc.ReadRequest = @ptrCast(payload);
    const value = memory.readF32(req.address);
    var buf: [4]u8 = undefined;
    std.mem.writeInt(u32, &buf, @bitCast(value), .little);
    sendResponse(pipe, .ok, &buf);
}

fn handleReadBytes(pipe: win.HANDLE, payload: []const u8) void {
    if (payload.len < @sizeOf(rpc.ReadBytesRequest)) {
        sendResponse(pipe, .error_read_failed, &.{});
        return;
    }
    const req: *align(1) const rpc.ReadBytesRequest = @ptrCast(payload);
    // Allocate on the stack for small reads, fall back for large ones.
    // For now, cap at a reasonable size.
    if (req.count > 4096) {
        sendResponse(pipe, .error_read_failed, &.{});
        return;
    }
    var buf: [4096]u8 = undefined;
    const slice = memory.readBytes(req.address, buf[0..req.count]);
    sendResponse(pipe, .ok, slice);
}

fn handleReadString(pipe: win.HANDLE, payload: []const u8) void {
    if (payload.len < @sizeOf(rpc.ReadRequest)) {
        sendResponse(pipe, .error_read_failed, &.{});
        return;
    }
    const req: *align(1) const rpc.ReadRequest = @ptrCast(payload);
    var buf: [512]u8 = undefined;
    const slice = memory.readString(req.address, &buf);
    sendResponse(pipe, .ok, slice);
}

// ---------------------------------------------------------------------------
// Write handlers
// ---------------------------------------------------------------------------

fn handleWriteBytes(pipe: win.HANDLE, payload: []const u8) void {
    if (payload.len < @sizeOf(rpc.WriteBytesRequest)) {
        sendResponse(pipe, .error_write_failed, &.{});
        return;
    }
    const req: *align(1) const rpc.WriteBytesRequest = @ptrCast(payload);
    const data = payload[@sizeOf(rpc.WriteBytesRequest)..];
    if (data.len != req.length) {
        sendResponse(pipe, .error_write_failed, &.{});
        return;
    }
    memory.writeBytes(req.address, data);
    sendResponse(pipe, .ok, &.{});
}

fn handleWriteU8(pipe: win.HANDLE, payload: []const u8) void {
    if (payload.len < @sizeOf(rpc.ReadRequest) + 1) {
        sendResponse(pipe, .error_write_failed, &.{});
        return;
    }
    const req: *align(1) const rpc.ReadRequest = @ptrCast(payload[0..@sizeOf(rpc.ReadRequest)]);
    const value: u8 = payload[@sizeOf(rpc.ReadRequest)];
    var buf = [1]u8{value};
    memory.writeBytes(req.address, &buf);
    sendResponse(pipe, .ok, &.{});
}

fn handleWriteU32(pipe: win.HANDLE, payload: []const u8) void {
    if (payload.len < @sizeOf(rpc.ReadRequest) + 4) {
        sendResponse(pipe, .error_write_failed, &.{});
        return;
    }
    const req: *align(1) const rpc.ReadRequest = @ptrCast(payload[0..@sizeOf(rpc.ReadRequest)]);
    const value_bytes: *align(1) const [4]u8 = @ptrCast(payload[@sizeOf(rpc.ReadRequest)..][0..4]);
    memory.writeBytes(req.address, value_bytes);
    sendResponse(pipe, .ok, &.{});
}

// ---------------------------------------------------------------------------
// Object enumeration handler
// ---------------------------------------------------------------------------

fn handleEnumerateObjects(pipe: win.HANDLE, payload: []const u8) void {
    if (payload.len < @sizeOf(rpc.EnumerateObjectsRequest)) {
        sendResponse(pipe, .error_read_failed, &.{});
        return;
    }
    const req: *align(1) const rpc.EnumerateObjectsRequest = @ptrCast(payload);

    const objects = enumeration.collect(
        req.enumerate_visible_objects_ptr,
        req.get_object_ptr_ptr,
    );

    // Build response: [count: u32][ObjectInfo...]
    var resp_buf: [@sizeOf(u32) + 4096 * @sizeOf(rpc.ObjectInfo)]u8 = undefined;
    std.mem.writeInt(u32, resp_buf[0..4], @intCast(objects.len), .little);

    const obj_bytes = std.mem.sliceAsBytes(objects);
    const total_len: usize = 4 + obj_bytes.len;
    if (total_len <= resp_buf.len) {
        @memcpy(resp_buf[4..total_len], obj_bytes);
        sendResponse(pipe, .ok, resp_buf[0..total_len]);
    } else {
        log.err("[pipe-server] enumerate_objects: too many objects ({d})", .{objects.len});
        sendResponse(pipe, .error_read_failed, &.{});
    }
}

// ---------------------------------------------------------------------------
// Run-on-main-thread handler
// ---------------------------------------------------------------------------

fn handleRunOnMainThread(pipe: win.HANDLE, payload: []const u8) void {
    if (payload.len < @sizeOf(rpc.RunOnMainThreadRequest)) {
        sendResponse(pipe, .error_write_failed, &.{});
        return;
    }
    const req: *align(1) const rpc.RunOnMainThreadRequest = @ptrCast(payload);

    if (wndproc.queueCall(req.func_ptr, req.arg)) {
        sendResponse(pipe, .ok, &.{});
    } else {
        sendResponse(pipe, .error_write_failed, &.{});
    }
}

// ---------------------------------------------------------------------------
// Fastcall shim handlers
// ---------------------------------------------------------------------------

/// Generic 2-arg fastcall (EnumerateVisibleObjects, LootSlot).
/// Request: Fastcall2Request { func_ptr, arg1, arg2 }
fn handleFastcall2(pipe: win.HANDLE, payload: []const u8) void {
    if (payload.len < @sizeOf(rpc.Fastcall2Request)) {
        sendResponse(pipe, .error_write_failed, &.{});
        return;
    }
    const req: *align(1) const rpc.Fastcall2Request = @ptrCast(payload);
    _ = fastcall.call2(req.func_ptr, req.arg1, req.arg2);
    sendResponse(pipe, .ok, &.{});
}

/// LuaCall: 2-arg fastcall, but second arg is the "Unused" string.
/// For now, host sends func_ptr + code_ptr, stub passes 0 as second arg.
fn handleFastcallLuaCall(pipe: win.HANDLE, payload: []const u8) void {
    if (payload.len < @sizeOf(rpc.Fastcall2Request)) {
        sendResponse(pipe, .error_write_failed, &.{});
        return;
    }
    const req: *align(1) const rpc.Fastcall2Request = @ptrCast(payload);
    fastcall.luaCall(req.func_ptr, req.arg1);
    sendResponse(pipe, .ok, &.{});
}

/// GetText: 3-arg fastcall, returns a pointer (usize).
fn handleFastcallGetText(pipe: win.HANDLE, payload: []const u8) void {
    if (payload.len < @sizeOf(rpc.Fastcall3Request)) {
        sendResponse(pipe, .error_write_failed, &.{});
        return;
    }
    const req: *align(1) const rpc.Fastcall3Request = @ptrCast(payload);
    const result = fastcall.getText(req.func_ptr, req.arg1);
    var buf: [@sizeOf(usize)]u8 = undefined;
    std.mem.writeInt(usize, &buf, result, .little);
    sendResponse(pipe, .ok, &buf);
}

/// Intersect: 4-arg fastcall, returns a BYTE (u8).
fn handleFastcallIntersect(pipe: win.HANDLE, payload: []const u8) void {
    if (payload.len < @sizeOf(rpc.Fastcall4Request)) {
        sendResponse(pipe, .error_write_failed, &.{});
        return;
    }
    const req: *align(1) const rpc.Fastcall4Request = @ptrCast(payload);
    const result = fastcall.intersect(req.func_ptr, req.arg1, req.arg2, req.arg3, req.arg4);
    sendResponse(pipe, .ok, &.{result});
}

/// Intersect2: 6-arg fastcall, returns a bool.
fn handleFastcallIntersect2(pipe: win.HANDLE, payload: []const u8) void {
    if (payload.len < @sizeOf(rpc.Fastcall6Request)) {
        sendResponse(pipe, .error_write_failed, &.{});
        return;
    }
    const req: *align(1) const rpc.Fastcall6Request = @ptrCast(payload);
    const result = fastcall.intersect2(
        req.func_ptr, req.arg1, req.arg2, req.arg3, req.arg4, req.arg5,
    );
    sendResponse(pipe, .ok, &.{@intFromBool(result)});
}

/// SellItemByGuid: special 4-reg + 4-stack fastcall with two u64 args.
fn handleFastcallSellItem(pipe: win.HANDLE, payload: []const u8) void {
    if (payload.len < @sizeOf(rpc.FastcallSellItemRequest)) {
        sendResponse(pipe, .error_write_failed, &.{});
        return;
    }
    const req: *align(1) const rpc.FastcallSellItemRequest = @ptrCast(payload);
    fastcall.sellItemByGuid(req.func_ptr, req.count, req.vendor_guid, req.item_guid);
    sendResponse(pipe, .ok, &.{});
}

/// BuyVendorItem: fastcall with u64 vendor guid + int literal.
fn handleFastcallBuyItem(pipe: win.HANDLE, payload: []const u8) void {
    if (payload.len < @sizeOf(rpc.FastcallBuyItemRequest)) {
        sendResponse(pipe, .error_write_failed, &.{});
        return;
    }
    const req: *align(1) const rpc.FastcallBuyItemRequest = @ptrCast(payload);
    fastcall.buyVendorItem(req.func_ptr, req.item_index, req.quantity, req.vendor_guid);
    sendResponse(pipe, .ok, &.{});
}

/// GetObjectPtr: fastcall with u64 object guid + line + file args.
fn handleFastcallGetObjPtr(pipe: win.HANDLE, payload: []const u8) void {
    if (payload.len < @sizeOf(rpc.FastcallGetObjPtrRequest)) {
        sendResponse(pipe, .error_write_failed, &.{});
        return;
    }
    const req: *align(1) const rpc.FastcallGetObjPtrRequest = @ptrCast(payload);
    const result = fastcall.getObjectPtr(
        req.func_ptr, req.type_mask, req.object_guid, req.line, req.file,
    );
    var buf: [@sizeOf(usize)]u8 = undefined;
    std.mem.writeInt(usize, &buf, result, .little);
    sendResponse(pipe, .ok, &buf);
}

// ---------------------------------------------------------------------------
// Detour management handlers
// ---------------------------------------------------------------------------

/// Global detour storage. Each detour saves the original bytes so we can
/// restore them on remove. Max 32 active detours.
const MAX_DETOURS: usize = 32;

const DetourEntry = struct {
    target_addr: usize,
    original_bytes: [6]u8,
    active: bool,
};

var detours: [MAX_DETOURS]DetourEntry = undefined;
var detour_count: usize = 0;

fn handleInstallDetour(pipe: win.HANDLE, payload: []const u8) void {
    if (payload.len < @sizeOf(rpc.InstallDetourRequest)) {
        sendResponse(pipe, .error_write_failed, &.{});
        return;
    }
    const req: *align(1) const rpc.InstallDetourRequest = @ptrCast(payload);

    if (detour_count >= MAX_DETOURS) {
        log.err("[pipe-server] install_detour: max detours reached", .{});
        sendResponse(pipe, .error_write_failed, &.{});
        return;
    }

    // Save the original 6 bytes at the target address.
    var original_bytes: [6]u8 = undefined;
    const src: [*]const u8 = @ptrFromInt(req.target_addr);
    @memcpy(&original_bytes, src[0..6]);

    // Build the detour trampoline: PUSH hook_addr (5 bytes) + RET (1 byte) = 6 bytes.
    // Same pattern as Detour.cs:
    //   0x68 [hook_addr as u32 LE] 0xC3
    var detour_bytes: [6]u8 = undefined;
    detour_bytes[0] = 0x68; // PUSH imm32
    std.mem.writeInt(u32, detour_bytes[1..5], @truncate(req.hook_addr), .little);
    detour_bytes[5] = 0xC3; // RET

    // Write the detour.
    memory.writeBytes(req.target_addr, &detour_bytes);

    // Store for later removal.
    detours[detour_count] = .{
        .target_addr = req.target_addr,
        .original_bytes = original_bytes,
        .active = true,
    };
    detour_count += 1;

    log.info("[pipe-server] install_detour: target=0x{x} hook=0x{x}", .{
        req.target_addr, req.hook_addr,
    });

    // Return the original bytes in the response.
    sendResponse(pipe, .ok, &original_bytes);
}

fn handleRemoveDetour(pipe: win.HANDLE, payload: []const u8) void {
    if (payload.len < @sizeOf(rpc.RemoveDetourRequest)) {
        sendResponse(pipe, .error_write_failed, &.{});
        return;
    }
    const req: *align(1) const rpc.RemoveDetourRequest = @ptrCast(payload);

    // Find the detour entry.
    for (&detours) |*entry| {
        if (entry.active and entry.target_addr == req.target_addr) {
            // Restore original bytes.
            memory.writeBytes(entry.target_addr, &entry.original_bytes);
            entry.active = false;
            log.info("[pipe-server] remove_detour: target=0x{x}", .{req.target_addr});
            sendResponse(pipe, .ok, &.{});
            return;
        }
    }

    log.warn("[pipe-server] remove_detour: target=0x{x} not found", .{req.target_addr});
    sendResponse(pipe, .error_write_failed, &.{});
}

// ---------------------------------------------------------------------------
// Response writer
// ---------------------------------------------------------------------------

fn sendResponse(pipe: win.HANDLE, response: rpc.Response, payload: []const u8) void {
    // Response format: [Response: u8][payload_len: u32][payload]
    const payload_len: u32 = @intCast(payload.len);
    var header: [5]u8 = undefined;
    header[0] = @intFromEnum(response);
    std.mem.writeInt(u32, header[1..5], payload_len, .little);

    // Write header.
    var bytes_written: win.DWORD = 0;
    _ = win.WriteFile(pipe, @ptrCast(&header), 5, &bytes_written, null);

    // Write payload.
    if (payload.len > 0) {
        _ = win.WriteFile(pipe, @ptrCast(@constCast(payload.ptr)), payload_len, &bytes_written, null);
    }
}
