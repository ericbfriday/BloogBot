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
    }
}
