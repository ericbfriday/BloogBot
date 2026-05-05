//! bloog-launcher — minimal Zig replacement for the C# Bootstrapper.
//!
//! Starts wow.exe SUSPENDED, allocates memory inside it, writes the path to
//! the stub DLL, and uses CreateRemoteThread(LoadLibraryW, path) to load
//! the stub. Once LoadLibraryW returns we free the path memory and resume
//! wow.exe's main thread.
//!
//! This is a fairly faithful port of `Bootstrapper/Program.cs` with a few
//! deliberate divergences — see zig/NOTES.md for the lessons-learned log.

const std = @import("std");
const builtin = @import("builtin");
const win = @import("winapi");

comptime {
    if (builtin.os.tag != .windows) {
        @compileError(
            "bloog-launcher only supports Windows targets. " ++
                "Cross-compile from Linux / macOS with " ++
                "`zig build -Dtarget=x86-windows-gnu` (must be x86, not x86_64, " ++
                "because WoW 1.12.1 / 2.4.3 / 3.3.5 are 32-bit).",
        );
    }
}

const Error = error{
    InvalidArguments,
    CreateProcessFailed,
    GetKernel32Failed,
    GetLoadLibraryFailed,
    AllocFailed,
    WriteFailed,
    CreateThreadFailed,
    ThreadWaitFailed,
    ResumeFailed,
};

/// Global verbose flag — set from argv before injectDll runs.
var verbose: bool = false;

fn log(comptime fmt: []const u8, args: anytype) void {
    if (verbose) {
        std.debug.print("[bloog-launcher] " ++ fmt ++ "\n", args);
    }
}

fn killAndReturn(handle: win.HANDLE, err: Error) Error {
    _ = win.TerminateProcess(handle, 1);
    return err;
}

