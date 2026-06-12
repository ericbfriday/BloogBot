# Death Knight WotLK Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add WotLK Death Knight support to BloogBot, starting with shared DK resource support and a Blood Death Knight grinding bot.

**Architecture:** Keep native FFI unchanged for the first implementation. Add a testable managed DK resource model, expose WotLK Lua-backed rune and runic-power reads through `LocalPlayer`, add a DK-specific combat helper, then build one `BloodDeathKnightBot` project that follows the existing per-bot DLL pattern.

**Tech Stack:** Legacy Visual Studio 2022 solution, .NET Framework C# projects, MSTest, MEF bot loading, WotLK 3.3.5 Lua APIs, x86 MSBuild.

---

## Ralph Loop Execution Model

The controller runs this as a phase loop:

1. Lock the next phase scope from this plan.
2. Dispatch read-only specialists for uncertain areas.
3. Dispatch implementation workers with disjoint file ownership.
4. Review worker output for plan compliance.
5. Review integrated code quality.
6. Run the phase verification commands.
7. Update this plan or create a follow-up note if live-client evidence changes the assumptions.

Implementation workers are not alone in the codebase. They must not revert edits made by other workers, and they must adjust their implementation to accommodate already-present changes.

## Repo Build Commands

Run from `D:\dev\bloog\add-dk-for-wotlk-clients`.

Restore:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" .\BloogBot.sln /t:Restore /p:"RestorePackagesConfig=true;Configuration=Debug;Platform=x86" /nologo /m:1
```

Build:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" .\BloogBot.sln /t:Build /p:"Configuration=Debug;Platform=x86" /nologo /m:1
```

