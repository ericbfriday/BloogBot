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
        const string AspectOfTheHawk   = "Aspect of the Hawk";   // Level 10 — best grinding aspect
        const string AspectOfTheMonkey = "Aspect of the Monkey"; // Level 8  — fallback pre-10
        const string AspectOfTheViper  = "Aspect of the Viper";  // may be active on entry after mana-starved combat; Hawk cast below supersedes it
        const string CallPet = "Call Pet";

        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly LocalPlayer player;

        public BuffSelfState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
            player.SetTarget(player.Guid);
        }

        public void Update()
        {
            // Ensure the pet is summoned before heading into combat.
            // PetManagerState will call us back (via Push/Pop) once the pet is ready.
            if (player.KnowsSpell(CallPet) && ObjectManager.Pet == null)
            {
                botStates.Pop();
                botStates.Push(new PetManagerState(botStates, container));
                return;
            }

            // Apply the best aspect we know:
            //   Aspect of the Hawk if learned (level 10+), otherwise Aspect of the Monkey.
            if (player.KnowsSpell(AspectOfTheHawk))
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
            if (player.IsSpellReady(name))
                player.LuaCall($"CastSpellByName('{name}')");
        }
    }
}
