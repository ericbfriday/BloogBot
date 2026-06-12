using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShadowPriestBot;

namespace BloogBotTests
{
    [TestClass]
    public class ShadowPriestPowerlevelCombatRangeTests
    {
        [TestMethod]
        public void NonCasterPowerlevelTargetsUseCloserCombatRange()
        {
            Assert.AreEqual(26, ShadowPriestPowerlevelCombatRange.GetDesiredRange(
                targetIsCasting: false,
                targetIsChanneling: false));
        }

        [TestMethod]
        public void CastingPowerlevelTargetsStillUseMindFlayRange()
        {
            Assert.AreEqual(19, ShadowPriestPowerlevelCombatRange.GetDesiredRange(
                targetIsCasting: true,
                targetIsChanneling: false));
        }

        [TestMethod]
        public void MovesCloserWhenOutsideDesiredPowerlevelRange()
        {
            Assert.IsTrue(ShadowPriestPowerlevelCombatRange.ShouldMoveCloser(
                distanceToTarget: 29,
                targetIsCasting: false,
                targetIsChanneling: false));
        }

        [TestMethod]
        public void StopsMovingWhenInsideDesiredPowerlevelRange()
        {
            Assert.IsFalse(ShadowPriestPowerlevelCombatRange.ShouldMoveCloser(
                distanceToTarget: 25,
                targetIsCasting: false,
                targetIsChanneling: false));
        }
    }
}
