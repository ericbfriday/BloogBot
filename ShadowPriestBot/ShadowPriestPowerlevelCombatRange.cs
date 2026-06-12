namespace ShadowPriestBot
{
    static class ShadowPriestPowerlevelCombatRange
    {
        const int StandardCombatRange = 26;
        const int MindFlayRange = 19;

        internal static int GetDesiredRange(bool targetIsCasting, bool targetIsChanneling) =>
            targetIsCasting || targetIsChanneling
                ? MindFlayRange
                : StandardCombatRange;

        internal static bool ShouldMoveCloser(double distanceToTarget, bool targetIsCasting, bool targetIsChanneling) =>
            distanceToTarget > GetDesiredRange(targetIsCasting, targetIsChanneling);
    }
}
