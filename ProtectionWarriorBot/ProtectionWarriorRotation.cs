using BloogBot.Game.Enums;

namespace ProtectionWarriorBot
{
    static class ProtectionWarriorRotation
    {
        internal const string DemoralizingShout = "Demoralizing Shout";
        internal const string ThunderClap = "Thunder Clap";

        // With multiple aggressors, first establish AoE threat (Demo Shout + Thunder Clap).
        internal static bool ShouldUseMultiTargetAbilities(
            int aggressorCount,
            bool targetHasDemoralizingShout,
            bool targetHasThunderClap) =>
            aggressorCount >= 2 &&
            (!targetHasDemoralizingShout || !targetHasThunderClap);

        // Single-target rotation runs for one aggressor, or once AoE threat is established.
        internal static bool ShouldUseSingleTargetAbilities(
            int aggressorCount,
            bool targetHasDemoralizingShout,
            bool targetHasThunderClap) =>
            aggressorCount == 1 ||
            (targetHasDemoralizingShout && targetHasThunderClap);

        internal static bool ShouldRend(
            int targetHealthPercent,
            bool targetHasRend,
            CreatureType targetCreatureType) =>
            targetHealthPercent > 50 &&
            !targetHasRend &&
            targetCreatureType != CreatureType.Elemental &&
            targetCreatureType != CreatureType.Undead;
    }
}
