using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using System.Collections.Generic;

namespace RetributionPaladinBot
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

        const string HolyLight = "Holy Light";

        public RestState(Stack<IBotState> botStates, IDependencyContainer container)
            : base(botStates, container)
        {
        }

        public override void Update()
        {
            if (player.IsCasting) return;

            if (InCombat || (HealthOk && ManaOk))
            {
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

            var holyLightRank = SelectHolyLightRank(
                player.HealthPercent,
                CanCastHolyLight(rank: -1),
                CanCastHolyLight(rank: 1));
            if (ShouldHeal(HealthOk, player.IsEating, player.IsDrinking, holyLightRank.HasValue) && Wait.For("HealSelfDelay", 3500, true))
            {
                player.Stand();
                CastHolyLight(holyLightRank.Value);
            }
        }

        bool HealthOk => player.HealthPercent > 90;

        bool ManaOk => IsManaOk(player.Level, player.ManaPercent, player.IsDrinking, drinkItem != null);

        internal static bool IsManaOk(int level, int manaPercent, bool isDrinking, bool hasDrink) =>
            !hasDrink ||
            (level <= 10 && manaPercent > lowLevelManaReadyPercent) ||
            manaPercent >= fullManaReadyPercent ||
            (manaPercent >= manaReadyPercent && !isDrinking);

        internal static bool ShouldUseDrink(int level, bool hasDrink, bool isDrinking, int manaPercent) =>
            level > 10 &&
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

        internal static int? SelectHolyLightRank(
            int healthPercent,
            bool canCastHighestRank,
            bool canCastRankOne)
        {
            if (healthPercent < 70 && canCastHighestRank)
                return -1;

            if (canCastRankOne)
                return 1;

            return canCastHighestRank ? -1 : (int?)null;
        }

        internal static bool ShouldHeal(bool healthOk, bool isEating, bool isDrinking, bool canCastHeal) =>
            !healthOk &&
            !isEating &&
            !isDrinking &&
            canCastHeal;

        bool CanCastHolyLight(int rank) =>
            player.KnowsSpell(HolyLight) &&
            player.IsSpellReady(HolyLight, rank) &&
            player.Mana >= player.GetManaCost(HolyLight, rank);

        void CastHolyLight(int rank)
        {
            var spell = rank < 1 ? HolyLight : $"{HolyLight}(Rank {rank})";
            player.LuaCall($"CastSpellByName('{spell}',1)");
        }
    }
}
