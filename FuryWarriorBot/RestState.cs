using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using System.Collections.Generic;
using System.Linq;

namespace FuryWarriorBot
{
    class RestState : RestStateBase
    {
        public RestState(Stack<IBotState> botStates, IDependencyContainer container)
            : base(botStates, container, trackDrink: false)
        {
        }

        public override void Update()
        {
            if (player.HealthPercent >= 95 ||
                player.HealthPercent >= 80 && !player.IsEating ||
                ObjectManager.Player.IsInCombat ||
                ObjectManager.Units.Any(u => u.TargetGuid == ObjectManager.Player.Guid))
            {
                StopResting();
                TryRunRestockErrands(28, 0);
                return;
            }

            TryEat(delayMs: 250);
        }

        protected override bool InCombat => ObjectManager.Aggressors.Any();
    }
}
