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

        // The WotLK leveling rotation is Frostbolt-first once trained.
        internal static string SelectNuke(bool knowsFrostbolt, int playerLevel)
        {
            if (!knowsFrostbolt)
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

        internal static bool ShouldUseShatterSpender(
            bool targetIsFrozen,
            bool hasFingersOfFrost) =>
            targetIsFrozen || hasFingersOfFrost;

        internal static bool ShouldSummonWaterElemental(
            bool hasWaterElemental,
            int targetHealthPercent,
            int aggressorCount) =>
            !hasWaterElemental &&
            (targetHealthPercent >= 60 || aggressorCount > 1);

        internal static bool ShouldUseIcyVeins(
            int aggressorCount,
            bool isHighValueTarget,
            int targetHealthPercent) =>
            targetHealthPercent >= 20 &&
            (aggressorCount > 1 ||
                isHighValueTarget ||
                targetHealthPercent >= 80);

        internal static bool ShouldUseColdSnap(
            int aggressorCount,
            bool knowsFrostNova,
            bool frostNovaReady,
            bool knowsIcyVeins,
            bool icyVeinsReady,
            bool knowsSummonWaterElemental,
            bool summonWaterElementalReady,
            int targetHealthPercent,
            int playerHealthPercent) =>
            playerHealthPercent > 30 &&
            ((aggressorCount > 1 && knowsFrostNova && !frostNovaReady) ||
                (targetHealthPercent >= 80 &&
                    ((knowsIcyVeins && !icyVeinsReady) ||
                        (knowsSummonWaterElemental && !summonWaterElementalReady))));

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
