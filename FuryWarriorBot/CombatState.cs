using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FuryWarriorBot
{
    class CombatState : CombatStateBase, IBotState
    {
        const string BattleStance = FuryWarriorRotation.BattleStance;
        const string BerserkerStance = FuryWarriorRotation.BerserkerStance;

        const string BattleShout = "Battle Shout";
        const string BerserkerRage = "Berserker Rage";
        const string Berserking = "Berserking";
        const string BloodFury = "Blood Fury";
        const string Bloodrage = "Bloodrage";
        const string Bloodthirst = "Bloodthirst";
        const string Cleave = "Cleave";
        const string DeathWish = "Death Wish";
        const string DemoralizingShout = "Demoralizing Shout";
        const string Execute = "Execute";
        const string HeroicStrike = "Heroic Strike";
        const string Overpower = "Overpower";
        const string Pummel = "Pummel";
        const string Rend = "Rend";
        const string Retaliation = "Retaliation";
        const string Slam = "Slam";
        const string SunderArmor = "Sunder Armor";
        const string ThunderClap = "Thunder Clap";
        const string Hamstring = "Ham String";
        const string IntimidatingShout = "Intimidating Shout";
        const string Whirlwind = "Whirlwind";

        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly WoWUnit target;
        readonly LocalPlayer player;

        bool slamReady;
        int slamReadyStartTime;

        bool losBackpedaling;
        int losBackpedalStartTime;

        bool backpedaling;
        int backpedalStartTime;
        int backpedalDuration;

        bool initialized;
        int combatStateEnterTime = Environment.TickCount;

        internal CombatState(
            Stack<IBotState> botStates,
            IDependencyContainer container,
            WoWUnit target,
            bool loot = true) : base(botStates, container, target, 5, loot)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
            this.target = target;

            WoWEventHandler.OnSlamReady += OnSlamReadyCallback;
        }

        ~CombatState()
        {
            WoWEventHandler.OnSlamReady -= OnSlamReadyCallback;
        }

        public new void Update()
        {
            if (Environment.TickCount - backpedalStartTime > backpedalDuration)
            {
                player.StopMovement(ControlBits.Back);
                // player.StopMovement(ControlBits.StrafeLeft);
                // player.StopMovement(ControlBits.Right);
                backpedaling = false;
            }

            if (backpedaling)
                return;

            if (Environment.TickCount - slamReadyStartTime > 250)
            {
                slamReady = false;
            }

            //if (!FacingAllTargets && ObjectManager.Aggressors.Count() >= 2 && AggressorsInMelee)
            //{
            //    WalkBack(50);
            //    return;
            //}

            if (base.Update())
                return;

            var currentStance = player.CurrentStance;
            var spellcastingAggressors = ObjectManager.Aggressors
                .Where(a => a.Mana > 0);
            // Use these abilities when fighting any number of mobs.
            if (TryUseRotationAbility(BerserkerStance, condition: FuryWarriorRotation.ShouldEnterBerserkerStance(
                player.Level,
                currentStance,
                target.HasDebuff(Rend),
                target.HealthPercent,
                target.CreatureType))) return;

            if (TryUseRotationAbility(Pummel, 10, condition: FuryWarriorRotation.ShouldPummel(currentStance, target.Mana, target.IsCasting, target.IsChanneling))) return;

            // TryUseAbility(Rend, 10, (currentStance == BattleStance && target.HealthPercent > 50 && !target.HasDebuff(Rend) && (target.CreatureType != CreatureType.Elemental && target.CreatureType != CreatureType.Undead)));

            if (TryUseRotationAbility(DeathWish, 10, condition: FuryWarriorRotation.ShouldUseDeathWish(player.IsSpellReady(DeathWish), target.HealthPercent))) return;

            if (TryUseRotationAbility(BattleShout, 10, condition: FuryWarriorRotation.ShouldUseBattleShout(player.HasBuff(BattleShout)))) return;

            if (TryUseRotationAbilityById(BloodFury, 4, 0, FuryWarriorRotation.ShouldUseBloodFury(target.HealthPercent))) return;

            if (TryUseRotationAbility(Bloodrage, condition: FuryWarriorRotation.ShouldUseBloodrage(target.HealthPercent))) return;

            if (TryUseRotationAbility(Execute, 15, condition: FuryWarriorRotation.ShouldExecute(target.HealthPercent))) return;

            if (TryUseRotationAbility(BerserkerRage, condition: FuryWarriorRotation.ShouldUseBerserkerRage(target.HealthPercent, currentStance))) return;

            if (TryUseRotationAbility(Overpower, 5, condition: FuryWarriorRotation.ShouldOverpower(currentStance, player.CanOverpower))) return;

            // Use these abilities if you are fighting TWO OR MORE mobs at once.
            if (ObjectManager.Aggressors.Count() >= 2)
            {
                if (TryUseRotationAbility(IntimidatingShout, 25, condition: FuryWarriorRotation.ShouldUseIntimidatingShout(
                    target.HasDebuff(IntimidatingShout),
                    player.HasBuff(Retaliation),
                    ObjectManager.Aggressors.All(a => a.Position.DistanceTo(player.Position) < 10)))) return;

                if (TryUseRotationAbility(DemoralizingShout, 10, condition: FuryWarriorRotation.ShouldUseDemoralizingShout(target.HasDebuff(DemoralizingShout)))) return;

                // TryUseAbility(Cleave, 20, target.HealthPercent > 20 && FacingAllTargets);

                if (TryUseRotationAbility(Whirlwind, 25, condition: FuryWarriorRotation.ShouldWhirlwind(
                    currentStance,
                    target.HealthPercent,
                    target.HasDebuff(IntimidatingShout),
                    AggressorsInMelee))) return;

                if (TryUseRotationAbility(Retaliation, 0, condition: FuryWarriorRotation.ShouldUseRetaliation(
                    player.IsSpellReady(Retaliation),
                    spellcastingAggressors.Count(),
                    currentStance,
                    FacingAllTargets,
                    ObjectManager.Aggressors.Any(a => a.HasDebuff(IntimidatingShout))))) return;
            }

            // Use these abilities if you are fighting only one mob at a time, or multiple and one or more are not in melee range.
            if (ObjectManager.Aggressors.Count() >= 1 || (ObjectManager.Aggressors.Count() > 1 && !AggressorsInMelee))
            {
                if (TryUseRotationAbility(Slam, 15, condition: FuryWarriorRotation.ShouldSlam(target.HealthPercent, slamReady), callback: SlamCallback)) return;

                // TryUseAbility(Rend, 10, (currentStance == BattleStance && target.HealthPercent > 50 && !target.HasDebuff(Rend) && (target.CreatureType != CreatureType.Elemental && target.CreatureType != CreatureType.Undead)));

                if (TryUseRotationAbility(Bloodthirst, 30)) return;

                if (TryUseRotationAbility(Hamstring, 10, condition: FuryWarriorRotation.ShouldHamstring(target.CreatureType, target.HasDebuff(Hamstring)))) return;

                if (TryUseRotationAbility(HeroicStrike, FuryWarriorRotation.HeroicStrikeRageRequirement(player.Level), condition: FuryWarriorRotation.ShouldHeroicStrike(target.HealthPercent))) return;

                if (TryUseRotationAbility(Execute, 15, condition: FuryWarriorRotation.ShouldExecute(target.HealthPercent))) return;

                if (TryUseRotationAbility(SunderArmor, 15, condition: FuryWarriorRotation.ShouldSunderArmor(target.HealthPercent, target.HasDebuff(SunderArmor)))) return;
            }
        }

        void OnSlamReadyCallback(object sender, EventArgs e)
        {
            OnSlamReady();
        }

        void OnSlamReady()
        {
            slamReady = true;
            slamReadyStartTime = Environment.TickCount;
        }

        void SlamCallback()
        {
            slamReady = false;
        }

        // Check to see if toon is facing all the targets and they are within melee, used to determine if player should walkbackwards to reposition targets in front of mob.
        bool FacingAllTargets
        {
            get
            {
                return ObjectManager.Aggressors.All(a => a.Position.DistanceTo(player.Position) < 7 && player.IsInCleave(a.Position));
            }
        }

        // Check to see if toon is with melee distance of mobs.  This is used to determine if player should use single mob rotation or multi-mob rotation.
        bool AggressorsInMelee
        {
            get
            {
                return ObjectManager.Aggressors.All(a => a.Position.DistanceTo(player.Position) < 7);
            }
        }

        void WalkBack(int milleseconds)
        {
            backpedaling = true;
            backpedalStartTime = Environment.TickCount;
            backpedalDuration = milleseconds;
            player.StartMovement(ControlBits.Back);
            // player.StartMovement(ControlBits.StrafeLeft);
            // player.StartMovement(ControlBits.Right);
        }
    }
}
