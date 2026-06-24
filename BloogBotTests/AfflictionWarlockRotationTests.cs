using AfflictionWarlockBot;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BloogBotTests
{
    [TestClass]
    public class AfflictionWarlockRotationTests
    {
        [TestMethod]
        public void DrainSoulFinishesDyingTargets()
        {
            Assert.IsTrue(AfflictionWarlockRotation.ShouldDrainSoul(20));
            Assert.IsFalse(AfflictionWarlockRotation.ShouldDrainSoul(21));
        }

        [TestMethod]
        public void WandIsUsedWhileDotsFinishAWoundedTarget()
        {
            Assert.IsTrue(AfflictionWarlockRotation.ShouldUseWand(true, 100, 60, false, false));
            Assert.IsFalse(AfflictionWarlockRotation.ShouldUseWand(true, 100, 61, false, false));
            Assert.IsFalse(AfflictionWarlockRotation.ShouldUseWand(true, 100, 20, false, false));
            Assert.IsFalse(AfflictionWarlockRotation.ShouldUseWand(true, 100, 60, true, false));
            Assert.IsFalse(AfflictionWarlockRotation.ShouldUseWand(false, 5, 60, false, false));
        }

        [TestMethod]
        public void WandIsUsedWhenLowOnManaEvenMidCast()
        {
            // Low mana is checked before the casting guards in the original expression.
            Assert.IsTrue(AfflictionWarlockRotation.ShouldUseWand(true, 10, 100, true, true));
        }

        [TestMethod]
        public void LifeTapConvertsSurplusHealthIntoMana()
        {
            Assert.IsTrue(AfflictionWarlockRotation.ShouldLifeTap(86, 79));
            Assert.IsFalse(AfflictionWarlockRotation.ShouldLifeTap(85, 79));
            Assert.IsFalse(AfflictionWarlockRotation.ShouldLifeTap(86, 80));
        }

        [TestMethod]
        public void DeathCoilInterruptsCastersWorthInterrupting()
        {
            Assert.IsTrue(AfflictionWarlockRotation.ShouldDeathCoil(true, false, 50));
            Assert.IsTrue(AfflictionWarlockRotation.ShouldDeathCoil(false, true, 50));
            Assert.IsFalse(AfflictionWarlockRotation.ShouldDeathCoil(false, false, 50));
            Assert.IsFalse(AfflictionWarlockRotation.ShouldDeathCoil(true, false, 20));
        }

        [TestMethod]
        public void DotsRespectTheirHealthFloors()
        {
            Assert.IsTrue(AfflictionWarlockRotation.ShouldApplyDot(false, 91, 90));
            Assert.IsFalse(AfflictionWarlockRotation.ShouldApplyDot(false, 90, 90));
            Assert.IsFalse(AfflictionWarlockRotation.ShouldApplyDot(true, 91, 90));
            Assert.IsTrue(AfflictionWarlockRotation.ShouldApplyDot(false, 31, 30));
            Assert.IsFalse(AfflictionWarlockRotation.ShouldApplyDot(false, 30, 30));
        }

        [TestMethod]
        public void ShadowBoltIsAlwaysAvailableWithoutAWand()
        {
            Assert.IsTrue(AfflictionWarlockRotation.ShouldShadowBolt(41, true));
            Assert.IsFalse(AfflictionWarlockRotation.ShouldShadowBolt(40, true));
            Assert.IsTrue(AfflictionWarlockRotation.ShouldShadowBolt(40, false));
        }

        // Priority-chain invariant: a wand-equipped warlock hands off from hard-cast
        // Shadow Bolt to Drain Soul at the bottom of the health bar with no overlap.
        [TestMethod]
        public void DrainSoulAndShadowBoltCoverComplementaryHealthBands()
        {
            Assert.IsTrue(AfflictionWarlockRotation.ShouldDrainSoul(20));
            Assert.IsFalse(AfflictionWarlockRotation.ShouldShadowBolt(20, true));

            Assert.IsFalse(AfflictionWarlockRotation.ShouldDrainSoul(41));
            Assert.IsTrue(AfflictionWarlockRotation.ShouldShadowBolt(41, true));
        }
    }
}
