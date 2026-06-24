using System.Linq;

namespace BalanceDruidBot
{
    static class BalanceDruidRotation
    {
        static readonly string[] ImmuneToNatureDamage = { "Vortex", "Whirlwind", "Whirling", "Dust", "Cyclone" };

        internal static bool IsNatureImmune(string targetName) =>
            targetName != null && ImmuneToNatureDamage.Any(targetName.Contains);

        internal static bool ShouldHealSelf(
            int playerHealthPercent,
            bool canAffordHealingTouch,
            bool canAffordRejuvenation) =>
            playerHealthPercent < 30 && (canAffordHealingTouch || canAffordRejuvenation);

        // Cleansing requires caster form; never break Moonkin Form for it.
        internal static bool ShouldCleanseSelf(bool isAfflicted, bool inMoonkinForm) =>
            isAfflicted && !inMoonkinForm;

        internal static bool ShouldInsectSwarm(
            bool targetHasInsectSwarm,
            int targetHealthPercent,
            bool targetIsNatureImmune) =>
            !targetHasInsectSwarm &&
            targetHealthPercent > 20 &&
            !targetIsNatureImmune;

        // Refresh Moonfire only once the dot has dropped.
        internal static bool ShouldMoonfire(bool targetHasMoonfire) =>
            !targetHasMoonfire;

        // Wrath is the nature-damage nuke; useless against nature-immune targets.
        internal static bool ShouldWrath(bool targetIsNatureImmune) =>
            !targetIsNatureImmune;

        // Tap the mana battery before it runs dry.
        internal static bool ShouldInnervate(int manaPercent) =>
            manaPercent < 10;

        // Top off a wounded power-level target.
        internal static bool ShouldRegrowth(int powerlevelTargetHealthPercent) =>
            powerlevelTargetHealthPercent < 40;
    }
}
