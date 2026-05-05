//! Shared RPC opcodes and message types for stub↔host communication.
//!
//! Wire format: [opcode: u8][payload_len: u32][payload: [payload_len]u8]
//!
//! The stub runs inside wow.exe, the host runs out-of-process. All
//! communication goes over a named pipe (\\.\pipe\bloogbot). The
//! host sends requests, the stub sends responses.

// ---------------------------------------------------------------------------
// Opcodes — host → stub (requests)
// ---------------------------------------------------------------------------

pub const Opcode = enum(u8) {
    // Memory read operations.
    read_u8 = 0x01,
    read_u16 = 0x02,
    read_u32 = 0x03,
    read_u64 = 0x04,
    read_f32 = 0x05,
    read_bytes = 0x06,
    read_string = 0x07,

    // Memory write operations.
    write_bytes = 0x10,
    write_u8 = 0x11,
    write_u32 = 0x12,

    // Object enumeration.
    enumerate_objects = 0x20,

    // Thread synchronization (run on WoW main thread).
    run_on_main_thread = 0x30,

    // Fastcall shims (9 exports from FastCall.dll).
    fastcall_lua_call = 0x40,
    fastcall_loot_slot = 0x41,
    fastcall_get_text = 0x42,
    fastcall_intersect = 0x43,
    fastcall_intersect2 = 0x44,
    fastcall_sell_item_by_guid = 0x45,
    fastcall_buy_vendor_item = 0x46,
    fastcall_get_object_ptr = 0x47,
    fastcall_enumerate_visible_objects = 0x48,

    // Detour management.
    install_detour = 0x50,
    remove_detour = 0x51,

    // Lifecycle.
    ping = 0xF0,
    shutdown = 0xFF,

    _,
};

// ---------------------------------------------------------------------------
// Response codes — stub → host
// ---------------------------------------------------------------------------

pub const Response = enum(u8) {
    ok = 0x00,
    error_invalid_opcode = 0x01,
    error_read_failed = 0x02,
    error_write_failed = 0x03,
    error_pipe_error = 0x04,
    error_timeout = 0x05,
    error_not_implemented = 0x06,

    _,
};

// ---------------------------------------------------------------------------
// Message header (fixed 5 bytes: 1 opcode + 4 length)
//
// We DON'T use an extern struct for the header because extern structs
// have field alignment padding (u32 after u8 would pad to 8 bytes).
// Instead, we parse the 5-byte wire format manually.
// ---------------------------------------------------------------------------

pub const HEADER_SIZE: usize = 5; // 1 + 4

/// Parse an opcode byte from the header.
pub fn parseOpcode(header_bytes: *const [HEADER_SIZE]u8) Opcode {
    return @enumFromInt(header_bytes[0]);
}

/// Parse the payload length from the header (little-endian u32).
pub fn parsePayloadLen(header_bytes: *const [HEADER_SIZE]u8) u32 {
    return std.mem.readInt(u32, header_bytes[1..5], .little);
}

/// Build a header byte array from opcode and payload length.
pub fn buildHeader(opcode: Opcode, payload_len: u32) [HEADER_SIZE]u8 {
    var buf: [HEADER_SIZE]u8 = undefined;
    buf[0] = @intFromEnum(opcode);
    std.mem.writeInt(u32, buf[1..5], payload_len, .little);
    return buf;
}

// ---------------------------------------------------------------------------
// Convenience types for specific messages
// ---------------------------------------------------------------------------

/// Request body for read_* operations: just the address to read from.
pub const ReadRequest = extern struct {
    address: usize,
};

/// Request body for read_bytes: address + count.
pub const ReadBytesRequest = extern struct {
    address: usize,
    count: u32,
};

/// Request body for write_bytes: address + length + data follows.
pub const WriteBytesRequest = extern struct {
    address: usize,
    length: u32,
    // data: [length]u8 follows in payload after this struct
};

/// Response body for enumerate_objects: count + array of ObjectInfo follows.
pub const EnumerateObjectsResponse = extern struct {
    count: u32,
    // objects: [count]ObjectInfo follows in payload after this struct
};

