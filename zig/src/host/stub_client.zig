//! Typed pipe RPC client for bloog-host.
//!
//! Connects to the named pipe \\.\pipe\bloogbot opened by bloog-stub.
//! Provides typed methods for each RPC operation (read_u8, write_bytes,
//! enumerate_objects, fastcall shims, etc.) that handle wire framing.
//!
//! Port of the out-of-process side of the stub↔host IPC protocol.

const std = @import("std");
const win = @import("winapi");
const log = @import("log");
const rpc = @import("rpc_types");

const pipe_name = std.unicode.utf8ToUtf16LeStringLiteral("\\\\.\\pipe\\bloogbot");

// ---------------------------------------------------------------------------
// Client state
// ---------------------------------------------------------------------------

pipe: win.HANDLE,
connected: bool,

const Self = @This();

// ---------------------------------------------------------------------------
// Connection
// ---------------------------------------------------------------------------

/// Connect to the stub's named pipe. Blocks until connected or error.
pub fn connect() !Self {
    const pipe = win.CreateFileW(
        pipe_name,
        win.GENERIC_READ | win.GENERIC_WRITE,
        0,
        null,
        win.OPEN_EXISTING,
        0,
        null,
    ) orelse return error.PipeConnectFailed;

    // CreateFileW returns INVALID_HANDLE_VALUE on failure, not null.
    if (pipe == win.INVALID_HANDLE_VALUE) return error.PipeConnectFailed;

    // Verify it's a pipe (not a file that happens to have the same name).
    // For simplicity, we just trust the connection.
    log.info("[stub-client] connected to \\{{.\\pipe\\bloogbot", .{});

    return .{
        .pipe = pipe,
        .connected = true,
    };
}

/// Disconnect from the stub's pipe.
pub fn disconnect(self: *Self) void {
    if (self.pipe) |p| {
        _ = win.CloseHandle(p);
    }
    self.connected = false;
}

// ---------------------------------------------------------------------------
// Low-level request/response
// ---------------------------------------------------------------------------

/// Send an RPC request (opcode + payload) and read the response.
/// Returns the response payload as a caller-owned slice allocated from `allocator`.
fn rpcCall(self: *Self, allocator: std.mem.Allocator, opcode: rpc.Opcode, payload: []const u8) ![]u8 {
    // Build and send header + payload.
    const header = rpc.buildHeader(opcode, @intCast(payload.len));
    var bytes_written: win.DWORD = 0;
    const write_ok = win.WriteFile(self.pipe, @ptrCast(&header), @intCast(header.len), &bytes_written, null);
    if (write_ok == win.FALSE) {
        log.err("[stub-client] WriteFile header failed: GLE={d}", .{win.GetLastError()});
        return error.PipeWriteFailed;
    }
    if (payload.len > 0) {
        const write_ok2 = win.WriteFile(self.pipe, @ptrCast(@constCast(payload.ptr)), @intCast(payload.len), &bytes_written, null);
        if (write_ok2 == win.FALSE) {
            log.err("[stub-client] WriteFile payload failed: GLE={d}", .{win.GetLastError()});
            return error.PipeWriteFailed;
        }
    }

    // Read response header (5 bytes).
    var resp_header: [rpc.HEADER_SIZE]u8 = undefined;
    var total_read: win.DWORD = 0;
    while (total_read < rpc.HEADER_SIZE) {
        var bytes_read: win.DWORD = 0;
        const read_ok = win.ReadFile(self.pipe, @ptrCast(&resp_header[total_read]), @intCast(rpc.HEADER_SIZE - total_read), &bytes_read, null);
        if (read_ok == win.FALSE or bytes_read == 0) return error.PipeReadFailed;
        total_read += bytes_read;
    }

    const response_code: rpc.Response = @enumFromInt(resp_header[0]);
    const resp_payload_len: usize = @intCast(std.mem.readInt(u32, resp_header[1..5], .little));

    // Check for error responses.
    if (response_code != .ok) {
        // Drain any payload bytes.
        if (resp_payload_len > 0) {
            var discard: [4096]u8 = undefined;
            var drain: usize = 0;
            while (drain < resp_payload_len) {
                var bytes_read: win.DWORD = 0;
                const to_read: win.DWORD = @intCast(@min(resp_payload_len - drain, discard.len));
                const read_ok = win.ReadFile(self.pipe, @ptrCast(&discard), to_read, &bytes_read, null);
                if (read_ok == win.FALSE or bytes_read == 0) break;
                drain += bytes_read;
            }
        }
        return error.RpcError;
    }

    // Read response payload.
    if (resp_payload_len == 0) return &[_]u8{};

    const buf = try allocator.alloc(u8, resp_payload_len);
    total_read = 0;
    while (total_read < resp_payload_len) {
        var bytes_read: win.DWORD = 0;
        const read_ok = win.ReadFile(self.pipe, @ptrCast(buf.ptr + total_read), @intCast(resp_payload_len - total_read), &bytes_read, null);
        if (read_ok == win.FALSE or bytes_read == 0) {
            allocator.free(buf);
            return error.PipeReadFailed;
        }
        total_read += bytes_read;
    }

    return buf;
}

