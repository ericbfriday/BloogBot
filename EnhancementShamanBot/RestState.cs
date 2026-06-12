using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

namespace EnhancementShamanBot
{
    class RestState : IBotState
    {
        const int stackCount = 5;
        const int lowLevelManaReadyPercent = 50;
        const int manaReadyPercent = 65;
        const int fullManaReadyPercent = 90;
        const int foodHealthPercent = 80;

        const string HealingWave = "Healing Wave";

        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly LocalPlayer player;
        readonly WoWItem foodItem;
        readonly WoWItem drinkItem;

        public RestState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;

            foodItem = Inventory.GetAllItems()
                .FirstOrDefault(i => i.Info.Name == container.BotSettings.Food);

            drinkItem = Inventory.GetAllItems()
                .FirstOrDefault(i => i.Info.Name == container.BotSettings.Drink);
        }

        public void Update()
        {
            if (player.IsCasting) return;

            if (InCombat || (HealthOk && ManaOk))
            {
                Wait.RemoveAll();
                player.Stand();
                botStates.Pop();

                var foodCount = foodItem == null ? 0 : Inventory.GetItemCount(foodItem.ItemId);
                var drinkCount = drinkItem == null ? 0 : Inventory.GetItemCount(drinkItem.ItemId);
                if (!InCombat && (foodCount == 0 || drinkCount == 0) && !container.RunningErrands)
                {
                    var foodToBuy = 12 - (foodCount / stackCount);
                    var drinkToBuy = 28 - (drinkCount / stackCount);
                    var itemsToBuy = new Dictionary<string, int>();

                    if (foodToBuy > 0 && !string.IsNullOrEmpty(container.BotSettings.Food))
                        itemsToBuy.Add(container.BotSettings.Food, foodToBuy);

                    if (drinkToBuy > 0 && !string.IsNullOrEmpty(container.BotSettings.Drink))
                        itemsToBuy.Add(container.BotSettings.Drink, drinkToBuy);

                    if (!itemsToBuy.Any())
                        return;

                    var currentHotspot = container.GetCurrentHotspot();
                    if (currentHotspot.TravelPath != null)
                    {
                        botStates.Push(new TravelState(botStates, container, currentHotspot.TravelPath.Waypoints, 0));
                        botStates.Push(new MoveToPositionState(botStates, container, currentHotspot.TravelPath.Waypoints[0]));
                    }

                    botStates.Push(new BuyItemsState(botStates, currentHotspot.Innkeeper.Name, itemsToBuy));
                    botStates.Push(new SellItemsState(botStates, container, currentHotspot.Innkeeper.Name));
                    botStates.Push(new MoveToPositionState(botStates, container, currentHotspot.Innkeeper.Position));
                    container.CheckForTravelPath(botStates, true, false);
                    container.RunningErrands = true;
                }

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

            var healRank = GetHealingWaveRank();
            var canCastHeal = player.KnowsSpell(HealingWave) &&
                player.IsSpellReady(HealingWave, healRank) &&
                player.Mana >= player.GetManaCost(HealingWave, healRank);
            if (ShouldHeal(HealthOk, player.IsEating, player.IsDrinking, canCastHeal) && Wait.For("HealSelfDelay", 3500, true))
            {
                player.Stand();
                CastHealingWave(healRank);
            }
        }

        bool HealthOk => player.HealthPercent > 90;

        bool ManaOk => IsManaOk(player.Level, player.ManaPercent, player.IsDrinking, drinkItem != null);

        bool InCombat => ObjectManager.Player.IsInCombat || ObjectManager.Units.Any(u => u.TargetGuid == ObjectManager.Player.Guid);

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

        internal static bool ShouldHeal(bool healthOk, bool isEating, bool isDrinking, bool canCastHeal) =>
            !healthOk &&
            !isEating &&
            !isDrinking &&
            canCastHeal;
    }
}
