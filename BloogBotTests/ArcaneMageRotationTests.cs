using ArcaneMageBot;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BloogBotTests
{
    [TestClass]
    public class ArcaneMageRotationTests
    {
        [TestMethod]
        public void WandIsUsedOnlyWhenOutOfManaAndNotCasting()
        {
            Assert.IsTrue(ArcaneMageRotation.ShouldUseWand(true, 10, false, false));
            Assert.IsFalse(ArcaneMageRotation.ShouldUseWand(false, 10, false, false));
            Assert.IsFalse(ArcaneMageRotation.ShouldUseWand(true, 11, false, false));
            Assert.IsFalse(ArcaneMageRotation.ShouldUseWand(true, 10, true, false));
        }

        [TestMethod]
        public void ManaShieldIsEmergencyOnly()
        {
            Assert.IsTrue(ArcaneMageRotation.ShouldManaShield(false, 19));
            Assert.IsFalse(ArcaneMageRotation.ShouldManaShield(false, 20));
            Assert.IsFalse(ArcaneMageRotation.ShouldManaShield(true, 19));
        }

        [TestMethod]
        public void FireballIsTheNukeBeforeFifteenOrWithPresenceOfMind()
        {
            Assert.IsTrue(ArcaneMageRotation.ShouldFireball(14, false));
            Assert.IsFalse(ArcaneMageRotation.ShouldFireball(15, false));
            Assert.IsTrue(ArcaneMageRotation.ShouldFireball(60, true));
        }

        [TestMethod]
        public void ArcaneMissilesTakesOverAtFifteen()
        {
            Assert.IsFalse(ArcaneMageRotation.ShouldArcaneMissiles(14));
            Assert.IsTrue(ArcaneMageRotation.ShouldArcaneMissiles(15));
        }
    }
}