fn injectDll(
    allocator: std.mem.Allocator,
    wow_path: []const u8,
    dll_path: []const u8,
) !void {
    // 1. Convert paths to NUL-terminated UTF-16. CreateProcessW and
    //    LoadLibraryW both want LPCWSTR.
    const wow_w = try std.unicode.utf8ToUtf16LeAllocZ(allocator, wow_path);
    defer allocator.free(wow_w);
    const dll_w = try std.unicode.utf8ToUtf16LeAllocZ(allocator, dll_path);
    defer allocator.free(dll_w);

    // 2. Start wow.exe with CREATE_SUSPENDED.
    //
    //    The C# Bootstrapper does NOT do this — it calls CreateProcess
    //    normally, then sleeps 1 second and prays. By suspending the process
    //    until LoadLibraryW returns we eliminate the race where wow.exe
    //    starts initializing Direct3D / window / Lua state before our stub
    //    has a chance to install its WndProc hook.
    var startup = std.mem.zeroes(win.STARTUPINFOW);
    startup.cb = @sizeOf(win.STARTUPINFOW);
    var pinfo: win.PROCESS_INFORMATION = undefined;

    if (win.CreateProcessW(
        wow_w.ptr,
        null,
        null,
        null,
        win.FALSE,
        win.CREATE_SUSPENDED,
        null,
        null,
        &startup,
        &pinfo,
    ) == win.FALSE) {
        std.debug.print(
            "[bloog-launcher] CreateProcessW failed: GLE={d}\n",
            .{win.GetLastError()},
        );
        return Error.CreateProcessFailed;
    }
    defer _ = win.CloseHandle(pinfo.hThread);
    defer _ = win.CloseHandle(pinfo.hProcess);
    log("CreateProcessW ok: pid={d} tid={d} hProcess={*} hThread={*}", .{
        pinfo.dwProcessId,
        pinfo.dwThreadId,
        pinfo.hProcess,
        pinfo.hThread,
    });

    // 3. Resolve LoadLibraryW.
    //
    //    kernel32.dll is loaded at the same base address in every process
    //    on a given boot, so the LoadLibraryW pointer we get from *our*
    //    kernel32 is also valid inside wow.exe. That stops being true for
    //    64-bit-only DLLs across a WOW64 boundary, which is one reason the
    //    launcher itself is built x86 — see the comptime check above.
    const k32_name = std.unicode.utf8ToUtf16LeStringLiteral("kernel32.dll");
    const kernel32 = win.GetModuleHandleW(k32_name) orelse {
        return killAndReturn(pinfo.hProcess, Error.GetKernel32Failed);
    };
    // Note: GetProcAddress takes `HMODULE` which is itself optional, so we
    // could pass `kernel32` either as an optional or unwrapped. We unwrap
    // for clarity and because the next call needs a non-optional pointer.
    const load_library_addr = win.GetProcAddress(kernel32, "LoadLibraryW") orelse {
        return killAndReturn(pinfo.hProcess, Error.GetLoadLibraryFailed);
    };
    // Reinterpret the opaque function address as a thread start routine.
    // LoadLibraryW(LPCWSTR) and ThreadProc(LPVOID) are ABI-compatible — both
    // are stdcall taking one pointer-sized arg and returning one
    // pointer-sized value. @alignCast is defensive: GetProcAddress returns
    // pointer-aligned addresses on every Windows architecture we target,
    // but the cast makes the intent explicit.
    const load_library: win.LPTHREAD_START_ROUTINE =
        @ptrCast(@alignCast(load_library_addr));
    log("LoadLibraryW at 0x{x}", .{@intFromPtr(load_library_addr)});

    // 4. Allocate path memory inside wow.exe and write the DLL path.
    //
    //    Two latent bugs the C# Bootstrapper has here:
    //      a. It allocates `loaderPath.Length` BYTES — that's the *char*
    //         count of the path — but then writes `Encoding.Unicode.GetBytes`
    //         which is 2 * char count. It only works because VirtualAllocEx
    //         rounds up to the page size.
    //      b. It never writes a trailing NUL. LoadLibraryW requires a
    //         NUL-terminated wide string. It gets one only because freshly
    //         committed pages are zero-filled.
    //    We do the right thing on both counts: allocate (chars + 1) * 2
    //    bytes and let the trailing 0 of utf8ToUtf16LeAllocZ become the
    //    terminator inside wow.exe.
    //
    //    Protection: PAGE_READWRITE is enough — the path is data that
    //    LoadLibraryW reads. We do not need PAGE_EXECUTE_READWRITE here
    //    despite the C# code using it.
    const path_bytes: win.SIZE_T = (dll_w.len + 1) * @sizeOf(u16);
    const remote_path = win.VirtualAllocEx(
        pinfo.hProcess,
        null,
        path_bytes,
        win.MEM_COMMIT | win.MEM_RESERVE,
        win.PAGE_READWRITE,
    ) orelse {
        std.debug.print(
            "[bloog-launcher] VirtualAllocEx failed: GLE={d}\n",
            .{win.GetLastError()},
        );
        return killAndReturn(pinfo.hProcess, Error.AllocFailed);
    };
    log("VirtualAllocEx ok: remote_path at 0x{x} ({d} bytes)", .{
        @intFromPtr(remote_path),
        path_bytes,
    });

    // Cast the UTF-16 path to the const-anyopaque buffer pointer that
    // WriteProcessMemory wants. We do this in two steps for clarity: first
    // narrow [*:0]u16 to a non-optional const-anyopaque (which is allowed —
    // adding const is fine, type-erasure is fine), then let the implicit
    // conversion to the optional `LPCVOID` parameter happen at the call.
    const path_buf: *const anyopaque = @ptrCast(dll_w.ptr);

    var written: win.SIZE_T = 0;
    if (win.WriteProcessMemory(
        pinfo.hProcess,
        remote_path,
        path_buf,
        path_bytes,
        &written,
    ) == win.FALSE or written != path_bytes) {
        std.debug.print(
            "[bloog-launcher] WriteProcessMemory failed: GLE={d} written={d}/{d}\n",
            .{ win.GetLastError(), written, path_bytes },
        );
        return killAndReturn(pinfo.hProcess, Error.WriteFailed);
    }

    // 5. Create a remote thread starting at LoadLibraryW(remote_path).
    //
    //    LoadLibraryW's signature is `HMODULE WINAPI LoadLibraryW(LPCWSTR)`,
    //    which is binary-compatible with `DWORD WINAPI ThreadProc(LPVOID)`
    //    on x86 stdcall — both take one pointer-sized argument and return
    //    a pointer-sized value. So we can use it directly as the thread
    //    entry point.
    const remote_thread = win.CreateRemoteThread(
        pinfo.hProcess,
        null,
        0,
        load_library,
        remote_path,
        0,
        null,
    ) orelse {
        std.debug.print(
            "[bloog-launcher] CreateRemoteThread failed: GLE={d}\n",
            .{win.GetLastError()},
        );
        return killAndReturn(pinfo.hProcess, Error.CreateThreadFailed);
    };
    log("CreateRemoteThread ok: hThread={*}", .{remote_thread});
    defer _ = win.CloseHandle(remote_thread);

    // 6. Block until LoadLibraryW returns. Then it is safe to free the
    //    path memory; the DLL has been mapped and DllMain has finished.
    if (win.WaitForSingleObject(remote_thread, win.INFINITE) != win.WAIT_OBJECT_0) {
        return killAndReturn(pinfo.hProcess, Error.ThreadWaitFailed);
    }
    log("LoadLibraryW returned in remote process", .{});

    // 7. Free the path buffer. (The C# Bootstrapper does this *immediately*
    //    after CreateRemoteThread without waiting, which is technically a
    //    use-after-free if LoadLibraryW hasn't read the path yet.)
    _ = win.VirtualFreeEx(pinfo.hProcess, remote_path, 0, win.MEM_RELEASE);

    // 8. Resume wow.exe's main thread. ResumeThread returns 0xFFFFFFFF
    //    (DWORD -1) on failure.
    const prev_count = win.ResumeThread(pinfo.hThread);
    if (prev_count == 0xFFFF_FFFF) {
        return killAndReturn(pinfo.hProcess, Error.ResumeFailed);
    }
    log("ResumeThread ok: previous suspend count={d}", .{prev_count});
}

pub fn main(init: std.process.Init) !void {
    const allocator: std.mem.Allocator = init.arena.allocator();

    const args = try init.minimal.args.toSlice(allocator);

    // Parse optional --verbose flag (must appear before positional args).
    var arg_idx: usize = 1;
    while (arg_idx < args.len) : (arg_idx += 1) {
        if (std.mem.eql(u8, args[arg_idx], "--verbose") or
            std.mem.eql(u8, args[arg_idx], "-v"))
        {
            verbose = true;
        } else {
            break;
        }
    }

    const positional = args[arg_idx..];
    if (positional.len != 2) {
        std.debug.print(
            \\usage: bloog-launcher.exe [--verbose] <path-to-wow.exe> <path-to-bloog-stub.dll>
            \\
            \\options:
            \\  --verbose, -v   Print detailed diagnostic output
            \\
            \\example:
            \\  bloog-launcher.exe -v "C:\WoW\WotLK\Wow.exe" "C:\BloogBot\bloog-stub.dll"
            \\
        , .{});
        return Error.InvalidArguments;
    }

    log("wow_path = '{s}'", .{positional[0]});
    log("dll_path = '{s}'", .{positional[1]});

    try injectDll(allocator, positional[0], positional[1]);
    std.debug.print(
        "[bloog-launcher] injected '{s}' into '{s}'\n",
        .{ positional[1], positional[0] },
    );
}
