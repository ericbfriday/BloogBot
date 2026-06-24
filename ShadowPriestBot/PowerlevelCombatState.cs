using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using BloogBot.AI.SharedStates;
using BloogBot.Game.Enums;

namespace ShadowPriestBot
{
    class PowerlevelCombatState : IBotState
    {
        const string AutoAttackLuaScript = "if IsCurrentAction('12') == nil then CastSpellByName('Attack') end";
        const string LosErrorMessage = "Target not in line of sight";
        const string WandLuaScript = "if IsAutoRepeatAction(11) == nil then CastSpellByName('Shoot') end";
        const string TurnOffWandLuaScript = "if IsAutoRepeatAction(11) ~= nil then CastSpellByName('Shoot') end";

        const string AbolishDisease = "Abolish Disease";
        const string CureDisease = "Cure Disease";
        const string DispelMagic = "Dispel Magic";
        const string InnerFire = "Inner Fire";
        const string LesserHeal = "Lesser Heal";
        const string MindBlast = "Mind Blast";
        const string MindFlay = "Mind Flay";
        const string PowerWordShield = "Power Word: Shield";
        const string PsychicScream = "Psychic Scream";
        const string ShadowForm = "Shadowform";
        const string ShadowWordPain = "Shadow Word: Pain";
        const string Smite = "Smite";
        const string VampiricEmbrace = "Vampiric Embrace";
        const string WeakenedSoul = "Weakened Soul";

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

            WoWEventHandler.OnErrorMessage += OnErrorMessageCallback;
        }

