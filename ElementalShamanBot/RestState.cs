using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using System.Collections.Generic;

namespace ElementalShamanBot
{
    class RestState : RestStateBase
    {
        const string HealingWave = "Healing Wave";

        public RestState(Stack<IBotState> botStates, IDependencyContainer container)
            : base(botStates, container, trackFood: false)
        {
            player.SetTarget(player.Guid);
        }

        public override void Update()
        {
            if (player.IsCasting) return;

            if (InCombat || (HealthOk && ManaOk))
            {
                StopResting();
                TryRunRestockErrands(0, 28);
                return;
            }

            if (!player.IsDrinking && Wait.For("HealSelfDelay", 3500, true))
            {
                player.Stand();
                if (player.HealthPercent < 70)
                    player.LuaCall($"CastSpellByName('{HealingWave}')");
                if (player.HealthPercent > 70 && player.HealthPercent < 85)
                {
                    if (player.Level >= 40)
                        player.LuaCall($"CastSpellByName('{HealingWave}(Rank 3)')");
                    else
                        player.LuaCall($"CastSpellByName('{HealingWave}(Rank 1)')");
                }
            }

            if (player.Level > 10)
                TryDrink(60, delayMs: 0);
        }

        bool HealthOk => player.HealthPercent > 90;

        bool ManaOk => (player.Level <= 10 && player.ManaPercent > 50) || player.ManaPercent >= 90 || (player.ManaPercent >= 65 && !player.IsDrinking);
    }
}
