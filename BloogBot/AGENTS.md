# BloogBot Core Instructions

These instructions apply to core engine, game-object, client integration, and shared combat-state work under `BloogBot\`.

## WotLK Death Knight Integration

For Death Knight resources, prefer managed Lua-backed wrappers before adding native FFI:

- Runic power: `UnitPower("player", 6)`
- Rune cooldown state: `GetRuneCooldown(slot)`
- Rune count: `GetRuneCount(slot)`
- Rune type: `GetRuneType(slot)`
- Usability/proc checks: `IsUsableSpell("Spell Name")`

Do not add new native WotLK FFI methods for DK resources unless live verification against WotLK 3.3.5.12340 proves that the Lua path is unavailable, protected, unreliable, or too noisy for stable combat decisions.

Do not force DK abilities through the existing mana/rage/energy helper paths. Add DK-specific resource and ability gating for rune costs, Death Runes, runic power, and proc-gated spells such as `Rune Strike`.

Every DK core path must guard against unsupported clients and classes:

- `ClientVersion.WotLK`
- `Class.DeathKnight`

`Death and Decay` is a special case because it is ground-targeted and `WotLKGameFunctionHandler.CastAtPosition` is not implemented. Defer it for the first Blood DK implementation unless the task explicitly requires it and the casting path is verified in a live client.

Keep first-pass ability costs simple and explicit for the spells the bot can actually cast. Consider `SpellRuneCost.dbc` only after the Lua-backed resource model and `BloodDeathKnightBot` behavior are proven.
