using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using System.Collections.Generic;
using System.Threading;

namespace FrostMageBot
{
    class RestState : RestStateBase
    {
        const string Evocation = "Evocation";

        readonly string[] configuredFoodNames;
        readonly string[] configuredDrinkNames;

        public RestState(Stack<IBotState> botStates, IDependencyContainer container)
            : base(botStates, container, trackFood: false, trackDrink: false)
        {
            configuredFoodNames = FrostMageConsumables.GetConfiguredNames(container.BotSettings.Food);
            configuredDrinkNames = FrostMageConsumables.GetConfiguredNames(container.BotSettings.Drink);
        }

        public override void Update()
        {
            var items = Inventory.GetAllItems();
            foodItem = FrostMageConsumables.SelectItem(
                items,
                i => i.Info?.Name,
                i => i.StackCount,
                configuredFoodNames,
                FrostMageConsumables.ConjuredFoodNames).Item;
            drinkItem = FrostMageConsumables.SelectItem(
                items,
                i => i.Info?.Name,
                i => i.StackCount,
                configuredDrinkNames,
                FrostMageConsumables.ConjuredDrinkNames).Item;

            if (player.IsChanneling)
                return;

            if (InCombat)
            {
                player.Stand();
                botStates.Pop();
                return;
            }

            if (HealthOk && ManaOk)
            {
                player.Stand();
                botStates.Pop();
                botStates.Push(new BuffSelfState(botStates, container));
                return;
            }

            if (player.ManaPercent < 20 && player.IsSpellReady(Evocation))
            {
                player.LuaCall($"CastSpellByName('{Evocation}')");
                Thread.Sleep(200);
                return;
            }

            TryEat(80, delayMs: 0);
            TryDrink(delayMs: 0);
        }

        bool HealthOk => foodItem == null || player.HealthPercent >= 90 || (player.HealthPercent >= 80 && !player.IsEating);

        bool ManaOk => drinkItem == null || player.ManaPercent >= 90 || (player.ManaPercent >= 80 && !player.IsDrinking);
    }
}
