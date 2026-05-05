# Migrating BloogBot off .NET — Strategy & MVP Plan

This document is an architectural review of BloogBot in its current form
and a concrete plan to port it off the .NET Framework. The recommended
target is **Zig**, with a hybrid in-process / out-of-process split.

It is intentionally written as a strategy doc, not an instruction set:
there is enough detail here to start the work, but the actual port will
be done incrementally on follow-up branches.

---

## 1. What BloogBot is today

BloogBot is a Vanilla / TBC / WotLK World of Warcraft bot whose entire
logic runs **inside `wow.exe`** as managed C# code. The flow is:

1. `Bootstrapper.exe` (`Bootstrapper/Program.cs:24-94`) launches
   `wow.exe`, then `VirtualAllocEx` + `WriteProcessMemory` +
   `CreateRemoteThread(LoadLibraryW, ...)` to inject `Loader.dll`.
2. `Loader.dll` (`Loader/dllmain.cpp:80-156`) hosts the .NET 4 CLR
   inside the WoW process via `CLRCreateInstance` →
   `ICLRRuntimeHost::Start` →
   `ExecuteInDefaultAppDomain("BloogBot.exe", "BloogBot.Loader", "Load")`.
3. `BloogBot.exe` (managed C#) runs the WPF UI, the bot loop, MEF-loads
   ~17 class-bot DLLs (`BotLoader.cs:35-69`), reads/writes WoW memory
   through raw `unsafe` C# pointers (`MemoryManager.cs:11-243`),
   assembles inline x86 hooks with `Fasm.NET.dll`
   (`MemoryManager.cs:295-336`), pumps work onto WoW's main thread by
   hijacking its window proc with `SetWindowLong(GWL_WNDPROC)` +
   `WM_USER` (`ThreadSynchronizer.cs:11-122`), and disables Warden by
   detouring its scan functions (`WardenDisabler.cs:170-530`).
4. Two more native DLLs are bridged in via P/Invoke: `FastCall.dll`
   (a `stdcall→fastcall` calling-convention shim with 9 exports,
   `FastCall/dllmain.cpp:20-89`) and `Navigation.dll` (Recast/Detour
   navmesh pathfinder over MaNGOS movemaps,
   `Navigation/DllMain.cpp:4-15`).

Code-size landmarks (approximate):

| Area                                  | LOC      |
|---------------------------------------|----------|
| Core C# (`BloogBot/`)                 | ~12,000  |
| 17 class bot plugins                  | ~11,200  |
| Shared states (`AI/SharedStates/`)    | ~2,300   |
| Native (`Loader/`, `FastCall/`, `Navigation/`) | ~3,000 (mostly Recast/Detour) |

---

## 2. Why port

The project is locked to:

- **Windows + .NET Framework 4** (CLR hosted in-process).
- **32-bit WoW client** (1.12.1 / 2.4.3 / 3.3.5 only).
- **Visual Studio 2022** and a heavy SDK chain (C++ Build Tools,
  WPF, MSBuild).
- **Microsoft-only libraries**: WPF, MEF (System.ComponentModel.Composition),
  System.Data.SQLite, Discord.Net, Fasm.NET, Newtonsoft.Json.

A port to Zig buys:

- A single small toolchain (`zig build`, no MSBuild, no NuGet).
- No GC. Deterministic memory access for in-process work; no CLR pauses
  or marshaling cost on the hot path of object enumeration.
- First-class `extern "C"` story for the existing Recast/Detour
  pathfinder — no managed wrapper needed.
- Easy cross-compilation, smaller binaries, comptime for WoW offset
  tables.

---

## 3. Why a *hybrid* architecture (and not pure in-process or pure out-of-process)

BloogBot's existing capabilities split cleanly into two tiers:

**Tier A — must run inside `wow.exe`.** Anything that requires the
`__fastcall` ABI of WoW's internal functions, anything that touches
WoW's main-thread WndProc, anything that detours Warden's scan
routines, and anything that wants to receive the `EnumerateVisibleObjects`
callback. These cannot be done cross-process without huge fidelity loss.

**Tier B — does not need to run inside `wow.exe`.** The state-machine bot
loop, the class-specific combat rotations, the SQLite-backed hotspots /
travel paths / NPC tables, the UI, Discord notifications, telemetry.
These are pure logic that today happens to share an address space with
WoW only because that's where the CLR was bootstrapped.

A hybrid split makes Tier A a tiny native DLL (Zig) and moves Tier B
into a separate Zig executable that talks to the stub over IPC. This:

- Removes the need for any CLR-equivalent inside `wow.exe`.
- Lets the bot logic crash, restart, hot-reload, or be replaced
  without touching the game.
- Lets the IPC surface evolve as the contract — the same surface
  could later be reimplemented in another language (Nim, Elixir+NIF,
  etc.) without touching the stub.
- Keeps the few things that genuinely require in-process access
  (Warden bypass, fastcall shims, WoW main-thread sync) in one
  small, well-tested binary.

---

## 4. Target architecture

```
+------------------------------------+        +-----------------------------+
| wow.exe (32-bit WotLK 3.3.5a)      |        | bloog-host (Zig exe)        |
|                                    |        |                             |
|  +------------------------------+  |  IPC   |  +-----------------------+  |
|  | bloog-stub.dll (Zig)         |<-+--------+->| ipc client            |  |
|  |  - DllMain bootstraps named  |  | named  |  | bot loop (state stack)|  |
|  |    pipe server               |  | pipe   |  | ObjectManager mirror  |  |
|  |  - ReadByte/Int/Uint/Float   |  | + ring |  | FrostMage rotation    |  |
|  |  - WriteBytes/PatchBytes     |  | buffer |  | Navigation FFI        |  |
|  |  - InjectAsm (keystone-zig)  |  |        |  | SQLite (zig-sqlite)   |  |
|  |  - FastCall shims            |  |        |  | HTTP/WS server        |  |
|  |  - WndProc hook (run-on-main)|  |        |  +-----------+-----------+  |
|  |  - Object enumeration cb     |  |        |              |              |
|  +------------------------------+  |        |   browser <--+ http://...   |
+------------------------------------+        +-----------------------------+
                ^
                |  injected by
        +-------+--------+
        | bloog-launcher |  (Zig exe; replaces Bootstrapper)
        | CreateProcess  |
        | + CreateRemote |
        |   Thread(LoadLibrary, "bloog-stub.dll")
        +----------------+
```

Three Zig artifacts replace the seven existing projects we touch in the
MVP:

| New artifact            | Replaces                                                          |
|-------------------------|-------------------------------------------------------------------|
| `bloog-launcher.exe`    | `Bootstrapper/Program.cs` + `Bootstrapper/WinImports.cs`          |
| `bloog-stub.dll`        | `Loader/dllmain.cpp` + `FastCall/dllmain.cpp` + the unsafe core of `BloogBot/MemoryManager.cs`, `Detour.cs`, `ThreadSynchronizer.cs` |
| `bloog-host.exe`        | `BloogBot.exe` (the managed bot) + WPF UI + selected class bot    |
| `Navigation.dll` (kept) | `Navigation/*` — left as-is, called via Zig `extern "C"` (it's pure C++/Recast/Detour, not .NET dependent) |

`Fasm.NET` is replaced by **keystone-zig** bindings, or — preferably —
hand-rolled byte templates for the ~5 distinct shapes the codebase
actually emits (`MemoryManager.cs:295-336`).

---

## 5. MVP scope

A vertical slice that proves the architecture end-to-end:

- **One client:** WotLK 3.3.5a (the best-tested in the README).
- **One class bot:** FrostMageBot (~9 files, ranged caster, no pet).
- **One UI:** localhost web UI (HTTP + WebSocket) — no WPF replacement.
- **One database:** SQLite with the subset of tables FrostMage grinding
  needs (`Hotspots`, `Npcs`, `TravelPaths`, `BlacklistedMobs`).

Explicitly **out of scope** for the MVP, but documented so the path is
visible:

- **`WardenDisabler`** (`BloogBot/WardenDisabler.cs`, 531 lines). Most
  WotLK private servers run with Warden inactive or muted server-side;
  Kronos / Warmane MVP testing should not require it. Phase 2 work.
- **Discord** (`DiscordClientWrapper.cs`). Replace later with HTTP
  webhook POSTs (~30 LOC of Zig).
- **Other 16 class bots.** The FrostMage port establishes the pattern;
  every additional class is mostly mechanical re-coding of its 4–5 state
  files.
- **Vanilla / TBC clients.** The `MemoryAddresses` table layout is
  identical across versions; adding them is a data change plus testing
  the `EnumerateVisibleObjects` calling-convention difference (vanilla
  is `thiscall`, TBC/WotLK are `cdecl` — see `ObjectManager.cs:16-19`).
- **Hotspot / TravelPath / NPC editor UIs.** CLI tooling first; web
  editor later.

---

## 6. Component-by-component plan

### 6.1 `bloog-launcher` (Zig, ~150 LOC)

Direct one-to-one port of `Bootstrapper/Program.cs:24-94`:

- `kernel32.CreateProcessW` → suspended start of `wow.exe`.
- `VirtualAllocEx` + `WriteProcessMemory` to write the stub DLL path.
- `CreateRemoteThread(GetProcAddress(kernel32, "LoadLibraryW"), pathPtr)`.
- `WaitForSingleObject` on the remote thread, then `VirtualFreeEx` and
  resume the main thread.

All Win32 functions are reachable from `std.os.windows` plus a few
`extern` declarations.

### 6.2 `bloog-stub.dll` (Zig, ~1500 LOC target)

A thin native shim. Loaded into `wow.exe`. Exposes a **named-pipe RPC**
on `\\.\pipe\bloogbot` (stdlib-only Zig); message framing is
length-prefixed `[u8 op, u32 len, payload]`. No CLR hosting, no .NET,
no managed code at all.

Responsibilities (mapping from existing files):

| Stub responsibility | Existing implementation we mirror |
|---|---|
| `read_u8/u16/u32/u64/f32/string` at address | `MemoryManager.cs:72-243` |
| `write_bytes(addr, bytes)` with `VirtualProtect` flip | `MemoryManager.cs:262-293` |
| `inject_asm(addr, asm_bytes)` | `MemoryManager.cs:295-336` (replace Fasm.NET with keystone-zig or precompiled templates) |
| `register_detour(target_fn, callback_id)` | `Detour.cs` (managed delegate → function pointer becomes stable C callback IDs the host requests by name) |
| `run_on_main_thread(callback_id, args)` | `ThreadSynchronizer.cs` — same `EnumWindows` + `SetWindowLong(GWL_WNDPROC)` + `WM_USER` mechanism, in Zig |
| `enumerate_visible_objects()` | `FastCall/dllmain.cpp:25-30` + `BloogBot/Game/ObjectManager.cs:54-265` (the *enumeration callback* runs in the stub; results are pushed across the pipe as a flat list of `{guid, type, descriptor_ptr}`) |
| `lua_call`, `loot_slot`, `get_text`, `intersect`, `intersect2`, `sell_item_by_guid`, `buy_vendor_item`, `get_object_ptr` | the 9 exports of `FastCall/dllmain.cpp:20-89`, preserved verbatim as Zig `__stdcall`-wrapping-`__fastcall` shims |

The stub does **no game logic at all**. It only knows about bytes,
addresses, calling-convention bridges, and the WoW window. Every
game-specific decision (which mob to attack, what spell to cast, where
to walk) lives in `bloog-host`, where iteration is fast.

### 6.3 `Navigation.dll` (kept as-is)

The Recast/Detour pathfinder is already pure C++ with two `__cdecl`
exports (`Navigation/DllMain.cpp:4-15`):

- `XYZ* CalculatePath(unsigned int mapId, XYZ start, XYZ end, bool smoothPath, int* length)`
- `void FreePathArr(XYZ* pathArr)`

Zig calls these directly via `extern "C"`. **No changes needed.** The
`mmaps/` directory is read by `Navigation.dll` itself. Eventually port
to Zig + recastnavigation Zig bindings, but not in the MVP.

### 6.4 `bloog-host` (Zig, ~3000 LOC for MVP)

Single Zig executable. Subsystems:

- **`stub_client.zig`** — typed wrapper around the named-pipe RPC. One
  method per stub op. Connection is reestablished on stub crash.
- **`memory_addresses.zig`** — port of
  `BloogBot/Game/MemoryAddresses.cs` (WotLK lines 9–101 only). Just a
  `pub const` block of constants. Comptime asserts non-zero.
- **`object_manager.zig`** — port of
  `BloogBot/Game/ObjectManager.cs:216-265`. Polls
  `enumerate_visible_objects` via the stub, builds typed
  `WoWUnit`/`WoWPlayer`/`WoWItem` structs by reading descriptor offsets
  through `stub_client.read_*`. WotLK uses cdecl callback signature
  (`ObjectManager.cs:19`); we follow that.
- **`game_objects.zig`** — port of relevant `BloogBot/Game/Objects/*.cs`
  for WotLK only: `WoWObject`, `WoWUnit`, `WoWPlayer`, `LocalPlayer`,
  `WoWItem`. Tagged unions instead of inheritance.
- **`bot.zig`** — port of `BloogBot/AI/Bot.cs:236-588`. The state-stack
  pattern translates cleanly:
  ```zig
  var states: std.BoundedArray(BotState, 16) = .{};
  while (running) {
      stub.run_on_main_thread(.{ .tick = states.peek() });
      std.time.sleep(50 * std.time.ns_per_ms);
  }
  ```
  `IBotState` (`BloogBot/AI/IBotState.cs`) is one method, `Update()`.
  In Zig we use a `BotState` tagged union with a `update(self, ctx)`
  method per variant.
- **`states/`** — port of just the shared states FrostMage actually
  uses (`BloogBot/AI/SharedStates/`): `GrindState`,
  `MoveToTargetStateBase`, `CombatStateBase`, `LootState`, `RestState`.
  Skip `EquipArmor`, `SellItems`, `BuyItems`, `Travel`, `Login`,
  `Powerlevel`, `Gather` until post-MVP.
- **`bots/frost_mage.zig`** — port of the four FrostMageBot files
  (`FrostMageBot/{CombatState,RestState,MoveToTargetState,BuffSelfState}.cs`).
  The MEF `[Export(typeof(IBot))]` mechanism (`BotLoader.cs:35-69`)
  collapses into a static registry; for MVP we hard-link FrostMage.
- **`db.zig`** — `zig-sqlite` (`vrischmann/zig-sqlite`) replaces
  `BloogBot/SqliteRepository.cs`. Schema mirrors
  `BloogBot/SqliteSchema.SQL` but only the tables FrostMage grinding
  needs.
- **`web.zig`** — `httpz` (`karlseguin/http.zig`) for HTTP + WebSocket
  on `127.0.0.1:7474`. Endpoints: `GET /` (static SPA),
  `GET /api/state`, `POST /api/start`, `POST /api/stop`,
  `WS /api/telemetry` (push of `Probe.cs`-equivalent data: position,
  target, HP, current state name, recent log lines).
- **`web/index.html` + `web/app.js`** — static, embedded into the binary
  via `@embedFile`. ~200 lines of vanilla JS subscribing to the WS
  feed. Replaces `BloogBot/UI/MainWindow.xaml` + `MainViewModel.cs`.

---

## 7. Critical files to read before each piece is implemented

| When implementing... | Read these first |
|---|---|
| `bloog-launcher` | `Bootstrapper/Program.cs`, `Bootstrapper/WinImports.cs` |
| Stub memory ops | `BloogBot/MemoryManager.cs:36-336` |
| Stub thread sync | `BloogBot/ThreadSynchronizer.cs:1-123` |
| Stub fastcall shims | `FastCall/dllmain.cpp:20-89`, `BloogBot/Game/VanillaGameFunctionHandler.cs`, `BloogBot/Game/WotLKGameFunctionHandler.cs` |
| Stub object enumeration | `BloogBot/Game/ObjectManager.cs:1-360` |
| WotLK offsets | `BloogBot/Game/MemoryAddresses.cs:9-101` |
| `WoWObject`/`WoWUnit`/`LocalPlayer` | `BloogBot/Game/Objects/WoWObject.cs:78-150`, `BloogBot/Game/Objects/WoWUnit.cs`, `BloogBot/Game/Objects/LocalPlayer.cs` |
| Bot loop and state stack | `BloogBot/AI/Bot.cs:15-616`, `BloogBot/AI/IBotState.cs`, `BloogBot/AI/IDependencyContainer.cs` |
| Shared states needed for grind | `BloogBot/AI/SharedStates/{GrindState,MoveToTargetStateBase,CombatStateBase,LootState,RestState}.cs` |
| FrostMageBot port | `FrostMageBot/{FrostMageBot,CombatState,MoveToTargetState,RestState,BuffSelfState}.cs` |
| Plugin loading we are replacing | `BloogBot/BotLoader.cs:35-69` |
| Navigation FFI surface | `Navigation/DllMain.cpp`, `BloogBot/Navigation.cs:17-42` |

---

## 8. Reused external dependencies

| Reuse | Source | Why |
|---|---|---|
| `Navigation.dll` binary | existing build of `Navigation/` | Pure C++, no .NET; massive savings |
| `mmaps/` directory | existing MaNGOS movemap dump | Data-only, format-stable |
| `botSettings.json` shape | `BloogBot/botSettings.json` | Same keys, parsed by `std.json` |
| `SqliteSchema.SQL` (subset) | `BloogBot/SqliteSchema.SQL` | Same schema, same data |

---

## 9. Verification / acceptance for the MVP

End-to-end test plan, in order:

1. **Stub-only smoke test (no game logic).**
   - `zig build` produces `bloog-launcher.exe`, `bloog-stub.dll`,
     `bloog-host.exe`.
   - Run `bloog-launcher.exe` against WotLK 3.3.5a `wow.exe`.
   - From `bloog-host`, call `stub_client.read_u32(0x00C79CD8)` (WotLK
     `LocalPlayerGuid`, `MemoryAddresses.cs:11`) at the character-select
     screen — expect 0; after entering world — expect a non-zero GUID.
   - Verify `\\.\pipe\bloogbot` connects, disconnects, and reconnects
     cleanly when `bloog-host` is restarted.
2. **Object manager parity.**
   - Log into the test character; stand near a vendor.
   - `bloog-host` should print a list of visible units/players with
     names matching what's on screen, and the local player's HP / mana
     / class reading exactly. Validate against `ObjectManager.cs`
     output for the same character on the original C# build.
3. **Navigation FFI.**
   - Call `CalculatePath(mapId=0, start=<known Elwynn coord>, end=<known
     Goldshire coord>)`. Compare against the C# test
     `NavigationTests/NavigationTests.cs::CalculatePathTest()` — same
     inputs, expect path within a small epsilon.
4. **`run_on_main_thread`.**
   - Issue a `LuaCall("/say hello from zig")` via the stub from a host
     worker thread. Confirm the chat line appears in the WoW client
     (proves the WndProc hook delivers the call onto WoW's main thread
     without crashing).
5. **FrostMage MVP.**
   - Place character at a hotspot, open `http://127.0.0.1:7474/`,
     click Start. Bot should: pick a target → walk to target → cast
     Frostbolt rotation → loot corpse → drink to recover mana → pick
     next target. Run for 30 minutes without crashing.
   - Compare behavior against original C# FrostMageBot at the same
     hotspot with the same character. Behavior should be qualitatively
     the same; differences in spell timing or path smoothing are
     acceptable.
6. **Restart resilience.**
   - Kill `bloog-host` mid-grind. WoW + stub stay alive. Relaunch host
     — it should reconnect, re-read state, resume.
7. **No regression in WoW client.**
   - Watch for stalls or freezes during the 30-minute run; the
     original ThreadSynchronizer floods `WM_USER` and any latency in
     the Zig WndProc hook will be visible as input lag.

A successful MVP is defined as steps 1–6 passing on a Warmane WotLK
3.3.5a test account.

---

## 10. Risks & open questions (post-MVP follow-up)

- **Keystone-zig vs. precompiled stubs.** If `keystone-zig` is too
  heavy a dependency, the `MemoryManager.InjectAssembly` call sites
  only emit ~5 distinct patterns (push-pad / call / pop-pad / jmp).
  Hand-coded byte templates are feasible.
- **Pipe vs. shared memory.** A named pipe is fine for the MVP (kHz
  of small messages tops). If telemetry or object-list traffic
  dominates, swap to a ring buffer in shared memory. The
  `stub_client.zig` API hides the transport.
- **Object enumeration callback runs in the `wow.exe` thread.** The
  Zig callback we register with `EnumerateVisibleObjects` must be
  `extern fn` with `__cdecl`/`__thiscall` per
  `MemoryAddresses.EnumerateVisibleObjectsFunPtr` semantics; results
  are buffered on the stub side and shipped to the host in batches.
- **Stub crash safety.** A bug in the stub crashes `wow.exe`. Keep
  stub code minimal, fuzz the RPC parser, prefer `try` / `errdefer`
  over panics.
- **Why not Nim?** Nim was a serious candidate. It compiles via C with
  good FFI and is more productive than Zig. The deciding factor was
  GC: Nim's `--gc:arc`/`--gc:orc` modes make in-process injection
  feasible but require ongoing care, while Zig has nothing to manage.
  For a project that ships into someone else's process, that
  simplicity wins.
- **Why not Elixir?** Elixir / OTP would be excellent for the bot
  loop, supervisor tree, and hot reload, but BEAM cannot be loaded
  inside `wow.exe`. Elixir is only viable for the host side, and even
  then introduces a heavyweight runtime and an extra language for
  something this small. If a future contributor wants to retarget the
  host, the IPC contract is the boundary that makes it possible — the
  stub never needs to know.