/// Per-object info returned during enumeration.
pub const ObjectInfo = extern struct {
    guid: u64,
    object_type: u8,
    descriptor_ptr: usize,
};

// ---------------------------------------------------------------------------
// Fastcall shim request types
// ---------------------------------------------------------------------------
// Each shim request carries the WoW function pointer + shim-specific args.
// On x86, usize = u32. u64 args are sent as raw 8-byte little-endian values.

/// Generic 2-arg fastcall: ECX=arg1, EDX=arg2.
/// Used by: EnumerateVisibleObjects, LuaCall, LootSlot.
pub const Fastcall2Request = extern struct {
    func_ptr: usize,
    arg1: usize, // ECX
    arg2: usize, // EDX
};

/// 3-arg fastcall: ECX=arg1, EDX=arg2, stack=arg3.
/// Used by: GetText.
pub const Fastcall3Request = extern struct {
    func_ptr: usize,
    arg1: usize, // ECX
    arg2: usize, // EDX
    arg3: usize, // stack
};

/// 4-arg fastcall: ECX=arg1, EDX=arg2, stack=arg3, stack=arg4.
/// Used by: Intersect.
pub const Fastcall4Request = extern struct {
    func_ptr: usize,
    arg1: usize,
    arg2: usize,
    arg3: usize,
    arg4: usize,
};

/// 6-arg fastcall: ECX=arg1, EDX=arg2, stack=arg3..arg6.
/// Used by: Intersect2.
pub const Fastcall6Request = extern struct {
    func_ptr: usize,
    arg1: usize,
    arg2: usize,
    arg3: usize,
    arg4: usize,
    arg5: usize,
    arg6: usize,
};

/// SellItemByGuid: ECX=count, EDX=0, stack=vendorGuid(u64), stack=itemGuid(u64).
/// u64 args sent as 8-byte LE.
pub const FastcallSellItemRequest = extern struct {
    func_ptr: usize,
    count: u32,
    _pad: u32, // alignment padding
    vendor_guid: u64,
    item_guid: u64,
};

/// BuyVendorItem: ECX=itemIndex, EDX=quantity, stack=vendorGuid(u64), stack=5.
pub const FastcallBuyItemRequest = extern struct {
    func_ptr: usize,
    item_index: u32,
    quantity: u32,
    vendor_guid: u64,
};

/// GetObjectPtr: ECX=typemask, EDX=guid_lo, stack=guid_hi, line, file.
pub const FastcallGetObjPtrRequest = extern struct {
    func_ptr: usize,
    type_mask: usize,
    object_guid: u64,
    line: usize,
    file: usize,
};

// ---------------------------------------------------------------------------
// Object enumeration request
// ---------------------------------------------------------------------------

/// Request to enumerate visible objects. The host provides the addresses of
/// two WoW functions: EnumerateVisibleObjects and GetObjectPtr.
pub const EnumerateObjectsRequest = extern struct {
    enumerate_visible_objects_ptr: usize,
    get_object_ptr_ptr: usize,
};

// ---------------------------------------------------------------------------
// Run-on-main-thread request
// ---------------------------------------------------------------------------

/// Request to call a function on WoW's main thread via WndProc hook.
/// The stub queues the call and triggers WM_USER to execute it.
pub const RunOnMainThreadRequest = extern struct {
    func_ptr: usize,
    arg: usize,
};

// ---------------------------------------------------------------------------
// Detour request types
// ---------------------------------------------------------------------------

/// Install a detour at target_addr: save original bytes, write PUSH+RET trampoline.
pub const InstallDetourRequest = extern struct {
    target_addr: usize,
    hook_addr: usize,
};

/// Remove a detour: restore original bytes at target_addr.
pub const RemoveDetourRequest = extern struct {
    target_addr: usize,
};

// ---------------------------------------------------------------------------
// Detour registration response
// ---------------------------------------------------------------------------

/// Response payload for install_detour: returns the number of saved bytes
/// and the original bytes themselves (for restore).
pub const DetourInfo = extern struct {
    target_addr: usize,
    saved_len: u32,
    // original_bytes: [saved_len]u8 follows after this struct
};

const std = @import("std");
