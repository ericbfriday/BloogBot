using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using System.Collections.Generic;

namespace RetributionPaladinBot
{
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
            player.SetTarget(player.Guid);
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

            var usedConsumable = false;

            if (ShouldUseFood(foodItem != null, player.IsEating, player.HealthPercent) && Wait.For("EatDelay", 2000, true))
            {
                foodItem.Use();
                usedConsumable = true;
            }

            if (ShouldUseDrink(player.Level, drinkItem != null, player.IsDrinking, player.ManaPercent) && Wait.For("DrinkDelay", 1000, true))
            {
                drinkItem.Use();
                usedConsumable = true;
            }

            if (usedConsumable)
                return;

            var canCastHeal = player.KnowsSpell(HolyLight) &&
                player.IsSpellReady(HolyLight) &&
                player.Mana >= player.GetManaCost(HolyLight);
            if (ShouldHeal(HealthOk, player.IsEating, player.IsDrinking, canCastHeal) && Wait.For("HealSelfDelay", 3500, true))
            {
                player.Stand();
                if (player.HealthPercent < 70)
                    player.LuaCall($"CastSpellByName('{HolyLight}')");
                if (player.HealthPercent > 70 && player.HealthPercent < 90)
                    player.LuaCall($"CastSpellByName('{HolyLight}(Rank 1)')");
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

        internal static bool ShouldHeal(bool healthOk, bool isEating, bool isDrinking, bool canCastHeal) =>
            !healthOk &&
            !isEating &&
            !isDrinking &&
            canCastHeal;
    }
}
