using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;

namespace EnhancementShamanBot
{
    class HealSelfState : IBotState
    {
        const string ChainHeal = EnhancementShamanRotation.ChainHeal;
        const string GiftOfTheNaaru = "Gift of the Naaru";
        const string WarStomp = "War Stomp";
        const string HealingWave = EnhancementShamanRotation.HealingWave;
        const string LesserHealingWave = EnhancementShamanRotation.LesserHealingWave;
        const string NatureSwiftness = "Nature's Swiftness";

        readonly Stack<IBotState> botStates;
        readonly LocalPlayer player;

        public HealSelfState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            player = ObjectManager.Player;

            if (player.KnowsSpell(WarStomp) && player.IsSpellReady(WarStomp))
                player.LuaCall($"CastSpellByName('{WarStomp}')");
        }

        public void Update()
        {
            if (player.IsCasting) return;

            player.StopAllMovement();

            if (player.HealthPercent > 70)
            {
                botStates.Pop();
                return;
            }

            if (player.HealthPercent < 45 && CanUseSelfSpell(NatureSwiftness))
            {
                CastSelfSpell(NatureSwiftness);
                return;
            }

            var healSpell = EnhancementShamanRotation.SelectSelfHeal(
                player.HealthPercent,
                maelstromStacks: 0,
                CanUseSelfSpell(LesserHealingWave),
                CanUseSelfSpell(HealingWave),
                CanUseSelfSpell(ChainHeal));
            if (healSpell != null)
            {
                CastSelfSpell(healSpell);
                return;
            }

            if (player.HealthPercent < 55 && CanUseSelfSpell(GiftOfTheNaaru))
            {
                CastSelfSpell(GiftOfTheNaaru);
                return;
            }

            botStates.Pop();
        }

        bool CanUseSelfSpell(string name) =>
            player.KnowsSpell(name) &&
            player.IsSpellReady(name) &&
            player.Mana >= player.GetManaCost(name);

        void CastSelfSpell(string name)
        {
            if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
            {
                player.LuaCall($"CastSpellByName('{name}',1)");
            }
            else
            {
                player.CastSpell(name, player.Guid);
            }
        }
    }
}
