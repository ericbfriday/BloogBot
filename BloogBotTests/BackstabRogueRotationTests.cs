using BackstabRogueBot;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BloogBotTests
{
    [TestClass]
    public class BackstabRogueRotationTests
    {
        [TestMethod]
        public void EviscerateSpendsFewerComboPointsOnDyingTargets()
        {
            Assert.IsTrue(BackstabRogueRotation.IsReadyToEviscerate(20, 2));
            Assert.IsTrue(BackstabRogueRotation.IsReadyToEviscerate(30, 3));
            Assert.IsTrue(BackstabRogueRotation.IsReadyToEviscerate(40, 4));
            Assert.IsTrue(BackstabRogueRotation.IsReadyToEviscerate(100, 5));
        }

        [TestMethod]
        public void EviscerateWaitsForComboPointsOnHealthyTargets()
        {
            Assert.IsFalse(BackstabRogueRotation.IsReadyToEviscerate(21, 2));
            Assert.IsFalse(BackstabRogueRotation.IsReadyToEviscerate(50, 4));
            Assert.IsFalse(BackstabRogueRotation.IsReadyToEviscerate(100, 4));
        }

        [TestMethod]
        public void SliceAndDiceUsesTwoOrThreeComboPointsOnHealthyTargets()
        {
            Assert.IsTrue(BackstabRogueRotation.ShouldSliceAndDice(false, 41, 2));
            Assert.IsTrue(BackstabRogueRotation.ShouldSliceAndDice(false, 41, 3));
            Assert.IsFalse(BackstabRogueRotation.ShouldSliceAndDice(false, 41, 4));
            Assert.IsFalse(BackstabRogueRotation.ShouldSliceAndDice(false, 41, 1));
            Assert.IsFalse(BackstabRogueRotation.ShouldSliceAndDice(false, 40, 2));
            Assert.IsFalse(BackstabRogueRotation.ShouldSliceAndDice(true, 41, 2));
        }

        [TestMethod]
        public void InterruptRequiresCastingManaUser()
        {
            Assert.IsTrue(BackstabRogueRotation.ReadyToInterrupt(100, true, false));
            Assert.IsTrue(BackstabRogueRotation.ReadyToInterrupt(100, false, true));
            Assert.IsFalse(BackstabRogueRotation.ReadyToInterrupt(0, true, false));
            Assert.IsFalse(BackstabRogueRotation.ReadyToInterrupt(100, false, false));
        }

        [TestMethod]
        public void KidneyShotInterruptsOnlyWithOneOrTwoComboPointsWhenKickIsDown()
        {
            Assert.IsTrue(BackstabRogueRotation.ShouldKidneyShotInterrupt(true, false, 1));
            Assert.IsTrue(BackstabRogueRotation.ShouldKidneyShotInterrupt(true, false, 2));
            Assert.IsFalse(BackstabRogueRotation.ShouldKidneyShotInterrupt(true, false, 0));
            Assert.IsFalse(BackstabRogueRotation.ShouldKidneyShotInterrupt(true, false, 3));
            Assert.IsFalse(BackstabRogueRotation.ShouldKidneyShotInterrupt(true, true, 2));
            Assert.IsFalse(BackstabRogueRotation.ShouldKidneyShotInterrupt(false, false, 2));
        }

        [TestMethod]
        public void ComboBuilderYieldsToFinishersAndInterrupts()
        {
            Assert.IsTrue(BackstabRogueRotation.ShouldUseComboBuilder(false, 3, false));
            Assert.IsFalse(BackstabRogueRotation.ShouldUseComboBuilder(true, 3, false));
            Assert.IsFalse(BackstabRogueRotation.ShouldUseComboBuilder(false, 5, false));
            Assert.IsFalse(BackstabRogueRotation.ShouldUseComboBuilder(false, 3, true));
        }
    }
}
