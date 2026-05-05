# Death Knight Support for BloogBot - Research Summary

> WotLK 3.3.5a (build 12340) - The ONLY version that supports Death Knights.

## Table of Contents
1. [Codebase Architecture](#1-codebase-architecture)
2. [Death Knight Class Overview](#2-death-knight-class-overview)
3. [DK Specs & Bot Profiles Needed](#3-dk-specs--bot-profiles-needed)
4. [Complete DK Ability List](#4-complete-dk-ability-list)
5. [Resource System: Runes + Runic Power](#5-resource-system-runes--runic-power)
6. [Combat Rotations by Spec](#6-combat-rotations-by-spec)
7. [New FFI/Memory Offsets Required](#7-new-ffimemory-offsets-required)
8. [Codebase Changes Required](#8-codebase-changes-required)
9. [Implementation Plan](#9-implementation-plan)
10. [Sources](#10-sources)

---

## 1. Codebase Architecture

### How Bot Profiles Work
Each bot profile is a **separate C# class library project** that exports an `IBot` implementation:

```
[BotProfile]Bot/
  [BotProfile]Bot.cs       - Implements IBot, MEF-exported
  CombatState.cs           - Extends CombatStateBase, contains rotation logic
  RestState.cs             - Implements IBotState for resting/eating
  MoveToTargetState.cs     - Extends MoveToTargetStateBase
  PowerlevelCombatState.cs - Powerleveling variant
  DependencyContainer      - Instantiated inline in IBot.GetDependencyContainer()
```

### Key Interfaces & Base Classes
| Type | File | Purpose |
|------|------|---------|
| `IBot` | `BloogBot/AI/IBot.cs` | Bot profile contract: Name, FileName, states |
| `Bot` (abstract) | `BloogBot/AI/Bot.cs` | Main loop, death handling, stuck detection |
| `IBotState` | `BloogBot/AI/IBotState.cs` | State interface (Update method) |
| `CombatStateBase` | `BloogBot/AI/SharedStates/CombatStateBase.cs` | Shared combat logic: facing, range, auto-attack, spell casting |
| `MoveToTargetStateBase` | `BloogBot/AI/SharedStates/MoveToTargetStateBase.cs` | Shared approach logic |
| `IDependencyContainer` | `BloogBot/AI/IDependencyContainer.cs` | Factory for states, settings, hotspots |

### Spell Casting Methods (CombatStateBase)
- **`TryCastSpell(name, condition, castOnSelf)`** - For mana-based spells (checks mana cost)
- **`TryUseAbility(name, requiredResource, condition)`** - For rage/energy (checks class resource)
- **`TryUseAbilityById(name, id, requiredRage, condition)`** - Variant with spell book ID

### Existing Class Enum
`DeathKnight = 6` already exists in `BloogBot/Game/Enums/Class.cs`. No enum change needed.

---

## 2. Death Knight Class Overview

### Starting Zone
- **Ebon Hold** (Scarlet Enclave, Eastern Plaguelands) - DKs start at level 55
- Quest chain from 55-58, then free to enter regular zones

### Key Mechanics
- **Plate armor** wearer (similar to Warrior/Paladin)
- **Melee-focused** with some ranged/area spells
- **Two-hand or Dual Wield** depending on spec
- **No mana** - uses unique Rune + Runic Power system
- **Self-healing** is core to the class (Death Strike, Blood spec)
- **Diseases** (Frost Fever, Blood Plague) are key damage multipliers
- **Presences** (like warrior stances): Blood/Frost/Unholy
- **Death Grip** - unique pull ability (ranged pull into melee)
- **Starts at level 55** - no need for low-level rotations

---

## 3. DK Specs & Bot Profiles Needed

### Recommended: Blood DK (Primary Profile)
**Why Blood for botting/grinding:**
- Best self-healing (Death Strike heals based on damage taken)
- Can pull multiple mobs and sustain
- Minimal downtime (no need for food if played well)
- Simple, forgiving rotation
- "Blood DK is the best grinding spec - chain pull mobs and stay at 100% HP" (Reddit consensus)

### Secondary: Frost DK (DPS Profile)
- High burst damage
- Howling Blast for AoE
- Dual Wield or 2H viable
- Good for dungeon leveling

### Tertiary: Unholy DK (DPS Profile)
- Pet (Raise Dead / Ghoul)
- Strong sustained damage
- Disease spreading (Pestilence)
- Good AoE with Death and Decay

---

## 4. Complete DK Ability List

### Core Abilities (All Specs)
| Spell Name | Level | Resource Cost | Type | Notes |
|------------|-------|--------------|------|-------|
| Blood Presence | 55 | None | Buff | +15% damage, heals 4% of damage dealt |
| Frost Presence | 57 | None | Buff | +60% armor, +10% health, +8% threat |
| Unholy Presence | 70 | None | Buff | +15% attack speed, +15% movement, -0.5s GCD |
| Icy Touch | 55 | 1 Frost Rune | Spell | Applies Frost Fever disease, ranged |
| Plague Strike | 55 | 1 Unholy Rune | Melee | Applies Blood Plague disease |
| Blood Strike | 55 | 1 Blood Rune | Melee | Basic strike, bonus per disease |
| Death Strike | 56 | 1 Frost + 1 Unholy | Melee | Heals based on damage taken (core Blood ability) |
| Death Coil | 55 | 40 Runic Power | Spell | Ranged shadow damage, can heal ghoul |
| Death Grip | 55 | None | Spell | Pulls target to you (35yd) |
| Mind Freeze | 57 | 20 Runic Power | Spell | Interrupt (off GCD) |
| Chains of Ice | 58 | 1 Frost Rune | Spell | Ranged snare |
| Strangulate | 59 | 1 Blood Rune | Spell | Silence (ranged) |
| Blood Boil | 58 | 1 Blood Rune | Spell | AoE damage, bonus per disease |
| Pestilence | 56 | 1 Blood Rune | Spell | Spreads diseases to nearby targets |
| Obliterate | 61 | 1 Frost + 1 Unholy | Melee | Heavy damage, consumes diseases (Frost core) |
| Blood Tap | 64 | None | Ability | Converts Blood Rune to Death Rune |
| Anti-Magic Shell | 68 | 20 Runic Power | Buff | Absorbs spell damage |
| Icebound Fortitude | 62 | 20 Runic Power | Buff | Damage reduction (stun immune) |
| Rune Strike | 67 | 30 Runic Power | Melee | After dodge/parry (like Overpower) |
| Horn of Winter | 65 | None | Buff | +Str/Agi (generates 10 RP) |
| Dark Command | 65 | None | Spell | Taunt |
| Empower Rune Weapon | 75 | None | CD | Activates all runes + 25 RP |
| Death and Decay | 60 | 1 Blood + 1 Frost + 1 Unholy | AoE | Ground-targeted AoE |
| Raise Dead | 56 | None | Summon | Summons ghoul pet |
| Death Pact | 66 | 40 Runic Power | Spell | Sacrifice ghoul for health |
| Army of the Dead | 80 | None | CD | Channel, summons army of ghouls |
| Path of Frost | 61 | None | Buff | Water walking |

### Blood-Specific Talents
| Spell Name | Talent Req | Resource | Notes |
|------------|-----------|----------|-------|
| Heart Strike | Blood | 1 Blood Rune | Primary Blood strike, cleaves 1 nearby |
| Death Rune Mastery | Blood | Passive | Death Strike converts runes to Death Runes |
| Mark of Blood | Blood | None | Debuff, heals attacker on hit |
| Vampiric Blood | Blood | None | +15% max HP, +35% healing received |
| Hysteria | Blood | None | +20% physical damage (to friendly) |
| Dancing Rune Weapon | Blood (51pt) | 60 RP | Parry bonus, copies attacks |

### Frost-Specific Talents
| Spell Name | Talent Req | Resource | Notes |
|------------|-----------|----------|-------|
| Howling Blast | Frost | 1 Frost + 1 Unholy | AoE frost damage |
| Frost Strike | Frost | 40 RP | Melee frost damage (can't be dodged/blocked/parried) |
| Killing Machine | Frost | Passive | Auto-attack crits proc instant Oblit/Howling Blast |
| Rime | Frost | Passive | Obliterate can proc free Howling Blast |
| Unbreakable Armor | Frost | 1 Frost Rune | +25% armor, +10% strength |
| Hungering Cold | Frost (51pt) | 40 RP | AoE freeze |

### Unholy-Specific Talents
| Spell Name | Talent Req | Resource | Notes |
|------------|-----------|----------|-------|
| Scourge Strike | Unholy | 1 Frost + 1 Unholy | Shadow damage strike, scales with diseases |
| Summon Gargoyle | Unholy | 60 RP | Ranged DPS pet |
| Bone Shield | Unholy | 1 Unholy Rune | Damage reduction charges |
| Anti-Magic Zone | Unholy | 1 Unholy Rune | Group spell absorb |
| Wandering Plague | Unholy | Passive | Diseases can spread via crits |
| Ebon Plaguebringer | Unholy | Passive | +spell damage taken, +crit |

---

## 5. Resource System: Runes + Runic Power

### Rune System
DKs have **6 rune slots**: 2 Blood, 2 Frost, 2 Unholy.

- Each rune has a **10-second cooldown** after use
- **Death Runes** can be used as any type (Blood/Frost/Unholy)
- Rune cooldowns are independent

### Runic Power (RP)
- Generated when using rune abilities (typically 10-25 RP per ability)
- Capped at 100 RP (130 with talents/glyphs)
- Spent on abilities: Death Coil (40), Frost Strike (40), Rune Strike (30), etc.
- **Mechanically similar to Warrior Rage** - builds up, then spend it

### Memory Offsets for WotLK 3.3.5a

Based on descriptor field layout analysis (UNIT_FIELD_* indices * 4):

| Field | Descriptor Offset | Notes |
|-------|-------------------|-------|
| Health | 0x60 | Already exists |
| Mana (Power1) | 0x64 | Already exists |
| Rage (Power2) | 0x68 | Already exists |
| Focus (Power3) | 0x6C | Unused |
| Energy (Power4) | 0x70 | Already exists |
| Happiness (Power5) | 0x74 | Pet only |
| Rune (Power6) | 0x78 | **NEW - for DK rune tracking** |
| **Runic Power (Power7)** | **0x7C** | **NEW - KEY OFFSET FOR DK** |
| MaxHealth | 0x80 | Already exists |
| MaxMana (MaxPower1) | 0x84 | Already exists |
| MaxRage (MaxPower2) | 0x88 | Not stored |
| MaxRunicPower (MaxPower7) | 0x9C | **NEW** |

### Important: Runic Power Scaling
- Like Rage, Runic Power is stored as `value * 10` in memory (e.g., 50 RP = 500 in memory)
- Need to divide by 10 when reading, same as Rage: `MemoryManager.ReadInt(...) / 10`
- Max RP base is 100 (1000 in memory)

---

## 6. Combat Rotations by Spec

### Blood DK Grinding Rotation (Primary Bot Profile)
Best for botting/grinding due to self-healing and multi-mob capability.

```
SINGLE TARGET:
1. Blood Presence (maintain buff)
2. Horn of Winter (maintain buff, generates RP)
3. Icy Touch (apply Frost Fever, ranged opener)
4. Plague Strike (apply Blood Plague)
5. Pestilence (if adds nearby, spread diseases)
6. Heart Strike (primary damage, cleaves)
7. Death Strike (self-heal, convert runes to Death Runes)
8. Blood Boil (if diseases active on multiple targets)
9. Rune Strike (when available after dodge/parry, dumps RP)
10. Death Coil (RP dump when no runes available)

MULTI-TARGET (2+ mobs):
1. Blood Presence
2. Icy Touch (main target)
3. Plague Strike (main target)
4. Pestilence (spread diseases)
5. Blood Boil (AoE damage)
6. Death and Decay (on CD for big pulls)
7. Heart Strike (cleave)
8. Death Strike (self-heal priority)
9. Rune Strike / Death Coil (RP dump)

DEFENSIVE:
- Icebound Fortitude (damage reduction)
- Anti-Magic Shell (spell damage)
- Vampiric Blood (healing boost)
- Death Pact (sacrifice ghoul for health)
- Blood Tap (emergency rune)
- Empower Rune Weapon (emergency all-runes + RP)
```

### Frost DK DPS Rotation (Secondary Profile)
```
SINGLE TARGET:
1. Frost Presence (or Blood Presence for solo)
2. Horn of Winter
3. Icy Touch (Frost Fever)
4. Plague Strike (Blood Plague)
5. Obliterate (primary damage, consumes diseases)
6. Howling Blast (on Rime proc - free)
7. Frost Strike (RP dump)
8. Blood Strike (blood rune dump)
9. Killing Machine procs: prioritise Obliterate/Howling Blast

MULTI-TARGET:
1. Howling Blast (primary AoE)
2. Blood Boil
3. Frost Strike (RP dump)
```

### Unholy DK DPS Rotation (Tertiary Profile)
```
SINGLE TARGET:
1. Unholy Presence (or Blood for solo)
2. Horn of Winter
3. Icy Touch
4. Plague Strike
5. Scourge Strike (primary damage)
6. Blood Strike (blood rune dump)
7. Death Coil (RP dump)
8. Summon Gargoyle (on CD)

MULTI-TARGET:
1. Icy Touch -> Plague Strike -> Pestilence
2. Death and Decay
3. Blood Boil
4. Scourge Strike
5. Death Coil
```

---

## 7. New FFI/Memory Offsets Required

### 7a. Runic Power Reading (CRITICAL - NEW)
**No new FFI function needed.** Runic Power can be read from the descriptor using existing `MemoryManager.ReadInt()`.

**New offset needed in `MemoryAddresses.cs`:**
```csharp
// In WotLK section:
WoWUnit_RunicPowerOffset = 0x7C;  // Power7 field in descriptor
```

**New property on `WoWUnit.cs`:**
```csharp
public int RunicPower => MemoryManager.ReadInt(GetDescriptorPtr() + MemoryAddresses.WoWUnit_RunicPowerOffset) / 10;
```

### 7b. CombatStateBase.TryUseAbility Enhancement (NEEDED)
The current `TryUseAbility` only checks Warrior (Rage) and Rogue (Energy):

```csharp
// CURRENT (line 216-220):
if (player.Class == Class.Warrior)
    playerResource = player.Rage;
else if (player.Class == Class.Rogue)
    playerResource = player.Energy;
// todo: feral druids (bear/cat form)
```

**Must add Death Knight support:**
```csharp
else if (player.Class == Class.DeathKnight)
    playerResource = player.RunicPower;
```

### 7c. Death Grip - Ranged Pull (NICE TO HAVE)
Death Grip is a unique ranged pull (35yd) that brings the target TO the DK. This is ideal for botting as it eliminates the "move to target" phase. Consider a custom `MoveToTargetState` that:
1. Opens with Death Grip at 30-35 yards
2. Only walks toward target if Death Grip is on cooldown

### 7d. Rune Cooldown Tracking (OPTIONAL)
For optimal rotation, tracking individual rune cooldowns would be ideal. However, `IsSpellReady()` already checks spell cooldowns which includes rune requirements. **Likely NOT needed for initial implementation** - the bot can simply try to cast spells and the game will handle rune gating.

If rune tracking is desired later, Lua can be used:
```lua
local start, duration, runeReady = GetRuneCooldown(runeIndex)
local runeType = GetRuneType(runeIndex)  -- 1=Blood, 2=Unholy, 3=Frost
```

### 7e. DK-Specific Presences (SIMILAR TO WARRIOR STANCES)
DK Presences work like Warrior Stances - only one can be active at a time. The bot needs:
- `CurrentPresence` property on `LocalPlayer` (similar to `CurrentStance`)
- Check in combat state to maintain correct presence

### 7f. Death Knight Diseases (IMPORTANT)
The bot needs to track two key diseases on targets:
- **Frost Fever** (applied by Icy Touch)
- **Blood Plague** (applied by Plague Strike)

These are debuffs and can be checked with existing `target.HasDebuff("Frost Fever")` / `target.HasDebuff("Blood Plague")`.

---

## 8. Codebase Changes Required

### Files to Modify
| File | Change |
|------|--------|
| `BloogBot/Game/MemoryAddresses.cs` | Add `WoWUnit_RunicPowerOffset = 0x7C` in WotLK section |
| `BloogBot/Game/Objects/WoWUnit.cs` | Add `RunicPower` property |
| `BloogBot/AI/SharedStates/CombatStateBase.cs` | Add DK to `TryUseAbility` resource check |
| `BloogBot/Game/Objects/LocalPlayer.cs` | Add `CurrentPresence` property (like `CurrentStance`) |

### New Files (Blood DK Profile - Primary)
| File | Purpose |
|------|---------|
| `BloodDKBot/BloodDKBot.cs` | IBot implementation |
| `BloodDKBot/CombatState.cs` | Blood DK rotation |
| `BloodDKBot/RestState.cs` | Rest state (minimal - DKs self-heal) |
| `BloodDKBot/MoveToTargetState.cs` | Custom approach with Death Grip |
| `BloodDKBot/BloodDKBot.csproj` | Project file |
| `BloodDKBot/app.config` | Config |
| `BloodDKBot/Properties/` | Assembly info |

### New Files (Frost DK Profile - Secondary)
Same structure as Blood but with Frost rotation.

### New Files (Unholy DK Profile - Tertiary)
Same structure but with Unholy rotation + pet management.

---

## 9. Implementation Plan

### Phase 1: Framework Changes
1. Add `WoWUnit_RunicPowerOffset` to `MemoryAddresses.cs` (WotLK section)
2. Add `RunicPower` property to `WoWUnit.cs`
3. Add `DeathKnight` case to `CombatStateBase.TryUseAbility()`
4. Add `CurrentPresence` to `LocalPlayer.cs`
5. Verify framework compiles

### Phase 2: Blood DK Bot (Primary Profile)
1. Create `BloodDKBot/` project
2. Implement `BloodDKBot.cs` (IBot)
3. Implement `CombatState.cs` with Blood rotation
4. Implement `RestState.cs` (minimal food usage, DK heals via Death Strike)
5. Implement `MoveToTargetState.cs` (Death Grip opener)
6. Add project to solution
7. Test on WotLK 3.3.5a client

### Phase 3: Frost DK Bot (Optional)
1. Create `FrostDKBot/` project
2. Frost rotation with Obliterate/Howling Blast priority

### Phase 4: Unholy DK Bot (Optional)
1. Create `UnholyDKBot/` project
2. Unholy rotation with Scourge Strike + pet management

### Phase 5: Polish
1. Rune-aware rotation optimization (if needed)
2. Path of Frost water walking support
3. Death Gate usage
4. DK starter zone support

---

## 10. Sources

- Wowhead WotLK DK Abilities: https://www.wowhead.com/wotlk/spells/abilities/death-knight
- Icy Veins Blood DK Spell Summary: https://www.icy-veins.com/wotlk-classic/blood-death-knight-tank-pve-spell-summary
- Joana's World DK Abilities: https://www.joanasworld.com/deathknight-abilities.php
- WotLK DB DK Class Skills: https://wotlkdb.com/?spells=7.6
- Warmane Unholy DK PvP Guide (3.3.5): https://forum.warmane.com/showthread.php?t=316091
- Warmane Frost DK PvE Guide (3.3.5): https://forum.warmane.com/showthread.php?t=349828
- Wowhead Blood DK Leveling: https://www.wowhead.com/wotlk/guide/classes/death-knight/blood/tank-leveling-tips
- Reddit Best DK Leveling Spec: https://www.reddit.com/r/classicwow/comments/w6e2vr/
- Overgear DK Guide: https://overgear.com/guides/wotlk/death-knight-guide/
- WotLKRotations GitHub (offsets): https://github.com/AzDeltaQQ/WotLKRotations
- BloogBot Source Code: This repository
