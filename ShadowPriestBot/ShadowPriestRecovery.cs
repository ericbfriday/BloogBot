namespace ShadowPriestBot
{
    static class ShadowPriestRecovery
    {
        internal const string AbolishDisease = "Abolish Disease";
        internal const string CureDisease = "Cure Disease";
        internal const string Heal = "Heal";
        internal const string LesserHeal = "Lesser Heal";
        internal const string Shadowform = "Shadowform";

        internal static string SelectHeal(
            int healthPercent,
            bool canCastHeal,
            bool canCastLesserHeal)
        {
            if (healthPercent < 50 && canCastHeal)
                return Heal;

            if (canCastLesserHeal)
                return LesserHeal;

            return canCastHeal ? Heal : null;
        }

        internal static string SelectDiseaseCure(bool knowsAbolishDisease, bool knowsCureDisease)
        {
            if (knowsAbolishDisease)
                return AbolishDisease;

            return knowsCureDisease ? CureDisease : null;
        }

        internal static bool ShouldLeaveShadowform(bool hasShadowform, string selectedHeal) =>
            hasShadowform && selectedHeal != null;
    }
}
