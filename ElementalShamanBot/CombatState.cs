using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

namespace ElementalShamanBot
{
    class CombatState : CombatStateBase, IBotState
    {
        const string Clearcasting = "Clearcasting";
        const string EarthShock = "Earth Shock";
        const string ElementalMastery = "Elemental Mastery";
        const string FlameShock = "Flame Shock";
        const string FlametongueWeapon = ElementalShamanRotation.FlametongueWeapon;
        const string FocusedCasting = "Focused Casting";
        const string GroundingTotem = "Grounding Totem";
        const string ManaSpringTotem = "Mana Spring Totem";
        const string HealingWave = "Healing Wave";
        const string LightningBolt = "Lightning Bolt";
        const string LightningShield = "Lightning Shield";
        const string RockbiterWeapon = ElementalShamanRotation.RockbiterWeapon;
        const string SearingTotem = "Searing Totem";
        const string StoneclawTotem = "Stoneclaw Totem";
        const string StoneskinTotem = "Stoneskin Totem";
        const string TremorTotem = "Tremor Totem";

        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly LocalPlayer player;
        readonly WoWUnit target;
        Position targetLastPosition;

        internal CombatState(
            Stack<IBotState> botStates,
            IDependencyContainer container,
            WoWUnit target,
            bool loot = true) : base(botStates, container, target, 30, loot)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
            this.target = target;
        }

        public new void Update()
        {
            if (player.HealthPercent < 30 && target.HealthPercent > 50 && player.Mana >= player.GetManaCost(HealingWave))
            {
                botStates.Push(new HealSelfState(botStates, container));
                return;
            }

            if (base.Update())
                return;

            // Don't attempt spells without line of sight. Strafing is handled in CombatStateBase.
            if (TriggerLosRecovery())
                return;

            var targetIsNatureImmune = ElementalShamanRotation.IsNatureImmune(target.Name);
            var targetIsFireImmune = ElementalShamanRotation.IsFireImmune(target.Name);
            var distanceToTarget = target.Position.DistanceTo(player.Position);

            // Snapshot movement against last tick's position, then record this tick's
            // position so the comparison stays correct even when the chain returns early.
            var targetMovingTowardPlayer = TargetMovingTowardPlayer;
            targetLastPosition = target.Position;

            if (TryCastNoTargetRotationSpell(GroundingTotem, ElementalShamanRotation.ShouldGroundingTotem(
                ObjectManager.Aggressors.Any(a => a.IsCasting), target.Mana)))
                return;

            if (TryCastRotationSpell(EarthShock, 0, 20, ElementalShamanRotation.ShouldEarthShock(
                targetIsNatureImmune,
                target.IsCasting,
                target.IsChanneling,
                player.HasBuff(Clearcasting))))
                return;

            if (TryCastRotationSpell(LightningBolt, 0, 30, ElementalShamanRotation.ShouldLightningBolt(
                targetIsNatureImmune,
                targetMovingTowardPlayer,
                distanceToTarget,
                player.HasBuff(FocusedCasting),
                target.HealthPercent,
                player.HasBuff(FocusedCasting) && target.HealthPercent > 20 && Wait.For("FocusedLightningBoltDelay", 4000, true))))
                return;

            if (TryCastNoTargetRotationSpell(TremorTotem, ElementalShamanRotation.ShouldTremorTotem(
                ElementalShamanRotation.IsFearingCreature(target.Name), IsTotemNearby(TremorTotem, 29))))
                return;

            if (TryCastNoTargetRotationSpell(StoneclawTotem, ElementalShamanRotation.ShouldStoneclawTotem(ObjectManager.Aggressors.Count())))
                return;

            if (TryCastNoTargetRotationSpell(StoneskinTotem, ElementalShamanRotation.ShouldStoneskinTotem(
                target.Mana,
                IsTotemNearby(StoneclawTotem, 19) || IsTotemNearby(StoneskinTotem, 19) || IsTotemNearby(TremorTotem, 19))))
                return;

            if (TryCastNoTargetRotationSpell(SearingTotem, ElementalShamanRotation.ShouldSearingTotem(
                target.HealthPercent,
                targetIsFireImmune,
                distanceToTarget,
                IsTotemNearby(SearingTotem, 19))))
                return;

            if (TryCastNoTargetRotationSpell(ManaSpringTotem, ElementalShamanRotation.ShouldManaSpringTotem(IsTotemNearby(ManaSpringTotem, 19))))
                return;

            if (TryCastRotationSpell(FlameShock, 0, 20, ElementalShamanRotation.ShouldFlameShock(
                target.HasDebuff(FlameShock),
                target.HealthPercent,
                targetIsNatureImmune,
                targetIsFireImmune)))
                return;

            if (TryCastNoTargetRotationSpell(LightningShield, ElementalShamanRotation.ShouldLightningShield(targetIsNatureImmune, player.HasBuff(LightningShield))))
                return;

            var weaponEnchant = ElementalShamanRotation.SelectWeaponEnchant(
                player.KnowsSpell(RockbiterWeapon),
                player.KnowsSpell(FlametongueWeapon),
                player.MainhandIsEnchanted,
                targetIsFireImmune);
            if (weaponEnchant != null && TryCastNoTargetRotationSpell(weaponEnchant))
                return;

            if (TryCastNoTargetRotationSpell(ElementalMastery))
                return;
        }

        bool IsTotemNearby(string name, float range) =>
            ObjectManager.Units.Any(u =>
                u.HealthPercent > 0 &&
                u.Position.DistanceTo(player.Position) < range &&
                u.Name != null &&
                u.Name.Contains(name));

        bool TargetMovingTowardPlayer =>
            targetLastPosition != null &&
            targetLastPosition.DistanceTo(player.Position) > target.Position.DistanceTo(player.Position);

        bool TargetIsFleeing =>
            targetLastPosition != null &&
            targetLastPosition.DistanceTo(player.Position) < target.Position.DistanceTo(player.Position);
    }
}
