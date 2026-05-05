# Death Knight Research Instructions

This directory is the source of truth for WotLK Death Knight implementation prerequisites, unresolved questions, gameplay assumptions, and live-client observations.

## Research Rules

Track a source for every new gameplay, spell, rotation, leveling, client API, DBC, or FFI claim. Add new sources to `sources.md` and update the relevant research note instead of leaving facts only in conversation history.

Distinguish clearly between:

- WotLK Classic guide assumptions.
- Original WotLK 3.3.5.12340 live-client verification.
- Inferences from existing BloogBot code.

When live-client probes are run, record:

- Full client version.
- Character class, level, presence, and relevant talents if known.
- Current runic power.
- Rune slot state, rune type, rune count, and cooldown values.
- Lua return shape for `UnitPower`, `GetRuneCooldown`, `GetRuneCount`, `GetRuneType`, and `IsUsableSpell`.
- Any mismatch between `IsSpellOnCooldown`, rune cooldowns, and actual cast usability.

## Implementation Guidance

Keep the default implementation guidance aligned with the root instructions: WotLK-only, `BloodDeathKnightBot` first, Frost and Unholy after shared DK rune/runic-power helpers are proven.

If new implementation work changes the recommended approach, update `README.md`, `codebase-findings.md`, `wotlk-death-knight-research.md`, and `ffi-and-client-integration.md` as needed so future agents can start from current facts.
