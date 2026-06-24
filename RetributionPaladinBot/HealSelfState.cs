using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

namespace RetributionPaladinBot
{
    class HealSelfState : IBotState
    {
        const string DivineProtection = "Divine Protection";
        const string HolyLight = "Holy Light";

        readonly Stack<IBotState> botStates;
        readonly LocalPlayer player;

        public HealSelfState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            player = ObjectManager.Player;
        }

        public void Update()
        {
            if (player.IsCasting) return;

            var knowsHolyLight = player.KnowsSpell(HolyLight);
            var holyLightReady = knowsHolyLight && player.IsSpellReady(HolyLight);
            var holyLightManaCost = knowsHolyLight ? player.GetManaCost(HolyLight) : int.MaxValue;
            if (player.HealthPercent > 70 || !CanCastSpell(
                knowsHolyLight,
                holyLightReady,
                player.Mana,
                holyLightManaCost))
            {
                botStates.Pop();
                return;
            }

            var knowsDivineProtection = player.KnowsSpell(DivineProtection);
            var divineProtectionReady = knowsDivineProtection && player.IsSpellReady(DivineProtection);
            var divineProtectionManaCost = knowsDivineProtection
                ? player.GetManaCost(DivineProtection)
                : int.MaxValue;
            if (CanCastSpell(
                knowsDivineProtection,
                divineProtectionReady,
                player.Mana,
                divineProtectionManaCost))
            {
                player.LuaCall($"CastSpellByName('{DivineProtection}')");
                return;
            }

            player.LuaCall($"CastSpellByName('{HolyLight}',1)");
        }

        internal static bool CanCastSpell(bool knowsSpell, bool spellReady, int mana, int manaCost) =>
            knowsSpell &&
            spellReady &&
            mana >= manaCost;
    }
}
