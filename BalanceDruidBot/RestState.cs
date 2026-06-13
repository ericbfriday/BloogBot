using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using System.Collections.Generic;
using System.Linq;

namespace BalanceDruidBot
{
    class RestState : RestStateBase
    {
        const string Regrowth = "Regrowth";
        const string Rejuvenation = "Rejuvenation";
        const string MoonkinForm = "Moonkin Form";

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
                    botStates.Push(new BuffSelfState(botStates));
            }

            if (player.HealthPercent < 60 && !player.HasBuff(Regrowth) && Wait.For("SelfHealDelay", 5000, true))
            {
                TryCastSpell(MoonkinForm, player.HasBuff(MoonkinForm));
                TryCastSpell(Regrowth);
            }

            if (player.HealthPercent < 80 && !player.HasBuff(Rejuvenation) && !player.HasBuff(Regrowth) && Wait.For("SelfHealDelay", 5000, true))
            {
                TryCastSpell(MoonkinForm, player.HasBuff(MoonkinForm));
                TryCastSpell(Rejuvenation);
            }

            if (player.Level >= 6)
                TryDrink(60, delayMs: 0);
        }

        bool HealthOk => player.HealthPercent >= 81;

        bool ManaOk => (player.Level < 6 && player.ManaPercent > 50) || player.ManaPercent >= 90 || (player.ManaPercent >= 65 && !player.IsDrinking);

        protected override bool InCombat => ObjectManager.Aggressors.Any();

        void TryCastSpell(string name, bool condition = true)
        {
            if (player.IsSpellReady(name) && !player.IsCasting && player.Mana > player.GetManaCost(name) && !player.IsDrinking && condition)
            {
                player.Stand();
                player.LuaCall($"CastSpellByName('{name}',1)");
            }
        }
    }
}
