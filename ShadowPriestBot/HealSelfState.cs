using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

namespace ShadowPriestBot
{
    class HealSelfState : IBotState
    {
        const string Renew = "Renew";

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

            var healingSpell = ShadowPriestRecovery.SelectHeal(
                player.HealthPercent,
                CanCastSelfSpell(ShadowPriestRecovery.Heal),
                CanCastSelfSpell(ShadowPriestRecovery.LesserHeal));
            if (player.HealthPercent > 70 || healingSpell == null)
            {
                if (CanCastSelfSpell(Renew))
                    CastSelfSpell(Renew);

                botStates.Pop();
                return;
            }

            if (ShadowPriestRecovery.ShouldLeaveShadowform(
                player.HasBuff(ShadowPriestRecovery.Shadowform),
                healingSpell))
            {
                CastSelfSpell(ShadowPriestRecovery.Shadowform);
                return;
            }

            CastSelfSpell(healingSpell);
        }

        bool CanCastSelfSpell(string name) =>
            player.KnowsSpell(name) &&
            player.IsSpellReady(name) &&
            player.Mana >= player.GetManaCost(name);

        void CastSelfSpell(string name) =>
            player.LuaCall($"CastSpellByName('{name}',1)");
    }
}
