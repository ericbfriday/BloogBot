//! WndProc hook for running code on WoW's main thread.
//!
//! WoW's game state can only be safely accessed from the main thread. The
//! ThreadSynchronizer pattern (from BloogBot/ThreadSynchronizer.cs) works by:
//!   1. Finding WoW's main window via EnumWindows (title = "World of Warcraft")
//!   2. Replacing the window's WndProc with our own via SetWindowLong(GWL_WNDPROC)
//!   3. When the host needs to run something on the main thread, the stub
//!      queues the function pointer + arg and sends a WM_USER message
//!   4. Our WndProc intercepts WM_USER, drains the queue calling each function,
//!      then forwards all other messages to the original WndProc
//!
//! IMPORTANT: This must be initialized before any run_on_main_thread calls.
//! Initialization happens lazily on first use or can be triggered by the host.

const std = @import("std");
const log = @import("log");
const win = @import("winapi");

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------

const GWL_WNDPROC: c_int = -4;
const WM_USER: win.DWORD = 0x0400;

/// Maximum queued main-thread calls. If the queue overflows, new calls are
/// dropped (the host should not be sending faster than WoW processes messages).
const MAX_QUEUE_SIZE: usize = 256;

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

/// A queued function call: func_ptr(arg) — simple one-arg convention.
const QueuedCall = extern struct {
    func_ptr: usize,
    arg: usize,
};

/// WndProc signature: LRESULT CALLBACK WindowProc(HWND, UINT, WPARAM, LPARAM)
const WNDPROC = *const fn (win.HANDLE, win.DWORD, usize, isize) callconv(.winapi) isize;

// ---------------------------------------------------------------------------
// Global state
// ---------------------------------------------------------------------------

/// The WoW window handle found by EnumWindows.
var wow_hwnd: win.HANDLE = null;

/// Original WndProc function pointer (restored on DLL unload).
var original_wndproc: ?*const anyopaque = null;

/// Whether the hook has been installed.
var hook_installed: bool = false;

/// Ring buffer of pending main-thread calls.
var call_queue: [MAX_QUEUE_SIZE]QueuedCall = undefined;
var queue_head: usize = 0;
var queue_tail: usize = 0;
var queue_count: usize = 0;

// ---------------------------------------------------------------------------
// WndProc hook
// ---------------------------------------------------------------------------

/// Our custom WndProc. Intercepts WM_USER messages to drain the call queue.
/// All other messages are forwarded to the original WndProc.
fn hookedWndProc(
    hWnd: win.HANDLE,
    Msg: win.DWORD,
    wParam: usize,
    lParam: isize,
) callconv(.winapi) isize {
    if (Msg == WM_USER) {
        // Drain the call queue.
        while (queue_count > 0) {
            const call = call_queue[queue_head];
            queue_head = (queue_head + 1) % MAX_QUEUE_SIZE;
            queue_count -= 1;

            if (call.func_ptr != 0) {
                // Call the queued function: func_ptr(arg).
                // The function pointer points to a cdecl/stdcall function
                // that takes one usize arg and returns void.
                const func: *const fn (usize) callconv(.c) void = @ptrFromInt(call.func_ptr);
                func(call.arg);
            }
        }
        return 0;
    }

    // Forward to original WndProc.
    if (original_wndproc) |orig| {
        const orig_proc: WNDPROC = @ptrCast(@alignCast(orig));
        return orig_proc(hWnd, Msg, wParam, lParam);
    }
    return 0;
}

// ---------------------------------------------------------------------------
// EnumWindows callback — find WoW's window
// ---------------------------------------------------------------------------

/// Context for the EnumWindows search.
const FindWindowContext = struct {
    wow_window: win.HANDLE,
    process_id: win.DWORD,
};

