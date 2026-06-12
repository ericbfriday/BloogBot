using BloogBot.Game.Enums;
using EnhancementShamanBot;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BloogBotTests
{
    [TestClass]
    public class EnhancementShamanRotationTests
    {
        [TestMethod]
        public void WotlkFireNovaRequiresActiveFireTotem()
        {
            Assert.IsFalse(EnhancementShamanRotation.ShouldUseWotlkFireNova(ClientVersion.WotLK, knowsFireNova: true, hasActiveFireTotem: false, targetIsFireImmune: false, manaPercent: 100));
            Assert.IsTrue(EnhancementShamanRotation.ShouldUseWotlkFireNova(ClientVersion.WotLK, knowsFireNova: true, hasActiveFireTotem: true, targetIsFireImmune: false, manaPercent: 100));
            Assert.IsFalse(EnhancementShamanRotation.ShouldUseWotlkFireNova(ClientVersion.TBC, knowsFireNova: true, hasActiveFireTotem: true, targetIsFireImmune: false, manaPercent: 100));
        }

        [TestMethod]
        public void LegacyClientsUseFireNovaTotemInsteadOfWotlkFireNova()
        {
            Assert.IsTrue(EnhancementShamanRotation.ShouldUseLegacyFireNovaTotem(ClientVersion.TBC, knowsFireNovaTotem: true, targetIsFireImmune: false, nearbyEnemyCount: 2));
            Assert.IsTrue(EnhancementShamanRotation.ShouldUseLegacyFireNovaTotem(ClientVersion.Vanilla, knowsFireNovaTotem: true, targetIsFireImmune: false, nearbyEnemyCount: 2));
            Assert.IsFalse(EnhancementShamanRotation.ShouldUseLegacyFireNovaTotem(ClientVersion.WotLK, knowsFireNovaTotem: true, targetIsFireImmune: false, nearbyEnemyCount: 2));
        }

        [TestMethod]
        public void MaelstromPrefersChainLightningForMultipleEnemies()
        {
            Assert.AreEqual(EnhancementShamanRotation.ChainLightning, EnhancementShamanRotation.SelectMaelstromSpell(
                maelstromStacks: 5,
                knowsChainLightning: true,
                knowsLightningBolt: true,
                targetIsNatureImmune: false,
                nearbyEnemyCount: 2));

            Assert.AreEqual(EnhancementShamanRotation.LightningBolt, EnhancementShamanRotation.SelectMaelstromSpell(
                maelstromStacks: 5,
                knowsChainLightning: true,
                knowsLightningBolt: true,
                targetIsNatureImmune: false,
                nearbyEnemyCount: 1));
        }

        [TestMethod]
        public void ShieldSelectionUsesWaterShieldForWotlkManaPressure()
        {
            Assert.AreEqual(EnhancementShamanRotation.WaterShield, EnhancementShamanRotation.SelectShield(
                ClientVersion.WotLK,
                knowsWaterShield: true,
                hasWaterShield: false,
                knowsLightningShield: true,
                hasLightningShield: false,
                isAoE: true,
                manaPercent: 100));

            Assert.AreEqual(EnhancementShamanRotation.LightningShield, EnhancementShamanRotation.SelectShield(
                ClientVersion.TBC,
                knowsWaterShield: true,
                hasWaterShield: false,
                knowsLightningShield: true,
                hasLightningShield: false,
                isAoE: true,
                manaPercent: 20));
        }

        [TestMethod]
        public void WotlkLevelingPrefersFlametongueWhileLegacyKeepsWindfuryPriority()
        {
            Assert.AreEqual(EnhancementShamanRotation.FlametongueWeapon, EnhancementShamanRotation.SelectMainhandWeaponEnchant(
                ClientVersion.WotLK,
                knowsFlametongueWeapon: true,
                knowsWindfuryWeapon: true,
                knowsRockbiterWeapon: true));

            Assert.AreEqual(EnhancementShamanRotation.WindfuryWeapon, EnhancementShamanRotation.SelectMainhandWeaponEnchant(
                ClientVersion.TBC,
                knowsFlametongueWeapon: true,
                knowsWindfuryWeapon: true,
                knowsRockbiterWeapon: true));
        }

        [TestMethod]
        public void SelfHealingPrefersLesserHealingWaveWhenDamaged()
        {
            Assert.AreEqual(EnhancementShamanRotation.LesserHealingWave, EnhancementShamanRotation.SelectSelfHeal(
                healthPercent: 55,
                maelstromStacks: 0,
                canUseLesserHealingWave: true,
                canUseHealingWave: true,
                canUseChainHeal: true));
        }

        [TestMethod]
        public void CriticalSelfHealingUsesMaelstromHealingWave()
        {
            Assert.AreEqual(EnhancementShamanRotation.HealingWave, EnhancementShamanRotation.SelectSelfHeal(
                healthPercent: 25,
                maelstromStacks: 5,
                canUseLesserHealingWave: true,
                canUseHealingWave: true,
                canUseChainHeal: true));
        }

        [TestMethod]
        public void SelfHealingDoesNotInterruptWhenStable()
        {
            Assert.IsNull(EnhancementShamanRotation.SelectSelfHeal(
                healthPercent: 75,
                maelstromStacks: 5,
                canUseLesserHealingWave: true,
                canUseHealingWave: true,
                canUseChainHeal: true));
        }
    }
}
