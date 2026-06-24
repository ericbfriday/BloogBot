using BalanceDruidBot;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BloogBotTests
{
    [TestClass]
    public class BalanceDruidRotationTests
    {
        [TestMethod]
        public void NatureImmunityMatchesNameFragments()
        {
            Assert.IsTrue(BalanceDruidRotation.IsNatureImmune("Swirling Vortex"));
            Assert.IsTrue(BalanceDruidRotation.IsNatureImmune("Dust Devil"));
            Assert.IsFalse(BalanceDruidRotation.IsNatureImmune("Kobold Miner"));
            Assert.IsFalse(BalanceDruidRotation.IsNatureImmune(null));
        }

        [TestMethod]
        public void HealSelfNeedsLowHealthAndManaForSomeHeal()
        {
            Assert.IsTrue(BalanceDruidRotation.ShouldHealSelf(29, true, false));
            Assert.IsTrue(BalanceDruidRotation.ShouldHealSelf(29, false, true));
            Assert.IsFalse(BalanceDruidRotation.ShouldHealSelf(30, true, true));
            Assert.IsFalse(BalanceDruidRotation.ShouldHealSelf(29, false, false));
        }

        [TestMethod]
        public void CleansingNeverBreaksMoonkinForm()
        {
            Assert.IsTrue(BalanceDruidRotation.ShouldCleanseSelf(true, false));
            Assert.IsFalse(BalanceDruidRotation.ShouldCleanseSelf(true, true));
            Assert.IsFalse(BalanceDruidRotation.ShouldCleanseSelf(false, false));
        }

        [TestMethod]
        public void InsectSwarmRespectsImmunityAndExecuteRange()
        {
            Assert.IsTrue(BalanceDruidRotation.ShouldInsectSwarm(false, 50, false));
            Assert.IsFalse(BalanceDruidRotation.ShouldInsectSwarm(true, 50, false));
            Assert.IsFalse(BalanceDruidRotation.ShouldInsectSwarm(false, 20, false));
            Assert.IsFalse(BalanceDruidRotation.ShouldInsectSwarm(false, 50, true));
        }

        [TestMethod]
        public void MoonfireIsRefreshedOnlyAfterItFallsOff()
        {
            Assert.IsTrue(BalanceDruidRotation.ShouldMoonfire(false));
            Assert.IsFalse(BalanceDruidRotation.ShouldMoonfire(true));
        }

        [TestMethod]
        public void WrathIsHeldAgainstNatureImmuneTargets()
        {
            Assert.IsTrue(BalanceDruidRotation.ShouldWrath(false));
            Assert.IsFalse(BalanceDruidRotation.ShouldWrath(true));
        }

        [TestMethod]
        public void InnervateOnlyWhenNearlyOutOfMana()
        {
            Assert.IsTrue(BalanceDruidRotation.ShouldInnervate(9));
            Assert.IsFalse(BalanceDruidRotation.ShouldInnervate(10));
        }

        [TestMethod]
        public void RegrowthRescuesAWoundedPowerlevelTarget()
        {
            Assert.IsTrue(BalanceDruidRotation.ShouldRegrowth(39));
            Assert.IsFalse(BalanceDruidRotation.ShouldRegrowth(40));
        }
    }
}
