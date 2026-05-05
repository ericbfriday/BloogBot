//! Object enumeration for the stub.
//!
//! WoW's EnumerateVisibleObjects function calls a user-provided callback once
//! for each visible object in the game world. On WotLK (3.3.5a), the callback
//! uses __cdecl convention:
//!   int callback(uint64 guid, int filter)
//!
//! The callback receives each object's GUID, looks up the object pointer via
//! GetObjectPtr (another fastcall function), reads the object type, and stores
//! the info into a global buffer. After enumeration completes, the buffer is
//! sent back to the host.
//!
//! Port of BloogBot/Game/ObjectManager.cs:232-318.

const std = @import("std");
const log = @import("log");
const rpc = @import("rpc_types");
const fastcall = @import("fastcall.zig");
const memory = @import("memory.zig");

// ---------------------------------------------------------------------------
// Constants from ObjectManager.cs
// ---------------------------------------------------------------------------

/// Offset from object base pointer to the object type field.
const OBJECT_TYPE_OFFSET: usize = 0x14;

/// Maximum number of objects we can collect in one enumeration pass.
/// WoW typically has 500-2000 visible objects. 4096 gives us headroom.
const MAX_OBJECTS: usize = 4096;

// ---------------------------------------------------------------------------
// Global enumeration buffer
// ---------------------------------------------------------------------------
// The callback writes into this buffer. The `collect()` function resets it
// before each pass and returns the populated slice.

var enum_buffer: [MAX_OBJECTS]rpc.ObjectInfo = undefined;
var enum_count: usize = 0;

/// The GetObjectPtr function address for the current enumeration pass.
/// Set by `collect()` before invoking EnumerateVisibleObjects.
var current_get_object_ptr: usize = 0;

// ---------------------------------------------------------------------------
// __cdecl callback — called by WoW for each visible object (WotLK)
// ---------------------------------------------------------------------------

/// WotLK callback: `int __cdecl callback(uint64 guid, int filter)`
/// Returns 1 to continue enumeration, 0 to stop.
fn cdeclCallback(guid: u64, filter: c_int) callconv(.c) c_int {
    _ = filter;

    if (enum_count >= MAX_OBJECTS) return 0; // buffer full, stop

    // Look up the object pointer via the game's GetObjectPtr fastcall.
    // The C# calls GetObjectPtr with (typemask=0, guid, line=0, file=0).
    // typemask=0 means "any type".
    const obj_ptr = fastcall.getObjectPtr(
        current_get_object_ptr,
        0, // typemask: 0 = any
        guid,
        0, // line (unused)
        0, // file (unused)
    );

    if (obj_ptr == 0) return 1; // skip invalid objects

    // Read the object type from the base pointer + offset.
    const object_type = memory.readU8(obj_ptr + OBJECT_TYPE_OFFSET);

    enum_buffer[enum_count] = .{
        .guid = guid,
        .object_type = object_type,
        .descriptor_ptr = obj_ptr,
    };
    enum_count += 1;

    return 1; // continue enumeration
}

// ---------------------------------------------------------------------------
// Public API
// ---------------------------------------------------------------------------

/// Enumerate all visible objects and return the collected ObjectInfo slice.
///
/// `enumerate_func_ptr` — address of WoW's EnumerateVisibleObjects function.
/// `get_obj_ptr_func` — address of WoW's GetObjectPtr function.
///
/// This must be called on WoW's main thread (via run_on_main_thread) because
/// EnumerateVisibleObjects interacts with the game's internal state.
pub fn collect(
    enumerate_func_ptr: usize,
    get_obj_ptr_func: usize,
) []const rpc.ObjectInfo {
    // Reset the buffer.
    enum_count = 0;
    current_get_object_ptr = get_obj_ptr_func;

    // Get our callback's function pointer. We need to take the address of
    // cdeclCallback as a usize to pass to the game's EnumerateVisibleObjects.
    const callback_ptr: usize = @intFromPtr(&cdeclCallback);

    // Call EnumerateVisibleObjects(callback_ptr, 0) via fastcall.
    // ECX = callback_ptr, EDX = 0 (filter).
    fastcall.enumerateVisibleObjects(enumerate_func_ptr, callback_ptr, 0);

    return enum_buffer[0..enum_count];
}
