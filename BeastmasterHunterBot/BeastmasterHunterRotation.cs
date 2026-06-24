using BloogBot.Game.Enums;

namespace BeastMasterHunterBot
{
    static class BeastmasterHunterRotation
    {
        internal const string AspectOfTheDragonhawk = "Aspect of the Dragonhawk";
        internal const string AspectOfTheHawk = "Aspect of the Hawk";
        internal const string AspectOfTheViper = "Aspect of the Viper";

        internal const string AimedShot = "Aimed Shot";
        internal const string ArcaneShot = "Arcane Shot";
        internal const string KillShot = "Kill Shot";
        internal const string MultiShot = "Multi-Shot";
        internal const string SteadyShot = "Steady Shot";

        const int ViperManaThresholdPct = 20;
        const int DamageAspectManaThresholdPct = 80;
        const int HuntersMarkMinTargetHealthPct = 20;
        const int HuntersMarkMinManaPct = 10;
        const int SerpentStingMinTargetHealthPct = 35;
        const int SerpentStingMinManaPct = 25;
        const int AimedShotMinManaPct = 25;
        const int MultiShotMinManaPct = 40;
        const int SteadyShotMinManaPct = 15;
        const int ArcaneShotMinManaPct = 45;

        internal static bool TargetHasAura(bool targetHasDebuff, bool targetHasBuff) =>
            targetHasDebuff || targetHasBuff;

        internal static bool ShouldApplyHuntersMark(
            bool targetHasHuntersMarkDebuff,
            bool targetHasHuntersMarkBuff,
            int targetHealthPercent,
            int manaPercent) =>
            !TargetHasAura(targetHasHuntersMarkDebuff, targetHasHuntersMarkBuff) &&
            targetHealthPercent >= HuntersMarkMinTargetHealthPct &&
            manaPercent >= HuntersMarkMinManaPct;

        internal static bool ShouldApplySerpentSting(
            bool targetHasSerpentStingDebuff,
            bool targetHasSerpentStingBuff,
            int targetHealthPercent,
            int manaPercent) =>
            !TargetHasAura(targetHasSerpentStingDebuff, targetHasSerpentStingBuff) &&
            targetHealthPercent >= SerpentStingMinTargetHealthPct &&
            manaPercent >= SerpentStingMinManaPct;

        internal static string SelectAspect(
            ClientVersion clientVersion,
            bool knowsAspectOfViper,
            bool hasAspectOfViper,
            bool knowsAspectOfDragonhawk,
            bool hasAspectOfDragonhawk,
            bool knowsAspectOfHawk,
            bool hasAspectOfHawk,
            int manaPercent)
        {
            if (clientVersion == ClientVersion.Vanilla || !knowsAspectOfViper)
                return null;

            if (manaPercent < ViperManaThresholdPct)
                return hasAspectOfViper ? null : AspectOfTheViper;

            if (manaPercent < DamageAspectManaThresholdPct || !hasAspectOfViper)
                return null;

            if (knowsAspectOfDragonhawk && !hasAspectOfDragonhawk)
                return AspectOfTheDragonhawk;

            if (knowsAspectOfHawk && !hasAspectOfHawk)
                return AspectOfTheHawk;

            return null;
        }

        internal static string SelectRangedShot(
            ClientVersion clientVersion,
            int targetHealthPercent,
            int manaPercent,
            bool canKillShot,
            bool canAimedShot,
            bool canMultiShot,
            bool canSteadyShot,
            bool canArcaneShot,
            bool isMoving)
        {
            if (clientVersion == ClientVersion.WotLK && targetHealthPercent <= 20 && canKillShot)
                return KillShot;

            if (clientVersion == ClientVersion.WotLK &&
                canAimedShot &&
                targetHealthPercent > 20 &&
                manaPercent >= AimedShotMinManaPct)
                return AimedShot;

            if (!isMoving &&
                canMultiShot &&
                targetHealthPercent > 20 &&
                manaPercent >= MultiShotMinManaPct)
                return MultiShot;

            if (!isMoving &&
                canSteadyShot &&
                targetHealthPercent > 10 &&
                manaPercent >= SteadyShotMinManaPct)
                return SteadyShot;

            if (canArcaneShot &&
                targetHealthPercent > 20 &&
                manaPercent >= ArcaneShotMinManaPct)
                return ArcaneShot;

            return null;
        }

        internal static bool ShouldUsePetOffensiveCooldown(
            bool petAlive,
            bool petHasCooldownBuff,
            int targetHealthPercent,
            int aggressorCount) =>
            petAlive &&
            !petHasCooldownBuff &&
            (targetHealthPercent >= 30 || aggressorCount > 1);

        internal static bool ShouldUseHunterOffensiveCooldown(
            bool hunterHasCooldownBuff,
            int targetHealthPercent,
            int aggressorCount) =>
            !hunterHasCooldownBuff &&
            (targetHealthPercent >= 50 || aggressorCount > 1);

        internal static bool ShouldUseMisdirection(
            bool petAlive,
            bool targetIsTargetingPlayer,
            int targetHealthPercent,
            int aggressorCount) =>
            petAlive &&
            targetHealthPercent > 20 &&
            (targetIsTargetingPlayer || aggressorCount > 1);

        internal static bool ShouldUseDefensiveCooldown(
            bool targetIsTargetingPlayer,
            int playerHealthPercent,
            bool playerHasDefensiveBuff) =>
            targetIsTargetingPlayer &&
            playerHealthPercent < 30 &&
            !playerHasDefensiveBuff;
    }
}
