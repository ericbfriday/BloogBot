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

            TryCastSpell(GroundingTotem, 0, int.MaxValue, ObjectManager.Aggressors.Any(a => a.IsCasting && target.Mana > 0));

            TryCastSpell(EarthShock, 0, 20, ElementalShamanRotation.ShouldEarthShock(
                ElementalShamanRotation.IsNatureImmune(target.Name),
                target.IsCasting,
                target.IsChanneling,
                player.HasBuff(Clearcasting)));

            TryCastSpell(LightningBolt, 0, 30, ElementalShamanRotation.ShouldLightningBolt(
                ElementalShamanRotation.IsNatureImmune(target.Name),
                TargetMovingTowardPlayer,
                target.Position.DistanceTo(player.Position),
                player.HasBuff(FocusedCasting),
                target.HealthPercent,
                player.HasBuff(FocusedCasting) && target.HealthPercent > 20 && Wait.For("FocusedLightningBoltDelay", 4000, true)));

            TryCastSpell(TremorTotem, 0, int.MaxValue, ElementalShamanRotation.IsFearingCreature(target.Name) && !ObjectManager.Units.Any(u => u.Position.DistanceTo(player.Position) < 29 && u.HealthPercent > 0 && u.Name.Contains(TremorTotem)));

            TryCastSpell(StoneclawTotem, 0, int.MaxValue, ObjectManager.Aggressors.Count() > 1);

            TryCastSpell(StoneskinTotem, 0, int.MaxValue, target.Mana == 0 && !ObjectManager.Units.Any(u => u.Position.DistanceTo(player.Position) < 19 && u.HealthPercent > 0 && (u.Name.Contains(StoneclawTotem) || u.Name.Contains(StoneskinTotem) || u.Name.Contains(TremorTotem))));

            TryCastSpell(SearingTotem, 0, int.MaxValue, target.HealthPercent > 70 && !ElementalShamanRotation.IsFireImmune(target.Name) && target.Position.DistanceTo(player.Position) < 20 && !ObjectManager.Units.Any(u => u.Position.DistanceTo(player.Position) < 19 && u.HealthPercent > 0 && u.Name.Contains(SearingTotem)));

            TryCastSpell(ManaSpringTotem, 0, int.MaxValue, !ObjectManager.Units.Any(u => u.Position.DistanceTo(player.Position) < 19 && u.HealthPercent > 0 && u.Name.Contains(ManaSpringTotem)));

            TryCastSpell(FlameShock, 0, 20, ElementalShamanRotation.ShouldFlameShock(
                target.HasDebuff(FlameShock),
                target.HealthPercent,
                ElementalShamanRotation.IsNatureImmune(target.Name),
                ElementalShamanRotation.IsFireImmune(target.Name)));

            TryCastSpell(LightningShield, 0, int.MaxValue, !ElementalShamanRotation.IsNatureImmune(target.Name) && !player.HasBuff(LightningShield));

            var weaponEnchant = ElementalShamanRotation.SelectWeaponEnchant(
                player.KnowsSpell(RockbiterWeapon),
                player.KnowsSpell(FlametongueWeapon),
                player.MainhandIsEnchanted,
                ElementalShamanRotation.IsFireImmune(target.Name));
            if (weaponEnchant != null)
                TryCastSpell(weaponEnchant, 0, int.MaxValue);

            TryCastSpell(ElementalMastery, 0, int.MaxValue);

            targetLastPosition = target.Position;
        }

        bool TargetMovingTowardPlayer =>
            targetLastPosition != null &&
            targetLastPosition.DistanceTo(player.Position) > target.Position.DistanceTo(player.Position);

        bool TargetIsFleeing =>
            targetLastPosition != null &&
            targetLastPosition.DistanceTo(player.Position) < target.Position.DistanceTo(player.Position);
    }
}
