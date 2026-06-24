namespace CombatRogueBot
{
    static class CombatRogueRotation
    {
        // Spend combo points earlier the closer the target is to death.
        internal static bool IsReadyToEviscerate(int targetHealthPercent, int comboPoints) =>
            targetHealthPercent <= 15 && comboPoints >= 2 ||
            targetHealthPercent <= 25 && comboPoints >= 3 ||
            targetHealthPercent <= 35 && comboPoints >= 4 ||
            comboPoints == 5;

        internal static bool ShouldSliceAndDice(
            bool hasSliceAndDice,
            int targetHealthPercent,
            int comboPoints) =>
            !hasSliceAndDice &&
            targetHealthPercent > 70 &&
            comboPoints == 2;

        internal static bool ReadyToInterrupt(
            int targetMana,
            bool targetIsCasting,
            bool targetIsChanneling) =>
            targetMana > 0 && (targetIsCasting || targetIsChanneling);

        internal static bool ShouldAdrenalineRush(int aggressorCount, int playerHealthPercent) =>
            aggressorCount == 3 && playerHealthPercent > 80;

        // Blood Fury is saved for the opener while the target is still healthy.
        internal static bool ShouldBloodFury(int targetHealthPercent) =>
            targetHealthPercent > 80;

        // Evasion is a panic cooldown for multi-mob pulls.
        internal static bool ShouldEvasion(int aggressorCount) =>
            aggressorCount > 1;

        // Blade Flurry's cleave window only pays off against more than one attacker.
        internal static bool ShouldBladeFlurry(int aggressorCount) =>
            aggressorCount > 1;

        // Builder cadence: keep stacking Sinister Strike until we cap at five combo points.
        internal static bool ShouldSinisterStrike(int comboPoints) =>
            comboPoints < 5;

        // Kick is the primary interrupt; use it whenever the target is casting and Kick is ready.
        internal static bool ShouldKickInterrupt(
            int targetMana,
            bool targetIsCasting,
            bool targetIsChanneling) =>
            ReadyToInterrupt(targetMana, targetIsCasting, targetIsChanneling);

        // Gouge is the interrupt fallback: only when an interrupt is needed and Kick isn't available.
        internal static bool ShouldGougeInterrupt(
            int targetMana,
            bool targetIsCasting,
            bool targetIsChanneling,
            bool kickReady) =>
            ReadyToInterrupt(targetMana, targetIsCasting, targetIsChanneling) && !kickReady;
    }
}
