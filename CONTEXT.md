# BloogBot Domain Glossary

Terms used consistently across the codebase and in architecture discussions.

- **Bot profile** — a per-class plugin project (e.g. `FrostMageBot`, `ArmsWarriorBot`)
  loaded by the core engine. Each profile supplies its states (combat, rest, buff,
  movement) for the shared state machine.
- **State** — an `IBotState` on the bot state stack. The top state's `Update()` runs
  every tick; states push/pop to transition.
- **Rotation** — a bot profile's pure decision module (`<Class>Rotation` /
  `<Class>CombatRotation`, e.g. `EnhancementShamanRotation`). Static functions over
  plain values (health %, known spells, immunities, combo points) that decide *what*
  to cast; no game-state reads, no side effects. The rotation is the test surface:
  every bot profile has a `<Class>RotationTests` suite of mock-free unit tests in
  `BloogBotTests`. CombatStates gather game state, ask the rotation, then act.
- **Rotation-casting helpers** — the protected `TryCastRotationSpell` /
  `TryCastNoTargetRotationSpell` / `CanCastRotationSpell` members on
  `CombatStateBase`. They check known/ready/mana/range/stunned once, so rotations can
  be written as `if (TryCastRotationSpell(...)) return;` priority chains. Bot
  profiles must not re-implement casting plumbing.
- **Rest** — recovering health/mana out of combat. `RestStateBase`
  (`BloogBot/AI/SharedStates`) owns the plumbing: configured food/drink lookup,
  in-combat detection, the stand-up/pop exit, eat/drink mechanics, and the restocking
  errand. Per-bot `RestState` subclasses hold only class policy: thresholds,
  self-heals, pet upkeep, form management.
- **Restocking errand** — the trip to the current hotspot's innkeeper to sell junk
  and buy food/drink back up to stack targets (`TryRunRestockErrands`). The common
  policy is *only when out* (a tracked consumable hit zero); some profiles restock
  whenever below target.
- **Hotspot** — a configured grinding area with target levels, waypoints, an
  innkeeper, and optionally a travel path.
- **Aggressor** — a unit currently hostile and engaged with the player (the working
  definition of "in combat" varies per profile; see each state's `InCombat`).
