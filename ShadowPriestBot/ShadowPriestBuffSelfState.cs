namespace ShadowPriestBot
{
    static class ShadowPriestBuffSelfState
    {
        internal static bool HasAllKnownBuffs(
            bool knowsPowerWordFortitude,
            bool hasPowerWordFortitude,
            bool knowsShadowProtection,
            bool hasShadowProtection) =>
            (!knowsPowerWordFortitude || hasPowerWordFortitude) &&
            (!knowsShadowProtection || hasShadowProtection);

        internal static bool ShouldCastBuff(
            bool hasBuff,
            bool knowsSpell,
            bool isSpellReady,
            bool hasEnoughMana = true,
            int playerLevel = 1,
            int requiredLevel = 1) =>
            !hasBuff &&
            knowsSpell &&
            playerLevel >= requiredLevel &&
            isSpellReady &&
            hasEnoughMana;
    }
}
