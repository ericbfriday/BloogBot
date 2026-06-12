using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

namespace ShadowPriestBot
{
    class BuffSelfState : IBotState
    {
        const string PowerWordFortitude = "Power Word: Fortitude";
        const string ShadowProtection = "Shadow Protection";

        readonly Stack<IBotState> botStates;
        readonly LocalPlayer player;

        public BuffSelfState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            player = ObjectManager.Player;
        }

        public void Update()
        {
            if (ShadowPriestBuffSelfState.HasAllKnownBuffs(
                player.KnowsSpell(PowerWordFortitude),
                player.HasBuff(PowerWordFortitude),
                player.KnowsSpell(ShadowProtection),
                player.HasBuff(ShadowProtection)))
            {
                botStates.Pop();
                return;
            }

            TryCastSpell(PowerWordFortitude);

            TryCastSpell(ShadowProtection);
        }

        void TryCastSpell(string name, int requiredLevel = 1)
        {
            var knowsSpell = player.KnowsSpell(name);
            var isSpellReady = knowsSpell && player.IsSpellReady(name);
            var hasEnoughMana = knowsSpell && player.Mana >= player.GetManaCost(name);

            if (ShadowPriestBuffSelfState.ShouldCastBuff(
                player.HasBuff(name),
                knowsSpell,
                isSpellReady,
                hasEnoughMana,
                player.Level,
                requiredLevel))
                player.LuaCall($"CastSpellByName('{name}',1)");
        }
    }
}
