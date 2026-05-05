//! Navigation FFI — loads Navigation.dll and calls CalculatePath/FreePathArr.
//!
//! Port of BloogBot/Navigation.cs. The Navigation DLL is a C++ library that
//! computes paths via Recast/Detour mmaps. It exposes two cdecl functions:
//!   CalculatePath(mapId, start, end, straightPath, &length) -> XYZ*
//!   FreePathArr(pathArr)
//!
//! The host loads the DLL at startup and delegates pathfinding to it.

const std = @import("std");
const win = @import("winapi");
const log = @import("log");
const game = @import("game_objects.zig");

// ---------------------------------------------------------------------------
// XYZ — matches the C++ struct in Navigation.h
// ---------------------------------------------------------------------------

pub const XYZ = extern struct {
    x: f32,
    y: f32,
    z: f32,
};

// ---------------------------------------------------------------------------
// Function pointer types (cdecl, matching Navigation.dll exports)
// ---------------------------------------------------------------------------

const CalculatePathFn = *const fn (map_id: u32, start: XYZ, end: XYZ, straight_path: bool, out_length: *u32) callconv(.c) ?[*]XYZ;
const FreePathArrFn = *const fn (path_arr: [*]XYZ) callconv(.c) void;

// ---------------------------------------------------------------------------
// State — loaded once at init
// ---------------------------------------------------------------------------

var calculate_path: CalculatePathFn = undefined;
var free_path_arr: FreePathArrFn = undefined;
var loaded = false;

// ---------------------------------------------------------------------------
// Init — must be called before any pathfinding
// ---------------------------------------------------------------------------

pub fn init() void {
    if (loaded) return;

    const dll_path = std.unicode.utf8ToUtf16LeStringLiteral("Navigation.dll");
    const dll = win.LoadLibraryW(dll_path) orelse {
        log.err("[navigation] failed to load Navigation.dll", .{});
        return;
    };

    const calc_sym = win.GetProcAddress(dll, "CalculatePath") orelse {
        log.err("[navigation] failed to find CalculatePath export", .{});
        return;
    };
    const free_sym = win.GetProcAddress(dll, "FreePathArr") orelse {
        log.err("[navigation] failed to find FreePathArr export", .{});
        return;
    };

    calculate_path = @ptrCast(@alignCast(calc_sym));
    free_path_arr = @ptrCast(@alignCast(free_sym));
    loaded = true;
    log.info("[navigation] Navigation.dll loaded successfully", .{});
}

// ---------------------------------------------------------------------------
// Public API
// ---------------------------------------------------------------------------

/// Calculate a full path from start to end. Caller must free the result.
/// Returns null if Navigation.dll is not loaded or pathfinding fails.
pub fn calculatePath(allocator: std.mem.Allocator, map_id: u32, start: game.Position, end: game.Position) ?[]game.Position {
    if (!loaded) return null;

    var length: u32 = 0;
    const raw_path = calculate_path(
        map_id,
        .{ .x = start.x, .y = start.y, .z = start.z },
        .{ .x = end.x, .y = end.y, .z = end.z },
        false,
        &length,
    ) orelse return null;
    defer free_path_arr(raw_path);

    if (length == 0) return null;

    const result = allocator.alloc(game.Position, length) catch return null;
    for (result, 0..) |*pos, i| {
        pos.* = .{ .x = raw_path[i].x, .y = raw_path[i].y, .z = raw_path[i].z };
    }
    return result;
}

/// Get the next waypoint from start toward end.
/// Returns the second waypoint in the calculated path (first is start).
/// If path has <=1 points, returns `end` directly.
pub fn getNextWaypoint(map_id: u32, start: game.Position, end: game.Position) game.Position {
    if (!loaded) return end;

    var length: u32 = 0;
    const raw_path = calculate_path(
        map_id,
        .{ .x = start.x, .y = start.y, .z = start.z },
        .{ .x = end.x, .y = end.y, .z = end.z },
        false,
        &length,
    ) orelse return end;
    defer free_path_arr(raw_path);

    if (length <= 1) return end;
    return .{ .x = raw_path[1].x, .y = raw_path[1].y, .z = raw_path[1].z };
}
