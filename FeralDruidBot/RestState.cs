using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using System.Collections.Generic;
using System.Linq;

namespace FeralDruidBot
{
    class RestState : RestStateBase
    {
        const string Regrowth = "Regrowth";
        const string Rejuvenation = "Rejuvenation";

        public RestState(Stack<IBotState> botStates, IDependencyContainer container)
            : base(botStates, container, trackFood: false)
        {
        }

        public override void Update()
        {
            if (player.IsCasting)
                return;

            if (InCombat)
            {
                StopResting();
                return;
            }

            if (HealthOk && ManaOk)
            {
                StopResting();
                if (!TryRunRestockErrands(0, 28))
                    botStates.Push(new BuffSelfState(botStates, container));
            }

            if (player.HealthPercent < 60 && !player.HasBuff(Regrowth))
                TryCastSpell(Regrowth);

            if (player.HealthPercent < 80 && !player.HasBuff(Rejuvenation) && !player.HasBuff(Regrowth))
                TryCastSpell(Rejuvenation);

            if (player.Level > 8)
                TryDrink(60, delayMs: 0);
        }

        bool HealthOk => player.HealthPercent >= 81;

        bool ManaOk => (player.Level <= 8 && player.ManaPercent > 50) || player.ManaPercent >= 90 || (player.ManaPercent >= 65 && !player.IsDrinking);

        protected override bool InCombat => ObjectManager.Aggressors.Any();

        void TryCastSpell(string name)
        {
            if (player.IsSpellReady(name) && !player.IsCasting && player.Mana > player.GetManaCost(name) && !player.IsDrinking)
            {
                if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                {
                    player.LuaCall($"CastSpellByName(\"{name}\", 1)");
                }
                else
                {
                    player.CastSpell(name, player.Guid);
                }
            }
        }
    }
}
