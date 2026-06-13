using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using System.Collections.Generic;

namespace ArcaneMageBot
{
    class RestState : RestStateBase
    {
        const string Evocation = "Evocation";

        public RestState(Stack<IBotState> botStates, IDependencyContainer container)
            : base(botStates, container)
        {
        }

        public override void Update()
        {
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

            if (player.IsChanneling)
                return;

            if (player.ManaPercent < 20 && player.IsSpellReady(Evocation))
            {
                player.LuaCall($"CastSpellByName('{Evocation}')");
                return;
            }

            if (player.Level > 3)
            {
                TryEat(80, delayMs: 0);
                TryDrink(80, delayMs: 0);
            }
        }

        bool HealthOk => player.HealthPercent > 90;

        bool ManaOk => (player.Level < 6 && player.ManaPercent > 60) || player.ManaPercent >= 90 || (player.ManaPercent >= 75 && !player.IsDrinking);
    }
}