// ---------------------------------------------------------------------------
// Typed RPC methods
// ---------------------------------------------------------------------------

/// Send a ping. Returns true if the stub responded with OK.
pub fn ping(self: *Self, allocator: std.mem.Allocator) !void {
    const resp = try self.rpcCall(allocator, .ping, &.{});
    allocator.free(resp);
}

/// Read a u8 from the target process at the given address.
pub fn readU8(self: *Self, allocator: std.mem.Allocator, address: usize) !u8 {
    const req = rpc.ReadRequest{ .address = address };
    const req_bytes = std.mem.asBytes(&req);
    const resp = try self.rpcCall(allocator, .read_u8, req_bytes);
    defer allocator.free(resp);
    if (resp.len < 1) return error.RpcError;
    return resp[0];
}

/// Read a u16 from the target process.
pub fn readU16(self: *Self, allocator: std.mem.Allocator, address: usize) !u16 {
    const req = rpc.ReadRequest{ .address = address };
    const resp = try self.rpcCall(allocator, .read_u16, std.mem.asBytes(&req));
    defer allocator.free(resp);
    if (resp.len < 2) return error.RpcError;
    return std.mem.readInt(u16, resp[0..2], .little);
}

/// Read a u32 from the target process.
pub fn readU32(self: *Self, allocator: std.mem.Allocator, address: usize) !u32 {
    const req = rpc.ReadRequest{ .address = address };
    const resp = try self.rpcCall(allocator, .read_u32, std.mem.asBytes(&req));
    defer allocator.free(resp);
    if (resp.len < 4) return error.RpcError;
    return std.mem.readInt(u32, resp[0..4], .little);
}

/// Read a u64 from the target process.
pub fn readU64(self: *Self, allocator: std.mem.Allocator, address: usize) !u64 {
    const req = rpc.ReadRequest{ .address = address };
    const resp = try self.rpcCall(allocator, .read_u64, std.mem.asBytes(&req));
    defer allocator.free(resp);
    if (resp.len < 8) return error.RpcError;
    return std.mem.readInt(u64, resp[0..8], .little);
}

/// Read a f32 from the target process.
pub fn readF32(self: *Self, allocator: std.mem.Allocator, address: usize) !f32 {
    const req = rpc.ReadRequest{ .address = address };
    const resp = try self.rpcCall(allocator, .read_f32, std.mem.asBytes(&req));
    defer allocator.free(resp);
    if (resp.len < 4) return error.RpcError;
    return @bitCast(std.mem.readInt(u32, resp[0..4], .little));
}

/// Read bytes from the target process.
pub fn readBytes(self: *Self, allocator: std.mem.Allocator, address: usize, count: u32) ![]u8 {
    const req = rpc.ReadBytesRequest{ .address = address, .count = count };
    return try self.rpcCall(allocator, .read_bytes, std.mem.asBytes(&req));
}

