# Codebase Findings

## Existing Support

- `BloogBot/Game/Enums/Class.cs:10` already defines `DeathKnight = 6`.
- `BloogBot/ClientHelper.cs:13-18` recognizes WotLK by executable version string `3, 3, 5, 12340`.
- `BloogBot/Game/Functions.cs` selects `WotLKGameFunctionHandler` when the client version is WotLK.
- `BloogBot/Game/WotLKGameFunctionHandler.cs` already supports casting by spell ID, Lua calls/results, spell cooldown checks, targeting, movement, aura reads, and DBC row lookup.
- `BloogBot/Game/WowDb.cs:234` includes `ClientDb.SpellRuneCost`, which is relevant for DK rune/runic-power costs but not currently used.
- `BloogBot/Game/Objects/WoWUnit.cs` already exposes WotLK auras through `GetAuraCount` and `GetAuraPointer`, so disease/buff checks such as `Frost Fever`, `Blood Plague`, `Horn of Winter`, and presences can likely use `HasBuff` and `HasDebuff`.

## Missing Resource Model

Current resource properties stop at mana, rage, and energy:

- `WoWUnit.Mana`
- `WoWUnit.Rage`
- `WoWUnit.Energy`

There is no `RunicPower`, rune slot state, rune type, or Death Rune support. This matters because DK rotations are rune scheduling problems, not just cooldown checks.

The current helper behavior is not DK-ready:

- `CombatStateBase.TryCastSpellInternal` requires `player.Mana >= player.GetManaCost(name)`.
- `CombatStateBase.TryUseAbility` only assigns `playerResource` for Warrior and Rogue.
- `CombatStateBase.TryUseAbilityById` is Warrior-rage-specific.

The safest approach is to add DK-specific helpers rather than force DK abilities through mana/rage/energy helpers.

## Bot Project Pattern

The repo uses one C# project per class/spec bot. A new DK bot should follow the same shape:

- `DeathKnightVariantBot.cs` exports `IBot` with `[Export(typeof(IBot))]`.
- `CombatState.cs` derives from `CombatStateBase`.
- `MoveToTargetState.cs` derives from `MoveToTargetStateBase`.
- `RestState.cs` implements food/rest behavior.
- `PowerlevelCombatState.cs` exists even if minimal at first.
- Optional `BuffSelfState.cs` handles Horn of Winter/presence/runeforge reminders.

Each existing bot project outputs to `..\Bot\`, which is why the repo instructions require `/m:1`.

## Loader and Solution Changes Needed

Adding a new bot requires:

- Add a new C# project to `BloogBot.sln`.
- Add the new bot DLL name to `BloogBot/BotLoader.cs:41`.
- Add project references to `BloogBot/BloogBot.csproj`.
- Ensure the bot project outputs `Debug|x86` to `..\Bot\`.

If the implementation adds all three variants, each DLL must be listed in `BotLoader`.

## WotLK-Only Guard

Death Knights only exist on WotLK in this codebase's supported client set. Add a guard in the DK bot path:

- `ClientHelper.ClientVersion == ClientVersion.WotLK`
- `ObjectManager.Player.Class == Class.DeathKnight`

The guard should fail clearly instead of allowing a Vanilla/TBC client or another class to execute DK-specific resource reads.

## Shared DK Helper Candidates

Add shared code under `BloogBot/Game/Objects/LocalPlayer.cs` or a small DK-specific helper class:

- `RunicPower`
- `MaxRunicPower`
- `GetRunes()`
- `HasReadyRune(RuneType type)`
- `HasReadyRunes(DkAbilityCost cost)`
- `IsDeathKnightAbilityUsable(string name, DkAbilityCost cost)`

Keep the first pass simple: use hard-coded costs for the small set of DK abilities the bot will cast, then consider reading `SpellRuneCost.dbc` once the behavior is proven.

## Local Search Evidence

Useful searches run for this research:

```powershell
rg -n "DeathKnight|LocalPlayerClass|WoWUnit_ManaOffset|WoWUnit_RageOffset|WoWUnit_EnergyOffset|TryUseAbility\(|TryCastSpellInternal|GetManaCost|IsSpellReady|CastSpellById|Runic|Rune" BloogBot -g "*.cs"
rg -n "botPaths|ReloadBots|FileName =>|AssemblyName|OutputPath|Project\(" BloogBot\BotLoader.cs BloogBot.sln
rg -n "const string|TryCastSpell|TryUseAbility|TryUseAbilityById|HealthPercent|ManaPercent|Energy|Rage|ComboPoints|HasBuff|HasDebuff|KnowsSpell|CreatureType|Aggressors" -g "*CombatState.cs" -g "*MoveToTargetState.cs" -g "*RestState.cs"
```
