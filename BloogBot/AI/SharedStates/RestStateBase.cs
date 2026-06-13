using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

namespace BloogBot.AI.SharedStates
{
    // Owns the rest-state plumbing shared by every bot profile: configured food/drink
    // lookup, the in-combat check, the stand-up/pop exit, eat/drink mechanics, and the
    // innkeeper restocking errand. Subclasses keep only class policy: thresholds,
    // self-heals, pet upkeep, and form management.
    public abstract class RestStateBase : IBotState
    {
        protected const int StackCount = 5;

        protected readonly Stack<IBotState> botStates;
        protected readonly IDependencyContainer container;
        protected readonly LocalPlayer player;
        protected WoWItem foodItem;
        protected WoWItem drinkItem;

        protected RestStateBase(
            Stack<IBotState> botStates,
            IDependencyContainer container,
            bool trackFood = true,
            bool trackDrink = true)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;

            if (trackFood)
                foodItem = FindConfiguredItem(container.BotSettings.Food);
            if (trackDrink)
                drinkItem = FindConfiguredItem(container.BotSettings.Drink);
        }

        public abstract void Update();

        protected static WoWItem FindConfiguredItem(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
                return null;

            return Inventory.GetAllItems()
                .FirstOrDefault(i => i.Info.Name == itemName);
        }

        protected virtual bool InCombat =>
            ObjectManager.Player.IsInCombat ||
            ObjectManager.Units.Any(u => u.TargetGuid == ObjectManager.Player.Guid);

        protected void StopResting()
        {
            Wait.RemoveAll();
            player.Stand();
            botStates.Pop();
        }

        // Use food when injured. A delayMs of 0 skips the retry throttle.
        protected bool TryEat(int healthPercent = 100, int delayMs = 500, string waitKey = "EatDelay")
        {
            if (foodItem != null &&
                !player.IsEating &&
                player.HealthPercent < healthPercent &&
                (delayMs <= 0 || Wait.For(waitKey, delayMs, true)))
            {
                foodItem.Use();
                return true;
            }

            return false;
        }

        protected bool TryDrink(int manaPercent = 100, int delayMs = 1000, string waitKey = "DrinkDelay")
        {
            if (drinkItem != null &&
                !player.IsDrinking &&
                player.ManaPercent < manaPercent &&
                (delayMs <= 0 || Wait.For(waitKey, delayMs, true)))
            {
                drinkItem.Use();
                return true;
            }

            return false;
        }

        // Queue the restocking errand: ride the travel path to the innkeeper, sell off
        // junk, and buy the configured food/drink back up to the given stack targets.
        // With onlyWhenOut (the common policy) the trip is only worth it once a tracked
        // consumable has run out completely. Returns true when the errand was queued.
        protected bool TryRunRestockErrands(int foodStacksTarget, int drinkStacksTarget, bool onlyWhenOut = true)
        {
            if (InCombat || container.RunningErrands)
                return false;

            var foodCount = foodItem == null ? 0 : Inventory.GetItemCount(foodItem.ItemId);
            var drinkCount = drinkItem == null ? 0 : Inventory.GetItemCount(drinkItem.ItemId);

            if (onlyWhenOut &&
                !(foodStacksTarget > 0 && foodCount == 0) &&
                !(drinkStacksTarget > 0 && drinkCount == 0))
                return false;

            var itemsToBuy = new Dictionary<string, int>();

            if (foodStacksTarget > 0)
            {
                var foodToBuy = foodStacksTarget - (foodCount / StackCount);
                if (foodToBuy > 0 && !string.IsNullOrEmpty(container.BotSettings.Food))
                    itemsToBuy.Add(container.BotSettings.Food, foodToBuy);
            }

            if (drinkStacksTarget > 0)
            {
                var drinkToBuy = drinkStacksTarget - (drinkCount / StackCount);
                if (drinkToBuy > 0 && !string.IsNullOrEmpty(container.BotSettings.Drink))
                    itemsToBuy.Add(container.BotSettings.Drink, drinkToBuy);
            }

            if (!itemsToBuy.Any())
                return false;

            var currentHotspot = container.GetCurrentHotspot();
            if (currentHotspot?.Innkeeper == null)
                return false;

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

            return true;
        }
    }
}
