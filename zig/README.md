# BloogBot Zig Workspace

This directory holds the Zig port of BloogBot, tracked in `Docs/MIGRATION_TO_ZIG.md`.
It is built independently of the existing C# `BloogBot.sln`; nothing in the
`.NET` projects depends on this folder.

## Layout

```
zig/
├── build.zig              # zig build entry point
├── build.zig.zon          # package manifest (Zig 0.14+)
├── README.md              # this file
├── NOTES.md               # lessons-learned log; read while iterating
├── src/
│   ├── common/
│   │   └── winapi.zig     # Win32 declarations shared across artifacts
│   └── launcher/
│       └── main.zig       # bloog-launcher (replaces Bootstrapper/)
```

Future siblings of `launcher/` will be `stub/` (in-process DLL injected
into `wow.exe`) and `host/` (out-of-process bot logic + web UI).

## Toolchain

- **Zig 0.14.0 or newer.** The build script uses the `b.path(...)` LazyPath
  API and the `.winapi` calling convention, both of which are 0.14+.
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
| `bloog-launcher` | Code complete; **not yet compile-verified** — see NOTES.md.   |
| `bloog-stub.dll` | Not started.                                                  |
| `bloog-host`     | Not started.                                                  |

`zig build` has not been run on the source tree yet, because the dev
sandbox where this scaffold was written had no internet access to
`ziglang.org`. First-build verification is the next step on a host that
has the toolchain installed.
