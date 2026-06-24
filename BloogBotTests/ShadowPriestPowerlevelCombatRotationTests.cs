using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShadowPriestBot;

namespace BloogBotTests
{
    [TestClass]
    public class ShadowPriestPowerlevelCombatRotationTests
    {
        [TestMethod]
        public void UnknownSpellsAreNotCastEvenIfReadyStateIsReported()
        {
            Assert.IsFalse(ShadowPriestPowerlevelCombatRotation.CanCastSpell(
                knowsSpell: false,
                isSpellReady: true,
                mana: 100,
                manaCost: 10,
                distanceToTarget: 20,
                minRange: 0,
                maxRange: 29,
                condition: true,
                isStunned: false,
                isCasting: false,
                isChanneling: false));
        }

        [TestMethod]
        public void PowerlevelTargetHealingUsesPowerlevelTargetRange()
        {
            Assert.IsFalse(ShadowPriestPowerlevelCombatRotation.ShouldHealPowerlevelTarget(
                powerlevelTargetHealthPercent: 40,
                knowsLesserHeal: true,
                isLesserHealReady: true,
                mana: 100,
                lesserHealManaCost: 10,
                distanceToPowerlevelTarget: 41,
                isStunned: false,
                isCasting: false,
                isChanneling: false));
        }

        [TestMethod]
        public void KnownReadyPowerlevelTargetHealCastsWhenTargetIsInRange()
        {
            Assert.IsTrue(ShadowPriestPowerlevelCombatRotation.ShouldHealPowerlevelTarget(
                powerlevelTargetHealthPercent: 40,
                knowsLesserHeal: true,
                isLesserHealReady: true,
                mana: 100,
                lesserHealManaCost: 10,
                distanceToPowerlevelTarget: 40,
                isStunned: false,
                isCasting: false,
                isChanneling: false));
        }

        [TestMethod]
        public void WandFallbackTriggersOnLowManaTotemsOrNearlyDeadTargets()
        {
            Assert.IsTrue(ShadowPriestPowerlevelCombatRotation.ShouldUseWand(true, false, false, 10, false, 100));
            Assert.IsTrue(ShadowPriestPowerlevelCombatRotation.ShouldUseWand(true, false, false, 100, true, 100));
            Assert.IsTrue(ShadowPriestPowerlevelCombatRotation.ShouldUseWand(true, false, false, 100, false, 10));
            Assert.IsFalse(ShadowPriestPowerlevelCombatRotation.ShouldUseWand(false, false, false, 10, true, 10));
            Assert.IsFalse(ShadowPriestPowerlevelCombatRotation.ShouldUseWand(true, true, false, 10, true, 10));
            Assert.IsFalse(ShadowPriestPowerlevelCombatRotation.ShouldUseWand(true, false, false, 100, false, 100));
        }

        [TestMethod]
        public void ShadowWordPainIsRefreshedOnlyOnHealthyUndottedTargets()
        {
            Assert.IsTrue(ShadowPriestPowerlevelCombatRotation.ShouldShadowWordPain(71, false));
            Assert.IsFalse(ShadowPriestPowerlevelCombatRotation.ShouldShadowWordPain(70, false));
            Assert.IsFalse(ShadowPriestPowerlevelCombatRotation.ShouldShadowWordPain(71, true));
        }

        [TestMethod]
        public void MindFlayNeedsRangeAndAShieldWhenShieldsAreKnown()
        {
            Assert.IsTrue(ShadowPriestPowerlevelCombatRotation.ShouldMindFlay(true, 19, true, true));
            // Shields known but not up yet: hold the channel.
            Assert.IsFalse(ShadowPriestPowerlevelCombatRotation.ShouldMindFlay(true, 19, true, false));
            // Shields not known: channel freely.
            Assert.IsTrue(ShadowPriestPowerlevelCombatRotation.ShouldMindFlay(true, 19, false, false));
            Assert.IsFalse(ShadowPriestPowerlevelCombatRotation.ShouldMindFlay(false, 19, false, false));
            Assert.IsFalse(ShadowPriestPowerlevelCombatRotation.ShouldMindFlay(true, 20, false, false));
        }

        [TestMethod]
        public void VampiricEmbraceIsAppliedToHealthyTargetsWhileHurt()
        {
            Assert.IsTrue(ShadowPriestPowerlevelCombatRotation.ShouldVampiricEmbrace(99, false, 51));
            Assert.IsFalse(ShadowPriestPowerlevelCombatRotation.ShouldVampiricEmbrace(100, false, 51));
            Assert.IsFalse(ShadowPriestPowerlevelCombatRotation.ShouldVampiricEmbrace(99, true, 51));
            Assert.IsFalse(ShadowPriestPowerlevelCombatRotation.ShouldVampiricEmbrace(99, false, 50));
        }

        [TestMethod]
        public void PsychicScreamFiresInMeleeOrAgainstNonElementalPacks()
        {
            Assert.IsTrue(ShadowPriestPowerlevelCombatRotation.ShouldPsychicScream(7, false, 1, false));
            Assert.IsFalse(ShadowPriestPowerlevelCombatRotation.ShouldPsychicScream(7, true, 1, false));
            Assert.IsTrue(ShadowPriestPowerlevelCombatRotation.ShouldPsychicScream(20, true, 2, false));
            Assert.IsFalse(ShadowPriestPowerlevelCombatRotation.ShouldPsychicScream(20, true, 2, true));
        }

        [TestMethod]
        public void PowerWordShieldRespectsWeakenedSoulAndExistingShield()
        {
            Assert.IsTrue(ShadowPriestPowerlevelCombatRotation.ShouldPowerWordShield(false, false, 100, 100));
            Assert.IsFalse(ShadowPriestPowerlevelCombatRotation.ShouldPowerWordShield(true, false, 100, 100));
            Assert.IsFalse(ShadowPriestPowerlevelCombatRotation.ShouldPowerWordShield(false, true, 100, 100));
            Assert.IsTrue(ShadowPriestPowerlevelCombatRotation.ShouldPowerWordShield(false, false, 20, 9));
            Assert.IsFalse(ShadowPriestPowerlevelCombatRotation.ShouldPowerWordShield(false, false, 20, 10));
        }

        [TestMethod]
        public void CureDiseaseNeverBreaksShadowform()
        {
            Assert.IsTrue(ShadowPriestPowerlevelCombatRotation.ShouldCureDisease(true, false));
            Assert.IsFalse(ShadowPriestPowerlevelCombatRotation.ShouldCureDisease(true, true));
            Assert.IsFalse(ShadowPriestPowerlevelCombatRotation.ShouldCureDisease(false, false));
        }
    }
}
