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
    }
}
