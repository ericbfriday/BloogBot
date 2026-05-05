//! x86 __fastcall calling convention dispatch helpers.
//!
//! WoW's internal functions use the MSVC `__fastcall` convention, which Zig
//! doesn't support natively. These helpers use inline x86 assembly to set up
//! the fastcall register/stack convention and call into WoW's code.
//!
//! x86 fastcall rules:
//!   - First 2 dword-sized args → ECX, EDX
//!   - Remaining args → pushed right-to-left on the stack
//!   - Return value → EAX
//!   - Caller cleans up stack args (register args don't touch the stack)
//!
//! Port of FastCall/dllmain.cpp — 9 exports that wrap fastcall WoW functions.
//! In our architecture, these aren't DLL exports. The host sends RPC requests
//! with the function pointer + args, and the stub dispatches via these helpers.
//!
//! Register pressure: x86 has limited GP registers (EAX, EBX, ECX, EDX, ESI, EDI).
//! To avoid "more registers than available" errors with LLVM's register allocator,
//! stack args use `"m"` (memory) constraints so the asm loads them from stack
//! slots instead of requiring dedicated registers.

const std = @import("std");

// ---------------------------------------------------------------------------
// Generic fastcall dispatchers
// ---------------------------------------------------------------------------

/// Call a 2-arg fastcall function (args in ECX, EDX only — no stack args).
pub fn call2(func: usize, ecx_val: usize, edx_val: usize) usize {
    var result: usize = undefined;
    asm volatile (
        \\ movl %[ecx_val], %%ecx
        \\ movl %[edx_val], %%edx
        \\ call *%[func]
        \\ movl %%eax, %[result]
        : [result] "=r" (result),
        : [func] "r" (func),
          [ecx_val] "r" (ecx_val),
          [edx_val] "r" (edx_val),
    );
    return result;
}

/// Call a 3-arg fastcall function (2 reg + 1 stack arg).
pub fn call3(func: usize, ecx_val: usize, edx_val: usize, stack1: usize) usize {
    var result: usize = undefined;
    asm volatile (
        \\ pushl %[stack1]
        \\ movl %[ecx_val], %%ecx
        \\ movl %[edx_val], %%edx
        \\ call *%[func]
        \\ addl $4, %%esp
        \\ movl %%eax, %[result]
        : [result] "=r" (result),
        : [func] "r" (func),
          [ecx_val] "r" (ecx_val),
          [edx_val] "r" (edx_val),
          [stack1] "m" (stack1),
    );
    return result;
}

/// Call a 4-arg fastcall function (2 reg + 2 stack args).
pub fn call4(
    func: usize,
    ecx_val: usize,
    edx_val: usize,
    stack1: usize,
    stack2: usize,
) usize {
    var result: usize = undefined;
    asm volatile (
        \\ pushl %[stack2]
        \\ pushl %[stack1]
        \\ movl %[ecx_val], %%ecx
        \\ movl %[edx_val], %%edx
        \\ call *%[func]
        \\ addl $8, %%esp
        \\ movl %%eax, %[result]
        : [result] "=r" (result),
        : [func] "r" (func),
          [ecx_val] "r" (ecx_val),
          [edx_val] "r" (edx_val),
          [stack1] "m" (stack1),
          [stack2] "m" (stack2),
    );
    return result;
}

/// Call a 5-arg fastcall function (2 reg + 3 stack args).
pub fn call5(
    func: usize,
    ecx_val: usize,
    edx_val: usize,
    stack1: usize,
    stack2: usize,
    stack3: usize,
) usize {
    var result: usize = undefined;
    asm volatile (
        \\ pushl %[stack3]
        \\ pushl %[stack2]
        \\ pushl %[stack1]
        \\ movl %[ecx_val], %%ecx
        \\ movl %[edx_val], %%edx
        \\ call *%[func]
        \\ addl $12, %%esp
        \\ movl %%eax, %[result]
        : [result] "=r" (result),
        : [func] "r" (func),
          [ecx_val] "r" (ecx_val),
          [edx_val] "r" (edx_val),
          [stack1] "m" (stack1),
          [stack2] "m" (stack2),
          [stack3] "m" (stack3),
    );
    return result;
}

