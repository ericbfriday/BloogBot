using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using System.Collections.Generic;

namespace ShadowPriestBot
{
    enum RestConsumable
    {
        None,
        Food,
        Drink
    }

    class RestState : RestStateBase
    {
        const int lowLevelManaReadyPercent = 50;
        const int manaReadyPercent = 65;
        const int fullManaReadyPercent = 90;
        const int foodHealthPercent = 80;

        public RestState(Stack<IBotState> botStates, IDependencyContainer container)
            : base(botStates, container)
        {
        }

        public override void Update()
        {
            if (player.IsCasting) return;

            if (InCombat)
            {
                StopResting();
                return;
            }

            if (HealthOk && ManaOk)
            {
                var diseaseCure = player.IsDiseased
                    ? ShadowPriestRecovery.SelectDiseaseCure(
                        player.KnowsSpell(ShadowPriestRecovery.AbolishDisease),
                        player.KnowsSpell(ShadowPriestRecovery.CureDisease))
                    : null;
                if (CanCastSelfSpell(diseaseCure))
                {
                    CastSelfSpell(diseaseCure);
                    return;
                }

                if (!player.HasBuff(ShadowPriestRecovery.Shadowform) && CanCastSelfSpell(ShadowPriestRecovery.Shadowform))
                {
                    CastSelfSpell(ShadowPriestRecovery.Shadowform);
                    return;
                }

                StopResting();
                if (!TryRunRestockErrands(12, 28))
                    botStates.Push(new BuffSelfState(botStates, container));
                return;
            }

            var consumable = SelectConsumable(
                player.Level,
                foodItem != null,
                player.IsEating,
                player.HealthPercent,
                drinkItem != null,
                player.IsDrinking,
                player.ManaPercent);
            if (consumable == RestConsumable.Food)
            {
                if (Wait.For("EatDelay", 2000, true))
                    foodItem.Use();

                return;
            }

            if (consumable == RestConsumable.Drink)
            {
                if (Wait.For("DrinkDelay", 1000, true))
                    drinkItem.Use();

                return;
            }

            var healingSpell = ShadowPriestRecovery.SelectHeal(
                player.HealthPercent,
                CanCastSelfSpell(ShadowPriestRecovery.Heal),
                CanCastSelfSpell(ShadowPriestRecovery.LesserHeal));
            if (ShouldHeal(HealthOk, player.IsEating, player.IsDrinking, healingSpell != null) && Wait.For("HealSelfDelay", 3500, true))
            {
                player.Stand();

                if (ShadowPriestRecovery.ShouldLeaveShadowform(
                    player.HasBuff(ShadowPriestRecovery.Shadowform),
                    healingSpell))
                {
                    CastSelfSpell(ShadowPriestRecovery.Shadowform);
                    return;
                }

                CastSelfSpell(healingSpell);
            }
        }

        bool HealthOk => player.HealthPercent > 90;

        bool ManaOk => IsManaOk(player.Level, player.ManaPercent, player.IsDrinking, drinkItem != null);

        internal static bool IsManaOk(int level, int manaPercent, bool isDrinking, bool hasDrink) =>
            !hasDrink ||
            (level < 5 && manaPercent > lowLevelManaReadyPercent) ||
            manaPercent >= fullManaReadyPercent ||
            (manaPercent >= manaReadyPercent && !isDrinking);

        internal static bool ShouldUseDrink(int level, bool hasDrink, bool isDrinking, int manaPercent) =>
            level >= 5 &&
            hasDrink &&
            !isDrinking &&
            manaPercent < manaReadyPercent;

        internal static bool ShouldUseFood(bool hasFood, bool isEating, int healthPercent) =>
            hasFood &&
            !isEating &&
            healthPercent < foodHealthPercent;

        internal static RestConsumable SelectConsumable(
            int level,
            bool hasFood,
            bool isEating,
            int healthPercent,
            bool hasDrink,
            bool isDrinking,
            int manaPercent)
        {
            if (isEating || isDrinking)
                return RestConsumable.None;

            if (ShouldUseFood(hasFood, isEating, healthPercent))
                return RestConsumable.Food;

            if (ShouldUseDrink(level, hasDrink, isDrinking, manaPercent))
                return RestConsumable.Drink;

            return RestConsumable.None;
        }

        internal static bool ShouldHeal(bool healthOk, bool isEating, bool isDrinking, bool canCastHeal) =>
            !healthOk &&
            !isEating &&
            !isDrinking &&
            canCastHeal;

        bool CanCastSelfSpell(string name) =>
            !string.IsNullOrEmpty(name) &&
            player.KnowsSpell(name) &&
            player.IsSpellReady(name) &&
            player.Mana >= player.GetManaCost(name);

        void CastSelfSpell(string name) =>
            player.LuaCall($"CastSpellByName('{name}',1)");
    }
}
