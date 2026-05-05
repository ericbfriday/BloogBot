# Zig Port — Lessons Learned

A running log of decisions, divergences, and gotchas surfaced while
porting BloogBot off .NET. Append-only; the most recent entries are at
the top.

---

## 2026-05-05 — Scaffolding the workspace and bloog-launcher

### Sandbox / toolchain reality

- **Zig is not installed in the dev sandbox** where this scaffold was
  written, and the hosts that distribute it (`ziglang.org`) are not on
  the allowlist. The official GitHub releases only ship the bootstrap
  source tarball (`zig-bootstrap-X.Y.Z.tar.xz`, ~44 MB), not pre-built
  binaries — those live on `ziglang.org`.
- **First-build verification is therefore deferred.** The launcher is
  written carefully and reviewed against the C# original, but it has
  not been through `zig build`. The first task on a Windows-capable
  (or even Linux-with-zig) machine should be a smoke-build:

  ```sh
  cd zig
  zig build
  ls -l zig-out/bin/bloog-launcher.exe
  ```

  Any compile errors are most likely to be in `winapi.zig` cast shapes
  (`@ptrCast` on `?*const anyopaque` ↔ `[*:0]const u16`) or the
  `LPTHREAD_START_ROUTINE` cast in `main.zig` step 5.

### Why this scaffold targets Zig 0.14+

- The `.winapi` calling convention is 0.14+. On 0.13 we'd have to write
  `.Stdcall` (x86) or `.C` (x86_64) at every extern site, which
  defeats the point of the unified surface in `winapi.zig`.
- `b.path(...)` returning a `LazyPath` is also 0.14+. The 0.13 form
  (`.{ .path = "..." }` literal) is gone.
- A reasonable rule of thumb going forward: pin to one stable Zig
  release per phase of the port, bump deliberately, and update the
  `minimum_zig_version` field in `build.zig.zon`. Don't track master.

### Why x86-windows-**gnu** by default, not -msvc

- MinGW ABI compiles cleanly with just the Zig toolchain — no MSVC
  install, no Windows SDK, no headers to vendor. That keeps the
  developer onboarding story to "install Zig, run `zig build`," which
  is exactly the value prop we cited for the port in the migration doc.
- The `-windows-msvc` triple wants the C runtime headers and import
  libs from a Visual Studio install. We'd inherit a chunk of the .NET
  toolchain pain we're trying to escape.
- WoW is 32-bit, so we go x86 not x86_64. Cross-bitness DLL injection
  doesn't work — a 64-bit launcher cannot inject into a 32-bit
  process via CreateRemoteThread + LoadLibraryW.

### Self-contained Win32 surface (`src/common/winapi.zig`)

We declare every Win32 type and function the project uses directly,
instead of `@import("std").os.windows`. Three reasons:

1. **Stability across Zig releases.** `std.os.windows` changes between
   versions. Pinning to it means the build matrix tracks the compiler
   matrix.
2. **Auditability.** When you're injecting code into another process,
   it's good for every kernel32 / user32 call to be visible in one
   file. Future code review can assert "we only call N Win32
   functions, here they are."
3. **Stub minimalism.** The stub DLL (next milestone) will be loaded
   inside `wow.exe`. We want a small, controlled set of imports —
   stdlib `start.zig` machinery brings in things like CRT init that
   we'd rather avoid in an injected DLL.

### Bugs / smells in the existing C# Bootstrapper, fixed on port

While porting `Bootstrapper/Program.cs` line-by-line into Zig, I
caught several issues I deliberately did **not** preserve. Each is
documented in a comment in `src/launcher/main.zig` next to the
relevant step. Tracked here for the lessons log:

