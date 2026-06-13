using BloogBot.Game.Enums;

namespace FuryWarriorBot
{
    static class FuryWarriorRotation
    {
        internal const string BattleStance = "Battle Stance";
        internal const string BerserkerStance = "Berserker Stance";

        internal static int HeroicStrikeRageRequirement(int playerLevel) =>
            playerLevel < 30 ? 15 : 45;

        // Stance dance: swap to Berserker once Rend is applied (or not worth applying).
        internal static bool ShouldEnterBerserkerStance(
            int playerLevel,
            string currentStance,
            bool targetHasRend,
            int targetHealthPercent,
            CreatureType targetCreatureType) =>
            playerLevel >= 30 &&
            currentStance == BattleStance &&
            (targetHasRend ||
                targetHealthPercent < 80 ||
                targetCreatureType == CreatureType.Elemental ||
                targetCreatureType == CreatureType.Undead);

        internal static bool ShouldPummel(
            string currentStance,
            int targetMana,
            bool targetIsCasting,
            bool targetIsChanneling) =>
            currentStance == BerserkerStance &&
            targetMana > 0 &&
            (targetIsCasting || targetIsChanneling);

        internal static bool ShouldWhirlwind(
            string currentStance,
            int targetHealthPercent,
            bool targetIsFeared,
            bool aggressorsInMelee) =>
            currentStance == BerserkerStance &&
            targetHealthPercent > 20 &&
            !targetIsFeared &&
            aggressorsInMelee;

        // If our target uses melee, but there's a caster attacking us, do not use Retaliation.
        internal static bool ShouldUseRetaliation(
            bool retaliationReady,
            int spellcastingAggressorCount,
            string currentStance,
            bool facingAllTargets,
            bool anyAggressorFeared) =>
            retaliationReady &&
            spellcastingAggressorCount == 0 &&
            currentStance == BattleStance &&
            facingAllTargets &&
            !anyAggressorFeared;
    }
}
