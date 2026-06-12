namespace ShadowPriestBot
{
    static class ShadowPriestPowerlevelCombatRotation
    {
        internal static bool CanCastSpell(
            bool knowsSpell,
            bool isSpellReady,
            int mana,
            int manaCost,
            double distanceToTarget,
            int minRange,
            int maxRange,
            bool condition,
            bool isStunned,
            bool isCasting,
            bool isChanneling) =>
            knowsSpell &&
            isSpellReady &&
            mana >= manaCost &&
            distanceToTarget >= minRange &&
            distanceToTarget <= maxRange &&
            condition &&
            !isStunned &&
            !isCasting &&
            !isChanneling;

        internal static bool ShouldHealPowerlevelTarget(
            int powerlevelTargetHealthPercent,
            bool knowsLesserHeal,
            bool isLesserHealReady,
            int mana,
            int lesserHealManaCost,
            double distanceToPowerlevelTarget,
            bool isStunned,
            bool isCasting,
            bool isChanneling) =>
            powerlevelTargetHealthPercent < 50 &&
            CanCastSpell(
                knowsLesserHeal,
                isLesserHealReady,
                mana,
                lesserHealManaCost,
                distanceToPowerlevelTarget,
                0,
                40,
                true,
                isStunned,
                isCasting,
                isChanneling);
    }
}
