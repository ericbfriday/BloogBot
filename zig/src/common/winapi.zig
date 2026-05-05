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

// DLL notification constants (used in DllMain dwReason).
pub const DLL_PROCESS_ATTACH: DWORD = 1;
pub const DLL_THREAD_ATTACH: DWORD = 2;
pub const DLL_THREAD_DETACH: DWORD = 3;
pub const DLL_PROCESS_DETACH: DWORD = 0;

// Named pipe constants.
pub const PIPE_ACCESS_DUPLEX: DWORD = 0x0000_0003;
pub const PIPE_TYPE_BYTE: DWORD = 0x0000_0000;
pub const PIPE_TYPE_MESSAGE: DWORD = 0x0000_0004;
pub const PIPE_READMODE_BYTE: DWORD = 0x0000_0000;
pub const PIPE_READMODE_MESSAGE: DWORD = 0x0000_0002;
pub const PIPE_WAIT: DWORD = 0x0000_0000;
pub const PIPE_NOWAIT: DWORD = 0x0000_0001;
pub const PIPE_UNLIMITED_INSTANCES: DWORD = 255;

// Named pipe connect result.
pub const ERROR_PIPE_CONNECTED: DWORD = 535;

// VirtualProtect constants (additional).
pub const PAGE_GUARD: DWORD = 0x100;
pub const PAGE_NOCACHE: DWORD = 0x200;

// File creation disposition.
pub const OPEN_EXISTING: DWORD = 3;

// Generic read/write access rights.
pub const GENERIC_READ: DWORD = 0x8000_0000;
pub const GENERIC_WRITE: DWORD = 0x4000_0000;

pub const INVALID_HANDLE_VALUE: HANDLE = @ptrFromInt(@as(usize, @bitCast(@as(isize, -1))));

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

// OVERLAPPED structure for async I/O (named pipes, etc.).
pub const OVERLAPPED = extern struct {
    Internal: usize,
    InternalHigh: usize,
    offset: DWORD,
    offsetHigh: DWORD,
    hEvent: HANDLE,
};

// LPOVERLAPPED_COMPLETION_ROUTINE for ReadFileEx / WriteFileEx.
pub const LPOVERLAPPED_COMPLETION_ROUTINE =
    *const fn (dwErrorCode: DWORD, dwNumberOfBytesTransfered: DWORD, lpOverlapped: *OVERLAPPED) callconv(.winapi) void;

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

pub extern "kernel32" fn LoadLibraryW(
    lpFileName: LPCWSTR,
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

pub extern "kernel32" fn GetCurrentProcessId() callconv(.winapi) DWORD;

// ---------------------------------------------------------------------------
// Additional kernel32 imports (stub, pipe server, memory ops)
// ---------------------------------------------------------------------------

pub extern "kernel32" fn CreateThread(
    lpThreadAttributes: ?*SECURITY_ATTRIBUTES,
    dwStackSize: SIZE_T,
    lpStartAddress: LPTHREAD_START_ROUTINE,
    lpParameter: LPVOID,
    dwCreationFlags: DWORD,
    lpThreadId: ?*DWORD,
) callconv(.winapi) HANDLE;

pub extern "kernel32" fn DisableThreadLibraryCalls(
    hModule: HMODULE,
) callconv(.winapi) BOOL;

pub extern "kernel32" fn GetModuleFileNameW(
    hModule: HMODULE,
    lpFilename: LPWSTR,
    nSize: DWORD,
) callconv(.winapi) DWORD;

pub extern "kernel32" fn VirtualProtect(
    lpAddress: LPVOID,
    dwSize: SIZE_T,
    flNewProtect: DWORD,
    lpflOldProtect: *DWORD,
) callconv(.winapi) BOOL;

pub extern "kernel32" fn Sleep(
    dwMilliseconds: DWORD,
) callconv(.winapi) void;

pub extern "kernel32" fn CreateNamedPipeW(
    lpName: LPCWSTR,
    dwOpenMode: DWORD,
    dwPipeMode: DWORD,
    nMaxInstances: DWORD,
    nOutBufferSize: DWORD,
    nInBufferSize: DWORD,
    nDefaultTimeOut: DWORD,
    lpSecurityAttributes: ?*SECURITY_ATTRIBUTES,
) callconv(.winapi) HANDLE;

pub extern "kernel32" fn ConnectNamedPipe(
    hNamedPipe: HANDLE,
    lpOverlapped: ?*OVERLAPPED,
) callconv(.winapi) BOOL;

pub extern "kernel32" fn DisconnectNamedPipe(
    hNamedPipe: HANDLE,
) callconv(.winapi) BOOL;

pub extern "kernel32" fn ReadFile(
    hFile: HANDLE,
    lpBuffer: LPVOID,
    nNumberOfBytesToRead: DWORD,
    lpNumberOfBytesRead: ?*DWORD,
    lpOverlapped: ?*OVERLAPPED,
) callconv(.winapi) BOOL;

pub extern "kernel32" fn WriteFile(
    hFile: HANDLE,
    lpBuffer: LPCVOID,
    nNumberOfBytesToWrite: DWORD,
    lpNumberOfBytesWritten: ?*DWORD,
    lpOverlapped: ?*OVERLAPPED,
) callconv(.winapi) BOOL;

pub extern "kernel32" fn FlushFileBuffers(
    hFile: HANDLE,
) callconv(.winapi) BOOL;

pub extern "kernel32" fn CreateFileW(
    lpFileName: LPCWSTR,
    dwDesiredAccess: DWORD,
    dwShareMode: DWORD,
    lpSecurityAttributes: ?*SECURITY_ATTRIBUTES,
    dwCreationDisposition: DWORD,
    dwFlagsAndAttributes: DWORD,
    hTemplateFile: HANDLE,
) callconv(.winapi) HANDLE;

// ---------------------------------------------------------------------------
// kernel32 debug output
// ---------------------------------------------------------------------------

pub extern "kernel32" fn OutputDebugStringA(
    lpOutputString: LPCSTR,
) callconv(.winapi) void;

// ---------------------------------------------------------------------------
// user32 imports (thread sync / WndProc hook — used later in Phase 3)
// ---------------------------------------------------------------------------

pub extern "user32" fn EnumWindows(
    lpEnumFunc: ?*const fn (hWnd: HANDLE, lParam: LPVOID) callconv(.winapi) BOOL,
    lParam: LPVOID,
) callconv(.winapi) BOOL;

pub extern "user32" fn GetWindowThreadProcessId(
    hWnd: HANDLE,
    lpdwProcessId: ?*DWORD,
) callconv(.winapi) DWORD;

pub extern "user32" fn IsWindowVisible(
    hWnd: HANDLE,
) callconv(.winapi) BOOL;

pub extern "user32" fn GetWindowTextLengthW(
    hWnd: HANDLE,
) callconv(.winapi) c_int;

pub extern "user32" fn GetWindowTextW(
    hWnd: HANDLE,
    lpString: LPWSTR,
    nMaxCount: c_int,
) callconv(.winapi) c_int;

pub extern "user32" fn SetWindowLongW(
    hWnd: HANDLE,
    nIndex: c_int,
    dwNewLong: isize,
) callconv(.winapi) isize;

pub extern "user32" fn CallWindowProcW(
    lpPrevWndFunc: ?*const anyopaque,
    hWnd: HANDLE,
    Msg: DWORD,
    wParam: usize,
    lParam: isize,
) callconv(.winapi) isize;

pub extern "user32" fn SendMessageW(
    hWnd: HANDLE,
    Msg: DWORD,
    wParam: usize,
    lParam: isize,
) callconv(.winapi) isize;
