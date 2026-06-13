using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using System.Collections.Generic;

namespace ProtectionPaladinBot
{
    class RestState : RestStateBase
    {
        const string HolyLight = "Holy Light";

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
                if (!TryRunRestockErrands(0, 28))
                    botStates.Push(new BuffSelfState(botStates, container));
            }

            if (!player.IsDrinking && Wait.For("HealSelfDelay", 3500, true))
            {
                player.Stand();
                if (player.HealthPercent < 70)
                {
                    player.LuaCall($"CastSpellByName('{HolyLight}')");
                }
                else if (player.HealthPercent > 70 && player.HealthPercent < 90)
                {
                    if (ClientHelper.ClientVersion != ClientVersion.WotLK)
                    {
                        player.LuaCall($"CastSpellByName('{HolyLight}(Rank 1)')");
                    }
                    else
                    {
                        // In WotLK holy light costs same amount of mana regardless of rank.
                        player.LuaCall($"CastSpellByName('{HolyLight}')");
                    }
                }
            }

            if (player.Level > 10 && TryDrink(60, delayMs: 1000, waitKey: "UseDrinkDelay"))
                return;
        }

        bool HealthOk => player.HealthPercent > 90;

        bool ManaOk => (player.Level <= 10 && player.ManaPercent > 50) || player.ManaPercent >= 90 || (player.ManaPercent >= 65 && !player.IsDrinking);
    }
}
