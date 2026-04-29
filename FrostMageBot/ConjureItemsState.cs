using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

namespace FrostMageBot
{
    class ConjureItemsState : IBotState
    {
        const string ConjureFood = "Conjure Food";
        const string ConjureWater = "Conjure Water";

        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly LocalPlayer player;

        WoWItem foodItem;
        WoWItem drinkItem;

        public ConjureItemsState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
        }

        public void Update()
        {
            foodItem = Inventory.GetAllItems()
                .Where(IsFoodItem)
                .OrderByDescending(i => i.Info.Name == container.BotSettings.Food)
                .ThenByDescending(i => i.StackCount)
                .FirstOrDefault();

            drinkItem = Inventory.GetAllItems()
                .Where(IsDrinkItem)
                .OrderByDescending(i => i.Info.Name == container.BotSettings.Drink)
                .ThenByDescending(i => i.StackCount)
                .FirstOrDefault();

            if (player.IsCasting)
                return;

            //player.Stand();

            if (player.ManaPercent < 20)
            {
                botStates.Pop();
                botStates.Push(new RestState(botStates, container));
                return;
            }

            if (Inventory.CountFreeSlots(false) == 0 || (foodItem != null || !player.KnowsSpell(ConjureFood)) && (drinkItem != null || !player.KnowsSpell(ConjureWater)))
            {
                botStates.Pop();

                if (player.ManaPercent <= 70)
                    botStates.Push(new RestState(botStates, container));

                return;
            }

            var foodCount = Inventory.GetAllItems()
                .Where(IsFoodItem)
                .Sum(i => i.StackCount);
            if ((foodItem == null || foodCount <= 2) && Wait.For("FrostMageConjureFood", 3000, true))
                TryCastSpell(ConjureFood);

            var drinkCount = Inventory.GetAllItems()
                .Where(IsDrinkItem)
                .Sum(i => i.StackCount);
            if ((drinkItem == null || drinkCount <= 2) && Wait.For("FrostMageConjureDrink", 3000, true))
                TryCastSpell(ConjureWater);
        }

        void TryCastSpell(string name)
        {
            if (player.IsSpellReady(name) && !player.IsCasting)
                player.LuaCall($"CastSpellByName('{name}')");
        }

        bool IsFoodItem(WoWItem item) =>
            item?.Info?.Name == container.BotSettings.Food || IsConjuredFood(item);

        bool IsDrinkItem(WoWItem item) =>
            item?.Info?.Name == container.BotSettings.Drink || IsConjuredDrink(item);

        bool IsConjuredFood(WoWItem item) => item?.Info?.Name?.StartsWith("Conjured ") == true && !item.Info.Name.Contains("Water");

        bool IsConjuredDrink(WoWItem item) => item?.Info?.Name?.StartsWith("Conjured ") == true && item.Info.Name.Contains("Water");
    }
}
