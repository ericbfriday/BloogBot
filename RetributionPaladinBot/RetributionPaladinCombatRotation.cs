namespace RetributionPaladinBot
{
    static class RetributionPaladinCombatRotation
    {
        internal const string DevotionAura = "Devotion Aura";
        internal const string JudgementOfLight = "Judgement of Light";
        internal const string JudgementOfWisdom = "Judgement of Wisdom";
        internal const string RetributionAura = "Retribution Aura";
        internal const string SanctityAura = "Sanctity Aura";

        internal static string SelectAura(
            bool knowsDevotionAura,
            bool hasDevotionAura,
            bool knowsRetributionAura,
            bool hasRetributionAura,
            bool knowsSanctityAura,
            bool hasSanctityAura)
        {
            if (knowsSanctityAura)
                return hasSanctityAura ? null : SanctityAura;

            if (knowsRetributionAura)
                return hasRetributionAura ? null : RetributionAura;

            if (knowsDevotionAura)
                return hasDevotionAura ? null : DevotionAura;

            return null;
        }

        internal static string SelectWotlkJudgement(
            bool knowsJudgementOfWisdom,
            bool targetHasJudgementOfWisdom,
            bool targetHasJudgementOfLight,
            bool hasActiveSeal)
        {
            if (!hasActiveSeal)
                return null;

            if (knowsJudgementOfWisdom && !targetHasJudgementOfWisdom)
                return JudgementOfWisdom;

            return targetHasJudgementOfLight ? null : JudgementOfLight;
        }

        internal static bool ShouldUseSealOfRighteousness(
            bool hasSealOfRighteousness,
            bool targetHasJudgementOfTheCrusader,
            bool knowsSealOfCommand,
            bool knowsJudgementOfTheCrusader) =>
            !hasSealOfRighteousness &&
            !knowsSealOfCommand &&
            (targetHasJudgementOfTheCrusader || !knowsJudgementOfTheCrusader);

        internal static bool ShouldUseLegacyJudgement(
            bool hasSealOfTheCrusader,
            bool hasSealOfRighteousness,
            bool hasSealOfCommand,
            int manaPercent,
            int targetHealthPercent) =>
            hasSealOfTheCrusader ||
            ((hasSealOfRighteousness || hasSealOfCommand) &&
            (manaPercent >= 95 || targetHealthPercent <= 3));
    }
}
