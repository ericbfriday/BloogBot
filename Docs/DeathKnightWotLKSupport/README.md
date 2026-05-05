# Death Knight WotLK Support Research

Date: 2026-05-04

This directory tracks the codebase findings, gameplay research, sources, and implementation information needed to add Death Knight support to BloogBot for WotLK clients.

## Artifacts

- `codebase-findings.md` - where Death Knight support plugs into the existing solution and what currently blocks it.
- `wotlk-death-knight-research.md` - variants, core abilities, rotations, and leveling strategy.
- `ffi-and-client-integration.md` - whether new native FFI is needed and which managed client wrappers should be added first.
- `sources.md` - external and local sources used.

## Short Answer

Basic Death Knight combat support should not require new native FFI first. The existing WotLK path already has Lua execution/results, spell casting by ID, spell cooldown checks, target/auras, targeting, movement, and DBC table access. What is missing is managed Death Knight resource support:

- Read runic power with Lua `UnitPower("player", 6)` or an equivalent wrapper.
- Read rune readiness and rune type with Lua `GetRuneCooldown`, `GetRuneCount`, and `GetRuneType`.
- Add DK-specific ability/resource gating instead of using the current mana/rage/energy helper paths.

New native FFI should only be added if the Lua wrappers prove unreliable in the injected WotLK 3.3.5 client or if resource polling is too slow/noisy. The code already has `ClientDb.SpellRuneCost`, so DBC-backed ability costs are another managed option, but the spell-to-rune-cost row mapping still needs implementation and verification.

## Recommended First Implementation Scope

Start with a `BloodDeathKnightBot` for open-world grinding/leveling. Blood is the safest first variant because the bot framework values low downtime, self-healing, and predictable melee behavior. Add Frost and Unholy after the DK resource wrapper and shared DK combat helper are proven.

If the goal is all variants immediately, keep the existing repo pattern and create one bot project per variant:

- `BloodDeathKnightBot`
- `FrostDeathKnightBot`
- `UnholyDeathKnightBot`

## Information Needed Before Coding

- Confirm initial variant scope: Blood only first, or Blood/Frost/Unholy projects in one pass.
- Confirm the supported WotLK executable target is still `3, 3, 5, 12340`.
- In a live WotLK Death Knight client, verify `LuaCallWithResults` can return numeric and boolean-like values from `UnitPower`, `GetRuneCooldown`, `GetRuneCount`, `GetRuneType`, and `IsUsableSpell`.
- Verify whether the current `IsSpellOnCooldown` delegate treats rune-gated spells as unavailable while runes are cooling down. If it does not, DK helpers must use rune state directly.
- Confirm exact WotLK aura names for proc-driven logic, especially Frost `Killing Machine` and `Rime`/`Freezing Fog`, before implementing Frost priorities.
- Decide whether runeforging should be a preflight warning only or an automated behavior requiring Runeforge proximity and item targeting.
- Decide whether the bot loader should hide DK bots on Vanilla/TBC or load them and fail fast when `ClientVersion != WotLK` or `player.Class != DeathKnight`.

## Key Risks

- The shared `CombatStateBase.TryCastSpell` checks mana cost and will reject or mis-handle many DK rune/runic-power abilities.
- `TryUseAbility` only knows Warrior rage and Rogue energy. It can cast DK abilities with no resource check, but that means wasted casts and poor rotation timing.
- `BotLoader` uses a hard-coded DLL list, so a new DK project will not appear unless the list is updated.
- WotLK Classic guide sources are a good gameplay baseline, but this repo targets the 3.3.5.12340 client. Live client verification is required before finalizing resource behavior.
