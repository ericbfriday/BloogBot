using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetributionPaladinBot;

namespace BloogBotTests
{
    [TestClass]
    public class RetributionPaladinCombatRotationTests
    {
        [TestMethod]
        public void WotlkJudgementUsesLightWhenWisdomIsUnknown()
        {
            Assert.AreEqual(
                RetributionPaladinCombatRotation.JudgementOfLight,
                RetributionPaladinCombatRotation.SelectWotlkJudgement(
                    knowsJudgementOfWisdom: false,
                    targetHasJudgementOfWisdom: false,
                    targetHasJudgementOfLight: false,
                    hasActiveSeal: true));
        }

        [TestMethod]
        public void WotlkJudgementPrefersWisdomWhenKnown()
        {
            Assert.AreEqual(
                RetributionPaladinCombatRotation.JudgementOfWisdom,
                RetributionPaladinCombatRotation.SelectWotlkJudgement(
                    knowsJudgementOfWisdom: true,
                    targetHasJudgementOfWisdom: false,
                    targetHasJudgementOfLight: false,
                    hasActiveSeal: true));
        }

        [TestMethod]
        public void WotlkJudgementRequiresActiveSeal()
        {
            Assert.IsNull(RetributionPaladinCombatRotation.SelectWotlkJudgement(
                knowsJudgementOfWisdom: false,
                targetHasJudgementOfWisdom: false,
                targetHasJudgementOfLight: false,
                hasActiveSeal: false));
        }

        [TestMethod]
        public void LowLevelSealOfRighteousnessDoesNotRequireUnknownCrusaderJudgement()
        {
            Assert.IsTrue(RetributionPaladinCombatRotation.ShouldUseSealOfRighteousness(
                hasSealOfRighteousness: false,
                targetHasJudgementOfTheCrusader: false,
                knowsSealOfCommand: false,
                knowsJudgementOfTheCrusader: false));
        }

        [TestMethod]
        public void SelectAuraUsesDevotionWhenHigherAurasAreUnknown()
        {
            Assert.AreEqual(
                RetributionPaladinCombatRotation.DevotionAura,
                RetributionPaladinCombatRotation.SelectAura(
                    knowsDevotionAura: true,
                    hasDevotionAura: false,
                    knowsRetributionAura: false,
                    hasRetributionAura: false,
                    knowsSanctityAura: false,
                    hasSanctityAura: false));
        }

        [TestMethod]
        public void SelectAuraKeepsActiveRetributionAuraWhenDevotionIsMissing()
        {
            Assert.IsNull(RetributionPaladinCombatRotation.SelectAura(
                knowsDevotionAura: true,
                hasDevotionAura: false,
                knowsRetributionAura: true,
                hasRetributionAura: true,
                knowsSanctityAura: false,
                hasSanctityAura: false));
        }

        [TestMethod]
        public void SelectAuraUpgradesDevotionToRetributionWhenRetributionIsKnown()
        {
            Assert.AreEqual(
                RetributionPaladinCombatRotation.RetributionAura,
                RetributionPaladinCombatRotation.SelectAura(
                    knowsDevotionAura: true,
                    hasDevotionAura: true,
                    knowsRetributionAura: true,
                    hasRetributionAura: false,
                    knowsSanctityAura: false,
                    hasSanctityAura: false));
        }
    }
}
