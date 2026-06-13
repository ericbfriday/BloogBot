using System.Linq;

namespace FrostMageBot
{
    static class FrostMageRotation
    {
        internal const string Fireball = "Fireball";
        internal const string Frostbolt = "Frostbolt";
        internal const string FireWard = "Fire Ward";
        internal const string FrostWard = "Frost Ward";

        static readonly string[] FireWardTargets = { "Fire", "Flame", "Infernal", "Searing", "Hellcaller", "Dragon", "Whelp" };
        static readonly string[] FrostWardTargets = { "Ice", "Frost" };

        // Frostbolt and Fireball ranks leapfrog each other at low levels;
        // from level 8 onward Frostbolt is always the stronger nuke.
        internal static string SelectNuke(bool knowsFrostbolt, int playerLevel)
        {
            if (!knowsFrostbolt)
                return Fireball;
            if (playerLevel >= 8)
                return Frostbolt;
            if (playerLevel >= 6)
                return Fireball;
            if (playerLevel >= 4)
                return Frostbolt;
            return Fireball;
        }

        internal static int CalculateNukeRange(int rangeTalentRank) =>
            29 + rangeTalentRank * 3;

        internal static string SelectWard(string targetName)
        {
            if (targetName == null)
                return null;
            if (FireWardTargets.Any(targetName.Contains))
                return FireWard;
            if (FrostWardTargets.Any(targetName.Contains))
                return FrostWard;
            return null;
        }

        internal static bool ShouldUseWard(int targetHealthPercent, int playerHealthPercent) =>
            targetHealthPercent > 20 || playerHealthPercent < 10;

        internal static bool ShouldUseWand(
            bool hasWand,
            int manaPercent,
            bool isCasting,
            bool isChanneling) =>
            hasWand && manaPercent <= 10 && !isCasting && !isChanneling;

        internal static bool ShouldEvocate(
            int playerHealthPercent,
            bool hasIceBarrier,
            int manaPercent,
            int targetHealthPercent) =>
            (playerHealthPercent > 50 || hasIceBarrier) &&
            manaPercent < 8 &&
            targetHealthPercent > 15;

        internal static bool ShouldIceBarrier(
            bool hasIceBarrier,
            int aggressorCount,
            bool frostNovaReady,
            int playerHealthPercent,
            int manaPercent,
            int targetHealthPercent) =>
            !hasIceBarrier &&
            (aggressorCount >= 2 ||
                (!frostNovaReady &&
                playerHealthPercent < 95 &&
                manaPercent > 40 &&
                (targetHealthPercent > 20 || playerHealthPercent < 10)));

        // Root the target for a frost shatter combo, but never when it would
        // pull additional nearby mobs into the fight.
        internal static bool ShouldFrostNova(
            bool targetIsTargetingPlayer,
            int targetHealthPercent,
            int playerHealthPercent,
            bool targetIsFrozen,
            bool otherUnitsNearby) =>
            targetIsTargetingPlayer &&
            (targetHealthPercent > 20 || playerHealthPercent < 30) &&
            !targetIsFrozen &&
            !otherUnitsNearby;
    }
}