Test:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" .\Bot\BloogBotTests.dll
```

If `CalculatePathTest` fails with `System.Runtime.InteropServices.SEHException`, check whether `Bot\mmaps` exists before treating it as a DK regression.

## Phase Boundaries

Phase 1 creates the shared DK resource and combat API. It must build and the new pure logic tests must pass without a live WoW client.

Phase 2 creates `BloodDeathKnightBot`, wires the solution and loader, and keeps `Death and Decay` out of the first rotation because WotLK `CastAtPosition` is unimplemented.

Phase 3 validates the behavior in a live WotLK 3.3.5.12340 Death Knight client and records exact Lua return shapes.

Phase 4 adds Frost and Unholy variants after rune/runic-power helpers are proven.

## File Structure

Shared DK model:

- Create `BloogBot/Game/Enums/RuneType.cs`: rune type enum matching WotLK Lua return values.
- Create `BloogBot/Game/RuneState.cs`: immutable-enough value object for one rune slot.
- Create `BloogBot/Game/DkAbilityCost.cs`: typed rune and runic-power cost object.
- Create `BloogBot/Game/DeathKnightResources.cs`: pure parsing, cost-map, and rune-matching logic.
- Modify `BloogBot/Game/Objects/LocalPlayer.cs`: Lua-backed `RunicPower`, rune reads, and `IsDeathKnightAbilityUsable`.
- Modify `BloogBot/AI/SharedStates/CombatStateBase.cs`: protected DK cast helper.
- Modify `BloogBot/BloogBot.csproj`: include the new shared files.
- Create `BloogBotTests/DeathKnightResourceTests.cs`: MSTest coverage for pure DK resource logic.
- Modify `BloogBotTests/BloogBotTests.csproj`: include the new test file.

Blood bot:

- Create `BloodDeathKnightBot/BloodDeathKnightBot.csproj`: legacy C# library project outputting to `..\Bot\`.
- Create `BloodDeathKnightBot/BloodDeathKnightBot.cs`: MEF `IBot` export.
- Create `BloodDeathKnightBot/CombatState.cs`: Blood DK priority list.
- Create `BloodDeathKnightBot/BuffSelfState.cs`: `Blood Presence` and `Horn of Winter`.
- Create `BloodDeathKnightBot/MoveToTargetState.cs`: melee approach with optional `Death Grip` opener.
- Create `BloodDeathKnightBot/RestState.cs`: food-based recovery, no mana/drink path.
- Create `BloodDeathKnightBot/PowerlevelCombatState.cs`: minimal combat wrapper for powerlevel mode.
- Create `BloodDeathKnightBot/Properties/AssemblyInfo.cs` and `BloodDeathKnightBot/app.config`.
- Modify `BloogBot.sln`: add the project, x86 configurations, and Bots folder nesting.
- Modify `BloogBot/BotLoader.cs`: add `BloodDeathKnightBot.dll` after the shared code builds the DLL.

---

## Phase 1: Shared DK Resource Foundation

### Task 1: Pure DK Resource Model

**Files:**

- Create: `BloogBot/Game/Enums/RuneType.cs`
- Create: `BloogBot/Game/RuneState.cs`
- Create: `BloogBot/Game/DkAbilityCost.cs`
- Create: `BloogBot/Game/DeathKnightResources.cs`
- Modify: `BloogBot/BloogBot.csproj`
- Create: `BloogBotTests/DeathKnightResourceTests.cs`
- Modify: `BloogBotTests/BloogBotTests.csproj`

- [ ] **Step 1: Write failing tests for rune parsing and matching**

Create `BloogBotTests/DeathKnightResourceTests.cs` with these tests:

```csharp
using BloogBot.Game;
using BloogBot.Game.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace BloogBotTests
{
    [TestClass]
    public class DeathKnightResourceTests
    {
        [TestMethod]
        public void ParseRuneStateCoercesLuaStrings()
        {
            var state = DeathKnightResources.ParseRuneState(1, "100.5", "10", "0", "4", "1");

            Assert.AreEqual(1, state.Slot);
            Assert.AreEqual(RuneType.Death, state.Type);
            Assert.IsFalse(state.Ready);
            Assert.AreEqual(1, state.Count);
            Assert.AreEqual(10f, state.CooldownRemainingSeconds);
        }

        [TestMethod]
        public void ParseRuneStateDefaultsInvalidLuaStrings()
        {
            var state = DeathKnightResources.ParseRuneState(2, "", "bad", "", "", "");

            Assert.AreEqual(2, state.Slot);
            Assert.AreEqual(RuneType.Unknown, state.Type);
            Assert.IsFalse(state.Ready);
            Assert.AreEqual(0, state.Count);
            Assert.AreEqual(0f, state.CooldownRemainingSeconds);
        }

        [TestMethod]
        public void HasReadyRunesUsesExactRunesFirst()
        {
            var runes = new[]
            {
                new RuneState(1, RuneType.Blood, true, 1, 0),
                new RuneState(2, RuneType.Frost, true, 1, 0),
                new RuneState(3, RuneType.Unholy, true, 1, 0),
            };

            var cost = new DkAbilityCost(bloodRunes: 1, frostRunes: 1, unholyRunes: 1);

            Assert.IsTrue(DeathKnightResources.HasReadyRunes(runes, cost));
        }

        [TestMethod]
        public void HasReadyRunesUsesDeathRunesAsWildcards()
        {
            var runes = new[]
            {
                new RuneState(1, RuneType.Blood, true, 1, 0),
                new RuneState(2, RuneType.Death, true, 1, 0),
                new RuneState(3, RuneType.Unholy, true, 1, 0),
            };

            var cost = new DkAbilityCost(bloodRunes: 1, frostRunes: 1, unholyRunes: 1);

            Assert.IsTrue(DeathKnightResources.HasReadyRunes(runes, cost));
        }

        [TestMethod]
        public void HasReadyRunesRejectsCoolingRunes()
        {
            var runes = new[]
            {
                new RuneState(1, RuneType.Blood, true, 1, 0),
                new RuneState(2, RuneType.Frost, false, 1, 3.5f),
                new RuneState(3, RuneType.Unholy, true, 1, 0),
                new RuneState(4, RuneType.Death, false, 1, 4.5f),
            };

            var cost = new DkAbilityCost(bloodRunes: 1, frostRunes: 1, unholyRunes: 1);

            Assert.IsFalse(DeathKnightResources.HasReadyRunes(runes, cost));
        }

        [TestMethod]
        public void HasEnoughRunicPowerChecksThreshold()
        {
            var cost = new DkAbilityCost(runicPower: 40);

            Assert.IsFalse(DeathKnightResources.HasEnoughRunicPower(39, cost));
            Assert.IsTrue(DeathKnightResources.HasEnoughRunicPower(40, cost));
        }

        [TestMethod]
        public void GetKnownCostReturnsBloodStrikeCost()
        {
            var cost = DeathKnightResources.GetKnownCost("Blood Strike");

            Assert.AreEqual(1, cost.BloodRunes);
            Assert.AreEqual(0, cost.FrostRunes);
            Assert.AreEqual(0, cost.UnholyRunes);
            Assert.AreEqual(0, cost.RunicPower);
        }

        [TestMethod]
        public void GetKnownCostReturnsZeroCostForUnknownSpell()
        {
            var cost = DeathKnightResources.GetKnownCost("A Made Up Spell");

            Assert.AreEqual(0, cost.TotalRunes);
            Assert.AreEqual(0, cost.RunicPower);
        }
    }
}
```

- [ ] **Step 2: Add the test file to the test project**

Add this compile entry to `BloogBotTests/BloogBotTests.csproj`:

```xml
<Compile Include="DeathKnightResourceTests.cs" />
```

- [ ] **Step 3: Run tests to verify they fail because the production types do not exist**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" .\BloogBotTests\BloogBotTests.csproj /t:Build /p:"Configuration=Debug;Platform=x86" /nologo /m:1
```

