using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

namespace ProtectionWarriorBot
{
    class CombatState : CombatStateBase, IBotState
    {
        const string BattleShout = "Battle Shout";
        const string Berserking = "Berserking";
        const string Bloodrage = "Bloodrage";
        const string ConcussionBlow = "Concussion Blow";
        const string DemoralizingShout = ProtectionWarriorRotation.DemoralizingShout;
        const string Execute = "Execute";
        const string HeroicStrike = "Heroic Strike";
        const string LastStand = "Last Stand";
        const string Overpower = "Overpower";
        const string Rend = "Rend";
        const string Retaliation = "Retaliation";
        const string ShieldBash = "Shield Bash";
        const string ShieldSlam = "Shield Slam";
        const string ThunderClap = ProtectionWarriorRotation.ThunderClap;

        readonly WoWUnit target;
        readonly LocalPlayer player;

        internal CombatState(
            Stack<IBotState> botStates,
            IDependencyContainer container,
            WoWUnit target,
            bool loot = true) : base(botStates, container, target, 3, loot)
        {
            player = ObjectManager.Player;
            this.target = target;
        }

        public new void Update()
        {
            if (base.Update())
                return;

            if (TryUseRotationAbility(Bloodrage, condition: target.HealthPercent > 50)) return;

            var aggressorCount = ObjectManager.Aggressors.Count();
            var targetHasDemoralizingShout = target.HasDebuff(DemoralizingShout);
            var targetHasThunderClap = target.HasDebuff(ThunderClap);
            var allAggressorsWithin10Yards = ObjectManager.Aggressors.All(a => a.Position.DistanceTo(player.Position) < 10);

            if (TryUseRotationAbility(Retaliation, condition: ProtectionWarriorRotation.ShouldUseRetaliation(aggressorCount))) return;

            if (ProtectionWarriorRotation.ShouldUseMultiTargetAbilities(aggressorCount, targetHasDemoralizingShout, targetHasThunderClap))
            {
                if (TryUseRotationAbility(DemoralizingShout, 10, condition: ProtectionWarriorRotation.ShouldUseDemoralizingShout(targetHasDemoralizingShout, allAggressorsWithin10Yards))) return;

                if (TryUseRotationAbility(ThunderClap, 20, condition: ProtectionWarriorRotation.ShouldUseThunderClap(targetHasThunderClap, allAggressorsWithin10Yards))) return;
            }
            else if (ProtectionWarriorRotation.ShouldUseSingleTargetAbilities(aggressorCount, targetHasDemoralizingShout, targetHasThunderClap))
            {
                if (TryUseRotationAbility(LastStand, condition: ProtectionWarriorRotation.ShouldUseLastStand(player.HealthPercent))) return;

                if (TryUseRotationAbility(Overpower, 5, condition: player.CanOverpower)) return;

                if (TryUseRotationAbility(Berserking, 5, condition: player.HealthPercent < 30)) return;

                if (TryUseRotationAbility(ShieldBash, 10, condition: ProtectionWarriorRotation.ShouldUseShieldBash(target.IsCasting, target.Mana))) return;

                if (TryUseRotationAbility(Rend, 10, condition: ProtectionWarriorRotation.ShouldRend(target.HealthPercent, target.HasDebuff(Rend), target.CreatureType))) return;

                if (TryUseRotationAbility(BattleShout, 10, condition: !player.HasBuff(BattleShout))) return;

                if (TryUseRotationAbility(ConcussionBlow, 15, condition: !target.IsStunned && target.HealthPercent > 40)) return;

                if (TryUseRotationAbility(Execute, 20, condition: target.HealthPercent < 20)) return;

                if (TryUseRotationAbility(ShieldSlam, 20, condition: ProtectionWarriorRotation.ShouldUseShieldSlam(target.HealthPercent))) return;

                if (TryUseRotationAbility(HeroicStrike, 40, condition: target.HealthPercent > 40 && !player.IsCasting)) return;
            }
        }
    }
}
