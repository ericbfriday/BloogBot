using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArmsWarriorBot
{
    class CombatState : CombatStateBase, IBotState
    {
        const string SunderArmorIcon = "Interface\\Icons\\Ability_Warrior_Sunder";

        const string BattleShout = "Battle Shout";
        const string Bloodrage = "Bloodrage";
        const string BloodFury = "Blood Fury";
        const string DemoralizingShout = ArmsWarriorRotation.DemoralizingShout;
        const string Execute = "Execute";
        const string Hamstring = ArmsWarriorRotation.Hamstring;
        const string HeroicStrike = ArmsWarriorRotation.HeroicStrike;
        const string MortalStrike = "Mortal Strike";
        const string Overpower = "Overpower";
        const string Rend = ArmsWarriorRotation.Rend;
        const string Retaliation = "Retaliation";
        const string SunderArmor = ArmsWarriorRotation.SunderArmor;
        const string SweepingStrikes = ArmsWarriorRotation.SweepingStrikes;
        const string ThunderClap = ArmsWarriorRotation.ThunderClap;
        const string IntimidatingShout = "Intimidating Shout";

        readonly WoWUnit target;
        readonly LocalPlayer player;

        internal CombatState(
            Stack<IBotState> botStates,
            IDependencyContainer container,
            WoWUnit target,
            bool loot = true) : base(botStates, container, target, 5, loot)
        {
            player = ObjectManager.Player;
            this.target = target;
        }

        public new void Update()
        {
            if (base.Update())
                return;

            var aggressors = ObjectManager.Aggressors.ToList();

            // Use these abilities when fighting any number of mobs.   
            TryUseAbility(Bloodrage, condition: target.HealthPercent > 50);

            TryUseAbilityById(BloodFury, 4, condition: target.HealthPercent > 80);

            TryUseAbility(Overpower, 5, player.CanOverpower);

            TryUseAbility(Execute, 15, target.HealthPercent < 20);

            // Use these abilities if you are fighting exactly one mob.
            if (aggressors.Count() == 1)
            {
                TryUseAbility(Hamstring, 10, ArmsWarriorRotation.ShouldHamstring(
                    target.CreatureType == CreatureType.Humanoid,
                    target.Name,
                    target.HealthPercent,
                    target.HasDebuff(Hamstring)));

                TryUseAbility(BattleShout, 10, !player.HasBuff(BattleShout));

                TryUseAbility(Rend, 10, ArmsWarriorRotation.ShouldRend(target.HealthPercent, target.HasDebuff(Rend), target.CreatureType));

                var sunderDebuff = target.GetDebuffs(LuaTarget.Target).FirstOrDefault(f => f.Icon == SunderArmorIcon);
                TryUseAbility(SunderArmor, 15, ArmsWarriorRotation.ShouldSunderArmor(
                    sunderDebuff?.StackCount,
                    target.Level,
                    player.Level,
                    target.Health,
                    ArmsWarriorRotation.IsSunderTarget(target.Name)));

                TryUseAbility(MortalStrike, 30);

                TryUseAbility(HeroicStrike, ArmsWarriorRotation.HeroicStrikeRageRequirement(player.Level), target.HealthPercent > 30);
            }

            // Use these abilities if you are fighting TWO OR MORE mobs at once.
            if (aggressors.Count() >= 2)
            {
                TryUseAbility(IntimidatingShout, 25, !(target.HasDebuff(IntimidatingShout) || player.HasBuff(Retaliation)) && aggressors.All(a => a.Position.DistanceTo(player.Position) < 10) && !ObjectManager.Units.Any(u => u.Guid != target.Guid && u.Position.DistanceTo(player.Position) < 10 && u.UnitReaction == UnitReaction.Neutral));

                TryUseAbility(Retaliation, 0, player.IsSpellReady(Retaliation) && ObjectManager.Aggressors.All(a => a.Position.DistanceTo(player.Position) < 10) && !ObjectManager.Aggressors.Any(a => a.HasDebuff(IntimidatingShout)));

                TryUseAbility(DemoralizingShout, 10, aggressors.Any(a => !a.HasDebuff(DemoralizingShout) && a.HealthPercent > 50) && aggressors.All(a => a.Position.DistanceTo(player.Position) < 10) && (!player.IsSpellReady(IntimidatingShout) || player.HasBuff(Retaliation)) && !ObjectManager.Units.Any(u => (u.Guid != target.Guid && u.Position.DistanceTo(player.Position) < 10 && u.UnitReaction == UnitReaction.Neutral) || u.HasDebuff(IntimidatingShout)));

                TryUseAbility(ThunderClap, 20, aggressors.Any(a => !a.HasDebuff(ThunderClap) && a.HealthPercent > 50) && aggressors.All(a => a.Position.DistanceTo(player.Position) < 8) && (!player.IsSpellReady(IntimidatingShout) || player.HasBuff(Retaliation)) && !ObjectManager.Units.Any(u => (u.Guid != target.Guid && u.Position.DistanceTo(player.Position) < 8 && u.UnitReaction == UnitReaction.Neutral) || u.HasDebuff(IntimidatingShout)));

                TryUseAbility(SweepingStrikes, 30, !player.HasBuff(SweepingStrikes) && target.HealthPercent > 30);

                if (ArmsWarriorRotation.ShouldUseSingleTargetFillersInAoe(
                    target.HasDebuff(ThunderClap),
                    player.KnowsSpell(ThunderClap),
                    target.HasDebuff(DemoralizingShout),
                    player.KnowsSpell(DemoralizingShout),
                    target.HealthPercent,
                    player.HasBuff(SweepingStrikes),
                    player.IsSpellReady(SweepingStrikes)))
                {
                    TryUseAbility(Rend, 10, ArmsWarriorRotation.ShouldRend(target.HealthPercent, target.HasDebuff(Rend), target.CreatureType));

                    TryUseAbility(MortalStrike, 30);

                    TryUseAbility(HeroicStrike, ArmsWarriorRotation.HeroicStrikeRageRequirement(player.Level), target.HealthPercent > 30);
                }
            }
        }
    }
}
