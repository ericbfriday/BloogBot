using BloogBot.Game.Enums;

namespace RetributionPaladinBot
{
    enum SelfBuffCastMode
    {
        LuaCastOnSelf,
        CastSpellOnPlayer
    }

    static class RetributionPaladinBuffSelfState
    {
        internal const string BlessingOfKings = "Blessing of Kings";
        internal const string BlessingOfMight = "Blessing of Might";
        internal const string BlessingOfSanctuary = "Blessing of Sanctuary";

        internal static bool HasRequiredBlessing(
            bool knowsBlessingOfMight,
            bool hasBlessingOfMight,
            bool hasBlessingOfKings,
            bool hasBlessingOfSanctuary) =>
            !knowsBlessingOfMight ||
            hasBlessingOfMight ||
            hasBlessingOfKings ||
            hasBlessingOfSanctuary;

        internal static string SelectBlessing(
            bool knowsBlessingOfMight,
            bool knowsBlessingOfKings,
            bool knowsBlessingOfSanctuary)
        {
            if (knowsBlessingOfSanctuary)
                return BlessingOfSanctuary;

            if (knowsBlessingOfKings)
                return BlessingOfKings;

            return knowsBlessingOfMight ? BlessingOfMight : null;
        }

        internal static bool ShouldCastBlessing(
            bool hasBuff,
            bool knowsSpell,
            bool isSpellReady,
            bool hasEnoughMana) =>
            !hasBuff &&
            knowsSpell &&
            isSpellReady &&
            hasEnoughMana;

        internal static SelfBuffCastMode GetSelfBuffCastMode(ClientVersion clientVersion) =>
            clientVersion == ClientVersion.Vanilla
                ? SelfBuffCastMode.LuaCastOnSelf
                : SelfBuffCastMode.CastSpellOnPlayer;
    }
}
