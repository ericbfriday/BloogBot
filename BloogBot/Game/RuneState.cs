using BloogBot.Game.Enums;

namespace BloogBot.Game
{
    public class RuneState
    {
        public RuneState(int slot, RuneType type, bool ready, int count, float cooldownRemainingSeconds)
        {
            Slot = slot;
            Type = type;
            Ready = ready;
            Count = count;
            CooldownRemainingSeconds = cooldownRemainingSeconds;
        }

        public int Slot { get; }

        public RuneType Type { get; }

        public bool Ready { get; }

        public int Count { get; }

        public float CooldownRemainingSeconds { get; }
    }
}
