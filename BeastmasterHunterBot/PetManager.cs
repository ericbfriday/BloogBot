// Friday owns this file!

using BeastMasterHunterBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

namespace BeastmasterHunterBot
{
    class PetManagerState : IBotState
    {
        internal const string CallPet = "Call Pet";
        internal const string RevivePet = "Revive Pet";
        const string FeedPet = "Feed Pet";


        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly LocalPlayer player;
        public PetManagerState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
        }

        public void Update()
        {
            if (player.IsCasting)
                return;

            var pet = ObjectManager.Pet;
            var recoverySpell = SelectRecoverySpell(
                pet != null,
                pet != null && pet.HealthPercent > 0,
                player.KnowsSpell(CallPet),
                player.KnowsSpell(RevivePet));
            if (recoverySpell == null)
            {
                player.Stand();
                botStates.Pop();
                botStates.Push(new BuffSelfState(botStates, container));
                return;
            }

            if (player.IsSpellReady(recoverySpell) &&
                player.Mana >= player.GetManaCost(recoverySpell))
                player.LuaCall($"CastSpellByName('{recoverySpell}')");
        }

        internal static string SelectRecoverySpell(
            bool petExists,
            bool petAlive,
            bool knowsCallPet,
            bool knowsRevivePet)
        {
            if (petExists)
                return !petAlive && knowsRevivePet ? RevivePet : null;

            return knowsCallPet ? CallPet : null;
        }

        public void Feed(string parFoodName)
        {
            if (true /*Inventory.Instance.GetItemCount(parFoodName) != 0*/)
            {
                const string checkFeedPet = "{0} = 0; if CursorHasSpell() then CanFeedMyPet = 1 end;";
                var result = player.LuaCallWithResults(checkFeedPet);
                if (result[0].Trim().Contains("0"))
                {
                    const string feedPet = "CastSpellByName('Feed Pet'); TargetUnit('Pet');";
                    player.LuaCall(feedPet);
                }
                const string usePetFood1 =
                    "for bag = 0,4 do for slot = 1,GetContainerNumSlots(bag) do local item = GetContainerItemLink(bag,slot) if item then if string.find(item, '";
                const string usePetFood2 = "') then PickupContainerItem(bag,slot) break end end end end";
                player.LuaCall(usePetFood1 + parFoodName.Replace("'", "\\'") + usePetFood2);
            }
            player.LuaCall("ClearCursor()");
        }
    }
}
