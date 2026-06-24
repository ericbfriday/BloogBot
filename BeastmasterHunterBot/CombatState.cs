// Friday owns this file!

using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

namespace BeastMasterHunterBot
{
    class CombatState : CombatStateBase, IBotState
    {
        const string AutoAttackLuaScript = "if IsCurrentAction('84') == nil then CastSpellByName('Attack') end";
        const string GunLuaScript = "if IsAutoRepeatAction(11) == nil then CastSpellByName('Auto Shot') end";

        // Trainable abilities — level gates enforced at training time, but we double-check level
        // to avoid trying to cast spells the bot hasn't yet visited a trainer for.
        const string HuntersMark = "Hunter's Mark";      // Level 1
        const string RaptorStrike = "Raptor Strike";     // Level 1
        const string SerpentSting = "Serpent Sting";     // Level 4
        const string ArcaneShot = "Arcane Shot";         // Level 8
        const string ConcussiveShot = "Concussive Shot"; // Level 8
        const string MendPet = "Mend Pet";               // Level 12
        const string WingClip = "Wing Clip";             // Level 12
        const string MongooseBite = "Mongoose Bite";     // Level 16
        const string MultiShot = "Multi-Shot";           // Level 18
        const string RapidFire = "Rapid Fire";           // Level 26
        const string AimedShot = BeastmasterHunterRotation.AimedShot;   // Level 28 talent/rank path
        const string SteadyShot = BeastmasterHunterRotation.SteadyShot; // Level 50
        const string KillShot = BeastmasterHunterRotation.KillShot;     // WotLK: level 71
        const string Deterrence = "Deterrence";          // Level 60

        // BM talent abilities — guarded by KnowsSpell rather than a fixed level,
        // since the actual level depends on how deeply the player has specced.
        const string Intimidation = "Intimidation";  // ~20 pts into BM tree
        const string BestialWrath = "Bestial Wrath"; // ~50 pts into BM tree (deep BM)

        // TBC+ abilities — additionally guarded by ClientVersion.
        const string KillCommand = "Kill Command"; // TBC: level 66
        const string Misdirection = "Misdirection"; // TBC: level 70

        // Aspects — used for in-combat mana management (Viper is TBC+ only).
        const string AspectOfTheDragonhawk = BeastmasterHunterRotation.AspectOfTheDragonhawk;
        const string AspectOfTheHawk = BeastmasterHunterRotation.AspectOfTheHawk;
        const string AspectOfTheViper = BeastmasterHunterRotation.AspectOfTheViper;

        readonly WoWUnit target;
        readonly LocalPlayer player;
        bool petOrderedToAttack;

        internal CombatState(
            Stack<IBotState> botStates,
            IDependencyContainer container,
            WoWUnit target,
            bool loot = true) : base(botStates, container, target, 28, loot)
        {
            player = ObjectManager.Player;
            this.target = target;
        }

