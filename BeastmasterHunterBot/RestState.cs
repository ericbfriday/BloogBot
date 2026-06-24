// Friday owns this file!

using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using System.Collections.Generic;

namespace BeastMasterHunterBot
{
    enum RestConsumable
    {
        None,
        Food,
        Drink
    }

    // TODO: add in ammo buying/management
    class RestState : RestStateBase
    {
        const string MendPet = "Mend Pet";

        public RestState(Stack<IBotState> botStates, IDependencyContainer container)
            : base(botStates, container)
        {
        }

        public override void Update()
        {
            RefreshConsumables();

            if (InCombat)
            {
                StopResting();
                return;
            }

            if (player.IsCasting)
                return;

            var pet = ObjectManager.Pet;
            if (player.IsEating || player.IsDrinking)
                return;

            if (pet != null && foodItem != null && !pet.IsHappy() && !pet.HasBuff("Feed Pet Effect") && Wait.For("FeedPetDelay", 3000, true))
            {
                FeedPet();
                return;
            }

            var playerKnowsMendPet = player.KnowsSpell(MendPet);
            if (HealthOk && ManaOk && IsPetHealthOk(pet != null, pet?.HealthPercent ?? 0, playerKnowsMendPet))
            {
                StopResting();
                botStates.Push(new BuffSelfState(botStates, container));

                RefreshConsumables();
                TryRunRestockErrands(12, 28);
                return;
            }

            var consumable = SelectConsumable(
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
                if (Wait.For("DrinkDelay", 2000, true))
                    drinkItem.Use();

                return;
            }

            var mendPetReady = playerKnowsMendPet && player.IsSpellReady(MendPet);
            var mendPetManaCost = playerKnowsMendPet ? player.GetManaCost(MendPet) : int.MaxValue;
            if (ShouldCastMendPet(
                pet != null,
                pet?.HealthPercent ?? 0,
                pet?.HasBuff(MendPet) ?? false,
                playerKnowsMendPet,
                mendPetReady,
                player.Mana,
                mendPetManaCost))
                player.LuaCall($"CastSpellByName('{MendPet}')");
        }

        void RefreshConsumables()
        {
            foodItem = FindConfiguredItem(container.BotSettings.Food);
            drinkItem = FindConfiguredItem(container.BotSettings.Drink);
        }

        void FeedPet()
        {
            if (string.IsNullOrEmpty(container.BotSettings.Food)) return;

            var foodName = container.BotSettings.Food;

            player.LuaCall("CastSpellByName('Feed Pet')");
            player.LuaCall(
                "for bag = 0,4 do for slot = 1,GetContainerNumSlots(bag) do local item = GetContainerItemLink(bag,slot) " +
                "if item then if string.find(item, '" + foodName.Replace("'", "\\'") + "') then " +
                "PickupContainerItem(bag,slot) break end end end end");
            player.LuaCall("ClearCursor()");
        }

        bool HealthOk => foodItem == null || player.HealthPercent >= 95 || (player.HealthPercent >= 80 && !player.IsEating);

        bool ManaOk => drinkItem == null || player.ManaPercent >= 95 || (player.ManaPercent >= 80 && !player.IsDrinking);

        internal static RestConsumable SelectConsumable(
            bool hasFood,
            bool isEating,
            int healthPercent,
            bool hasDrink,
            bool isDrinking,
            int manaPercent)
        {
            if (isEating || isDrinking)
                return RestConsumable.None;

            if (hasFood && healthPercent < 80)
                return RestConsumable.Food;

            if (hasDrink && manaPercent < 80)
                return RestConsumable.Drink;

            return RestConsumable.None;
        }

        internal static bool IsPetHealthOk(bool petExists, int petHealthPercent, bool playerKnowsMendPet) =>
            !petExists ||
            petHealthPercent == 0 ||
            petHealthPercent >= 90 ||
            !playerKnowsMendPet;

        internal static bool ShouldCastMendPet(
            bool petExists,
            int petHealthPercent,
            bool petHasMendPetBuff,
            bool playerKnowsMendPet,
            bool mendPetReady,
            int playerMana,
            int mendPetManaCost) =>
            petExists &&
            petHealthPercent > 0 &&
            petHealthPercent < 90 &&
            !petHasMendPetBuff &&
            playerKnowsMendPet &&
            mendPetReady &&
            playerMana >= mendPetManaCost;

        internal static bool ShouldAddErrandItem(string itemName, int amountToBuy) =>
            amountToBuy > 0 &&
            !string.IsNullOrWhiteSpace(itemName);
    }
}
