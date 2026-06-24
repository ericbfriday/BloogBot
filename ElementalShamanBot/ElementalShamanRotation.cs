using System.Linq;

namespace ElementalShamanBot
{
    static class ElementalShamanRotation
    {
        internal const string FlametongueWeapon = "Flametongue Weapon";
        internal const string RockbiterWeapon = "Rockbiter Weapon";

        static readonly string[] FearingCreatures = { "Scorpid Terror" };
        static readonly string[] FireImmuneCreatures = { "Rogue Flame Spirit", "Burning Destroyer" };
        static readonly string[] NatureImmuneCreatures = { "Swirling Vortex", "Gusting Vortex", "Dust Stormer" };

        internal static bool IsFearingCreature(string targetName) =>
            FearingCreatures.Contains(targetName);

        internal static bool IsFireImmune(string targetName) =>
            FireImmuneCreatures.Contains(targetName);

        internal static bool IsNatureImmune(string targetName) =>
            NatureImmuneCreatures.Contains(targetName);

        internal static bool ShouldEarthShock(
            bool targetIsNatureImmune,
            bool targetIsCasting,
            bool targetIsChanneling,
            bool hasClearcasting) =>
            !targetIsNatureImmune &&
            (targetIsCasting || targetIsChanneling || hasClearcasting);

        // Lightning Bolt only when the cast can finish before the target reaches
        // melee, or when Focused Casting makes pushback a non-issue.
        internal static bool ShouldLightningBolt(
            bool targetIsNatureImmune,
            bool targetMovingTowardPlayer,
            float distanceToTarget,
            bool hasFocusedCasting,
            int targetHealthPercent,
            bool focusedCastingDelayElapsed) =>
            !targetIsNatureImmune &&
            ((targetMovingTowardPlayer && distanceToTarget > 15) ||
                (!targetMovingTowardPlayer && distanceToTarget > 5) ||
                (hasFocusedCasting && targetHealthPercent > 20 && focusedCastingDelayElapsed));

        internal static bool ShouldFlameShock(
            bool targetHasFlameShock,
            int targetHealthPercent,
            bool targetIsNatureImmune,
            bool targetIsFireImmune) =>
            !targetHasFlameShock &&
            (targetHealthPercent >= 50 || targetIsNatureImmune) &&
            !targetIsFireImmune;

        // Totem drops: each is gated on the totem not already being live nearby so we
        // don't waste a global cooldown redropping an active totem.
        internal static bool ShouldGroundingTotem(bool anyAggressorCasting, int targetMana) =>
            anyAggressorCasting && targetMana > 0;

        internal static bool ShouldTremorTotem(bool targetIsFearingCreature, bool tremorTotemNearby) =>
            targetIsFearingCreature && !tremorTotemNearby;

        internal static bool ShouldStoneclawTotem(int aggressorCount) =>
            aggressorCount > 1;

        internal static bool ShouldStoneskinTotem(int targetMana, bool earthTotemNearby) =>
            targetMana == 0 && !earthTotemNearby;

        internal static bool ShouldSearingTotem(
            int targetHealthPercent,
            bool targetIsFireImmune,
            float distanceToTarget,
            bool searingTotemNearby) =>
            targetHealthPercent > 70 &&
            !targetIsFireImmune &&
            distanceToTarget < 20 &&
            !searingTotemNearby;

        internal static bool ShouldManaSpringTotem(bool manaSpringTotemNearby) =>
            !manaSpringTotemNearby;

        internal static bool ShouldLightningShield(bool targetIsNatureImmune, bool hasLightningShield) =>
            !targetIsNatureImmune && !hasLightningShield;

        internal static string SelectWeaponEnchant(
            bool knowsRockbiterWeapon,
            bool knowsFlametongueWeapon,
            bool mainhandIsEnchanted,
            bool targetIsFireImmune)
        {
            if (knowsRockbiterWeapon &&
                (targetIsFireImmune || (!mainhandIsEnchanted && !knowsFlametongueWeapon)))
                return RockbiterWeapon;

            if (knowsFlametongueWeapon && !mainhandIsEnchanted && !targetIsFireImmune)
                return FlametongueWeapon;

            return null;
        }
    }
}
