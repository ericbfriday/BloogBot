using BloogBot.Game.Enums;

namespace EnhancementShamanBot
{
    static class EnhancementShamanRotation
    {
        internal const string ChainHeal = "Chain Heal";
        internal const string ChainLightning = "Chain Lightning";
        internal const string FlametongueWeapon = "Flametongue Weapon";
        internal const string HealingStreamTotem = "Healing Stream Totem";
        internal const string HealingWave = "Healing Wave";
        internal const string LesserHealingWave = "Lesser Healing Wave";
        internal const string LightningBolt = "Lightning Bolt";
        internal const string LightningShield = "Lightning Shield";
        internal const string RockbiterWeapon = "Rockbiter Weapon";
        internal const string WaterShield = "Water Shield";
        internal const string WindfuryWeapon = "Windfury Weapon";

        internal static bool ShouldUseWotlkFireNova(
            ClientVersion clientVersion,
            bool knowsFireNova,
            bool hasActiveFireTotem,
            bool targetIsFireImmune,
            int manaPercent) =>
            clientVersion == ClientVersion.WotLK &&
            knowsFireNova &&
            hasActiveFireTotem &&
            !targetIsFireImmune &&
            manaPercent >= 25;

        internal static bool ShouldUseLegacyFireNovaTotem(
            ClientVersion clientVersion,
            bool knowsFireNovaTotem,
            bool targetIsFireImmune,
            int nearbyEnemyCount) =>
            clientVersion != ClientVersion.WotLK &&
            knowsFireNovaTotem &&
            !targetIsFireImmune &&
            nearbyEnemyCount > 1;

        internal static string SelectMaelstromSpell(
            int maelstromStacks,
            bool knowsChainLightning,
            bool knowsLightningBolt,
            bool targetIsNatureImmune,
            int nearbyEnemyCount)
        {
            if (maelstromStacks < 5 || targetIsNatureImmune)
                return null;

            if (nearbyEnemyCount > 1 && knowsChainLightning)
                return ChainLightning;

            return knowsLightningBolt ? LightningBolt : null;
        }

        internal static string SelectSelfHeal(
            int healthPercent,
            int maelstromStacks,
            bool canUseLesserHealingWave,
            bool canUseHealingWave,
            bool canUseChainHeal)
        {
            if (healthPercent >= 65)
                return null;

            if (maelstromStacks >= 5 && healthPercent < 35 && canUseHealingWave)
                return HealingWave;

            if (canUseLesserHealingWave)
                return LesserHealingWave;

            if (healthPercent < 45 && canUseHealingWave)
                return HealingWave;

            if (maelstromStacks >= 5 && canUseChainHeal)
                return ChainHeal;

            return null;
        }

        internal static string SelectShield(
            ClientVersion clientVersion,
            bool knowsWaterShield,
            bool hasWaterShield,
            bool knowsLightningShield,
            bool hasLightningShield,
            bool isAoE,
            int manaPercent)
        {
            if (clientVersion == ClientVersion.WotLK &&
                knowsWaterShield &&
                !hasWaterShield &&
                (isAoE || manaPercent < 35))
                return WaterShield;

            if (knowsLightningShield && !hasLightningShield)
                return LightningShield;

            return null;
        }

        internal static string SelectMainhandWeaponEnchant(
            ClientVersion clientVersion,
            bool knowsFlametongueWeapon,
            bool knowsWindfuryWeapon,
            bool knowsRockbiterWeapon)
        {
            if (clientVersion == ClientVersion.WotLK && knowsFlametongueWeapon)
                return FlametongueWeapon;

            if (knowsWindfuryWeapon)
                return WindfuryWeapon;

            if (knowsFlametongueWeapon)
                return FlametongueWeapon;

            return knowsRockbiterWeapon ? RockbiterWeapon : null;
        }

        internal static string SelectOffhandWeaponEnchant(
            bool knowsFlametongueWeapon,
            bool knowsWindfuryWeapon) =>
            knowsFlametongueWeapon
                ? FlametongueWeapon
                : knowsWindfuryWeapon ? WindfuryWeapon : null;
    }
}