fn enumWindowsCallback(hWnd: win.HANDLE, lParam: win.LPVOID) callconv(.winapi) win.BOOL {
    const ctx: *FindWindowContext = @ptrCast(@alignCast(lParam.?));

    // Check if this window belongs to our process.
    var pid: win.DWORD = 0;
    _ = win.GetWindowThreadProcessId(hWnd, &pid);
    if (pid != ctx.process_id) return win.TRUE; // continue

    // Check if visible.
    if (win.IsWindowVisible(hWnd) != win.TRUE) return win.TRUE;

    // Check window title length.
    const title_len = win.GetWindowTextLengthW(hWnd);
    if (title_len == 0) return win.TRUE;

    // Read the window title.
    var title_buf: [256]u16 = undefined;
    const chars_copied = win.GetWindowTextW(hWnd, @ptrCast(&title_buf), @intCast(title_buf.len));
    if (chars_copied == 0) return win.TRUE;

    const title_slice = title_buf[0..@as(usize, @intCast(chars_copied))];

    // Compare with "World of Warcraft".
    const wow_title = std.unicode.utf8ToUtf16LeStringLiteral("World of Warcraft");
    const wow_title_len: usize = wow_title.len / 2; // utf16 code units
    if (title_slice.len == wow_title_len and std.mem.eql(u16, title_slice, wow_title[0..wow_title_len])) {
        ctx.wow_window = hWnd;
        return win.FALSE; // stop enumeration
    }

    return win.TRUE; // continue
}

// ---------------------------------------------------------------------------
// Initialization
// ---------------------------------------------------------------------------

/// Find WoW's window and install the WndProc hook.
/// Returns true on success.
pub fn init() bool {
    if (hook_installed) return true;

    // Get our process ID.
    const pid = win.GetCurrentProcessId();

    // Find WoW's main window.
    var ctx = FindWindowContext{
        .wow_window = null,
        .process_id = pid,
    };
    _ = win.EnumWindows(enumWindowsCallback, @ptrCast(&ctx));

    if (ctx.wow_window == null) {
        log.warn("[wndproc] WoW window not found (pid={d})", .{pid});
        return false;
    }

    wow_hwnd = ctx.wow_window;
    log.info("[wndproc] found WoW window: hwnd=0x{x}", .{@intFromPtr(wow_hwnd.?)});

    // Install the WndProc hook.
    const old_proc = win.SetWindowLongW(
        wow_hwnd.?,
        GWL_WNDPROC,
        @bitCast(@intFromPtr(&hookedWndProc)),
    );
    if (old_proc == 0) {
        log.err("[wndproc] SetWindowLongW failed: GLE={d}", .{win.GetLastError()});
        return false;
    }

    original_wndproc = @ptrFromInt(@as(usize, @bitCast(old_proc)));
    hook_installed = true;

    log.info("[wndproc] hook installed, original=0x{x}", .{old_proc});
    return true;
}

/// Remove the WndProc hook and restore the original.
pub fn deinit() void {
    if (!hook_installed) return;
    if (wow_hwnd) |hwnd| {
        if (original_wndproc) |orig| {
            _ = win.SetWindowLongW(hwnd, GWL_WNDPROC, @bitCast(@intFromPtr(orig)));
        }
    }
    hook_installed = false;
    original_wndproc = null;
    wow_hwnd = null;
    log.info("[wndproc] hook removed", .{});
}

// ---------------------------------------------------------------------------
// Public API
// ---------------------------------------------------------------------------

/// Queue a function call to be executed on WoW's main thread.
/// The stub sends a WM_USER message to trigger the WndProc, which drains the queue.
///
/// `func_ptr` — address of a function with signature `void f(usize arg)`.
/// `arg` — single argument passed to the function.
///
/// Returns true if queued successfully, false if the queue is full or the
/// hook is not installed.
pub fn queueCall(func_ptr: usize, arg: usize) bool {
    if (!hook_installed) {
        // Try to initialize lazily.
        if (!init()) return false;
    }

    if (queue_count >= MAX_QUEUE_SIZE) {
        log.warn("[wndproc] call queue full, dropping func=0x{x}", .{func_ptr});
        return false;
    }

    call_queue[queue_tail] = .{
        .func_ptr = func_ptr,
        .arg = arg,
    };
    queue_tail = (queue_tail + 1) % MAX_QUEUE_SIZE;
    queue_count += 1;

    // Trigger the WndProc to drain the queue.
    _ = win.SendMessageW(wow_hwnd.?, WM_USER, 0, 0);

    return true;
}

/// Check if the WndProc hook is installed.
pub fn isHooked() bool {
    return hook_installed;
}
