using FeralDruidBot;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BloogBotTests
{
    [TestClass]
    public class FeralDruidRotationTests
    {
        [TestMethod]
        public void FormProgressionFollowsLevel()
        {
            Assert.IsNull(FeralDruidRotation.SelectForm(12));
            Assert.AreEqual(FeralDruidRotation.BearForm, FeralDruidRotation.SelectForm(13));
            Assert.AreEqual(FeralDruidRotation.BearForm, FeralDruidRotation.SelectForm(19));
            Assert.AreEqual(FeralDruidRotation.CatForm, FeralDruidRotation.SelectForm(20));
        }

        [TestMethod]
        public void MaulGetsCheaperWithLevelButNeverBelowTen()
        {
            Assert.AreEqual(11, FeralDruidRotation.MaulRageRequirement(13));
            Assert.AreEqual(10, FeralDruidRotation.MaulRageRequirement(14));
            Assert.AreEqual(10, FeralDruidRotation.MaulRageRequirement(19));
        }

        [TestMethod]
        public void CasterLevelsMeleeWhenLowOnMana()
        {
            Assert.IsTrue(FeralDruidRotation.ShouldMoveIntoMeleeForMana(19, 6));
            Assert.IsFalse(FeralDruidRotation.ShouldMoveIntoMeleeForMana(20, 6));
            Assert.IsFalse(FeralDruidRotation.ShouldMoveIntoMeleeForMana(19, 5));
        }

        [TestMethod]
        public void PullingWithWrathRequiresKnownReadyAffordableSpell()
        {
            Assert.IsTrue(FeralDruidRotation.CanPullWithWrath(true, true, 20, 10, false));
            Assert.IsFalse(FeralDruidRotation.CanPullWithWrath(false, true, 20, 10, false));
            Assert.IsFalse(FeralDruidRotation.CanPullWithWrath(true, false, 20, 10, false));
            Assert.IsFalse(FeralDruidRotation.CanPullWithWrath(true, true, 9, 10, false));
            Assert.IsFalse(FeralDruidRotation.CanPullWithWrath(true, true, 20, 10, true));
        }

        [TestMethod]
        public void PullingWithFeralChargeRequiresKnownReadyEnergyAndCatForm()
        {
            Assert.IsTrue(FeralDruidRotation.CanPullWithFeralCharge(true, true, 10, true, false));
            Assert.IsFalse(FeralDruidRotation.CanPullWithFeralCharge(false, true, 10, true, false));
            Assert.IsFalse(FeralDruidRotation.CanPullWithFeralCharge(true, false, 10, true, false));
            Assert.IsFalse(FeralDruidRotation.CanPullWithFeralCharge(true, true, 9, true, false));
            Assert.IsFalse(FeralDruidRotation.CanPullWithFeralCharge(true, true, 10, false, false));
            Assert.IsFalse(FeralDruidRotation.CanPullWithFeralCharge(true, true, 10, true, true));
        }

        [TestMethod]
        public void BearAbilitiesRequireBearFormAndRage()
        {
            Assert.IsTrue(FeralDruidRotation.CanUseBearAbility(true, 10, 10, false, true, true));
            Assert.IsFalse(FeralDruidRotation.CanUseBearAbility(true, 9, 10, false, true, true));
            Assert.IsFalse(FeralDruidRotation.CanUseBearAbility(true, 10, 10, false, false, true));
            Assert.IsFalse(FeralDruidRotation.CanUseBearAbility(true, 10, 10, true, true, true));
            Assert.IsFalse(FeralDruidRotation.CanUseBearAbility(false, 10, 10, false, true, true));
        }

        [TestMethod]
        public void CatAbilitiesGateOnComboPointsOnlyWhenRequired()
        {
            Assert.IsTrue(FeralDruidRotation.CanUseCatAbility(true, 35, 35, true, 1, false, true, true));
            Assert.IsFalse(FeralDruidRotation.CanUseCatAbility(true, 35, 35, true, 0, false, true, true));
            Assert.IsTrue(FeralDruidRotation.CanUseCatAbility(true, 35, 35, false, 0, false, true, true));
            Assert.IsFalse(FeralDruidRotation.CanUseCatAbility(true, 34, 35, false, 0, false, true, true));
            Assert.IsFalse(FeralDruidRotation.CanUseCatAbility(true, 35, 35, false, 0, false, false, true));
        }

        [TestMethod]
        public void RipNeedsFiveComboPointsAndNoExistingRip()
        {
            Assert.IsTrue(FeralDruidRotation.ShouldRip(false, 5));
            Assert.IsFalse(FeralDruidRotation.ShouldRip(false, 4));
            Assert.IsFalse(FeralDruidRotation.ShouldRip(true, 5));
        }

        [TestMethod]
        public void DemoralizingRoarIsForPacksThatLackTheDebuff()
        {
            Assert.IsTrue(FeralDruidRotation.ShouldDemoralizingRoar(2, false));
            Assert.IsFalse(FeralDruidRotation.ShouldDemoralizingRoar(1, false));
            Assert.IsFalse(FeralDruidRotation.ShouldDemoralizingRoar(2, true));
        }

        [TestMethod]
        public void DamageCooldownsAreSavedForLivelyTargets()
        {
            Assert.IsTrue(FeralDruidRotation.ShouldBerserk(31, false));
            Assert.IsFalse(FeralDruidRotation.ShouldBerserk(30, false));
            Assert.IsFalse(FeralDruidRotation.ShouldBerserk(31, true));

            Assert.IsTrue(FeralDruidRotation.ShouldTigersFury(31, false));
            Assert.IsFalse(FeralDruidRotation.ShouldTigersFury(30, false));
            Assert.IsFalse(FeralDruidRotation.ShouldTigersFury(31, true));
        }

        [TestMethod]
        public void FaerieFireIsRefreshedWhenMissing()
        {
            Assert.IsTrue(FeralDruidRotation.ShouldFaerieFire(false));
            Assert.IsFalse(FeralDruidRotation.ShouldFaerieFire(true));
        }

        [TestMethod]
        public void FinisherCadenceSpendsAtFiveComboPoints()
        {
            Assert.IsTrue(FeralDruidRotation.ShouldFerociousBite(5));
            Assert.IsFalse(FeralDruidRotation.ShouldFerociousBite(4));
        }

        [TestMethod]
        public void RakeBleedsHealthyTargetsThatLackIt()
        {
            Assert.IsTrue(FeralDruidRotation.ShouldRake(51, false));
            Assert.IsFalse(FeralDruidRotation.ShouldRake(50, false));
            Assert.IsFalse(FeralDruidRotation.ShouldRake(51, true));
        }
    }
}