/// Call a 6-arg fastcall function (2 reg + 4 stack args).
pub fn call6(
    func: usize,
    ecx_val: usize,
    edx_val: usize,
    stack1: usize,
    stack2: usize,
    stack3: usize,
    stack4: usize,
) usize {
    var result: usize = undefined;
    asm volatile (
        \\ pushl %[stack4]
        \\ pushl %[stack3]
        \\ pushl %[stack2]
        \\ pushl %[stack1]
        \\ movl %[ecx_val], %%ecx
        \\ movl %[edx_val], %%edx
        \\ call *%[func]
        \\ addl $16, %%esp
        \\ movl %%eax, %[result]
        : [result] "=r" (result),
        : [func] "r" (func),
          [ecx_val] "r" (ecx_val),
          [edx_val] "r" (edx_val),
          [stack1] "m" (stack1),
          [stack2] "m" (stack2),
          [stack3] "m" (stack3),
          [stack4] "m" (stack4),
    );
    return result;
}

// ---------------------------------------------------------------------------
// Convenience: split u64 into two u32 for stack passing
// ---------------------------------------------------------------------------

/// Split a u64 into low and high dwords for x86 stack passing.
pub fn splitU64(val: u64) struct { low: usize, high: usize } {
    return .{
        .low = @as(usize, @truncate(val)),
        .high = @as(usize, @truncate(val >> 32)),
    };
}

// ---------------------------------------------------------------------------
// High-level shim wrappers (one per FastCall.dll export)
// ---------------------------------------------------------------------------

/// EnumerateVisibleObjects(callback: uint, filter: int)
/// fastcall: ECX=callback, EDX=filter
pub fn enumerateVisibleObjects(func_ptr: usize, callback: usize, filter: i32) void {
    _ = call2(func_ptr, callback, @bitCast(filter));
}

/// LuaCall(code: *char, unused: *char)
/// fastcall: ECX=code, EDX="Unused"
pub fn luaCall(func_ptr: usize, code: usize) void {
    _ = call2(func_ptr, code, 0);
}

/// LootSlot(slot: uint, unused: int)
/// fastcall: ECX=slot, EDX=0
pub fn lootSlot(func_ptr: usize, slot: u32) void {
    _ = call2(func_ptr, slot, 0);
}

/// GetText(varName: *char, nonSense: uint, zero: int) → uint
/// fastcall: ECX=varName, EDX=0xFFFFFFFF, stack=0
pub fn getText(func_ptr: usize, var_name: usize) usize {
    return call3(func_ptr, var_name, 0xFFFF_FFFF, 0);
}

/// Intersect(points: *XYZXYZ, distance: *float, intersection: *Intersection, flags: uint) → BYTE
/// fastcall: ECX=points, EDX=distance, stack=intersection, stack=flags
pub fn intersect(
    func_ptr: usize,
    points: usize,
    distance: usize,
    intersection: usize,
    flags: usize,
) u8 {
    return @truncate(call4(func_ptr, points, distance, intersection, flags));
}

/// Intersect2(p1: *XYZ, p2: *XYZ, ignore: int, intersection: *XYZ, distance: *float, flags: uint) → bool
/// fastcall: ECX=p1, EDX=p2, stack=ignore, stack=intersection, stack=distance, stack=flags
pub fn intersect2(
    func_ptr: usize,
    p1: usize,
    p2: usize,
    intersection: usize,
    distance: usize,
    flags: usize,
) bool {
    const result = call6(func_ptr, p1, p2, 0, intersection, distance, flags);
    return result != 0;
}

/// SellItemByGuid(count: uint, zero: uint, vendorGuid: ulong, itemGuid: ulong)
/// On x86: ECX=count, EDX=0, stack=vendor_lo, vendor_hi, item_lo, item_hi
pub fn sellItemByGuid(
    func_ptr: usize,
    count: u32,
    vendor_guid: u64,
    item_guid: u64,
) void {
    const vendor = splitU64(vendor_guid);
    const item = splitU64(item_guid);
    _ = call6(func_ptr, count, 0, vendor.low, vendor.high, item.low, item.high);
}

/// BuyVendorItem(itemIndex: uint, quantity: uint, vendorGuid: ulong, _one: int)
/// On x86: ECX=itemIndex, EDX=quantity, stack=vendor_lo, vendor_hi, 5
pub fn buyVendorItem(
    func_ptr: usize,
    item_index: u32,
    quantity: u32,
    vendor_guid: u64,
) void {
    const vendor = splitU64(vendor_guid);
    _ = call5(func_ptr, item_index, quantity, vendor.low, vendor.high, 5);
}

/// GetObjectPtr(typemask: int, objectGuid: ulong, line: int, file: *char) → uint
/// On x86: ECX=typemask, EDX=guid_lo, stack=guid_hi, line, file
pub fn getObjectPtr(
    func_ptr: usize,
    type_mask: usize,
    object_guid: u64,
    line: usize,
    file: usize,
) usize {
    const guid = splitU64(object_guid);
    return call5(func_ptr, type_mask, guid.low, guid.high, line, file);
}
