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
        internal const string Regrowth = "Regrowth";
        internal const string Rejuvenation = "Rejuvenation";

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

                return;
            }

            var heal = SelectRestHeal(
                player.HealthPercent,
                player.HasBuff(Regrowth),
                CanCastSpell(Regrowth),
                player.HasBuff(Rejuvenation),
                CanCastSpell(Rejuvenation));
            if (heal != null)
            {
                CastSpell(heal);
                return;
            }

            if (player.Level > 8)
                TryDrink(65, delayMs: 0);
        }

        bool HealthOk => IsHealthOk(player.HealthPercent);

        bool ManaOk => IsManaOk(player.Level, player.ManaPercent, player.IsDrinking, drinkItem != null);

        protected override bool InCombat => ObjectManager.Aggressors.Any();

        bool CanCastSpell(string name) =>
            player.KnowsSpell(name) &&
            player.IsSpellReady(name) &&
            !player.IsCasting &&
            player.Mana >= player.GetManaCost(name) &&
            !player.IsDrinking;

        void CastSpell(string name)
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

        internal static bool IsHealthOk(int healthPercent) => healthPercent >= 80;

        internal static bool IsManaOk(int level, int manaPercent, bool isDrinking, bool hasDrink) =>
            !hasDrink ||
            (level <= 8 && manaPercent > 50) ||
            manaPercent >= 90 ||
            (manaPercent >= 65 && !isDrinking);

        internal static string SelectRestHeal(
            int healthPercent,
            bool hasRegrowth,
            bool canCastRegrowth,
            bool hasRejuvenation,
            bool canCastRejuvenation)
        {
            if (healthPercent < 60 && !hasRegrowth && canCastRegrowth)
                return Regrowth;

            if (healthPercent < 80 && !hasRegrowth && !hasRejuvenation && canCastRejuvenation)
                return Rejuvenation;

            return null;
        }
    }
}