/// Read a NUL-terminated string from the target process.
pub fn readString(self: *Self, allocator: std.mem.Allocator, address: usize) ![]u8 {
    const req = rpc.ReadRequest{ .address = address };
    const resp = try self.rpcCall(allocator, .read_string, std.mem.asBytes(&req));
    // Trim at NUL if present.
    for (resp, 0..) |b, i| {
        if (b == 0) return resp[0..i];
    }
    return resp;
}

/// Write bytes to the target process.
pub fn writeBytes(self: *Self, allocator: std.mem.Allocator, address: usize, data: []const u8) !void {
    // Build payload: WriteBytesRequest + data.
    const total_len = @sizeOf(rpc.WriteBytesRequest) + data.len;
    const buf = try allocator.alloc(u8, total_len);
    defer allocator.free(buf);
    const header: *rpc.WriteBytesRequest = @ptrCast(buf.ptr);
    header.* = .{ .address = address, .length = @intCast(data.len) };
    @memcpy(buf[@sizeOf(rpc.WriteBytesRequest)..], data);
    const resp = try self.rpcCall(allocator, .write_bytes, buf);
    defer allocator.free(resp);
}

/// Write a u8 to the target process.
pub fn writeU8(self: *Self, allocator: std.mem.Allocator, address: usize, value: u8) !void {
    var buf: [@sizeOf(rpc.ReadRequest) + 1]u8 = undefined;
    const req: *rpc.ReadRequest = @ptrCast(&buf);
    req.* = .{ .address = address };
    buf[@sizeOf(rpc.ReadRequest)] = value;
    const resp = try self.rpcCall(allocator, .write_u8, &buf);
    defer allocator.free(resp);
}

/// Write a u32 to the target process.
pub fn writeU32(self: *Self, allocator: std.mem.Allocator, address: usize, value: u32) !void {
    var buf: [@sizeOf(rpc.ReadRequest) + 4]u8 = undefined;
    const req: *rpc.ReadRequest = @ptrCast(&buf);
    req.* = .{ .address = address };
    std.mem.writeInt(u32, buf[@sizeOf(rpc.ReadRequest)..][0..4], value, .little);
    const resp = try self.rpcCall(allocator, .write_u32, &buf);
    defer allocator.free(resp);
}

/// Enumerate visible objects. Returns raw response payload for parsing.
pub fn enumerateObjects(
    self: *Self,
    allocator: std.mem.Allocator,
    enum_func_ptr: usize,
    get_obj_ptr: usize,
) ![]u8 {
    const req = rpc.EnumerateObjectsRequest{
        .enumerate_visible_objects_ptr = enum_func_ptr,
        .get_object_ptr_ptr = get_obj_ptr,
    };
    return try self.rpcCall(allocator, .enumerate_objects, std.mem.asBytes(&req));
}

/// Request a shutdown of the stub (graceful).
pub fn shutdown(self: *Self, allocator: std.mem.Allocator) !void {
    const resp = try self.rpcCall(allocator, .shutdown, &.{});
    defer allocator.free(resp);
}

/// Install a detour at target_addr redirecting to hook_addr.
/// Returns the original bytes that were overwritten.
pub fn installDetour(self: *Self, allocator: std.mem.Allocator, target_addr: usize, hook_addr: usize) ![]u8 {
    const req = rpc.InstallDetourRequest{
        .target_addr = target_addr,
        .hook_addr = hook_addr,
    };
    return try self.rpcCall(allocator, .install_detour, std.mem.asBytes(&req));
}

/// Remove a detour, restoring original bytes.
pub fn removeDetour(self: *Self, allocator: std.mem.Allocator, target_addr: usize) !void {
    const req = rpc.RemoveDetourRequest{ .target_addr = target_addr };
    const resp = try self.rpcCall(allocator, .remove_detour, std.mem.asBytes(&req));
    defer allocator.free(resp);
}
