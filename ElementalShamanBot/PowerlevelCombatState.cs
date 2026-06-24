using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ElementalShamanBot
{
    class PowerlevelCombatState : IBotState
    {

        const string LosErrorMessage = "Target not in line of sight";
        const string AutoAttackLuaScript = "if IsCurrentAction('12') == nil then CastSpellByName('Attack') end";

        const string Clearcasting = "Clearcasting";
        const string EarthShock = "Earth Shock";
        const string ElementalMastery = "Elemental Mastery";
        const string FlameShock = "Flame Shock";
        const string FlametongueWeapon = "Flametongue Weapon";
        const string FocusedCasting = "Focused Casting";
        const string GroundingTotem = "Grounding Totem";
        const string ManaSpringTotem = "Mana Spring Totem";
        const string HealingWave = "Healing Wave";
        const string LightningBolt = "Lightning Bolt";
        const string LightningShield = "Lightning Shield";
        const string RockbiterWeapon = "Rockbiter Weapon";
        const string SearingTotem = "Searing Totem";
        const string StoneclawTotem = "Stoneclaw Totem";
        const string StoneskinTotem = "Stoneskin Totem";
        const string TremorTotem = "Tremor Totem";
        const string LesserHealingWave = "Lesser Healing Wave";

        Position targetLastPosition;

        bool noLos;
        int noLosStartTime;

        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly WoWUnit target;
        readonly WoWPlayer powerlevelTarget;
        readonly LocalPlayer player;

        public PowerlevelCombatState(Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target, WoWPlayer powerlevelTarget)
        {
            this.botStates = botStates;
            this.container = container;
            this.target = target;
            this.powerlevelTarget = powerlevelTarget;
            player = ObjectManager.Player;
        }

        public void Update()
        {
            if (Environment.TickCount - noLosStartTime > 1000)
            {
                player.StopAllMovement();
                noLos = false;
            }

            if (noLos)
            {
                var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
                player.MoveToward(nextWaypoint);
                return;
            }


            if (player.HealthPercent < 30 && target.HealthPercent > 50 && player.Mana >= player.GetManaCost(HealingWave))
            {
                botStates.Push(new HealSelfState(botStates, container));
                return;
            }

            // pop state when the target is dead
            if (target.Health == 0)
            {
                botStates.Pop();

                if (player.ManaPercent < 20)
                    botStates.Push(new RestState(botStates, container));

                return;
            }

            if (player.TargetGuid != player.Guid & !player.IsCasting)
                player.SetTarget(target.Guid);

            // ensure we're facing the target
            if (!player.IsFacing(target.Position)) player.Face(target.Position);

            // ensure auto-attack is turned on
            player.LuaCall(AutoAttackLuaScript);

            var targetIsNatureImmune = ElementalShamanRotation.IsNatureImmune(target.Name);
            var targetIsFireImmune = ElementalShamanRotation.IsFireImmune(target.Name);

            // ensure we're in melee range
            if (player.Position.DistanceTo(target.Position) > 35 || (targetIsNatureImmune || player.Mana < player.GetManaCost(LightningBolt) && (player.Position.DistanceTo(target.Position) > 3)))
            {
                var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
                player.MoveToward(nextWaypoint);
            }
            else
                player.StopAllMovement();

            // Don't attempt spells without line of sight; close distance until we have it.
            if (!player.InLosWith(target.Position))
            {
                var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
                player.MoveToward(nextWaypoint);
                return;
            }

            // Snapshot movement against last tick's position, then record this tick's
            // position so the comparison stays correct even when the chain returns early.
            var targetMovingTowardPlayer = TargetMovingTowardPlayer;
            targetLastPosition = target.Position;

            // ----- COMBAT ROTATION -----
            var partyMembers = ObjectManager.GetPartyMembers();
            var healTarget = partyMembers.FirstOrDefault(p => p.HealthPercent < 50);

            if (healTarget != null && player.Mana > player.GetManaCost(HealingWave))
            {
                player.SetTarget(healTarget.Guid);
                if (TryCastSpell(HealingWave))
                    return;
            }

            if (TryCastSpell(LightningBolt, ElementalShamanRotation.ShouldLightningBolt(
                    targetIsNatureImmune,
                    targetMovingTowardPlayer,
                    target.Position.DistanceTo(player.Position),
                    player.HasBuff(FocusedCasting),
                    target.HealthPercent,
                    player.HasBuff(FocusedCasting) && target.HealthPercent > 20 && Wait.For("FocusedLightningBoltDelay", 4000, true))
                && player.ManaPercent > 50 && target.HealthPercent < 90))
                return;

            if (TryCastSpell(FlameShock, ElementalShamanRotation.ShouldFlameShock(
                    target.HasDebuff(FlameShock),
                    target.HealthPercent,
                    targetIsNatureImmune,
                    targetIsFireImmune)
                && player.ManaPercent > 50 && target.HealthPercent < 90))
                return;

            if (TryCastSpell(LightningShield, ElementalShamanRotation.ShouldLightningShield(targetIsNatureImmune, player.HasBuff(LightningShield))))
                return;

            var weaponEnchant = ElementalShamanRotation.SelectWeaponEnchant(
                player.KnowsSpell(RockbiterWeapon),
                player.KnowsSpell(FlametongueWeapon),
                player.MainhandIsEnchanted,
                targetIsFireImmune);
            if (weaponEnchant != null && TryCastSpell(weaponEnchant))
                return;

            if (TryCastSpell(ManaSpringTotem, ElementalShamanRotation.ShouldManaSpringTotem(IsTotemNearby(ManaSpringTotem, 19))))
                return;

            if (TryCastSpell(ElementalMastery))
                return;
        }

        bool TryCastSpell(string name, bool condition = true, Action callback = null)
        {
            if (player.IsSpellReady(name) && player.Mana >= player.GetManaCost(name) && condition && !player.IsStunned && !player.IsCasting && !player.IsChanneling)
            {
                player.LuaCall($"CastSpellByName(\"{name}\")");
                callback?.Invoke();
                return true;
            }

            return false;
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

        void OnErrorMessageCallback(object sender, OnUiMessageArgs e)
        {
            if (e.Message == LosErrorMessage)
            {
                noLos = true;
                noLosStartTime = Environment.TickCount;
            }
        }
    }
}
