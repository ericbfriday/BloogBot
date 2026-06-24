// Friday owns this file!

using BeastmasterHunterBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

namespace BeastMasterHunterBot
{
    class BuffSelfState : IBotState
    {
        const string AspectOfTheDragonhawk = BeastmasterHunterRotation.AspectOfTheDragonhawk;
        const string AspectOfTheHawk   = "Aspect of the Hawk";   // Level 10 — best grinding aspect
        const string AspectOfTheMonkey = "Aspect of the Monkey"; // Level 8  — fallback pre-10
        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly LocalPlayer player;

        public BuffSelfState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
        }

        public void Update()
        {
            if (player.IsCasting)
                return;

            // Ensure the pet is summoned before heading into combat.
            // PetManagerState will call us back (via Push/Pop) once the pet is ready.
            var pet = ObjectManager.Pet;
            var petRecoverySpell = PetManagerState.SelectRecoverySpell(
                pet != null,
                pet != null && pet.HealthPercent > 0,
                player.KnowsSpell(PetManagerState.CallPet),
                player.KnowsSpell(PetManagerState.RevivePet));
            if (petRecoverySpell != null)
            {
                botStates.Pop();
                botStates.Push(new PetManagerState(botStates, container));
                return;
            }

            // Apply the best aspect we know.
            if (player.KnowsSpell(AspectOfTheDragonhawk))
            {
                if (player.HasBuff(AspectOfTheDragonhawk))
                {
                    botStates.Pop();
                    return;
                }
                TryCastSpell(AspectOfTheDragonhawk);
            }
            else if (player.KnowsSpell(AspectOfTheHawk))
            {
                if (player.HasBuff(AspectOfTheHawk))
                {
                    botStates.Pop();
                    return;
                }
                TryCastSpell(AspectOfTheHawk);
            }
            else if (player.KnowsSpell(AspectOfTheMonkey))
            {
                if (player.HasBuff(AspectOfTheMonkey))
                {
                    botStates.Pop();
                    return;
                }
                TryCastSpell(AspectOfTheMonkey);
            }
            else
            {
                // No aspect known yet — nothing to apply, proceed.
                botStates.Pop();
            }
        }

        void TryCastSpell(string name)
        {
            if (player.KnowsSpell(name) &&
                player.IsSpellReady(name) &&
                player.Mana >= player.GetManaCost(name))
                player.LuaCall($"CastSpellByName('{name}')");
        }
    }
}
