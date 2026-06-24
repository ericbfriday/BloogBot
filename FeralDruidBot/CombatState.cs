using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FeralDruidBot
{
    class CombatState : CombatStateBase, IBotState
    {
        // Shapeshifting
        const string BearForm = FeralDruidRotation.BearForm;
        const string CatForm = FeralDruidRotation.CatForm;

        // Bear
        const string Maul = "Maul";
        const string Enrage = "Enrage";
        const string DemoralizingRoar = "Demoralizing Roar";

        // Cat
        const string Claw = "Claw";
        const string Rake = "Rake";
        const string Rip = "Rip";
        const string TigersFury = "Tiger's Fury";
        const string FeralCharge = "Feral Charge - Cat";
        const string FaerieFire = "Faerie Fire (Feral)";
        const string FerociousBite = "Ferocious Bite";
        const string Mangle = "Mangle (Cat)";
        const string Berserk = "Berserk";

        // Human
        const string HealingTouch = "Healing Touch";
        const string Moonfire = "Moonfire";
        const string Wrath = "Wrath";

        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly LocalPlayer player;
        readonly WoWUnit target;

        internal CombatState(
            Stack<IBotState> botStates,
            IDependencyContainer container,
            WoWUnit target,
            bool loot = true) :
            base(
                botStates,
                container,
                target,
                desiredRange: ObjectManager.Player.Level <= 12 ? 30 : 4,
                loot)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
            this.target = target;
        }

        public new void Update()
        {
            if (player.HealthPercent < 30 && player.KnowsSpell(HealingTouch) && player.Mana >= player.GetManaCost(HealingTouch))
            {
                Wait.RemoveAll();
                botStates.Push(new HealSelfState(botStates, container, target));
                return;
            }

            if (base.Update())
            {
                return;
            }

            // Melee hybrid — no proactive LOS gate; the base closes the distance.
            // Energy/rage strikes go through the form-aware ability helpers (the shared
            // TryUseRotationAbility can't gate druid form or resolve cat/bear resources);
            // form entry and caster-form utility go through the mana cast helpers.

            // if less than level 13, use spellcasting
            if (player.Level <= 12)
            {
                // if low on mana, move into melee range
                if (FeralDruidRotation.ShouldMoveIntoMeleeForMana(player.ManaPercent, player.Position.DistanceTo(target.Position)))
                {
                    player.MoveToward(target.Position);
                    return;
                }
                else player.StopAllMovement();

                if (TryCastRotationSpell(Moonfire, 0, 10, !target.HasDebuff(Moonfire)))
                    return;

                if (TryCastRotationSpell(Wrath, 0, 30))
                    return;
            }
            // bear form
            else if (player.Level > 12 && player.Level < 20)
            {
                if (TryCastRotationSpell(BearForm, 0, 50, player.CurrentShapeshiftForm != BearForm && Wait.For("BearFormDelay", 1000, true)))
                    return;

                if (TryUseBearAbility(DemoralizingRoar, 10, FeralDruidRotation.ShouldDemoralizingRoar(ObjectManager.Aggressors.Count(), target.HasDebuff(DemoralizingRoar))))
                    return;

                if (TryUseBearAbility(Enrage, castOnSelf: true))
                    return;

                if (TryUseBearAbility(Maul, FeralDruidRotation.MaulRageRequirement(player.Level)))
                    return;
            }
            // cat form
            else if (player.Level >= 20)
            {
                if (player.Position.DistanceTo(target.Position) > 8 && TryUseCatAbility(FeralCharge, requiredEnergy: 10))
                    return;

                if (TryCastRotationSpell(CatForm, 0, int.MaxValue, player.CurrentShapeshiftForm != CatForm))
                    return;

                if (TryUseCatAbility(Berserk, 0, condition: FeralDruidRotation.ShouldBerserk(target.HealthPercent, player.HasBuff(Berserk)), castOnSelf: true))
                    return;

                if (TryUseCatAbility(TigersFury, 30, condition: FeralDruidRotation.ShouldTigersFury(target.HealthPercent, player.HasBuff(TigersFury)), castOnSelf: true))
                    return;

                if (TryCastRotationSpell(FaerieFire, FeralDruidRotation.ShouldFaerieFire(target.HasDebuff(FaerieFire))))
                    return;

                if (TryUseCatAbility(Rip, 30, true, condition: FeralDruidRotation.ShouldRip(target.HasDebuff(Rip), player.ComboPoints)))
                    return;

                if (TryUseCatAbility(FerociousBite, 35, true, condition: FeralDruidRotation.ShouldFerociousBite(player.ComboPoints)))
                    return;

                if (TryUseCatAbility(Rake, 35, false, condition: FeralDruidRotation.ShouldRake(target.HealthPercent, target.HasDebuff(Rake))))
                    return;

                if (TryUseCatAbility(Mangle, 40, false, condition: player.KnowsSpell(Mangle)))
                    return;

                if (TryUseCatAbility(Claw, 40, false, condition: !player.KnowsSpell(Mangle)))
                    return;
            }
        }

        bool TryUseBearAbility(string name, int requiredRage = 0, bool condition = true, Action callback = null, bool castOnSelf = false)
        {
            if (!player.KnowsSpell(name))
                return false;

            if (FeralDruidRotation.CanUseBearAbility(
                player.IsSpellReady(name),
                player.Rage,
                requiredRage,
                player.IsStunned,
                player.CurrentShapeshiftForm == BearForm,
                condition))
            {
                CastAbility(name, castOnSelf ? player.Guid : target.Guid);
                callback?.Invoke();
                return true;
            }

            return false;
        }

        bool TryUseCatAbility(string name, int requiredEnergy = 0, bool requiresComboPoints = false, bool condition = true, Action callback = null, bool castOnSelf = false)
        {
            if (!player.KnowsSpell(name))
                return false;

            if (FeralDruidRotation.CanUseCatAbility(
                player.IsSpellReady(name),
                player.Energy,
                requiredEnergy,
                requiresComboPoints,
                player.ComboPoints,
                player.IsStunned,
                player.CurrentShapeshiftForm == CatForm,
                condition))
            {
                CastAbility(name, castOnSelf ? player.Guid : target.Guid);
                callback?.Invoke();
                return true;
            }

            return false;
        }

        void CastAbility(string name, ulong targetGuid)
        {
            if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                player.LuaCall($"CastSpellByName(\"{name}\")");
            else
                player.CastSpell(name, targetGuid);
        }
    }
}
