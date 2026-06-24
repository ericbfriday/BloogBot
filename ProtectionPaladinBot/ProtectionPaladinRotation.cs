namespace ProtectionPaladinBot
{
    static class ProtectionPaladinRotation
    {
        internal const string DevotionAura = "Devotion Aura";
        internal const string RetributionAura = "Retribution Aura";

        internal static string SelectAura(
            bool knowsRetributionAura,
            bool hasDevotionAura,
            bool hasRetributionAura)
        {
            if (knowsRetributionAura)
                return hasRetributionAura ? null : RetributionAura;

            return hasDevotionAura ? null : DevotionAura;
        }

        internal static bool ShouldLayOnHands(bool canAffordHolyLight, int playerHealthPercent) =>
            !canAffordHolyLight && playerHealthPercent < 10;

        internal static bool ShouldJudgementOfWisdom(
            bool targetHasJudgementOfWisdom,
            bool hasActiveSeal) =>
            !targetHasJudgementOfWisdom && hasActiveSeal;

        internal static bool ShouldJudgementOfLight(
            bool targetHasJudgementOfLight,
            bool hasActiveSeal,
            bool knowsJudgementOfWisdom) =>
            !targetHasJudgementOfLight && hasActiveSeal && !knowsJudgementOfWisdom;

        internal static bool ShouldUseLegacyJudgement(
            bool hasSealOfTheCrusader,
            bool hasSealOfRighteousness,
            int manaPercent,
            int targetHealthPercent) =>
            hasSealOfTheCrusader ||
            (hasSealOfRighteousness && (manaPercent >= 95 || targetHealthPercent <= 3));

        // Refill mana with Divine Plea once we dip below half.
        internal static bool ShouldDivinePlea(int manaPercent) =>
            manaPercent < 50;

        internal static bool ShouldPurify(bool isPoisoned, bool isDiseased) =>
            isPoisoned || isDiseased;

        // Exorcism only bites undead and demons.
        internal static bool ShouldExorcism(bool targetIsUndeadOrDemon) =>
            targetIsUndeadOrDemon;

        // Stun freely against non-humanoids; against humanoids save it for the execute.
        internal static bool ShouldHammerOfJustice(bool targetIsHumanoid, int targetHealthPercent) =>
            !targetIsHumanoid || targetHealthPercent < 20;

        // Consecration is only worth the mana when more than one mob is on us.
        internal static bool ShouldConsecration(int aggressorCount) =>
            aggressorCount > 1;

        internal static bool ShouldSealOfTheCrusader(bool hasSealOfTheCrusader, bool targetHasJudgementOfTheCrusader) =>
            !hasSealOfTheCrusader && !targetHasJudgementOfTheCrusader;

        internal static bool ShouldHolyShield(bool hasHolyShield, int targetHealthPercent) =>
            !hasHolyShield && targetHealthPercent > 50;

        internal static bool ShouldSealOfRighteousness(
            bool hasSealOfRighteousness,
            bool targetHasJudgementOfTheCrusader,
            bool knowsJudgementOfTheCrusader) =>
            !hasSealOfRighteousness &&
            (targetHasJudgementOfTheCrusader || !knowsJudgementOfTheCrusader);
    }
}
