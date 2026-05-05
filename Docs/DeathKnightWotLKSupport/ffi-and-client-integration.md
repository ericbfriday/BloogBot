# FFI and WotLK Client Integration

## Conclusion

No new native FFI method is required for the first Death Knight implementation if WotLK Lua results work for rune and runic-power APIs. Add managed wrappers first. Keep native FFI as a fallback.

## Existing WotLK Integration That Can Be Reused

- `WotLKGameFunctionHandler.CastSpellById` casts known spells at a target GUID.
- `WotLKGameFunctionHandler.LuaCall` and `Functions.LuaCallWithResult` already let managed code execute client Lua and read global result variables.
- `WotLKGameFunctionHandler.IsSpellOnCooldown` already wraps the WotLK spell cooldown function.
- `WoWUnit.Buffs` and `WoWUnit.Debuffs` use WotLK aura functions, which should cover DK presences, Horn of Winter, diseases, and talent procs.
- `WowDb.Tables` can access WotLK DBC tables, and `ClientDb.SpellRuneCost` is already enumerated.

## Managed Wrappers Needed

Add these as C# helpers, likely on `LocalPlayer` or a small `DeathKnightResources` helper:

```csharp
public enum RuneType
{
    Blood = 1,
    Unholy = 2, // WotLK Lua names this CHROMATIC in GetRuneType.
    Frost = 3,
    Death = 4
}

public sealed class RuneState
{
    public int Slot { get; set; }
    public RuneType Type { get; set; }
    public bool Ready { get; set; }
    public float CooldownRemainingSeconds { get; set; }
}
```

Lua-backed methods to verify and add:

```lua
-- Runic power.
result = UnitPower("player", 6)

-- Rune cooldown.
start, duration, ready = GetRuneCooldown(slot)

-- Rune count and rune type.
count = GetRuneCount(slot)
runeType = GetRuneType(slot)

-- Usability check. Useful for Rune Strike and resource validation.
usable, noResource = IsUsableSpell("Death Strike")
```

Because `LuaCallWithResults` reads strings from global variables, booleans should be coerced in the Lua snippet:

```lua
local start, duration, ready = GetRuneCooldown(1)
{0} = tostring(start)
{1} = tostring(duration)
{2} = ready and "1" or "0"
```

## Ability Cost Handling

Use a small static map first. It is easier to test than DBC decoding and covers the initial bot:

```text
Icy Touch: 1 Frost
Plague Strike: 1 Unholy
Blood Strike: 1 Blood
Heart Strike: 1 Blood
Pestilence: 1 Blood
Blood Boil: 1 Blood
Death Strike: 1 Frost + 1 Unholy
Obliterate: 1 Frost + 1 Unholy
Scourge Strike: 1 Unholy
Death and Decay: 1 Blood + 1 Frost + 1 Unholy
Death Coil: runic power
Frost Strike: runic power
Rune Strike: runic power and reactive usability
Mind Freeze: runic power
Icebound Fortitude: runic power
Anti-Magic Shell: runic power
Death Pact: runic power and ghoul state
```

After the first bot works, consider reading `SpellRuneCost.dbc` automatically. The WotLK Modding Wiki lists `SpellRuneCost` columns as `ID`, `Blood`, `Unholy`, `Frost`, and `RunicPower`; this repo already has the table enum but no row parser or spell-to-cost lookup.

## Where New Native FFI Might Become Necessary

Only add native WotLK methods if one of these is proven in a live client:

- `LuaCallWithResults` cannot reliably return rune booleans/numbers.
- `UnitPower("player", 6)` is unavailable or protected in the 3.3.5.12340 client.
- `GetRuneCooldown`, `GetRuneCount`, or `GetRuneType` return unusable data through injected Lua.
- Existing `IsSpellOnCooldown` does not distinguish rune cooldowns and Lua `IsUsableSpell` is not enough for stable rotation decisions.
- Ground-targeted `Death and Decay` cannot be cast safely through Lua cursor targeting and needs a real `CastAtPosition` implementation.

## Death and Decay Special Case

`IGameFunctionHandler.CastAtPosition` exists, but `WotLKGameFunctionHandler.CastAtPosition` throws `NotImplementedException`. Death and Decay is valuable for DK AoE, but it is ground-targeted.

First-pass options:

- Defer `Death and Decay` and still ship a functional Blood DK grinder.
- Cast through Lua and target at the current target/player location if the client allows it.
- Implement WotLK `CastAtPosition` later if AoE quality is a requirement.

## Verification Script Ideas

Run these through the existing probe/test path in a live WotLK DK client:

```csharp
var rp = ObjectManager.Player.LuaCallWithResults("{0} = tostring(UnitPower('player', 6))");

var rune = ObjectManager.Player.LuaCallWithResults(@"
    local start, duration, ready = GetRuneCooldown(1)
    {0} = tostring(start)
    {1} = tostring(duration)
    {2} = ready and '1' or '0'
    {3} = tostring(GetRuneType(1))
");

var usable = ObjectManager.Player.LuaCallWithResults(@"
    local usable, noResource = IsUsableSpell('Death Strike')
    {0} = usable and '1' or '0'
    {1} = noResource and '1' or '0'
");
```

Record results with full client version, class, level, active presence, current runic power, and which runes were on cooldown.