Expected: compile failure mentioning missing `DeathKnightResources`, `RuneType`, `RuneState`, or `DkAbilityCost`.

- [ ] **Step 4: Add the minimal production model**

Create `BloogBot/Game/Enums/RuneType.cs`:

```csharp
namespace BloogBot.Game.Enums
{
    public enum RuneType
    {
        Unknown = 0,
        Blood = 1,
        Unholy = 2,
        Frost = 3,
        Death = 4
    }
}
```

Create `BloogBot/Game/RuneState.cs`:

```csharp
using BloogBot.Game.Enums;

namespace BloogBot.Game
{
    public class RuneState
    {
        public RuneState(int slot, RuneType type, bool ready, int count, float cooldownRemainingSeconds)
        {
            Slot = slot;
            Type = type;
            Ready = ready;
            Count = count;
            CooldownRemainingSeconds = cooldownRemainingSeconds;
        }

        public int Slot { get; }

        public RuneType Type { get; }

        public bool Ready { get; }

        public int Count { get; }

        public float CooldownRemainingSeconds { get; }
    }
}
```

Create `BloogBot/Game/DkAbilityCost.cs`:

```csharp
namespace BloogBot.Game
{
    public class DkAbilityCost
    {
        public DkAbilityCost(int bloodRunes = 0, int frostRunes = 0, int unholyRunes = 0, int runicPower = 0)
        {
            BloodRunes = bloodRunes;
            FrostRunes = frostRunes;
            UnholyRunes = unholyRunes;
            RunicPower = runicPower;
        }

        public int BloodRunes { get; }

        public int FrostRunes { get; }

        public int UnholyRunes { get; }

        public int RunicPower { get; }

        public int TotalRunes => BloodRunes + FrostRunes + UnholyRunes;
    }
}
```

Create `BloogBot/Game/DeathKnightResources.cs`:

