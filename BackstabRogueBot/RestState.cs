// Nat owns this file!

using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using System.Collections.Generic;
using System.Linq;

namespace BackstabRogueBot
{
    class RestState : RestStateBase
    {
        const string Cannibalize = "Cannibalize";

        public RestState(Stack<IBotState> botStates, IDependencyContainer container)
            : base(botStates, container, trackDrink: false)
        {
        }

        public override void Update()
        {
            if (player.IsChanneling)
                return;

            if (player.HealthPercent >= 95 ||
                player.HealthPercent >= 80 && !player.IsEating ||
                ObjectManager.Player.IsInCombat ||
                ObjectManager.Units.Any(u => u.TargetGuid == ObjectManager.Player.Guid))
            {
                StopResting();
                TryRunRestockErrands(24, 0);
                return;
            }

            if (player.IsSpellReady(Cannibalize) && player.TastyCorpsesNearby)
            {
                player.LuaCall($"CastSpellByName('{Cannibalize}')");
                return;
            }

            TryEat(delayMs: 500);
        }

        protected override bool InCombat => ObjectManager.Aggressors.Any();
    }
}
