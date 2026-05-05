//! Memory read/write primitives for the stub.
//!
//! These operate on the host process's address space (the stub is loaded
//! inside wow.exe, so raw pointer reads work). Write operations use
//! VirtualProtect to flip page permissions when needed.
//!
//! Port of BloogBot/MemoryManager.cs:72-293.

const std = @import("std");
const win = @import("winapi");

// ---------------------------------------------------------------------------
// Read primitives
// ---------------------------------------------------------------------------

pub fn readU8(address: usize) u8 {
    if (address == 0) return 0;
    const ptr: *const u8 = @ptrFromInt(address);
    return ptr.*;
}

pub fn readU16(address: usize) u16 {
    if (address == 0) return 0;
    const ptr: *align(1) const u16 = @ptrFromInt(address);
    return ptr.*;
}

pub fn readU32(address: usize) u32 {
    if (address == 0) return 0;
    const ptr: *align(1) const u32 = @ptrFromInt(address);
    return ptr.*;
}

pub fn readU64(address: usize) u64 {
    if (address == 0) return 0;
    const ptr: *align(1) const u64 = @ptrFromInt(address);
    return ptr.*;
}

pub fn readF32(address: usize) f32 {
    if (address == 0) return 0;
    const ptr: *align(1) const f32 = @ptrFromInt(address);
    return ptr.*;
}

pub fn readIntPtr(address: usize) usize {
    if (address == 0) return 0;
    const ptr: *align(1) const usize = @ptrFromInt(address);
    return ptr.*;
}

/// Read bytes into the provided buffer. Returns the slice actually read
/// (may be shorter than buf.len if address is 0).
pub fn readBytes(address: usize, buf: []u8) []u8 {
    if (address == 0 or buf.len == 0) return &[_]u8{};
    const src: [*]const u8 = @ptrFromInt(address);
    @memcpy(buf, src[0..buf.len]);
    return buf;
}

/// Read a NUL-terminated ASCII string into the provided buffer.
/// Returns the string slice (without NUL terminator).
pub fn readString(address: usize, buf: []u8) []u8 {
    if (address == 0) return &[_]u8{};
    const src: [*:0]const u8 = @ptrFromInt(address);
    const len = std.mem.indexOfSentinel(u8, 0, src);
    const copy_len = @min(len, buf.len - 1);
    @memcpy(buf[0..copy_len], src[0..copy_len]);
    buf[copy_len] = 0;
    return buf[0..copy_len];
}

// ---------------------------------------------------------------------------
// Write primitives
// ---------------------------------------------------------------------------

/// Write bytes to the target address. Uses VirtualProtect to ensure
/// the page is writable, then restores the original protection.
/// Port of MemoryManager.cs:271-293.
pub fn writeBytes(address: usize, data: []const u8) void {
    if (address == 0 or data.len == 0) return;

    const ptr: [*]u8 = @ptrFromInt(address);

    // Flip page protection to PAGE_EXECUTE_READWRITE.
    var old_protect: win.DWORD = 0;
    _ = win.VirtualProtect(
        @ptrCast(ptr),
        data.len,
        win.PAGE_EXECUTE_READWRITE,
        &old_protect,
    );

    // Write the bytes.
    @memcpy(ptr[0..data.len], data);

    // Restore original protection.
    _ = win.VirtualProtect(
        @ptrCast(ptr),
        data.len,
        old_protect,
        &old_protect,
    );
}

/// Write a single u8 to the target address.
pub fn writeU8(address: usize, value: u8) void {
    writeBytes(address, &.{value});
}

/// Write a u32 (little-endian) to the target address.
pub fn writeU32(address: usize, value: u32) void {
    var buf: [4]u8 = undefined;
    std.mem.writeInt(u32, &buf, value, .little);
    writeBytes(address, &buf);
}