```csharp
using BloogBot.Game.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BloogBot.Game
{
    public static class DeathKnightResources
    {
        public const int RuneSlotCount = 6;

        static readonly IDictionary<string, DkAbilityCost> KnownCosts =
            new Dictionary<string, DkAbilityCost>(StringComparer.OrdinalIgnoreCase)
            {
                { "Icy Touch", new DkAbilityCost(frostRunes: 1) },
                { "Plague Strike", new DkAbilityCost(unholyRunes: 1) },
                { "Blood Strike", new DkAbilityCost(bloodRunes: 1) },
                { "Heart Strike", new DkAbilityCost(bloodRunes: 1) },
                { "Pestilence", new DkAbilityCost(bloodRunes: 1) },
                { "Blood Boil", new DkAbilityCost(bloodRunes: 1) },
                { "Death Strike", new DkAbilityCost(frostRunes: 1, unholyRunes: 1) },
                { "Obliterate", new DkAbilityCost(frostRunes: 1, unholyRunes: 1) },
                { "Scourge Strike", new DkAbilityCost(unholyRunes: 1) },
                { "Death and Decay", new DkAbilityCost(bloodRunes: 1, frostRunes: 1, unholyRunes: 1) },
                { "Death Coil", new DkAbilityCost(runicPower: 40) },
                { "Rune Strike", new DkAbilityCost(runicPower: 20) },
                { "Frost Strike", new DkAbilityCost(runicPower: 40) },
                { "Mind Freeze", new DkAbilityCost(runicPower: 20) },
                { "Icebound Fortitude", new DkAbilityCost(runicPower: 20) },
                { "Anti-Magic Shell", new DkAbilityCost(runicPower: 20) },
                { "Death Pact", new DkAbilityCost(runicPower: 40) },
            };

        public static RuneState ParseRuneState(
            int slot,
            string start,
            string duration,
            string ready,
            string runeType,
            string count)
        {
            var cooldownRemainingSeconds = ParseFloat(duration);

            return new RuneState(
                slot,
                ParseRuneType(runeType),
                ready == "1",
                ParseInt(count),
                cooldownRemainingSeconds);
        }

        public static DkAbilityCost GetKnownCost(string spellName)
        {
            DkAbilityCost cost;
            return spellName != null && KnownCosts.TryGetValue(spellName, out cost)
                ? cost
                : new DkAbilityCost();
        }

        public static bool HasReadyRunes(IEnumerable<RuneState> runes, DkAbilityCost cost)
        {
            var readyRunes = runes
                .Where(r => r.Ready && r.Count > 0)
                .ToList();

            var deathRunes = readyRunes.Count(r => r.Type == RuneType.Death);

            return HasRunesOfType(readyRunes, RuneType.Blood, cost.BloodRunes, ref deathRunes)
                && HasRunesOfType(readyRunes, RuneType.Frost, cost.FrostRunes, ref deathRunes)
                && HasRunesOfType(readyRunes, RuneType.Unholy, cost.UnholyRunes, ref deathRunes);
        }

        public static bool HasEnoughRunicPower(int runicPower, DkAbilityCost cost) =>
            runicPower >= cost.RunicPower;

        static bool HasRunesOfType(IList<RuneState> runes, RuneType type, int required, ref int deathRunes)
        {
            if (required <= 0)
                return true;

            var exact = runes.Count(r => r.Type == type);
            if (exact >= required)
                return true;

            var missing = required - exact;
            if (deathRunes < missing)
                return false;

            deathRunes -= missing;
            return true;
        }

        static RuneType ParseRuneType(string value)
        {
            var type = ParseInt(value);
            return Enum.IsDefined(typeof(RuneType), type) ? (RuneType)type : RuneType.Unknown;
        }

        static int ParseInt(string value)
        {
            int parsed;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                ? parsed
                : 0;
        }

        static float ParseFloat(string value)
        {
            float parsed;
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                ? parsed
                : 0f;
        }
    }
}
```

- [ ] **Step 5: Add the production files to the main project**

Add these compile entries to `BloogBot/BloogBot.csproj`:

```xml
<Compile Include="Game\DeathKnightResources.cs" />
<Compile Include="Game\DkAbilityCost.cs" />
<Compile Include="Game\Enums\RuneType.cs" />
<Compile Include="Game\RuneState.cs" />
```

- [ ] **Step 6: Run the focused test project build**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" .\BloogBotTests\BloogBotTests.csproj /t:Build /p:"Configuration=Debug;Platform=x86" /nologo /m:1
```

Expected: build succeeds.

### Task 2: LocalPlayer DK Lua Wrappers

**Files:**

- Modify: `BloogBot/Game/Objects/LocalPlayer.cs`

- [ ] **Step 1: Add Lua-backed properties and methods**

Add these members near existing player spell/resource helpers:

```csharp
public int RunicPower
{
    get
    {
        var result = LuaCallWithResults("{0} = tostring(UnitPower('player', 6))");
        return result.Length > 0 ? ParseLuaInt(result[0]) : 0;
    }
}