        public new void Update()
        {
            if (base.Update())
                return;

            var aspect = BeastmasterHunterRotation.SelectAspect(
                ClientHelper.ClientVersion,
                player.KnowsSpell(AspectOfTheViper),
                player.HasBuff(AspectOfTheViper),
                player.KnowsSpell(AspectOfTheDragonhawk),
                player.HasBuff(AspectOfTheDragonhawk),
                player.KnowsSpell(AspectOfTheHawk),
                player.HasBuff(AspectOfTheHawk),
                player.ManaPercent);
            if (aspect != null && TryCastRotationSpell(aspect, castOnSelf: true))
            {
                return;
            }

            var pet = ObjectManager.Pet;
            var petAlive = pet != null && pet.HealthPercent > 0;
            var aggressorCount = ObjectManager.Aggressors.Count();
            var targetIsTargetingPlayer = target.TargetGuid == player.Guid;

            // Send the pet to attack once per combat engagement.
            // We only do this once; afterwards the game's assist AI keeps it attacking.
            if (petAlive && !petOrderedToAttack)
            {
                pet.Attack();
                petOrderedToAttack = true;
            }

            var playerKnowsMendPet = player.KnowsSpell(MendPet);
            var mendPetManaCost = playerKnowsMendPet ? player.GetManaCost(MendPet) : int.MaxValue;
            if (ShouldCastMendPet(
                petAlive,
                pet?.HealthPercent ?? 0,
                pet?.HasBuff(MendPet) == true,
                playerKnowsMendPet,
                player.IsSpellReady(MendPet),
                player.Mana,
                mendPetManaCost))
            {
                player.LuaCall($"CastSpellByName('{MendPet}')");
                return;
            }

            var gun = Inventory.GetEquippedItem(EquipSlot.Ranged);
            var distanceToTarget = player.Position.DistanceTo(target.Position);
            var canUseRanged = gun != null && distanceToTarget > 5 && distanceToTarget < 34;
            var inMelee = distanceToTarget <= 5;

            if (gun == null)
            {
                // No ranged weapon equipped — fall back to pure melee.
                player.LuaCall(AutoAttackLuaScript);
                TryCastSpell(RaptorStrike, 0, 5);
                TryCastSpell(MongooseBite, 0, 5, player.Level >= 16);
                return;
            }

            if (canUseRanged)
            {
                if (TriggerLosRecovery())
                    return;

                // Auto Shot must always be toggled on during ranged combat.
                player.LuaCall(GunLuaScript);

                if (TryCastRotationSpell(HuntersMark, 0, 34, BeastmasterHunterRotation.ShouldApplyHuntersMark(
                    target.HasDebuff(HuntersMark),
                    target.HasBuff(HuntersMark),
                    target.HealthPercent,
                    player.ManaPercent)))
                    return;

                if (TryCastRotationSpell(Deterrence, BeastmasterHunterRotation.ShouldUseDefensiveCooldown(
                    targetIsTargetingPlayer,
                    player.HealthPercent,
                    player.HasBuff(Deterrence)), castOnSelf: true))
                    return;

                // --- BM burst cooldowns (use on cooldown) ---

                if (TryCastRotationSpell(BestialWrath, BeastmasterHunterRotation.ShouldUsePetOffensiveCooldown(
                    petAlive,
                    pet?.HasBuff(BestialWrath) == true,
                    target.HealthPercent,
                    aggressorCount)))
                    return;

                if (TryCastRotationSpell(RapidFire, BeastmasterHunterRotation.ShouldUseHunterOffensiveCooldown(
                    player.HasBuff(RapidFire),
                    target.HealthPercent,
                    aggressorCount), castOnSelf: true))
                    return;

                if (TryCastRotationSpell(Intimidation, 0, 34, BeastmasterHunterRotation.ShouldUsePetOffensiveCooldown(
                    petAlive,
                    TargetHasAura(Intimidation),
                    target.HealthPercent,
                    aggressorCount)))
                    return;

                if (ClientHelper.ClientVersion != ClientVersion.Vanilla &&
                    TryCastRotationSpell(KillCommand, BeastmasterHunterRotation.ShouldUsePetOffensiveCooldown(
                        petAlive,
                        pet?.HasBuff(KillCommand) == true,
                        target.HealthPercent,
                        aggressorCount)))
                    return;

                if (TryCastSpellOnPet(Misdirection, pet, BeastmasterHunterRotation.ShouldUseMisdirection(
                    petAlive,
                    targetIsTargetingPlayer,
                    target.HealthPercent,
                    aggressorCount)))
                    return;

                // --- Sustained ranged rotation ---

                if (TryCastRotationSpell(SerpentSting, 0, 34, BeastmasterHunterRotation.ShouldApplySerpentSting(
                    target.HasDebuff(SerpentSting),
                    target.HasBuff(SerpentSting),
                    target.HealthPercent,
                    player.ManaPercent)))
                    return;

                // Concussive Shot: slows a fleeing target so it can't escape.
                if (TryCastRotationSpell(ConcussiveShot, 0, 34, target.HealthPercent < 20 && !TargetHasAura(ConcussiveShot)))
                    return;

                var selectedShot = BeastmasterHunterRotation.SelectRangedShot(
                    ClientHelper.ClientVersion,
                    target.HealthPercent,
                    player.ManaPercent,
                    CanCastRotationSpell(KillShot, 0, 34, ClientHelper.ClientVersion == ClientVersion.WotLK),
                    CanCastRotationSpell(AimedShot, 0, 34, ClientHelper.ClientVersion == ClientVersion.WotLK),
                    CanCastRotationSpell(MultiShot, 0, 34, true),
                    CanCastRotationSpell(SteadyShot, 0, 34, ClientHelper.ClientVersion != ClientVersion.Vanilla),
                    CanCastRotationSpell(ArcaneShot, 0, 34, true),
                    player.IsMoving);
                if (selectedShot != null && TryCastRotationSpell(selectedShot, 0, 34))
                    return;
            }
            else if (inMelee)
            {
                // Target is in melee range — use Wing Clip to slow it and create distance,
                // then fall back on melee strikes until we can resume shooting.
                if (TryCastRotationSpell(Deterrence, BeastmasterHunterRotation.ShouldUseDefensiveCooldown(
                    targetIsTargetingPlayer,
                    player.HealthPercent,
                    player.HasBuff(Deterrence)), castOnSelf: true))
                    return;

                TryCastSpell(WingClip, 0, 5, !TargetHasAura(WingClip));

                // Intimidation in melee is especially valuable: the stun buys time to back away.
                TryCastSpell(Intimidation, 0, 5, BeastmasterHunterRotation.ShouldUsePetOffensiveCooldown(
                    petAlive,
                    TargetHasAura(Intimidation),
                    target.HealthPercent,
                    aggressorCount));

                TryCastSpell(RaptorStrike, 0, 5);
                TryCastSpell(MongooseBite, 0, 5, player.Level >= 16);
            }
        }

        bool TargetHasAura(string auraName) => target.HasDebuff(auraName) || target.HasBuff(auraName);

        bool TryCastSpellOnPet(string name, LocalPet pet, bool condition)
        {
            if (pet == null || !condition || player.IsStunned || player.IsCasting || player.IsChanneling)
                return false;

            if (!player.KnowsSpell(name) || !player.IsSpellReady(name) || player.Mana < player.GetManaCost(name))
                return false;

            if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                player.LuaCall($"CastSpellByName('{name}')");
            else
                player.CastSpell(name, pet.Guid);

            return true;
        }

        internal static bool ShouldCastMendPet(
            bool petAlive,
            int petHealthPercent,
            bool petHasMendPetBuff,
            bool playerKnowsMendPet,
            bool mendPetReady,
            int playerMana,
            int mendPetManaCost) =>
            petAlive &&
            petHealthPercent < 75 &&
            !petHasMendPetBuff &&
            playerKnowsMendPet &&
            mendPetReady &&
            playerMana >= mendPetManaCost;
    }
}
