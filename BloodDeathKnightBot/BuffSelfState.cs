using BloogBot;
using BloogBot.AI;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;

namespace BloodDeathKnightBot
{
    class BuffSelfState : IBotState
    {
        const string BloodPresence = "Blood Presence";
        const string HornOfWinter = "Horn of Winter";

        readonly Stack<IBotState> botStates;
        readonly LocalPlayer player;

        public BuffSelfState(Stack<IBotState> botStates, IDependencyContainer container)
        {
            this.botStates = botStates;
            player = ObjectManager.Player;
        }

        public void Update()
        {
            if (ClientHelper.ClientVersion != ClientVersion.WotLK || player.Class != Class.DeathKnight)
            {
                botStates.Pop();
                return;
            }

            if (TryCastSelfBuff(BloodPresence))
                return;

            if (TryCastSelfBuff(HornOfWinter))
                return;

            botStates.Pop();
        }

        bool TryCastSelfBuff(string name)
        {
            if (player.KnowsSpell(name) && !player.HasBuff(name) && player.IsSpellReady(name) && !player.IsCasting && !player.IsChanneling)
            {
                player.LuaCall($"CastSpellByName('{name}',1)");
                return true;
            }

            return false;
        }
    }
}