public IReadOnlyList<RuneState> GetRunes()
{
    var runes = new List<RuneState>();
    for (var slot = 1; slot <= DeathKnightResources.RuneSlotCount; slot++)
    {
        runes.Add(GetRune(slot));
    }

    return runes;
}

public RuneState GetRune(int slot)
{
    var result = LuaCallWithResults($@"
        local start, duration, ready = GetRuneCooldown({slot})
        local remaining = 0
        if start and duration and ready ~= true then
            remaining = math.max(0, start + duration - GetTime())
        end
        {{0}} = tostring(start or 0)
        {{1}} = tostring(remaining)
        {{2}} = ready and '1' or '0'
        {{3}} = tostring(GetRuneType({slot}) or 0)
        {{4}} = tostring(GetRuneCount({slot}) or 0)");

    return DeathKnightResources.ParseRuneState(
        slot,
        GetLuaResult(result, 0),
        GetLuaResult(result, 1),
        GetLuaResult(result, 2),
        GetLuaResult(result, 3),
        GetLuaResult(result, 4));
}

public bool HasReadyRune(RuneType type) =>
    GetRunes().Any(r => r.Ready && r.Count > 0 && (r.Type == type || r.Type == RuneType.Death));

public bool HasReadyRunes(DkAbilityCost cost) =>
    DeathKnightResources.HasReadyRunes(GetRunes(), cost);

public bool IsDeathKnightAbilityUsable(string spellName, DkAbilityCost cost = null)
{
    if (ClientHelper.ClientVersion != ClientVersion.WotLK || Class != Class.DeathKnight)
        return false;

    if (!KnowsSpell(spellName) || !IsSpellReady(spellName))
        return false;

    var requiredCost = cost ?? DeathKnightResources.GetKnownCost(spellName);
    if (!DeathKnightResources.HasEnoughRunicPower(RunicPower, requiredCost))
        return false;

    if (!HasReadyRunes(requiredCost))
        return false;

    var escapedName = FormatLua(spellName);
    var result = LuaCallWithResults($"local usable = IsUsableSpell('{escapedName}'); {{0}} = usable and '1' or '0'");
    return result.Length > 0 && result[0] == "1";
}
```

Add small helpers near the existing private `FormatLua` method:

```csharp
static string GetLuaResult(string[] results, int index) =>
    results.Length > index ? results[index] : string.Empty;

static int ParseLuaInt(string value)
{
    int parsed;
    return int.TryParse(value, out parsed) ? parsed : 0;
}

private static string FormatLua(string str) =>
    str.Replace("'", "\\'").Replace("\"", "\\\"");
```

- [ ] **Step 2: Build the main project**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" .\BloogBot\BloogBot.csproj /t:Build /p:"Configuration=Debug;Platform=x86" /nologo /m:1
```

Expected: build succeeds.

### Task 3: Shared Combat DK Cast Helper

**Files:**

- Modify: `BloogBot/AI/SharedStates/CombatStateBase.cs`

- [ ] **Step 1: Add a protected DK ability helper**

Add this method beside `TryUseAbility`:

```csharp
protected void TryUseDeathKnightAbility(string name, DkAbilityCost cost = null, bool condition = true, Action callback = null)
{
    if (!condition || player.IsStunned || player.IsCasting)
        return;

    var requiredCost = cost ?? DeathKnightResources.GetKnownCost(name);
    if (!player.IsDeathKnightAbilityUsable(name, requiredCost))
        return;

    player.CastSpell(name, target.Guid);
    callback?.Invoke();
}
```

This intentionally does not modify `TryCastSpellInternal` because that method is mana-gated for caster classes.

- [ ] **Step 2: Build the main project**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" .\BloogBot\BloogBot.csproj /t:Build /p:"Configuration=Debug;Platform=x86" /nologo /m:1
```

Expected: build succeeds.

- [ ] **Step 3: Run the new tests**

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" .\Bot\BloogBotTests.dll /Tests:BloogBotTests.DeathKnightResourceTests
```

Expected: all `DeathKnightResourceTests` pass.

## Phase 2: BloodDeathKnightBot MVP

### Task 4: Bot Project Skeleton

**Files:**

- Create: `BloodDeathKnightBot/BloodDeathKnightBot.csproj`
- Create: `BloodDeathKnightBot/BloodDeathKnightBot.cs`
- Create: `BloodDeathKnightBot/Properties/AssemblyInfo.cs`
- Create: `BloodDeathKnightBot/app.config`
- Modify: `BloogBot.sln`
- Modify: `BloogBot/BotLoader.cs`

The bot class should export `IBot`, use `Name => "Blood Death Knight"`, and use `FileName => "BloodDeathKnightBot.dll"`. The project GUID should be a new GUID and must be used consistently in `BloogBot.sln`.

### Task 5: Blood DK States

**Files:**

- Create: `BloodDeathKnightBot/CombatState.cs`
- Create: `BloodDeathKnightBot/BuffSelfState.cs`
- Create: `BloodDeathKnightBot/MoveToTargetState.cs`
- Create: `BloodDeathKnightBot/RestState.cs`
- Create: `BloodDeathKnightBot/PowerlevelCombatState.cs`

Blood combat priority:

1. Stop immediately if `ClientHelper.ClientVersion != ClientVersion.WotLK` or `player.Class != Class.DeathKnight`.
2. `Mind Freeze` when the target is casting or channeling and runic power is sufficient.
3. `Rune Tap` below 45 percent health if known and ready.
4. `Icebound Fortitude` below 35 percent health if runic power is sufficient.
5. `Icy Touch` if the target lacks `Frost Fever`.
6. `Plague Strike` if the target lacks `Blood Plague`.
7. `Pestilence` when fighting at least two aggressors and both diseases are present.
8. `Death Strike` below 70 percent health when Frost and Unholy runes are ready.
9. `Blood Boil` for at least three aggressors.
10. `Heart Strike` if known, otherwise `Blood Strike`.
11. `Death Coil` at 80 or more runic power.

`Death and Decay` stays out of this phase because WotLK `CastAtPosition` throws `NotImplementedException`.

### Task 6: Phase 2 Verification

Run:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" .\BloogBot.sln /t:Restore /p:"RestorePackagesConfig=true;Configuration=Debug;Platform=x86" /nologo /m:1
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" .\BloogBot.sln /t:Build /p:"Configuration=Debug;Platform=x86" /nologo /m:1
& "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" .\Bot\BloogBotTests.dll
```

Expected:

- `BloodDeathKnightBot.dll` exists under `Bot\`.
- Existing tests still pass unless the known `Bot\mmaps` environment gap is present.
- If the native VC++ FileTracker error appears, report it separately from managed DK changes.

## Phase 3: Live WotLK Verification

Create `Docs/DeathKnightWotLKSupport/live-client-verification.md` after running a WotLK 3.3.5.12340 Death Knight client. Record:

- Client executable version.
- Character level and active presence.
- Raw outputs for `UnitPower('player', 6)`.
- Raw outputs for slots 1 through 6 from `GetRuneCooldown`, `GetRuneCount`, and `GetRuneType`.
- Whether `IsSpellOnCooldown` reports rune-gated spells as unavailable.
- Whether `IsUsableSpell` returns stable values for `Death Strike`, `Rune Strike`, `Mind Freeze`, and `Icebound Fortitude`.

## Phase 4: Frost and Unholy Variants

Add `FrostDeathKnightBot` after verifying `Killing Machine` and `Rime`/`Freezing Fog` aura names in the target client.

Add `UnholyDeathKnightBot` after deciding the first pet-control surface for `Raise Dead`, ghoul availability, and `Death Pact`.

Consider `SpellRuneCost.dbc` parsing only after the hard-coded map is validated against live-client behavior.
