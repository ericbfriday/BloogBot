//! bloog-stub — in-process DLL injected into the target process.
//!
//! Architecture:
//!   1. DllMain (DLL_PROCESS_ATTACH): stores HMODULE, calls DisableThreadLibraryCalls,
//!      spawns background thread via CreateThread.
//!   2. Background thread: runs the named pipe server on \\.\pipe\bloogbot.
//!   3. Pipe server: accepts connections from bloog-host, processes RPC requests.
//!
//! IMPORTANT: DllMain must not block. All real work happens on the background thread.

const std = @import("std");
const builtin = @import("builtin");
const win = @import("winapi");
const log = @import("log");
const pipe_server = @import("pipe_server.zig");

comptime {
    if (builtin.os.tag != .windows) {
        @compileError("bloog-stub only supports Windows targets (x86-windows-gnu).");
    }
}

// ---------------------------------------------------------------------------
// Global state
// ---------------------------------------------------------------------------

/// The HMODULE passed to DllMain. Stored so the pipe server thread
/// and other helpers can reference it (e.g., to get the DLL path).
pub var g_hmodule: win.HMODULE = null;

// Zig 0.16 changed std.os.windows.BOOL from c_int to Bool(c_int).
const StdBool = std.os.windows.BOOL;

// ---------------------------------------------------------------------------
// DllMain — the DLL entry point called by the loader.
// ---------------------------------------------------------------------------

pub export fn DllMain(
    hModule: win.HMODULE,
    ul_reason_for_call: win.DWORD,
    lpReserved: win.LPVOID,
) callconv(.winapi) StdBool {
    _ = lpReserved;

    switch (ul_reason_for_call) {
        win.DLL_PROCESS_ATTACH => {
            g_hmodule = hModule;

            // Disable DLL_THREAD_ATTACH/DETACH notifications — we don't need
            // them and they add overhead on every thread create/exit.
            _ = win.DisableThreadLibraryCalls(hModule);

            log.info("[bloog-stub] DLL_PROCESS_ATTACH", .{});

            // Spawn the pipe server thread. CreateThread is technically unsafe
            // inside DllMain per MSDN docs, but this is the same pattern
            // Loader.dll and FastCall.dll use. The thread won't touch the
            // loader lock because it only does pipe I/O.
            const thread = win.CreateThread(
                null,
                0,
                pipeServerThread,
                null,
                0,
                null,
            );
            if (thread != null) {
                _ = win.CloseHandle(thread);
            } else {
                log.err("[bloog-stub] CreateThread failed: GLE={d}", .{win.GetLastError()});
            }
        },
        win.DLL_PROCESS_DETACH => {
            log.info("[bloog-stub] DLL_PROCESS_DETACH", .{});
        },
        else => {},
    }

    return @enumFromInt(@as(c_int, @intFromBool(true)));
}

// ---------------------------------------------------------------------------
// Background thread entry point
// ---------------------------------------------------------------------------

fn pipeServerThread(lpParam: win.LPVOID) callconv(.winapi) win.DWORD {
    _ = lpParam;
    log.info("[bloog-stub] pipe server thread started", .{});

    pipe_server.run();

    log.info("[bloog-stub] pipe server thread exiting", .{});
    return 0;
}
