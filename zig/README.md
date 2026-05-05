# BloogBot Zig Workspace

This directory holds the Zig port of BloogBot, tracked in `Docs/MIGRATION_TO_ZIG.md`.
It is built independently of the existing C# `BloogBot.sln`; nothing in the
`.NET` projects depends on this folder.

## Layout

```
zig/
├── build.zig              # zig build entry point
├── build.zig.zon          # package manifest (Zig 0.16+)
├── README.md              # this file
├── NOTES.md               # lessons-learned log; read while iterating
├── src/
│   ├── common/
│   │   ├── winapi.zig     # Win32 declarations shared across artifacts
│   │   ├── log.zig        # Structured logging (stderr + OutputDebugString)
│   │   └── rpc_types.zig  # Shared RPC opcodes and message framing
│   ├── launcher/
│   │   └── main.zig       # bloog-launcher (replaces Bootstrapper/)
│   ├── stub/
│   │   ├── main.zig       # DllMain + background thread
│   │   ├── pipe_server.zig # Named pipe RPC server
│   │   ├── rpc.zig        # RPC type re-exports
│   │   └── memory.zig     # In-process memory read/write
│   └── test-target/
│       └── main.zig       # Minimal test exe for injection testing
```

`host/` (out-of-process bot logic + web UI) will be added in Phase 4+.

## Toolchain

- **Zig 0.16.0 or newer.** The build script uses the `b.path(...)` LazyPath
  API, the `.winapi` calling convention, and the `std.process.Init` main
  signature introduced in 0.16.
- **No external dependencies.** Everything is stdlib + a tiny hand-rolled
  Win32 surface in `src/common/winapi.zig`.

## Building

The default target is **32-bit Windows / MinGW ABI** (`x86-windows-gnu`),
because every WoW client BloogBot supports (1.12.1, 2.4.3, 3.3.5) is 32-bit
and we want a no-flags developer experience on Linux and macOS hosts.

```sh
# Cross-compile from any host:
zig build

# Debug build with verbose stderr:
zig build -Doptimize=Debug

# Sanity-build for the host (will fail on launcher because of comptime
# Windows guard — that is intentional):
zig build -Dtarget=native
```

Artifacts land in `zig/zig-out/bin/`.

## Running on Windows

The launcher is the only artifact in this MVP. Once you copy
`bloog-launcher.exe` and a built `bloog-stub.dll` (not yet implemented) to
the same folder on a Windows machine:

```cmd
bloog-launcher.exe "C:\WoW\WotLK\Wow.exe" "C:\BloogBot\bloog-stub.dll"
```

The launcher prints a single confirmation line on success and a `GLE=...`
diagnostic plus non-zero exit code on any Win32 failure.

## Status

| Artifact         | Status                                                       |
|------------------|--------------------------------------------------------------|
| `bloog-launcher` | **Compiles, runs, and injects** on Zig 0.16.0 (`x86-windows-gnu`). Verified against test-target.exe with `--verbose`. |
| `bloog-stub.dll` | **Compiles and injects**. DllMain + pipe server + memory read/write RPC. Pipe server listening on `\\.\pipe\bloogbot`. |
| `bloog-host`     | Not started.                                                  |
