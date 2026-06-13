namespace AfflictionWarlockBot
{
    static class AfflictionWarlockRotation
    {
        // Below this the target dies to dots; switch off the wand and drain its soul.
        internal static bool ShouldDrainSoul(int targetHealthPercent) =>
            targetHealthPercent <= 20;

        // Wand when low on mana, or while dots finish off a target that is
        // too hurt to be worth fresh casts but not yet drainable.
        internal static bool ShouldUseWand(
            bool hasWand,
            int manaPercent,
            int targetHealthPercent,
            bool isCasting,
            bool isChanneling) =>
            hasWand &&
            (manaPercent <= 10 ||
                (targetHealthPercent <= 60 && targetHealthPercent > 20) && !isChanneling && !isCasting);

        internal static bool ShouldLifeTap(int playerHealthPercent, int manaPercent) =>
            playerHealthPercent > 85 && manaPercent < 80;

        internal static bool ShouldDeathCoil(
            bool targetIsCasting,
            bool targetIsChanneling,
            int targetHealthPercent) =>
            (targetIsCasting || targetIsChanneling) && targetHealthPercent > 20;

        // Dots are only worth applying while the target will live long enough
        // to pay back the cast; each dot has its own break-even health floor.
        internal static bool ShouldApplyDot(
            bool targetHasDot,
            int targetHealthPercent,
            int minTargetHealthPercent) =>
            !targetHasDot && targetHealthPercent > minTargetHealthPercent;

        internal static bool ShouldShadowBolt(int targetHealthPercent, bool hasWand) =>
            targetHealthPercent > 40 || !hasWand;
    }
}
