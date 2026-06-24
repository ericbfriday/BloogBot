using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using System.Collections.Generic;

namespace EnhancementShamanBot
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

        const string HealingWave = EnhancementShamanRotation.HealingWave;
        const string LesserHealingWave = EnhancementShamanRotation.LesserHealingWave;

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
                TryRunRestockErrands(12, 28);
                return;
            }

            var consumable = GetConsumableToUse(
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

            var healRank = GetHealingWaveRank();
            var canCastHeal = player.KnowsSpell(HealingWave) &&
                player.IsSpellReady(HealingWave, healRank) &&
                player.Mana >= player.GetManaCost(HealingWave, healRank);
            var canCastLesserHealingWave = player.KnowsSpell(LesserHealingWave) &&
                player.IsSpellReady(LesserHealingWave) &&
                player.Mana >= player.GetManaCost(LesserHealingWave);
            var healSpell = SelectRestHeal(player.HealthPercent, canCastLesserHealingWave, canCastHeal);
            if (ShouldHeal(HealthOk, player.IsEating, player.IsDrinking, healSpell != null) && Wait.For("HealSelfDelay", 3500, true))
            {
                player.Stand();

                if (healSpell == HealingWave)
                    CastHealingWave(healRank);
                else
                    player.LuaCall($"CastSpellByName(\"{healSpell}\", 1)");
            }
        }

        bool HealthOk => player.HealthPercent > 90;

        bool ManaOk => IsManaOk(player.Level, player.ManaPercent, player.IsDrinking, drinkItem != null);

        int GetHealingWaveRank()
        {
            if (player.HealthPercent < 70 ||
                (ClientHelper.ClientVersion == ClientVersion.WotLK && player.Level >= 40))
                return -1;

            return player.Level >= 40 ? 3 : 1;
        }

        void CastHealingWave(int rank)
        {
            string spell;
            if (rank < 1)
                spell = HealingWave;
            else
                spell = $"{HealingWave}(Rank {rank})";

            player.LuaCall($"CastSpellByName(\"{spell}\", 1)");
        }

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

        internal static string SelectRestHeal(
            int healthPercent,
            bool canUseLesserHealingWave,
            bool canUseHealingWave)
        {
            if (healthPercent < 70 && canUseHealingWave)
                return HealingWave;

            if (canUseLesserHealingWave)
                return LesserHealingWave;

            return canUseHealingWave ? HealingWave : null;
        }

        internal static RestConsumable GetConsumableToUse(
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
    }
}
