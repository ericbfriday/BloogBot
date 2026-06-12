using BloogBot.Game;

namespace EnhancementShamanBot
{
    static class ShamanTotemTracker
    {
        const float StaleThreshold = 18f;

        public static Position Anchor { get; private set; }

        public static void SetAnchor(Position position) => Anchor = position;

        public static void ClearAnchor() => Anchor = null;

        // True when the player has moved far enough from the anchor that totems are out of range.
        public static bool AreStale(Position playerPosition) =>
            Anchor != null && Anchor.DistanceTo(playerPosition) > StaleThreshold;
    }
}