        ~PowerlevelCombatState()
        {
            WoWEventHandler.OnErrorMessage -= OnErrorMessageCallback;
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

            if (player.HealthPercent < 30 && target.HealthPercent > 50 && player.KnowsSpell(LesserHeal) && player.Mana >= player.GetManaCost(LesserHeal))
            {
                botStates.Push(new HealSelfState(botStates, container));
                return;
            }

            if (target.TappedByOther)
            {
                botStates.Pop();
                return;
            }

            // when killing certain summoned units (like totems), our local reference to target will still have 100% health even after the totem is destroyed
            // so we need to lookup the target again in the object manager, and if it's null, we can assume it's dead and leave combat.
            var checkTarget = ObjectManager.Units.FirstOrDefault(u => u.Guid == target.Guid);
            if (target.Health == 0 || target.TappedByOther || checkTarget == null)
            {
                const string waitKey = "PopCombatState";

                if (Wait.For(waitKey, 1500))
                {
                    botStates.Pop();
                    botStates.Push(new LootState(botStates, container, target));
                    Wait.Remove(waitKey);
                }

                return;
            }

            if (player.TargetGuid != target.Guid)
                player.SetTarget(target.Guid);

            // ensure we're facing the target
            if (!player.IsFacing(target.Position)) player.Face(target.Position);

            var distanceToTarget = player.Position.DistanceTo(target.Position);

            if (ShadowPriestPowerlevelCombatRange.ShouldMoveCloser(distanceToTarget, target.IsCasting, target.IsChanneling))
                player.MoveToward(target.Position);
            else if (player.IsMoving)
                player.StopAllMovement();

            var hasWand = Inventory.GetEquippedItem(EquipSlot.Ranged) != null;

            // ensure auto-attack is turned on only if we don't have a wand
            if (!hasWand)
                player.LuaCall(AutoAttackLuaScript);

            // Don't attempt spells without line of sight; close distance until we have it.
            if (!player.InLosWith(target.Position))
            {
                var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
                player.MoveToward(nextWaypoint);
                return;
            }

            // ----- COMBAT ROTATION -----
            var useWand = (hasWand && player.ManaPercent <= 10 && !player.IsCasting && !player.IsChanneling) || target.CreatureType == CreatureType.Totem;
            if (useWand)
            {
                player.LuaCall(WandLuaScript);
                return;
            }

            if (TryCastSpell(ShadowForm, 0, int.MaxValue, !player.HasBuff(ShadowForm)))
                return;

            if (TryCastSpell(VampiricEmbrace, 0, 29, ShadowPriestPowerlevelCombatRotation.ShouldVampiricEmbrace(
                player.HealthPercent, target.HasDebuff(VampiricEmbrace), target.HealthPercent)))
                return;

            if (TryCastSpell(PsychicScream, 0, 7, ShadowPriestPowerlevelCombatRotation.ShouldPsychicScream(
                distanceToTarget, player.HasBuff(PowerWordShield), ObjectManager.Aggressors.Count(), target.CreatureType == CreatureType.Elemental)))
                return;

            if (TryCastSpell(ShadowWordPain, 0, 29, ShadowPriestPowerlevelCombatRotation.ShouldShadowWordPain(
                target.HealthPercent, target.HasDebuff(ShadowWordPain))))
                return;

            if (TryCastSpell(DispelMagic, 0, int.MaxValue, player.HasMagicDebuff, castOnSelf: true))
                return;

            var shouldCureDisease = ShadowPriestPowerlevelCombatRotation.ShouldCureDisease(player.IsDiseased, player.HasBuff(ShadowForm));
            if (player.KnowsSpell(AbolishDisease))
            {
                if (TryCastSpell(AbolishDisease, 0, int.MaxValue, shouldCureDisease, castOnSelf: true))
                    return;
            }
            else if (TryCastSpell(CureDisease, 0, int.MaxValue, shouldCureDisease, castOnSelf: true))
                return;

            if (TryCastSpell(InnerFire, 0, int.MaxValue, !player.HasBuff(InnerFire)))
                return;

            if (TryCastSpell(PowerWordShield, 0, int.MaxValue, ShadowPriestPowerlevelCombatRotation.ShouldPowerWordShield(
                player.HasDebuff(WeakenedSoul), player.HasBuff(PowerWordShield), target.HealthPercent, player.HealthPercent), castOnSelf: true))
                return;

            if (TryCastSpell(MindBlast, 0, 29))
                return;

            if (ShadowPriestPowerlevelCombatRotation.ShouldMindFlay(
                player.KnowsSpell(MindFlay), distanceToTarget, player.KnowsSpell(PowerWordShield), player.HasBuff(PowerWordShield)))
            {
                if (TryCastSpell(MindFlay, 0, 19))
                    return;
            }
            else if (TryCastSpell(Smite, 0, 29, !player.HasBuff(ShadowForm)))
                return;

            if (powerlevelTarget.HealthPercent < 50)
            {
                var knowsLesserHeal = player.KnowsSpell(LesserHeal);
                var isLesserHealReady = knowsLesserHeal && player.IsSpellReady(LesserHeal);
                var lesserHealManaCost = knowsLesserHeal ? player.GetManaCost(LesserHeal) : int.MaxValue;
                var distanceToPowerlevelTarget = player.Position.DistanceTo(powerlevelTarget.Position);

                if (ShadowPriestPowerlevelCombatRotation.ShouldHealPowerlevelTarget(
                    powerlevelTarget.HealthPercent,
                    knowsLesserHeal,
                    isLesserHealReady,
                    player.Mana,
                    lesserHealManaCost,
                    distanceToPowerlevelTarget,
                    player.IsStunned,
                    player.IsCasting,
                    player.IsChanneling))
                {
                    player.SetTarget(powerlevelTarget.Guid);
                    if (TryCastSpell(LesserHeal, 0, 40, rangeTarget: powerlevelTarget))
                        return;
                }
            }
        }

        bool TryCastSpell(string name, int minRange, int maxRange, bool condition = true, Action callback = null, bool castOnSelf = false, WoWUnit rangeTarget = null)
        {
            var knowsSpell = player.KnowsSpell(name);
            var isSpellReady = knowsSpell && player.IsSpellReady(name);
            var manaCost = knowsSpell ? player.GetManaCost(name) : int.MaxValue;
            var distanceToTarget = player.Position.DistanceTo((rangeTarget ?? target).Position);

            if (ShadowPriestPowerlevelCombatRotation.CanCastSpell(
                knowsSpell,
                isSpellReady,
                player.Mana,
                manaCost,
                distanceToTarget,
                minRange,
                maxRange,
                condition,
                player.IsStunned,
                player.IsCasting,
                player.IsChanneling))
            {
                var castOnSelfString = castOnSelf ? ",1" : "";
                player.LuaCall($"CastSpellByName(\"{name}\"{castOnSelfString})");
                callback?.Invoke();
                return true;
            }

            return false;
        }

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
