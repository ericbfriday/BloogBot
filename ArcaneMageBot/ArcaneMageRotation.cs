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

        // Presence of Mind and Arcane Power are saved for targets worth bursting.
        internal static bool ShouldUseBurstCooldown(int targetHealthPercent) =>
            targetHealthPercent > 80;

        // Only interrupt a target that is actually casting and has a mana bar to burn.
        internal static bool ShouldCounterspell(int targetMana, bool targetIsCasting) =>
            targetMana > 0 && targetIsCasting;

        // Hold Fire Blast while Clearcasting so the free proc lands on a bigger nuke.
        internal static bool ShouldFireBlast(bool hasClearcasting) =>
            !hasClearcasting;

        // Root the target only when it won't drag extra mobs into melee range.
        internal static bool ShouldFrostNova(bool otherUnitsNearby) =>
            !otherUnitsNearby;

        // Fireball is the nuke until Arcane Missiles is strong enough at 15,
        // and again whenever Presence of Mind makes the long cast instant.
        internal static bool ShouldFireball(int playerLevel, bool hasPresenceOfMind) =>
            playerLevel < 15 || hasPresenceOfMind;

        internal static bool ShouldArcaneMissiles(int playerLevel) =>
            playerLevel >= 15;
    }
}
