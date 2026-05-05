# WotLK Death Knight Research

## Core Mechanics

Death Knights use health, six runes, and runic power. In WotLK there are two Blood runes, two Frost runes, and two Unholy runes. Death Runes can substitute for the normal typed runes. Many strikes consume runes and generate runic power; runic power is then spent on abilities such as Death Coil, Frost Strike, Rune Strike, Icebound Fortitude, and Anti-Magic Shell.

Death Knights also use three presences:

- `Blood Presence` - damage and self-healing, useful for leveling/grinding.
- `Frost Presence` - tanking/threat/mitigation, useful for hard pulls or tank variants.
- `Unholy Presence` - movement/attack speed/GCD benefits, useful in DPS and some opener contexts.

The core diseases are:

- `Frost Fever`, applied by `Icy Touch`.
- `Blood Plague`, applied by `Plague Strike`.

Most rotations start by applying both diseases, then spend runes according to variant.

## Variants

### Blood

Role: safest leveling/grinding and tank-oriented support.

Why it fits BloogBot first:

- Strong self-sustain through `Death Strike`, `Rune Tap`, `Vampiric Blood`, and `Death Pact`.
- Predictable melee flow.
- Less dependent on proc detection than Frost.
- Better downtime profile for unattended grinding.

Core combat abilities:

- Pull/control: `Death Grip`, `Dark Command`, `Chains of Ice`
- Diseases: `Icy Touch`, `Plague Strike`, `Pestilence`
- Rune spenders: `Heart Strike`, `Blood Strike`, `Death Strike`, `Blood Boil`, `Death and Decay`
- Runic power: `Rune Strike`, `Death Coil`
- Survival: `Rune Tap`, `Vampiric Blood`, `Icebound Fortitude`, `Anti-Magic Shell`, `Death Pact`
- Buffs/presence: `Horn of Winter`, `Blood Presence`, optionally `Frost Presence`

Leveling priority:

1. Keep `Horn of Winter` up.
2. Use `Blood Presence` for normal grinding.
3. Pull with `Death Grip` for ranged/caster or distant targets; otherwise use `Icy Touch`.
4. Apply `Frost Fever` and `Blood Plague` with `Icy Touch` and `Plague Strike`.
5. Use `Pestilence` if fighting more than one target.
6. Use `Heart Strike` or `Blood Strike` against one or two targets; use `Blood Boil` for more.
7. Use `Death Strike` as the main Frost/Unholy rune spender, especially when health is below a threshold.
8. Use `Death and Decay` when fighting three or more targets likely to live long enough.
9. Dump runic power with `Rune Strike` when reactive/procced, otherwise `Death Coil`.
10. Use `Mind Freeze` on casters when available.

### Frost

Role: burst DPS, dual-wield or two-handed melee, proc-responsive rotation.

Core combat abilities:

- Diseases: `Icy Touch`, `Plague Strike`, `Pestilence`
- Rune spenders: `Obliterate`, `Blood Strike`, `Blood Boil`, `Howling Blast`
- Runic power: `Frost Strike`
- Cooldowns/procs: `Unbreakable Armor`, `Killing Machine`, `Rime`/`Freezing Fog`, `Empower Rune Weapon`
- Utility: `Death Grip`, `Mind Freeze`, `Chains of Ice`

Leveling priority:

1. Apply and maintain diseases.
2. Use `Pestilence` for multi-target disease spread.
3. Use `Blood Strike` up to two targets, `Blood Boil` for more.
4. Use `Obliterate` as the main Frost/Unholy rune spender.
5. Use `Howling Blast` for two or more targets or when the Rime proc is active.
6. Dump runic power with `Frost Strike`.
7. Use `Death and Decay` for three or more sturdy targets.

Implementation note: Frost needs verified aura names for `Killing Machine` and the Rime proc before the bot can do a good priority list.

### Unholy

Role: disease/pet/AoE focused DPS.

Core combat abilities:

- Diseases: `Icy Touch`, `Plague Strike`, `Pestilence`
- Rune spenders: `Scourge Strike`, `Blood Strike`, `Blood Boil`, `Death and Decay`
- Runic power: `Death Coil`, possibly `Unholy Blight`
- Pet/cooldowns: `Raise Dead`, `Ghoul Frenzy`, `Summon Gargoyle`, `Army of the Dead`
- Utility: `Death Grip`, `Mind Freeze`, `Chains of Ice`

Leveling priority:

