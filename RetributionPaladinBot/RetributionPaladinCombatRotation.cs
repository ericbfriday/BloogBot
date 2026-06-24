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

        // Exorcism only bites undead and demons.
        internal static bool ShouldExorcism(bool targetIsUndeadOrDemon) =>
            targetIsUndeadOrDemon;

        // Stun freely against non-humanoids; against humanoids save it for the execute.
        internal static bool ShouldHammerOfJustice(bool targetIsHumanoid, int targetHealthPercent) =>
            !targetIsHumanoid || targetHealthPercent < 20;

        // Seal of the Crusader is the judgement-debuff setup seal; keep it up until judged.
        internal static bool ShouldSealOfTheCrusader(bool hasSealOfTheCrusader, bool targetHasJudgementOfTheCrusader) =>
            !hasSealOfTheCrusader && !targetHasJudgementOfTheCrusader;

        // Seal of Command takes over once the crusader debuff is applied.
        internal static bool ShouldSealOfCommand(bool hasSealOfCommand, bool targetHasJudgementOfTheCrusader) =>
            !hasSealOfCommand && targetHasJudgementOfTheCrusader;

        internal static bool ShouldHolyShield(bool hasHolyShield, int targetHealthPercent) =>
            !hasHolyShield && targetHealthPercent > 50;

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
