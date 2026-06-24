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

        // Retaliation is reserved for being swarmed by three or more aggressors.
        internal static bool ShouldUseRetaliation(int aggressorCount) =>
            aggressorCount >= 3;

        // Demoralizing Shout (AoE attack-power debuff) is refreshed only when it isn't
        // already on the target and every aggressor is inside its 10-yard radius.
        internal static bool ShouldUseDemoralizingShout(
            bool targetHasDemoralizingShout,
            bool allAggressorsWithin10Yards) =>
            !targetHasDemoralizingShout &&
            allAggressorsWithin10Yards;

        // Thunder Clap (AoE attack-speed debuff) follows the same upkeep rule as Demo Shout.
        internal static bool ShouldUseThunderClap(
            bool targetHasThunderClap,
            bool allAggressorsWithin10Yards) =>
            !targetHasThunderClap &&
            allAggressorsWithin10Yards;

        // Last Stand is an emergency cooldown popped when health drops to 8% or below.
        internal static bool ShouldUseLastStand(int playerHealthPercent) =>
            playerHealthPercent <= 8;

        // Shield Bash interrupts a casting target that still has a mana bar to interrupt.
        internal static bool ShouldUseShieldBash(
            bool targetIsCasting,
            int targetMana) =>
            targetIsCasting &&
            targetMana > 0;

        // Shield Slam is the core single-target threat strike above the Execute range.
        internal static bool ShouldUseShieldSlam(int targetHealthPercent) =>
            targetHealthPercent > 30;
    }
}