1. Keep ghoul active when practical.
2. Apply and maintain diseases.
3. Use `Pestilence` for multi-target pulls.
4. Use `Blood Strike` up to two targets and `Blood Boil` for more, while keeping `Desolation` up if talented.
5. Use `Scourge Strike` as the main Unholy rune spender once `Blood Plague` is active.
6. Use `Icy Touch` as the main Frost rune spender.
7. Dump runic power with `Death Coil`.
8. Use `Death and Decay` for three or more sturdy targets.

Implementation note: Unholy has more pet and cooldown state than Blood. It is a good second or third variant after the rune/runic-power helper is stable.

## Cross-Variant Ability List

| Area | Ability names to support | Notes |
| --- | --- | --- |
| Pulling | `Death Grip`, `Icy Touch`, `Chains of Ice` | Death Grip is especially useful for ranged/caster mobs. |
| Auto attack | `StartAttack()` via Lua | Existing non-Vanilla combat base already does this. |
| Diseases | `Icy Touch`, `Plague Strike`, `Frost Fever`, `Blood Plague`, `Pestilence` | Existing aura/debuff reads should work on WotLK. |
| Blood rune spenders | `Blood Strike`, `Heart Strike`, `Blood Boil`, `Pestilence` | `Heart Strike` is Blood talent specific. |
| Frost/Unholy spenders | `Death Strike`, `Obliterate`, `Scourge Strike` | Variant-specific primary spenders. |
| Runic power spenders | `Death Coil`, `Frost Strike`, `Rune Strike` | `Rune Strike` likely needs `IsUsableSpell` or error/proc handling. |
| AoE | `Death and Decay`, `Pestilence`, `Blood Boil`, `Howling Blast`, `Corpse Explosion` | `Death and Decay` targets ground; current `CastSpellAtPosition` is not implemented. Lua cursor casting may be needed. |
| Defensive | `Icebound Fortitude`, `Anti-Magic Shell`, `Rune Tap`, `Vampiric Blood`, `Death Pact`, `Bone Shield` | Some are talent specific. |
| Interrupt | `Mind Freeze`, `Strangulate` | Check runic power cost/cooldown before cast. |
| Buffs | `Horn of Winter`, `Blood Presence`, `Frost Presence`, `Unholy Presence` | Presences can be detected as buffs. |
| Pet | `Raise Dead`, `Death Pact`, `Ghoul Frenzy`, `Summon Gargoyle` | Start without complex pet control unless Unholy is in scope. |
| Runeforging | `Rune of the Fallen Crusader`, `Rune of Razorice`, `Rune of the Stoneskin Gargoyle` | Treat as preflight first; automation requires Runeforge and weapon targeting. |

## Suggested First Blood DK Grinding Rotation

This is intentionally bot-friendly rather than raid-optimal:

1. If not in WotLK or not a DK, stop with a clear message.
2. If `Horn of Winter` missing and spell ready, cast it.
3. Ensure `Blood Presence` unless tank/hard-pull mode is explicitly configured.
4. If target is far and `Death Grip` is ready, use it; otherwise approach and use `Icy Touch` when in range.
5. If target lacks `Frost Fever`, cast `Icy Touch`.
6. If target lacks `Blood Plague`, cast `Plague Strike`.
7. If aggressor count is at least 2 and diseases are on target, cast `Pestilence`.
8. If aggressor count is at least 3 and `Death and Decay` is supported, cast it near the player or target cluster.
9. If player health is below 70 percent and a Frost+Unholy rune pair is available, cast `Death Strike`.
10. If player health is below 45 percent, use `Rune Tap` if known/ready.
11. If player health is below 35 percent and runic power is sufficient, use `Icebound Fortitude` or `Death Pact` depending on ghoul availability.
12. If target is casting/channeling and `Mind Freeze` is usable, interrupt.
13. Use `Heart Strike` if known, else `Blood Strike`, against one or two targets.
14. Use `Blood Boil` against three or more targets.
15. Dump runic power with `Death Coil` when near cap and no survival cooldown needs it.

## Leveling Strategy Notes

- Death Knights start at level 55, so no low-level rotation path is needed.
- Buy/train all abilities while leveling; several situational abilities matter for bot survival.
- Blood is the recommended first unattended grind profile because it minimizes downtime.
- Frost and Unholy are viable for faster kills, but need proc/pet/AoE state to outperform Blood safely.
- The bot should use conservative multi-pull thresholds until DK defensive cooldowns and rune tracking are proven.
