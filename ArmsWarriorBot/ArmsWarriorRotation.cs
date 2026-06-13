using BloogBot.Game.Enums;
using System.Linq;

namespace ArmsWarriorBot
{
    static class ArmsWarriorRotation
    {
        internal const string DemoralizingShout = "Demoralizing Shout";
        internal const string Hamstring = "Hamstring";
        internal const string HeroicStrike = "Heroic Strike";
        internal const string Rend = "Rend";
        internal const string SunderArmor = "Sunder Armor";
        internal const string SweepingStrikes = "Sweeping Strikes";
        internal const string ThunderClap = "Thunder Clap";

        // High-armor, low-damage mob families where stacking Sunder Armor pays off.
        static readonly string[] SunderTargets = { "Snapjaw", "Snapper", "Tortoise", "Spikeshell", "Burrower", "Borer", // turtles
            "Bear", "Grizzly", "Ashclaw", "Mauler", "Shardtooth", "Plaguebear", "Bristlefur", "Thistlefur", // bears
            "Scorpid", "Flayer", "Stinger", "Lasher", "Pincer", // scorpids
            "Crocolisk", "Vicejaw", "Deadmire", "Snapper", "Daggermaw", // crocs
            "Crawler", "Crustacean", // crabs
            "Stag" }; // other

        internal static bool IsSunderTarget(string targetName) =>
            targetName != null && SunderTargets.Any(targetName.Contains);

        internal static int HeroicStrikeRageRequirement(int playerLevel) =>
            playerLevel < 30 ? 15 : 45;

        internal static bool ShouldHamstring(
            bool targetIsHumanoid,
            string targetName,
            int targetHealthPercent,
            bool targetHasHamstring) =>
            (targetIsHumanoid || (targetName != null && targetName.Contains("Plainstrider"))) &&
            targetHealthPercent < 30 &&
            !targetHasHamstring;

        internal static bool ShouldRend(
            int targetHealthPercent,
            bool targetHasRend,
            CreatureType targetCreatureType) =>
            targetHealthPercent > 50 &&
            !targetHasRend &&
            targetCreatureType != CreatureType.Elemental &&
            targetCreatureType != CreatureType.Undead;

        internal static bool ShouldSunderArmor(
            int? sunderStackCount,
            int targetLevel,
            int playerLevel,
            int targetHealth,
            bool targetIsSunderTarget) =>
            (sunderStackCount == null || sunderStackCount < 5) &&
            targetLevel >= playerLevel - 2 &&
            targetHealth > 40 &&
            targetIsSunderTarget;

        // In an AoE pull, fall back to the single-target fillers only once the AoE
        // tools are either applied, unknown, or no longer worth using.
        internal static bool ShouldUseSingleTargetFillersInAoe(
            bool targetHasThunderClap,
            bool knowsThunderClap,
            bool targetHasDemoralizingShout,
            bool knowsDemoralizingShout,
            int targetHealthPercent,
            bool hasSweepingStrikes,
            bool sweepingStrikesReady) =>
            (targetHasThunderClap || !knowsThunderClap || targetHealthPercent < 50) &&
            (targetHasDemoralizingShout || !knowsDemoralizingShout || targetHealthPercent < 50) &&
            (hasSweepingStrikes || !sweepingStrikesReady);
    }
}
