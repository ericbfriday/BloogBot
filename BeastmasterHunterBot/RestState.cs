// Friday owns this file!

using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using System.Collections.Generic;

namespace BeastMasterHunterBot
{
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

            var pet = ObjectManager.Pet;
            if (pet != null && !pet.IsHappy() && !pet.HasBuff("Feed Pet Effect") && Wait.For("FeedPetDelay", 3000, true))
            {
                FeedPet();
            }

            TryEat(80, delayMs: 2000);
            TryDrink(80, delayMs: 2000);

            if (pet != null && pet.HealthPercent > 0 && pet.HealthPercent < 90
                && !pet.HasBuff(MendPet) && player.IsSpellReady(MendPet))
            {
                player.LuaCall($"CastSpellByName('{MendPet}')");
            }

            if (HealthOk && ManaOk && PetHealthOk)
            {
                StopResting();
                botStates.Push(new BuffSelfState(botStates, container));

                RefreshConsumables();
                TryRunRestockErrands(12, 28);
                return;
            }
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

        bool PetHealthOk
        {
            get
            {
                var pet = ObjectManager.Pet;
                return pet == null || pet.HealthPercent == 0 || pet.HealthPercent >= 90;
            }
        }

        internal static bool ShouldAddErrandItem(string itemName, int amountToBuy) =>
            amountToBuy > 0 &&
            !string.IsNullOrWhiteSpace(itemName);
    }
}
