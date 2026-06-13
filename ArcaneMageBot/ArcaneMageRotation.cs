namespace ArcaneMageBot
{
    static class ArcaneMageRotation
    {
        internal const string ArcaneMissiles = "Arcane Missiles";
        internal const string Fireball = "Fireball";

        internal static bool ShouldUseWand(
            bool hasWand,
            int manaPercent,
            bool isCasting,
            bool isChanneling) =>
            hasWand && manaPercent <= 10 && !isCasting && !isChanneling;

        internal static bool ShouldManaShield(bool hasManaShield, int playerHealthPercent) =>
            !hasManaShield && playerHealthPercent < 20;

        // Fireball is the nuke until Arcane Missiles is strong enough at 15,
        // and again whenever Presence of Mind makes the long cast instant.
        internal static bool ShouldFireball(int playerLevel, bool hasPresenceOfMind) =>
            playerLevel < 15 || hasPresenceOfMind;

        internal static bool ShouldArcaneMissiles(int playerLevel) =>
            playerLevel >= 15;
    }
}
