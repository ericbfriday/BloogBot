namespace BackstabRogueBot
{
    static class BackstabRogueRotation
    {
        // Spend combo points earlier the closer the target is to death.
        internal static bool IsReadyToEviscerate(int targetHealthPercent, int comboPoints) =>
            targetHealthPercent <= 20 && comboPoints >= 2 ||
            targetHealthPercent <= 30 && comboPoints >= 3 ||
            targetHealthPercent <= 40 && comboPoints >= 4 ||
            comboPoints == 5;

        internal static bool ShouldSliceAndDice(
            bool hasSliceAndDice,
            int targetHealthPercent,
            int comboPoints) =>
            !hasSliceAndDice &&
            targetHealthPercent > 40 &&
            comboPoints >= 2 &&
            comboPoints <= 3;

        internal static bool ReadyToInterrupt(
            int targetMana,
            bool targetIsCasting,
            bool targetIsChanneling) =>
            targetMana > 0 && (targetIsCasting || targetIsChanneling);

        // Kidney Shot (with 1 or 2 combo points only) before Gouge: Gouge has a longer
        // cooldown and costs more energy, so it sometimes misses the cast window.
        internal static bool ShouldKidneyShotInterrupt(
            bool readyToInterrupt,
            bool kickReady,
            int comboPoints) =>
            readyToInterrupt &&
            !kickReady &&
            comboPoints >= 1 &&
            comboPoints <= 2;

        // Build combo points only while no finisher or interrupt is pending.
        internal static bool ShouldUseComboBuilder(
            bool readyToInterrupt,
            int comboPoints,
            bool readyToEviscerate) =>
            !readyToInterrupt &&
            comboPoints < 5 &&
            !readyToEviscerate;
    }
}
