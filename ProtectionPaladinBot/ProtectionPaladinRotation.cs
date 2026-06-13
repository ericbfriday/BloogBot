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

        internal static bool ShouldSealOfRighteousness(
            bool hasSealOfRighteousness,
            bool targetHasJudgementOfTheCrusader,
            bool knowsJudgementOfTheCrusader) =>
            !hasSealOfRighteousness &&
            (targetHasJudgementOfTheCrusader || !knowsJudgementOfTheCrusader);
    }
}
