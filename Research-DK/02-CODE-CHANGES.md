# Codebase Changes Detail - Death Knight Support

## Memory Offset Calculation Reference

WotLK 3.3.5a descriptor layout uses UNIT_FIELD_* enum indices multiplied by 4:

```
UNIT_FIELD_HEALTH    = 0x18 → 0x18 * 4 = 0x60 ✓ (matches BloogBot)
UNIT_FIELD_POWER1    = 0x19 → 0x19 * 4 = 0x64 ✓ (Mana, matches BloogBot)
UNIT_FIELD_POWER2    = 0x1A → 0x1A * 4 = 0x68 ✓ (Rage, matches BloogBot)
UNIT_FIELD_POWER3    = 0x1B → 0x1B * 4 = 0x6C (Focus - unused)
UNIT_FIELD_POWER4    = 0x1C → 0x1C * 4 = 0x70 ✓ (Energy, matches BloogBot)
UNIT_FIELD_POWER5    = 0x1D → 0x1D * 4 = 0x74 (Happiness - pet only)
UNIT_FIELD_POWER6    = 0x1E → 0x1E * 4 = 0x78 (Runes)
UNIT_FIELD_POWER7    = 0x1F → 0x1F * 4 = 0x7C (RUNIC POWER ← NEW)
UNIT_FIELD_MAXHEALTH = 0x20 → 0x20 * 4 = 0x80 ✓ (matches BloogBot)
UNIT_FIELD_MAXPOWER1 = 0x21 → 0x21 * 4 = 0x84 ✓ (MaxMana, matches BloogBot)
UNIT_FIELD_MAXPOWER7 = 0x27 → 0x27 * 4 = 0x9C (Max Runic Power ← NEW)
```

Verification: All existing BloogBot WotLK offsets for Health(0x60), Mana(0x64), Rage(0x68),
Energy(0x70), MaxHealth(0x80), MaxMana(0x84) match perfectly with this layout.

---

## Exact Changes Per File

### 1. BloogBot/Game/MemoryAddresses.cs

Add in the WotLK section (after `WoWUnit_EnergyOffset = 0x70;`):

```csharp
WoWUnit_RunicPowerOffset = 0x7C;
```

Add the static field declaration (after `public static int WoWUnit_EnergyOffset;`):

```csharp
public static int WoWUnit_RunicPowerOffset;
```

### 2. BloogBot/Game/Objects/WoWUnit.cs

Add after the `Energy` property:

```csharp
public int RunicPower => MemoryManager.ReadInt(GetDescriptorPtr() + MemoryAddresses.WoWUnit_RunicPowerOffset) / 10;
```

Note: Divide by 10 because RP is stored like Rage (value * 10 in memory).

### 3. BloogBot/AI/SharedStates/CombatStateBase.cs

In `TryUseAbility` method (~line 216-220), add DK case:

```csharp
if (player.Class == Class.Warrior)
    playerResource = player.Rage;
else if (player.Class == Class.Rogue)
    playerResource = player.Energy;
else if (player.Class == Class.DeathKnight)
    playerResource = player.RunicPower;
// todo: feral druids (bear/cat form)
```

### 4. BloogBot/Game/Objects/LocalPlayer.cs

Add DK presence constants and CurrentPresence property:

```csharp
// DEATH KNIGHT
const string BloodPresence = "Blood Presence";
const string FrostPresence = "Frost Presence";
const string UnholyPresence = "Unholy Presence";

public string CurrentPresence
{
    get
    {
        if (HasBuff(BloodPresence)) return BloodPresence;
        if (HasBuff(FrostPresence)) return FrostPresence;
        if (HasBuff(UnholyPresence)) return UnholyPresence;
        return "None";
    }
}
```

### 5. BloogBot.sln

Add new project references for BloodDKBot (and FrostDKBot/UnholyDKBot later).

---

## No New FFI Functions Needed

**Critical finding**: All DK abilities can be cast using the existing spell system:
- `player.CastSpell(spellName, targetGuid)` for WotLK
- `player.LuaCall($"CastSpellByName('{name}')")` for Vanilla compatibility
- `Functions.CastSpellById(spellId, targetGuid)` already works
- `Functions.IsSpellOnCooldown(spellId)` handles rune-based cooldowns

The game client itself handles rune consumption - the bot just needs to call
the spell and the client will check if runes are available. This means:
- **No new C++ FastCall functions needed**
- **No new memory reading patterns needed** (beyond Runic Power offset)
- **No new detour hooks needed**

This is the same approach all existing bot profiles use.

---

## DK Rest State Considerations

Unlike most melee classes, Blood DKs have minimal downtime:
- Death Strike provides substantial self-healing
- Blood Presence heals 4% of damage dealt
- Typically don't need food between pulls

The RestState for BloodDKBot should:
1. Only eat if HP < 50% and NOT in combat (rare)
2. Use Death Strike proactively when HP < 70%
3. Cast Horn of Winter while resting for free RP
4. Consider using Death Pact (sacrifice ghoul) for emergency heal

This means RestState can be very simple or even skipped if combat self-healing
is robust enough.

---

## Death Grip MoveToTarget Strategy

DKs have a unique advantage: Death Grip pulls targets from 35 yards.

Custom MoveToTargetState for DK:
1. At 30-35 yards: cast Death Grip (pulls target to melee range)
2. If Death Grip on CD or target too close: walk toward target
3. Once in melee range (5yd): proceed to combat

This eliminates the "walk to mob" phase for most pulls, making DK bots
significantly faster than other melee profiles.
