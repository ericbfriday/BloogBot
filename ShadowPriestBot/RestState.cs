using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

namespace ShadowPriestBot
{
    class RestState : IBotState
    {
        const int stackCount = 5;
        const int lowLevelManaReadyPercent = 50;
        const int manaReadyPercent = 65;
        const int fullManaReadyPercent = 90;
        const int foodHealthPercent = 80;

        const string AbolishDisease = "Abolish Disease";
        const string CureDisease = "Cure Disease";
        const string LesserHeal = "Lesser Heal";
        const string Heal = "Heal";
        const string ShadowForm = "Shadowform";

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
                if (player.KnowsSpell(ShadowForm) && !player.HasBuff(ShadowForm) && player.IsDiseased)
                {
                    if (player.KnowsSpell(AbolishDisease))
                        player.LuaCall($"CastSpellByName('{AbolishDisease}',1)");
                    else if (player.KnowsSpell(CureDisease))
                        player.LuaCall($"CastSpellByName('{CureDisease}',2)");

                    return;
                }

                if (player.KnowsSpell(ShadowForm) && !player.HasBuff(ShadowForm))
                    player.LuaCall($"CastSpellByName('{ShadowForm}')");

                Wait.RemoveAll();
                player.Stand();
                botStates.Pop();

                var foodCount = foodItem == null ? 0 : Inventory.GetItemCount(foodItem.ItemId);
                var drinkCount = drinkItem == null ? 0 : Inventory.GetItemCount(drinkItem.ItemId);
                var itemsToBuy = new Dictionary<string, int>();

                var foodToBuy = 12 - (foodCount / stackCount);
                if (foodToBuy > 0 && !string.IsNullOrEmpty(container.BotSettings.Food))
                    itemsToBuy.Add(container.BotSettings.Food, foodToBuy);

                var drinkToBuy = 28 - (drinkCount / stackCount);
                if (drinkToBuy > 0 && !string.IsNullOrEmpty(container.BotSettings.Drink))
                    itemsToBuy.Add(container.BotSettings.Drink, drinkToBuy);

                if (!InCombat && itemsToBuy.Any() && !container.RunningErrands)
                {
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
                else
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

            var healingSpell = player.KnowsSpell(Heal) ? Heal : LesserHeal;
            var canCastHeal = player.KnowsSpell(healingSpell) &&
                player.IsSpellReady(healingSpell) &&
                player.Mana >= player.GetManaCost(healingSpell);
            if (ShouldHeal(HealthOk, player.IsEating, player.IsDrinking, canCastHeal) && Wait.For("HealSelfDelay", 3500, true))
            {
                player.Stand();

                if (player.HealthPercent < 70)
                {
                    if (player.HasBuff(ShadowForm))
                        player.LuaCall($"CastSpellByName('{ShadowForm}')");
                }
                
                if (player.HealthPercent < 50)
                {
                    if (player.KnowsSpell(Heal))
                        player.LuaCall($"CastSpellByName('{Heal}',1)");
                    else
                        player.LuaCall($"CastSpellByName('{LesserHeal}',1)");
                }

                if (player.HealthPercent < 70)
                    player.LuaCall($"CastSpellByName('{LesserHeal}',1)");
            }
        }

        bool HealthOk => player.HealthPercent > 90;

        bool ManaOk => IsManaOk(player.Level, player.ManaPercent, player.IsDrinking, drinkItem != null);

        bool InCombat => ObjectManager.Player.IsInCombat || ObjectManager.Units.Any(u => u.TargetGuid == ObjectManager.Player.Guid);

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

        internal static bool ShouldHeal(bool healthOk, bool isEating, bool isDrinking, bool canCastHeal) =>
            !healthOk &&
            !isEating &&
            !isDrinking &&
            canCastHeal;
    }
}
