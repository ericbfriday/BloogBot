using System;

namespace FeralDruidBot
{
    static class FeralDruidRotation
    {
        internal const string BearForm = "Bear Form";
        internal const string CatForm = "Cat Form";

        // Caster until 13 (no forms), Bear Form until Cat Form at 20.
        internal static string SelectForm(int playerLevel)
        {
            if (playerLevel <= 12)
                return null;
            return playerLevel < 20 ? BearForm : CatForm;
        }

        // Maul gets cheaper relative to the rage pool as the bear levels.
        internal static int MaulRageRequirement(int playerLevel) =>
            Math.Max(15 - (playerLevel - 9), 10);

        // At caster levels, conserve mana by meleeing once the pool runs low.
        internal static bool ShouldMoveIntoMeleeForMana(int manaPercent, float distanceToTarget) =>
            manaPercent < 20 && distanceToTarget > 5;

        internal static bool CanPullWithWrath(
            bool knowsSpell,
            bool spellReady,
            int mana,
            int manaCost,
            bool isStunned) =>
            knowsSpell && spellReady && mana >= manaCost && !isStunned;

        internal static bool CanPullWithFeralCharge(
            bool knowsSpell,
            bool spellReady,
            int energy,
            bool inCatForm,
            bool isStunned) =>
            knowsSpell && spellReady && energy >= 10 && inCatForm && !isStunned;

        internal static bool CanUseBearAbility(
            bool spellReady,
            int rage,
            int requiredRage,
            bool isStunned,
            bool inBearForm,
            bool condition) =>
            spellReady && rage >= requiredRage && !isStunned && inBearForm && condition;

        internal static bool CanUseCatAbility(
            bool spellReady,
            int energy,
            int requiredEnergy,
            bool requiresComboPoints,
            int comboPoints,
            bool isStunned,
            bool inCatForm,
            bool condition) =>
            spellReady &&
            energy >= requiredEnergy &&
            (!requiresComboPoints || comboPoints > 0) &&
            !isStunned &&
            inCatForm &&
            condition;

        internal static bool ShouldRip(bool targetHasRip, int comboPoints) =>
            !targetHasRip && comboPoints >= 5;
    }
}
