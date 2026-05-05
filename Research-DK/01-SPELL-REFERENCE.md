# Death Knight Spell Reference - WotLK 3.3.5a

## Core Abilities (Learned from Trainer)

### Blood Tree
| Spell Name | Spell IDs (by Rank) | Level | Rune Cost |
|------------|-------------------|-------|-----------|
| Blood Strike | 45902, 49926, 49927, 49928, 49929, 49930 | 55-80 | 1 Blood |
| Blood Boil | 48721, 49939, 49940, 49941 | 58-78 | 1 Blood |
| Blood Tap | 45529 | 64 | None (CD) |
| Strangulate | 47476 | 59 | 1 Blood |
| Pestilence | 50842 | 56 | 1 Blood |
| Dark Command | 56222 | 65 | None |
| Hysteria | 49016 | Talent | None |
| Heart Strike | 55050, 55262, 55263, 55264 | Talent | 1 Blood |
| Vampiric Blood | 55233 | Talent | None (CD) |
| Mark of Blood | 49005 | Talent | None |
| Dancing Rune Weapon | 49028 | 51pt Talent | 60 RP |

### Frost Tree
| Spell Name | Spell IDs (by Rank) | Level | Rune Cost |
|------------|-------------------|-------|-----------|
| Icy Touch | 45477, 49896, 49903, 49904, 49909 | 55-78 | 1 Frost |
| Chains of Ice | 45524 | 58 | 1 Frost |
| Mind Freeze | 47528 | 57 | 20 RP |
| Frost Presence | 48263 | 57 | None |
| Icebound Fortitude | 48792 | 62 | 20 RP |
| Rune Strike | 56815 | 67 | 30 RP |
| Horn of Winter | 57330, 57623 | 65, 75 | None (gen RP) |
| Obliterate | 49020, 51423, 51424, 51425, 66198 | 61+ | 1F+1U |
| Path of Frost | 3714 | 61 | None |
| Howling Blast | 49184, 51411, 51412, 51413 | Talent | 1F+1U |
| Frost Strike | 49143, 51416, 51417, 51418 | Talent | 40 RP |
| Unbreakable Armor | 51271 | Talent | 1 Frost |
| Hungering Cold | 49203 | 51pt Talent | 40 RP |
| Lichborne | 49039 | Talent | None |

### Unholy Tree
| Spell Name | Spell IDs (by Rank) | Level | Rune Cost |
|------------|-------------------|-------|-----------|
| Plague Strike | 45462, 49917, 49918, 49919, 49920, 49921 | 55-80 | 1 Unholy |
| Death Coil | 47541, 49892, 49893, 49894, 49895 | 55-80 | 40 RP |
| Death Grip | 49576 | 55 | None |
| Raise Dead | 46584 | 56 | None |
| Death Strike | 49998, 49999, 45463, 49923, 49924 | 56-80 | 1F+1U |
| Death and Decay | 43265, 49936, 49937, 49938 | 60-80 | 1B+1F+1U |
| Death Pact | 48743 | 66 | 40 RP |
| Anti-Magic Shell | 48707 | 68 | 20 RP |
| Unholy Presence | 48265 | 70 | None |
| Army of the Dead | 42650 | 80 | Channel |
| Death Gate | 50977 | 55 | None |
| Summon Gargoyle | 49206 | Talent | 60 RP |
| Scourge Strike | 55090, 55265, 55266, 55267 | Talent | 1F+1U |
| Bone Shield | 49222 | Talent | 1 Unholy |
| Anti-Magic Zone | 51052 | Talent | 1 Unholy |
| Empower Rune Weapon | 47568 | 75 | None (CD) |

### Presences
| Presence | Spell ID | Level | Effect |
|----------|---------|-------|--------|
| Blood Presence | 48266 | 55 | +15% dmg, heals 4% of dmg dealt |
| Frost Presence | 48263 | 57 | +60% armor, +10% HP, +8% threat |
| Unholy Presence | 48265 | 70 | +15% atk speed, +15% move, -0.5s GCD |

### Runeforging (Weapon Enchants)
| Rune | Spell ID | Level | Effect |
|------|---------|-------|--------|
| Rune of Razorice | 53343 | 55 | +2% Frost dmg stacking, +5% Frost vuln |
| Rune of the Fallen Crusader | 53344 | 70 | Chance: +30% Str, heal 3% |
| Rune of Cinderglacier | 53341 | 55 | Chance: +20% Shadow/Frost dmg x2 |
| Rune of Swordshattering | 53323 | 63 | +4% parry |
| Rune of Spellshattering | 53342 | 57 | -2% spell dmg taken |
| Rune of the Stoneskin Gargoyle | 62158 | 70 | +25 Def, +1% Stam (2H only) |

---

## Diseases (Key Buffs/Debuffs for Rotation)

| Disease | Applied By | Spell ID | Duration | Notes |
|---------|-----------|---------|----------|-------|
| Frost Fever | Icy Touch / Howling Blast | 55095 | 15s | Frost DoT |
| Blood Plague | Plague Strike | 55078 | 15s | Shadow DoT |
| Ebon Plaguebringer | Unholy talent (replaces BP) | 51735 | 15s | +spell dmg, +crit |

---

## Blood DK Bot - Spell Name Constants

```csharp
// Presences
const string BloodPresence = "Blood Presence";
const string FrostPresence = "Frost Presence";
const string UnholyPresence = "Unholy Presence";

// Core Rotation
const string IcyTouch = "Icy Touch";
const string PlagueStrike = "Plague Strike";
const string BloodStrike = "Blood Strike";
const string HeartStrike = "Heart Strike";
const string DeathStrike = "Death Strike";
const string BloodBoil = "Blood Boil";
const string Pestilence = "Pestilence";
const string DeathCoil = "Death Coil";
const string RuneStrike = "Rune Strike";

// Diseases (for checking debuffs)
const string FrostFever = "Frost Fever";
const string BloodPlague = "Blood Plague";

// Buffs
const string HornOfWinter = "Horn of Winter";

// Utility
const string DeathGrip = "Death Grip";
const string MindFreeze = "Mind Freeze";
const string BloodTap = "Blood Tap";
const string EmpowerRuneWeapon = "Empower Rune Weapon";

// Defensive
const string IceboundFortitude = "Icebound Fortitude";
const string AntiMagicShell = "Anti-Magic Shell";
const string VampiricBlood = "Vampiric Blood";
const string DeathPact = "Death Pact";

// Pets
const string RaiseDead = "Raise Dead";
const string ArmyOfTheDead = "Army of the Dead";
```

---

## Runic Power Costs (for TryUseAbility)

| Ability | RP Cost |
|---------|---------|
| Death Coil | 40 |
| Frost Strike | 40 |
| Rune Strike | 30 |
| Mind Freeze | 20 |
| Anti-Magic Shell | 20 |
| Icebound Fortitude | 20 |
| Hungering Cold | 40 |
| Dancing Rune Weapon | 60 |
| Summon Gargoyle | 60 |
| Death Pact | 40 |
