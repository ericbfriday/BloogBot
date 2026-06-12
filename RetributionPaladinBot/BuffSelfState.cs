using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

namespace RetributionPaladinBot
{
    class BuffSelfState : IBotState
    {
        readonly Stack<IBotState> botStates;
        readonly LocalPlayer player;

        public BuffSelfState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            player = ObjectManager.Player;
        }

        public void Update()
        {
            if (RetributionPaladinBuffSelfState.HasRequiredBlessing(
                player.KnowsSpell(RetributionPaladinBuffSelfState.BlessingOfMight),
                player.HasBuff(RetributionPaladinBuffSelfState.BlessingOfMight),
                player.HasBuff(RetributionPaladinBuffSelfState.BlessingOfKings),
                player.HasBuff(RetributionPaladinBuffSelfState.BlessingOfSanctuary)))
            {
                botStates.Pop();
                return;
            }

            var blessing = RetributionPaladinBuffSelfState.SelectBlessing(
                player.KnowsSpell(RetributionPaladinBuffSelfState.BlessingOfMight),
                player.KnowsSpell(RetributionPaladinBuffSelfState.BlessingOfKings),
                player.KnowsSpell(RetributionPaladinBuffSelfState.BlessingOfSanctuary));

            if (blessing != null)
                TryCastSpell(blessing);
        }

        void TryCastSpell(string name)
        {
            var knowsSpell = player.KnowsSpell(name);
            var isSpellReady = knowsSpell && player.IsSpellReady(name);
            var hasEnoughMana = knowsSpell && player.Mana >= player.GetManaCost(name);

            if (!RetributionPaladinBuffSelfState.ShouldCastBlessing(
                player.HasBuff(name),
                knowsSpell,
                isSpellReady,
                hasEnoughMana))
                return;

            if (RetributionPaladinBuffSelfState.GetSelfBuffCastMode(ClientHelper.ClientVersion) == SelfBuffCastMode.LuaCastOnSelf)
                player.LuaCall($"CastSpellByName(\"{name}\",1)");
            else
                player.CastSpell(name, player.Guid);
        }
    }
}
