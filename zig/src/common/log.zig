//! Structured logging for the BloogBot Zig port.
//!
//! In the stub (inside wow.exe): logs go to OutputDebugStringA only.
//! In the host (out-of-process): logs go to stderr + OutputDebugStringA.
//! In the launcher: logs go to stderr only.
//!
//! Usage:
//!   const log = @import("log");
//!   log.info("connected to pipe, waiting for host", .{});
//!   log.err("ReadFile failed: GLE={d}", .{win.GetLastError()});

const std = @import("std");
const builtin = @import("builtin");
const win = @import("winapi");

pub const Level = enum(u8) {
    debug = 0,
    info = 1,
    warn = 2,
    err = 3,
};

/// Minimum log level. Can be overridden at comptime.
pub var min_level: Level = .info;

/// Log a message. Uses std.debug.print for stderr and OutputDebugStringA
/// for the debugger output window. The prefix includes the level tag and
/// the source module name (derived from the caller's file).
pub fn log(comptime level: Level, comptime fmt: []const u8, args: anytype) void {
    if (@intFromEnum(level) < @intFromEnum(min_level)) return;

    const prefix = switch (level) {
        .debug => "[DBG]",
        .info => "[INF]",
        .warn => "[WRN]",
        .err => "[ERR]",
    };

    // Print to stderr.
    std.debug.print(prefix ++ " " ++ fmt ++ "\n", args);

    // Also send to OutputDebugStringA. We need to format into a
    // fixed buffer because OutputDebugStringA takes a C string.
    // Use a stack buffer to avoid allocations (the stub runs inside
    // wow.exe where we want minimal heap usage).
    var buf: [1024]u8 = undefined;
    const slice = std.fmt.bufPrint(&buf, prefix ++ " " ++ fmt, args) catch "|log msg truncated|";
    // NUL-terminate for OutputDebugStringA.
    if (slice.len < buf.len) buf[slice.len] = 0;
    win.OutputDebugStringA(@ptrCast(buf[0..slice.len :0]));
}

/// Convenience wrappers.
pub fn debug(comptime fmt: []const u8, args: anytype) void {
    log(.debug, fmt, args);
}
pub fn info(comptime fmt: []const u8, args: anytype) void {
    log(.info, fmt, args);
}
pub fn warn(comptime fmt: []const u8, args: anytype) void {
    log(.warn, fmt, args);
}
pub fn err(comptime fmt: []const u8, args: anytype) void {
    log(.err, fmt, args);
}
