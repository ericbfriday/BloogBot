//! Self-contained Win32 declarations.
//!
//! We declare every Win32 type and function the project uses here rather
//! than relying on `std.os.windows`, for three reasons:
//!
//!   1. The `std.os.windows` surface churns between Zig releases. Binding to
//!      it ties our build matrix to a specific compiler version.
//!   2. Auditability: every kernel32/user32 call this project makes is
//!      visible in this one file. That matters when you're injecting code
//!      into someone else's process.
//!   3. The stub (which is loaded inside wow.exe) wants a minimal,
//!      controlled set of imports — no surprise dependencies on stdlib
//!      machinery.
//!
//! Calling convention: we use `.winapi`, which Zig 0.14+ resolves to
//! `__stdcall` on x86 and the Microsoft x64 ABI on x86_64. That matches the
//! ABI every `kernel32.dll` / `user32.dll` export uses.

// ---------------------------------------------------------------------------
// Primitive types
// ---------------------------------------------------------------------------

pub const BOOL = c_int;
pub const BYTE = u8;
pub const WORD = u16;
pub const DWORD = u32;
pub const SIZE_T = usize;

pub const HANDLE = ?*anyopaque;
pub const HMODULE = ?*anyopaque;
pub const HINSTANCE = HMODULE;
pub const FARPROC = ?*const fn () callconv(.winapi) isize;

pub const LPVOID = ?*anyopaque;
pub const LPCVOID = ?*const anyopaque;

pub const LPCSTR = [*:0]const u8;
pub const LPSTR = [*:0]u8;
pub const LPCWSTR = [*:0]const u16;
pub const LPWSTR = [*:0]u16;

pub const FALSE: BOOL = 0;
pub const TRUE: BOOL = 1;

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------

pub const INFINITE: DWORD = 0xFFFF_FFFF;
pub const WAIT_OBJECT_0: DWORD = 0x0000_0000;
pub const WAIT_FAILED: DWORD = 0xFFFF_FFFF;

pub const MEM_COMMIT: DWORD = 0x0000_1000;
pub const MEM_RESERVE: DWORD = 0x0000_2000;
pub const MEM_RELEASE: DWORD = 0x0000_8000;

pub const PAGE_READONLY: DWORD = 0x02;
pub const PAGE_READWRITE: DWORD = 0x04;
pub const PAGE_EXECUTE: DWORD = 0x10;
pub const PAGE_EXECUTE_READ: DWORD = 0x20;
pub const PAGE_EXECUTE_READWRITE: DWORD = 0x40;

pub const CREATE_SUSPENDED: DWORD = 0x0000_0004;
pub const CREATE_DEFAULT_ERROR_MODE: DWORD = 0x0400_0000;

// ---------------------------------------------------------------------------
// Structures
// ---------------------------------------------------------------------------

pub const SECURITY_ATTRIBUTES = extern struct {
    nLength: DWORD,
    lpSecurityDescriptor: LPVOID,
    bInheritHandle: BOOL,
};

pub const STARTUPINFOW = extern struct {
    cb: DWORD,
    lpReserved: ?LPWSTR,
    lpDesktop: ?LPWSTR,
    lpTitle: ?LPWSTR,
    dwX: DWORD,
    dwY: DWORD,
    dwXSize: DWORD,
    dwYSize: DWORD,
    dwXCountChars: DWORD,
    dwYCountChars: DWORD,
    dwFillAttribute: DWORD,
    dwFlags: DWORD,
    wShowWindow: WORD,
    cbReserved2: WORD,
    lpReserved2: ?[*]BYTE,
    hStdInput: HANDLE,
    hStdOutput: HANDLE,
    hStdError: HANDLE,
};

pub const PROCESS_INFORMATION = extern struct {
    hProcess: HANDLE,
    hThread: HANDLE,
    dwProcessId: DWORD,
    dwThreadId: DWORD,
};

// LoadLibraryW has the right shape to be a thread start routine:
//   DWORD WINAPI ThreadProc(LPVOID lpParameter);
// We cast its address to this type when calling CreateRemoteThread.
pub const LPTHREAD_START_ROUTINE =
    *const fn (lpThreadParameter: LPVOID) callconv(.winapi) DWORD;

// ---------------------------------------------------------------------------
// kernel32 imports
// ---------------------------------------------------------------------------

pub extern "kernel32" fn CreateProcessW(
    lpApplicationName: ?LPCWSTR,
    lpCommandLine: ?LPWSTR,
    lpProcessAttributes: ?*SECURITY_ATTRIBUTES,
    lpThreadAttributes: ?*SECURITY_ATTRIBUTES,
    bInheritHandles: BOOL,
    dwCreationFlags: DWORD,
    lpEnvironment: LPVOID,
    lpCurrentDirectory: ?LPCWSTR,
    lpStartupInfo: *STARTUPINFOW,
    lpProcessInformation: *PROCESS_INFORMATION,
) callconv(.winapi) BOOL;

pub extern "kernel32" fn GetModuleHandleW(
    lpModuleName: ?LPCWSTR,
) callconv(.winapi) HMODULE;

pub extern "kernel32" fn GetProcAddress(
    hModule: HMODULE,
    lpProcName: LPCSTR,
) callconv(.winapi) ?*anyopaque;

pub extern "kernel32" fn VirtualAllocEx(
    hProcess: HANDLE,
    lpAddress: LPVOID,
    dwSize: SIZE_T,
    flAllocationType: DWORD,
    flProtect: DWORD,
) callconv(.winapi) LPVOID;

pub extern "kernel32" fn VirtualFreeEx(
    hProcess: HANDLE,
    lpAddress: LPVOID,
    dwSize: SIZE_T,
    dwFreeType: DWORD,
) callconv(.winapi) BOOL;

pub extern "kernel32" fn WriteProcessMemory(
    hProcess: HANDLE,
    lpBaseAddress: LPVOID,
    lpBuffer: LPCVOID,
    nSize: SIZE_T,
    lpNumberOfBytesWritten: ?*SIZE_T,
) callconv(.winapi) BOOL;

pub extern "kernel32" fn CreateRemoteThread(
    hProcess: HANDLE,
    lpThreadAttributes: ?*SECURITY_ATTRIBUTES,
    dwStackSize: SIZE_T,
    lpStartAddress: LPTHREAD_START_ROUTINE,
    lpParameter: LPVOID,
    dwCreationFlags: DWORD,
    lpThreadId: ?*DWORD,
) callconv(.winapi) HANDLE;

pub extern "kernel32" fn WaitForSingleObject(
    hHandle: HANDLE,
    dwMilliseconds: DWORD,
) callconv(.winapi) DWORD;

pub extern "kernel32" fn ResumeThread(
    hThread: HANDLE,
) callconv(.winapi) DWORD;

pub extern "kernel32" fn CloseHandle(
    hObject: HANDLE,
) callconv(.winapi) BOOL;

pub extern "kernel32" fn TerminateProcess(
    hProcess: HANDLE,
    uExitCode: u32,
) callconv(.winapi) BOOL;

pub extern "kernel32" fn GetLastError() callconv(.winapi) DWORD;
