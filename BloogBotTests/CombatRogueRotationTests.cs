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

        [TestMethod]
        public void BloodFuryIsSavedForTheOpenerAtHighHealth()
        {
            Assert.IsTrue(CombatRogueRotation.ShouldBloodFury(81));
            Assert.IsFalse(CombatRogueRotation.ShouldBloodFury(80));
        }

        [TestMethod]
        public void EvasionRequiresMoreThanOneAttacker()
        {
            Assert.IsTrue(CombatRogueRotation.ShouldEvasion(2));
            Assert.IsFalse(CombatRogueRotation.ShouldEvasion(1));
            Assert.IsFalse(CombatRogueRotation.ShouldEvasion(0));
        }

        [TestMethod]
        public void BladeFlurryRequiresMoreThanOneAttacker()
        {
            Assert.IsTrue(CombatRogueRotation.ShouldBladeFlurry(2));
            Assert.IsFalse(CombatRogueRotation.ShouldBladeFlurry(1));
            Assert.IsFalse(CombatRogueRotation.ShouldBladeFlurry(0));
        }

        [TestMethod]
        public void SinisterStrikeBuildsUntilComboPointsAreCapped()
        {
            Assert.IsTrue(CombatRogueRotation.ShouldSinisterStrike(0));
            Assert.IsTrue(CombatRogueRotation.ShouldSinisterStrike(4));
            Assert.IsFalse(CombatRogueRotation.ShouldSinisterStrike(5));
        }

        [TestMethod]
        public void KickInterruptsCastingManaUsers()
        {
            Assert.IsTrue(CombatRogueRotation.ShouldKickInterrupt(100, true, false));
            Assert.IsTrue(CombatRogueRotation.ShouldKickInterrupt(100, false, true));
            Assert.IsFalse(CombatRogueRotation.ShouldKickInterrupt(0, true, false));
            Assert.IsFalse(CombatRogueRotation.ShouldKickInterrupt(100, false, false));
        }

        [TestMethod]
        public void GougeOnlyInterruptsWhenKickIsUnavailable()
        {
            // Interrupt needed and Kick down: Gouge fires.
            Assert.IsTrue(CombatRogueRotation.ShouldGougeInterrupt(100, true, false, false));
            // Interrupt needed but Kick ready: defer to Kick.
            Assert.IsFalse(CombatRogueRotation.ShouldGougeInterrupt(100, true, false, true));
            // No interrupt needed: Gouge stays holstered even with Kick down.
            Assert.IsFalse(CombatRogueRotation.ShouldGougeInterrupt(100, false, false, false));
            Assert.IsFalse(CombatRogueRotation.ShouldGougeInterrupt(0, true, false, false));
        }
    }
}
