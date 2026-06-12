// Friday owns this file!

using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;

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

        // BM talent abilities — guarded by KnowsSpell rather than a fixed level,
        // since the actual level depends on how deeply the player has specced.
        const string Intimidation = "Intimidation";  // ~20 pts into BM tree
        const string BestialWrath = "Bestial Wrath"; // ~50 pts into BM tree (deep BM)

        // TBC+ abilities — additionally guarded by ClientVersion.
        const string KillCommand = "Kill Command"; // TBC: level 66

        // Aspects — used for in-combat mana management (Viper is TBC+ only).
        const string AspectOfTheHawk  = "Aspect of the Hawk";
        const string AspectOfTheViper = "Aspect of the Viper";
        const int ViperManaThresholdPct = 20; // drop to Viper below this
        const int HawkManaThresholdPct  = 80; // return to Hawk above this

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

            // Switch to Viper when low on mana; return to Hawk once recovered.
            // Viper does not exist in Vanilla — guard prevents a KnowsSpell false positive.
            if (ClientHelper.ClientVersion != ClientVersion.Vanilla && player.KnowsSpell(AspectOfTheViper))
            {
                if (player.ManaPercent < ViperManaThresholdPct && !player.HasBuff(AspectOfTheViper))
                    TryCastSpell(AspectOfTheViper, true, null, true);
                else if (player.ManaPercent >= HawkManaThresholdPct && player.HasBuff(AspectOfTheViper))
                    TryCastSpell(AspectOfTheHawk, true, null, true);
            }

            var pet = ObjectManager.Pet;
            var petAlive = pet != null && pet.HealthPercent > 0;

            // Send the pet to attack once per combat engagement.
            // We only do this once; afterwards the game's assist AI keeps it attacking.
            if (petAlive && !petOrderedToAttack)
            {
                pet.Attack();
                petOrderedToAttack = true;
            }

            // Keep the pet healthy — Mend Pet is channeled and auto-targets pet in all versions.
            if (ShouldCastMendPet(petAlive, pet?.HealthPercent ?? 0, pet?.HasBuff(MendPet) == true, player.IsSpellReady(MendPet)))
                player.LuaCall($"CastSpellByName('{MendPet}')");

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

                // Apply Hunter's Mark immediately — it increases ranged AP against the target.
                TryCastSpell(HuntersMark, 0, 34, !target.HasDebuff(HuntersMark));

                // --- BM burst cooldowns (use on cooldown) ---

                // Bestial Wrath: makes the pet go into a frenzy (immune to CC, +50% damage).
                // Requires a living pet; guarded by KnowsSpell because it's a talent.
                TryCastSpell(BestialWrath, petAlive && player.KnowsSpell(BestialWrath));

                // Rapid Fire: haste buff for the hunter — self-cast.
                // Trainable at level 26; castOnSelf ensures it lands on the hunter, not the target.
                TryCastSpell(RapidFire, player.Level >= 26, null, true);

                // Intimidation: pet stuns the target for 3 seconds.
                // Talent ability — requires living pet so the pet can deliver the stun.
                TryCastSpell(Intimidation, petAlive && player.KnowsSpell(Intimidation));

                // Kill Command (TBC/WotLK only): orders the pet to deliver a finishing move.
                // Only available in TBC+ and only when the pet is alive.
                if (ClientHelper.ClientVersion != ClientVersion.Vanilla)
                    TryCastSpell(KillCommand, petAlive && player.KnowsSpell(KillCommand));

                // --- Sustained ranged rotation ---

                // Serpent Sting: keep the DoT rolling for passive damage.
                TryCastSpell(SerpentSting, 0, 34, !target.HasDebuff(SerpentSting));

                // Multi-Shot: hits multiple targets and deals solid damage; use on cooldown.
                TryCastSpell(MultiShot, 0, 34, player.Level >= 18 && player.ManaPercent > 40);

                // Concussive Shot: slows a fleeing target so it can't escape.
                TryCastSpell(ConcussiveShot, 0, 34, target.HealthPercent < 20 && player.Level >= 8);

                // Arcane Shot: instant-cast filler; avoid casting below 50% mana
                // so we always have enough for Serpent Sting and cooldowns.
                TryCastSpell(ArcaneShot, 0, 34, player.ManaPercent > 50);
            }
            else if (inMelee)
            {
                // Target is in melee range — use Wing Clip to slow it and create distance,
                // then fall back on melee strikes until we can resume shooting.
                TryCastSpell(WingClip, 0, 5, player.Level >= 12);

                // Intimidation in melee is especially valuable: the stun buys time to back away.
                TryCastSpell(Intimidation, 0, 5, petAlive && player.KnowsSpell(Intimidation));

                TryCastSpell(RaptorStrike, 0, 5);
                TryCastSpell(MongooseBite, 0, 5, player.Level >= 16);
            }
        }

        internal static bool ShouldCastMendPet(bool petAlive, int petHealthPercent, bool petHasMendPetBuff, bool mendPetReady) =>
            petAlive &&
            petHealthPercent < 75 &&
            !petHasMendPetBuff &&
            mendPetReady;
    }
}
