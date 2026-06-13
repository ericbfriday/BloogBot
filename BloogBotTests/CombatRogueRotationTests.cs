using CombatRogueBot;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BloogBotTests
{
    [TestClass]
    public class CombatRogueRotationTests
    {
        [TestMethod]
        public void EviscerateSpendsFewerComboPointsOnDyingTargets()
        {
            Assert.IsTrue(CombatRogueRotation.IsReadyToEviscerate(15, 2));
            Assert.IsTrue(CombatRogueRotation.IsReadyToEviscerate(25, 3));
            Assert.IsTrue(CombatRogueRotation.IsReadyToEviscerate(35, 4));
            Assert.IsTrue(CombatRogueRotation.IsReadyToEviscerate(100, 5));
        }

        [TestMethod]
        public void EviscerateWaitsForComboPointsOnHealthyTargets()
        {
            Assert.IsFalse(CombatRogueRotation.IsReadyToEviscerate(16, 2));
            Assert.IsFalse(CombatRogueRotation.IsReadyToEviscerate(36, 4));
            Assert.IsFalse(CombatRogueRotation.IsReadyToEviscerate(100, 4));
        }

        [TestMethod]
        public void SliceAndDiceUsesExactlyTwoComboPointsOnHealthyTargets()
        {
            Assert.IsTrue(CombatRogueRotation.ShouldSliceAndDice(false, 71, 2));
            Assert.IsFalse(CombatRogueRotation.ShouldSliceAndDice(false, 70, 2));
            Assert.IsFalse(CombatRogueRotation.ShouldSliceAndDice(false, 71, 3));
            Assert.IsFalse(CombatRogueRotation.ShouldSliceAndDice(true, 71, 2));
        }

        [TestMethod]
        public void InterruptRequiresCastingManaUser()
        {
            Assert.IsTrue(CombatRogueRotation.ReadyToInterrupt(100, true, false));
            Assert.IsTrue(CombatRogueRotation.ReadyToInterrupt(100, false, true));
            Assert.IsFalse(CombatRogueRotation.ReadyToInterrupt(0, true, false));
            Assert.IsFalse(CombatRogueRotation.ReadyToInterrupt(100, false, false));
        }

        [TestMethod]
        public void AdrenalineRushIsSavedForThreePullsAtHighHealth()
        {
            Assert.IsTrue(CombatRogueRotation.ShouldAdrenalineRush(3, 81));
            Assert.IsFalse(CombatRogueRotation.ShouldAdrenalineRush(2, 90));
            Assert.IsFalse(CombatRogueRotation.ShouldAdrenalineRush(3, 80));
        }
    }
}
