# Sources

## External Sources

- Icy Veins, Death Knight Class Overview for WotLK Classic: https://www.icy-veins.com/wotlk-classic/death-knight-class-overview
  - Used for rune/runic-power overview, presences, and Blood/Frost/Unholy specialization roles.
- Wowhead, Death Knight Class Guides and Overview in Wrath of the Lich King Classic: https://www.wowhead.com/wotlk/guide/classes/death-knight/overview
  - Used for class basics, resources, armor/weapons, roles, runeforging, presences, leveling/spec positioning, buffs, and utility.
- Icy Veins, Blood Death Knight Leveling Guide: https://www.icy-veins.com/wotlk-classic/blood-death-knight-leveling-guide
  - Used for Blood leveling priorities: diseases, Pestilence, Heart Strike/Blood Boil, Death Strike, Death and Decay, auto-attacking.
- Icy Veins, Frost Death Knight Leveling Guide: https://www.icy-veins.com/wotlk-classic/frost-death-knight-leveling-guide
  - Used for Frost leveling priorities: diseases, Pestilence, Blood Strike/Blood Boil, Obliterate, Howling Blast, Frost Strike, Death and Decay.
- Icy Veins, Unholy Death Knight Leveling Guide: https://www.icy-veins.com/wotlk-classic/unholy-death-knight-leveling-guide
  - Used for Unholy leveling priorities: diseases, Pestilence, Blood Strike/Blood Boil, Scourge Strike, Icy Touch, Death Coil, Death and Decay.
- Wowhead, Frost DK DPS Rotation/Cooldowns/Abilities: https://www.wowhead.com/wotlk/guide/classes/death-knight/frost/dps-rotation-cooldowns-abilities-pve
  - Used for Frost rotation priorities, Death Grip utility, Obliterate/Frost Strike roles, and Empower Rune Weapon context.
- Wowhead, Unholy DK DPS Rotation/Cooldowns/Abilities: https://www.wowhead.com/wotlk/guide/classes/death-knight/unholy/dps-rotation-cooldowns-abilities-pve
  - Used for Unholy disease, Death and Decay, Gargoyle, Ghoul Frenzy, and Scourge Strike notes.
- Wowhead, Blood DK Tank Rotation/Cooldowns/Abilities: https://www.wowhead.com/wotlk/guide/classes/death-knight/blood/tank-rotation-cooldowns-abilities-pve
  - Used for Blood priorities, Death Strike sustain, Icy Touch threat, Blood Boil AoE, defensive cooldowns, and opener references.
- Wowpedia, `GetRuneCooldown`: https://wowpedia.fandom.com/wiki/API_GetRuneCooldown
  - Used for Lua signature and return values for rune cooldown and readiness.
- Wowpedia, `GetRuneCount`: https://wowpedia.fandom.com/wiki/API_GetRuneCount
  - Used for Lua signature and return value for ready rune count by slot.
- Wowpedia, `GetRuneType`: https://wowpedia.fandom.com/wiki/API_GetRuneType
  - Used for rune type values: Blood, Chromatic/Unholy, Frost, Death.
- Wowpedia, `UnitPower`: https://wowpedia.fandom.com/wiki/API_UnitPower
  - Used for runic power resource ID and current power access.
- Wowpedia, `UnitPowerType`: https://wowpedia.fandom.com/wiki/API_UnitPowerType
  - Used to verify `RUNES` and `RUNIC_POWER` power tokens.
- WoTLK Modding Wiki, `SpellRuneCost`: https://wotlkdev.github.io/wiki/dbc/SpellRuneCost
  - Used for WotLK DBC columns relevant to rune and runic-power costs.
- Joana's World, WOTLK Death Knight Abilities and Talents Guide: https://www.joanasworld.com/deathknight-abilities.php
  - Used as a secondary source for early DK ability progression, starting combat sequence, runeforging, and practical leveling notes.

## Local Code Sources

- `BloogBot/Game/Enums/Class.cs`
  - Confirms `DeathKnight = 6` already exists.
- `BloogBot/ClientHelper.cs`
  - Confirms WotLK client detection by file version `3, 3, 5, 12340`.
- `BloogBot/Game/MemoryAddresses.cs`
  - Confirms WotLK function pointers, local class address, and current power offsets.
- `BloogBot/Game/WotLKGameFunctionHandler.cs`
  - Confirms reusable WotLK spell casting, Lua, cooldown, aura, targeting, movement, and DBC row methods.
- `BloogBot/Game/Functions.cs`
  - Confirms client-version-specific function handler selection.
- `BloogBot/Game/Objects/WoWUnit.cs`
  - Confirms current resource properties and WotLK aura/debuff access.
- `BloogBot/Game/Objects/LocalPlayer.cs`
  - Confirms spellbook refresh, spell lookup, spell readiness, mana cost lookup, and target-guid spell casting.
- `BloogBot/AI/SharedStates/CombatStateBase.cs`
  - Confirms current combat helper assumptions around mana, Warrior rage, Rogue energy, and non-Vanilla `StartAttack`.
- `BloogBot/BotLoader.cs`
  - Confirms bot DLL loading is hard-coded.
- Existing bot projects such as `FuryWarriorBot`, `CombatRogueBot`, and `RetributionPaladinBot`
  - Used as templates for bot project shape, state names, dependency container wiring, and output path behavior.

## Source Caveat

Most public gameplay guides are for WotLK Classic. They are appropriate for an initial ability/rotation model, but BloogBot targets the original 3.3.5.12340 client. Any resource behavior, Lua API return shape, spell names, proc aura names, and spell cost assumptions must be verified in that client before implementation is considered complete.
