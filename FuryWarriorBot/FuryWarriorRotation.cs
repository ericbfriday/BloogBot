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

        // Death Wish / Blood Fury are damage cooldowns saved for healthy targets.
        internal static bool ShouldUseDeathWish(bool deathWishReady, int targetHealthPercent) =>
            deathWishReady && targetHealthPercent > 80;

        internal static bool ShouldUseBloodFury(int targetHealthPercent) =>
            targetHealthPercent > 80;

        // Refresh Battle Shout whenever the buff has dropped.
        internal static bool ShouldUseBattleShout(bool hasBattleShout) =>
            !hasBattleShout;

        // Bloodrage builds rage early in the fight, before the target is low.
        internal static bool ShouldUseBloodrage(int targetHealthPercent) =>
            targetHealthPercent > 50;

        // Execute is the sub-20% finisher.
        internal static bool ShouldExecute(int targetHealthPercent) =>
            targetHealthPercent < 20;

        // Berserker Rage is only usable in Berserker Stance and saved for healthy targets.
        internal static bool ShouldUseBerserkerRage(int targetHealthPercent, string currentStance) =>
            targetHealthPercent > 70 && currentStance == BerserkerStance;

        // Overpower requires Battle Stance and a dodge proc.
        internal static bool ShouldOverpower(string currentStance, bool canOverpower) =>
            currentStance == BattleStance && canOverpower;

        // Intimidating Shout fears the pack; skip if already feared or while Retaliation is up,
        // and only fire when every aggressor is bunched in range.
        internal static bool ShouldUseIntimidatingShout(
            bool targetFeared,
            bool hasRetaliation,
            bool allAggressorsInRange) =>
            !(targetFeared || hasRetaliation) && allAggressorsInRange;

        // Demoralizing Shout is a debuff; only refresh when it is missing.
        internal static bool ShouldUseDemoralizingShout(bool targetHasDemoralizingShout) =>
            !targetHasDemoralizingShout;

        // Slam is only worth casting on the proc window above execute range.
        internal static bool ShouldSlam(int targetHealthPercent, bool slamReady) =>
            targetHealthPercent > 20 && slamReady;

        // Hamstring snares fleeing humanoids; refresh only when missing.
        internal static bool ShouldHamstring(CreatureType targetCreatureType, bool targetHasHamstring) =>
            targetCreatureType == CreatureType.Humanoid && !targetHasHamstring;

        // Heroic Strike is the rage dump above execute range.
        internal static bool ShouldHeroicStrike(int targetHealthPercent) =>
            targetHealthPercent > 30;

        // Sunder Armor is a debuff applied once the target has taken some damage.
        internal static bool ShouldSunderArmor(int targetHealthPercent, bool targetHasSunderArmor) =>
            targetHealthPercent < 80 && !targetHasSunderArmor;
    }
}
