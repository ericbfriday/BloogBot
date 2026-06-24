using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtectionPaladinBot;

namespace BloogBotTests
{
    [TestClass]
    public class ProtectionPaladinRotationTests
    {
        [TestMethod]
        public void AuraUpgradesFromDevotionToRetribution()
        {
            Assert.AreEqual(
                ProtectionPaladinRotation.DevotionAura,
                ProtectionPaladinRotation.SelectAura(knowsRetributionAura: false, hasDevotionAura: false, hasRetributionAura: false));

            Assert.AreEqual(
                ProtectionPaladinRotation.RetributionAura,
                ProtectionPaladinRotation.SelectAura(knowsRetributionAura: true, hasDevotionAura: true, hasRetributionAura: false));

            Assert.IsNull(ProtectionPaladinRotation.SelectAura(knowsRetributionAura: true, hasDevotionAura: false, hasRetributionAura: true));
            Assert.IsNull(ProtectionPaladinRotation.SelectAura(knowsRetributionAura: false, hasDevotionAura: true, hasRetributionAura: false));
        }

        [TestMethod]
        public void LayOnHandsIsTheLastResort()
        {
            Assert.IsTrue(ProtectionPaladinRotation.ShouldLayOnHands(canAffordHolyLight: false, playerHealthPercent: 9));
            Assert.IsFalse(ProtectionPaladinRotation.ShouldLayOnHands(canAffordHolyLight: true, playerHealthPercent: 9));
            Assert.IsFalse(ProtectionPaladinRotation.ShouldLayOnHands(canAffordHolyLight: false, playerHealthPercent: 10));
        }

        [TestMethod]
        public void WotlkJudgementsRequireAnActiveSeal()
        {
            Assert.IsTrue(ProtectionPaladinRotation.ShouldJudgementOfWisdom(false, true));
            Assert.IsFalse(ProtectionPaladinRotation.ShouldJudgementOfWisdom(false, false));
            Assert.IsFalse(ProtectionPaladinRotation.ShouldJudgementOfWisdom(true, true));

            Assert.IsTrue(ProtectionPaladinRotation.ShouldJudgementOfLight(false, true, false));
            Assert.IsFalse(ProtectionPaladinRotation.ShouldJudgementOfLight(false, true, true));
            Assert.IsFalse(ProtectionPaladinRotation.ShouldJudgementOfLight(true, true, false));
        }

        [TestMethod]
        public void LegacyJudgementFiresOnCrusaderOrFullManaRighteousness()
        {
            Assert.IsTrue(ProtectionPaladinRotation.ShouldUseLegacyJudgement(true, false, 50, 100));
            Assert.IsTrue(ProtectionPaladinRotation.ShouldUseLegacyJudgement(false, true, 95, 100));
            Assert.IsTrue(ProtectionPaladinRotation.ShouldUseLegacyJudgement(false, true, 50, 3));
            Assert.IsFalse(ProtectionPaladinRotation.ShouldUseLegacyJudgement(false, true, 94, 100));
            Assert.IsFalse(ProtectionPaladinRotation.ShouldUseLegacyJudgement(false, false, 100, 1));
        }

        [TestMethod]
        public void SealOfRighteousnessWaitsForCrusaderJudgementWhenKnown()
        {
            Assert.IsTrue(ProtectionPaladinRotation.ShouldSealOfRighteousness(false, true, true));
            Assert.IsTrue(ProtectionPaladinRotation.ShouldSealOfRighteousness(false, false, false));
            Assert.IsFalse(ProtectionPaladinRotation.ShouldSealOfRighteousness(false, false, true));
            Assert.IsFalse(ProtectionPaladinRotation.ShouldSealOfRighteousness(true, true, true));
        }

        [TestMethod]
        public void DivinePleaRefillsManaBelowHalf()
        {
            Assert.IsTrue(ProtectionPaladinRotation.ShouldDivinePlea(49));
            Assert.IsFalse(ProtectionPaladinRotation.ShouldDivinePlea(50));
        }

        [TestMethod]
        public void PurifyClearsPoisonOrDisease()
        {
            Assert.IsTrue(ProtectionPaladinRotation.ShouldPurify(true, false));
            Assert.IsTrue(ProtectionPaladinRotation.ShouldPurify(false, true));
            Assert.IsFalse(ProtectionPaladinRotation.ShouldPurify(false, false));
        }

        [TestMethod]
        public void ExorcismOnlyHitsUndeadAndDemons()
        {
            Assert.IsTrue(ProtectionPaladinRotation.ShouldExorcism(true));
            Assert.IsFalse(ProtectionPaladinRotation.ShouldExorcism(false));
        }

        [TestMethod]
        public void HammerOfJusticeIsHeldForHumanoidExecutes()
        {
            Assert.IsTrue(ProtectionPaladinRotation.ShouldHammerOfJustice(false, 100));
            Assert.IsFalse(ProtectionPaladinRotation.ShouldHammerOfJustice(true, 20));
            Assert.IsTrue(ProtectionPaladinRotation.ShouldHammerOfJustice(true, 19));
        }

        [TestMethod]
        public void ConsecrationNeedsMultipleAttackers()
        {
            Assert.IsTrue(ProtectionPaladinRotation.ShouldConsecration(2));
            Assert.IsFalse(ProtectionPaladinRotation.ShouldConsecration(1));
        }

        [TestMethod]
        public void SealOfTheCrusaderIsKeptUpUntilJudged()
        {
            Assert.IsTrue(ProtectionPaladinRotation.ShouldSealOfTheCrusader(false, false));
            Assert.IsFalse(ProtectionPaladinRotation.ShouldSealOfTheCrusader(true, false));
            Assert.IsFalse(ProtectionPaladinRotation.ShouldSealOfTheCrusader(false, true));
        }

        [TestMethod]
        public void HolyShieldIsHeldForHealthyTargets()
        {
            Assert.IsTrue(ProtectionPaladinRotation.ShouldHolyShield(false, 51));
            Assert.IsFalse(ProtectionPaladinRotation.ShouldHolyShield(true, 51));
            Assert.IsFalse(ProtectionPaladinRotation.ShouldHolyShield(false, 50));
        }
    }
}