| # | Issue (in C# Bootstrapper)                                                                                       | Why it works anyway                                                          | What the Zig port does                            |
|---|------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------|---------------------------------------------------|
| 1 | `VirtualAllocEx(... size = loaderPath.Length ...)` then writes `Encoding.Unicode.GetBytes(...)` — 2× the bytes.  | Kernel rounds VirtualAllocEx up to one 4 KiB page.                           | Allocates `(chars + 1) * 2` bytes intentionally.  |
| 2 | DLL path is written **without a NUL terminator**, but `LoadLibraryW` requires NUL-termination.                   | Freshly committed pages are zero-filled; first byte past the path is 0.      | `utf8ToUtf16LeAllocZ` produces a NUL terminator and we write it.  |
| 3 | `VirtualFreeEx` is called immediately after `CreateRemoteThread`, with only a `Thread.Sleep(1000)` between.       | LoadLibraryW usually finishes within 1 s; no incident reported.              | `WaitForSingleObject(thread, INFINITE)` first, then free.         |
| 4 | `CreateProcess` runs `wow.exe` normally (no `CREATE_SUSPENDED`); injection races initialization.                  | `Thread.Sleep(1000)` after CreateProcess hides most of the window.           | `CREATE_SUSPENDED` until LoadLibraryW returns, then `ResumeThread`. |
| 5 | No `CloseHandle` on `hProcess`, `hThread`, or the remote thread handle. Handle leak.                              | Process exits soon after; OS reclaims.                                        | `defer _ = CloseHandle(...)` at every acquisition.                |
| 6 | Path memory is allocated **`PAGE_EXECUTE_READWRITE`**.                                                            | Excess permissions don't break anything.                                      | `PAGE_READWRITE`. The path is data, not code.                     |
| 7 | Three separate `Thread.Sleep` "to prevent timing issues."                                                          | Empirically worked.                                                           | Replaced with `WaitForSingleObject` and `CREATE_SUSPENDED`.        |
| 8 | `GetLastError` is read into `Marshal.GetLastWin32Error()` *after* the next P/Invoke, which can clobber the error. | Most calls succeed; failures escape via thrown exceptions.                    | We log `GetLastError()` immediately after each failed call.        |

Caveat: I haven't run the new launcher yet. It's possible some of the
"bugs" above are load-bearing in some weird way (e.g., maybe wow.exe
doesn't like being CREATE_SUSPENDED for unusual reasons on certain
private-server clients). We'll find out at smoke-test time.

### Trade-off: arg-driven vs. config-file-driven

The C# Bootstrapper reads `bootstrapperSettings.json` for the WoW
path. The Zig port currently takes the WoW path and stub path as
positional CLI arguments instead.

Reasons:
- The launcher is now one of three artifacts (launcher, stub, host),
  and only the host should own bot configuration.
- Argv-driven launchers compose well with the `bloog-host` web UI:
  the host can spawn the launcher with whatever paths it has on hand.
- Less to break: no JSON parser in the launcher binary, no schema
  evolution to coordinate.

If we later want config-file support, it should be the host that reads
the config and forwards specific paths to the launcher.

### Open questions / future work

- **Attach mode.** Should the launcher support attaching to an
  already-running `wow.exe` (via `OpenProcess` instead of
  `CreateProcess`)? Useful for workflows where the player wants to
  hand-control the login screen and only enable the bot once they're
  in-game. Decision: defer until we have working memory reads —
  attach mode shouldn't change the injection mechanics.
- **Stub-DLL integrity check.** Pre-injection hash of the stub DLL
  against an expected SHA-256, refusing to inject if it doesn't
  match. Cheap to add; protects against accidentally injecting a
  wrong-version stub. Defer to phase 2.
- **`zig fmt` / CI.** No CI runs `zig build` or `zig fmt --check`
  yet. Adding a GitHub Actions workflow that runs both on pushes to
  branches matching `claude/plan-dotnet-migration-*` would catch
  regressions like the ones I'm worried about above.
- **`--verbose` flag.** Right now every failure prints with `GLE=`
  but successful runs print one line. A `--verbose` switch dumping
  pinfo and the resolved addresses would help debug live.

### Things I learned about Zig 0.14 build system

- `b.standardTargetOptions(.{ .default_target = ... })` is the right
  way to set a default cross-compile target while still letting
  `-Dtarget=...` override.
- `Module.subsystem` is set on the executable (`launcher.subsystem`,
  not on the module) and only applies to PE outputs; setting it on a
  Linux build is silently ignored.
- `b.installArtifact(...)` puts the result in `zig-out/bin/`. There's
  no need to set `install_subdir` for an EXE.
- Run-step plumbing (`b.addRunArtifact` + `if (b.args)
  run.addArgs(args)`) is unchanged from 0.13.

---

(Older entries below as the port progresses.)
