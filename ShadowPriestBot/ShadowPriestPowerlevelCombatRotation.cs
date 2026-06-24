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

        // Wand to conserve mana, to chip totems, or to finish a nearly-dead target —
        // but never mid-cast/channel.
        internal static bool ShouldUseWand(
            bool hasWand,
            bool isCasting,
            bool isChanneling,
            int manaPercent,
            bool targetIsTotem,
            int targetHealthPercent) =>
            hasWand && !isCasting && !isChanneling &&
            (manaPercent <= 10 || targetIsTotem || targetHealthPercent <= 10);

        internal static bool ShouldVampiricEmbrace(
            int playerHealthPercent,
            bool targetHasVampiricEmbrace,
            int targetHealthPercent) =>
            playerHealthPercent < 100 && !targetHasVampiricEmbrace && targetHealthPercent > 50;

        // Fear when something is in melee (and we're unshielded), or when adds pile on
        // a non-elemental.
        internal static bool ShouldPsychicScream(
            double distanceToTarget,
            bool hasPowerWordShield,
            int aggressorCount,
            bool targetIsElemental) =>
            (distanceToTarget < 8 && !hasPowerWordShield) ||
            (aggressorCount > 1 && !targetIsElemental);

        // Refresh Shadow Word: Pain only on a healthy target that has lost the dot.
        internal static bool ShouldShadowWordPain(int targetHealthPercent, bool targetHasShadowWordPain) =>
            targetHealthPercent > 70 && !targetHasShadowWordPain;

        internal static bool ShouldPowerWordShield(
            bool hasWeakenedSoul,
            bool hasPowerWordShield,
            int targetHealthPercent,
            int playerHealthPercent) =>
            !hasWeakenedSoul && !hasPowerWordShield &&
            (targetHealthPercent > 20 || playerHealthPercent < 10);

        // Cleansing requires caster form; never break Shadowform for it.
        internal static bool ShouldCureDisease(bool isDiseased, bool inShadowForm) =>
            isDiseased && !inShadowForm;

        // Mind Flay is the channel filler once known and in range, but only after a
        // shield is up (when shields are known) so melee pushback won't break it.
        internal static bool ShouldMindFlay(
            bool knowsMindFlay,
            double distanceToTarget,
            bool knowsPowerWordShield,
            bool hasPowerWordShield) =>
            knowsMindFlay &&
            distanceToTarget <= 19 &&
            (!knowsPowerWordShield || hasPowerWordShield);

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
