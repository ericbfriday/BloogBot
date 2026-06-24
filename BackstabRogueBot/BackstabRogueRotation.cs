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

        // Prefer Ghostly Strike as the combo builder whenever it's known and off cooldown.
        internal static bool ShouldUseGhostlyStrike(
            bool knowsGhostlyStrike,
            bool ghostlyStrikeReady,
            bool shouldUseComboBuilder) =>
            knowsGhostlyStrike &&
            ghostlyStrikeReady &&
            shouldUseComboBuilder;

        // Sinister Strike is the fallback combo builder when Ghostly Strike isn't available.
        internal static bool ShouldUseSinisterStrike(
            bool ghostlyStrikeReady,
            bool shouldUseComboBuilder) =>
            !ghostlyStrikeReady &&
            shouldUseComboBuilder;

        // Pop Blood Fury early, while the target is still healthy enough to make the burst worthwhile.
        internal static bool ShouldUseBloodFury(
            bool bloodFuryReady,
            int targetHealthPercent) =>
            bloodFuryReady &&
            targetHealthPercent > 80;

        // Defensive/cleave cooldowns are only worth it when more than one mob is on us.
        internal static bool ShouldUseMultiTargetCooldown(int aggressorCount) =>
            aggressorCount > 1;

        // Gouge is the interrupt of last resort: only when an interrupt is needed and Kick is on cooldown.
        internal static bool ShouldGougeInterrupt(
            bool readyToInterrupt,
            bool kickReady) =>
            readyToInterrupt &&
            !kickReady;
    }
}
