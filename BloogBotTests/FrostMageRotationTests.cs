using FrostMageBot;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BloogBotTests
{
    [TestClass]
    public class FrostMageRotationTests
    {
        [TestMethod]
        public void NukeSelectionUsesFrostboltAsSoonAsKnown()
        {
            Assert.AreEqual(FrostMageRotation.Fireball, FrostMageRotation.SelectNuke(false, 60));
            Assert.AreEqual(FrostMageRotation.Fireball, FrostMageRotation.SelectNuke(true, 3));
            Assert.AreEqual(FrostMageRotation.Frostbolt, FrostMageRotation.SelectNuke(true, 4));
            Assert.AreEqual(FrostMageRotation.Frostbolt, FrostMageRotation.SelectNuke(true, 6));
            Assert.AreEqual(FrostMageRotation.Frostbolt, FrostMageRotation.SelectNuke(true, 8));
            Assert.AreEqual(FrostMageRotation.Frostbolt, FrostMageRotation.SelectNuke(true, 60));
        }

        [TestMethod]
        public void NukeRangeGrowsWithRangeTalent()
        {
            Assert.AreEqual(29, FrostMageRotation.CalculateNukeRange(0));
            Assert.AreEqual(35, FrostMageRotation.CalculateNukeRange(2));
        }

        [TestMethod]
        public void WardSelectionMatchesTargetName()
        {
            Assert.AreEqual(FrostMageRotation.FireWard, FrostMageRotation.SelectWard("Searing Blade Cultist"));
            Assert.AreEqual(FrostMageRotation.FrostWard, FrostMageRotation.SelectWard("Ice Claw Bear"));
            Assert.IsNull(FrostMageRotation.SelectWard("Kobold Miner"));
            Assert.IsNull(FrostMageRotation.SelectWard(null));
        }

        [TestMethod]
        public void WardIsSkippedOnDyingTargetUnlessPlayerIsCritical()
        {
            Assert.IsTrue(FrostMageRotation.ShouldUseWard(50, 100));
            Assert.IsFalse(FrostMageRotation.ShouldUseWard(20, 50));
            Assert.IsTrue(FrostMageRotation.ShouldUseWard(20, 9));
        }

        [TestMethod]
        public void WandIsUsedOnlyWhenOutOfManaAndNotCasting()
        {
            Assert.IsTrue(FrostMageRotation.ShouldUseWand(true, 10, false, false));
            Assert.IsFalse(FrostMageRotation.ShouldUseWand(false, 10, false, false));
            Assert.IsFalse(FrostMageRotation.ShouldUseWand(true, 11, false, false));
            Assert.IsFalse(FrostMageRotation.ShouldUseWand(true, 10, true, false));
            Assert.IsFalse(FrostMageRotation.ShouldUseWand(true, 10, false, true));
        }

        [TestMethod]
        public void EvocationRequiresSafetyAndDeepManaDeficit()
        {
            Assert.IsTrue(FrostMageRotation.ShouldEvocate(60, false, 7, 50));
            Assert.IsTrue(FrostMageRotation.ShouldEvocate(20, true, 7, 50));
            Assert.IsFalse(FrostMageRotation.ShouldEvocate(20, false, 7, 50));
            Assert.IsFalse(FrostMageRotation.ShouldEvocate(60, false, 8, 50));
            Assert.IsFalse(FrostMageRotation.ShouldEvocate(60, false, 7, 15));
        }

        [TestMethod]
        public void IceBarrierIsUsedAgainstMultiplePullsOrWhenNovaIsDown()
        {
            Assert.IsTrue(FrostMageRotation.ShouldIceBarrier(false, 2, true, 100, 100, 100));
            Assert.IsTrue(FrostMageRotation.ShouldIceBarrier(false, 1, false, 90, 50, 50));
            Assert.IsFalse(FrostMageRotation.ShouldIceBarrier(true, 2, true, 100, 100, 100));
            Assert.IsFalse(FrostMageRotation.ShouldIceBarrier(false, 1, true, 90, 50, 50));
            Assert.IsFalse(FrostMageRotation.ShouldIceBarrier(false, 1, false, 90, 40, 50));
        }

        [TestMethod]
        public void FrostNovaIsHeldWhenItWouldPullExtraMobs()
        {
            Assert.IsTrue(FrostMageRotation.ShouldFrostNova(true, 50, 100, false, false));
            Assert.IsFalse(FrostMageRotation.ShouldFrostNova(true, 50, 100, false, true));
            Assert.IsFalse(FrostMageRotation.ShouldFrostNova(true, 50, 100, true, false));
            Assert.IsFalse(FrostMageRotation.ShouldFrostNova(false, 50, 100, false, false));
            Assert.IsFalse(FrostMageRotation.ShouldFrostNova(true, 20, 30, false, false));
            Assert.IsTrue(FrostMageRotation.ShouldFrostNova(true, 20, 29, false, false));
        }

        [TestMethod]
        public void ShatterSpendersRequireFrozenTargetOrFingersOfFrost()
        {
            Assert.IsTrue(FrostMageRotation.ShouldUseShatterSpender(true, false));
            Assert.IsTrue(FrostMageRotation.ShouldUseShatterSpender(false, true));
            Assert.IsFalse(FrostMageRotation.ShouldUseShatterSpender(false, false));
        }

        [TestMethod]
        public void WaterElementalIsSavedForMeaningfulFights()
        {
            Assert.IsTrue(FrostMageRotation.ShouldSummonWaterElemental(false, 80, 1));
            Assert.IsTrue(FrostMageRotation.ShouldSummonWaterElemental(false, 20, 2));
            Assert.IsFalse(FrostMageRotation.ShouldSummonWaterElemental(true, 80, 2));
            Assert.IsFalse(FrostMageRotation.ShouldSummonWaterElemental(false, 20, 1));
        }

        [TestMethod]
        public void IcyVeinsIsUsedForPacksOrHighValueTargets()
        {
            Assert.IsTrue(FrostMageRotation.ShouldUseIcyVeins(2, false, 20));
            Assert.IsTrue(FrostMageRotation.ShouldUseIcyVeins(1, true, 20));
            Assert.IsTrue(FrostMageRotation.ShouldUseIcyVeins(1, false, 85));
            Assert.IsFalse(FrostMageRotation.ShouldUseIcyVeins(1, false, 20));
        }

        [TestMethod]
        public void ColdSnapIsSavedForLostControlOrMajorCooldownReuse()
        {
            Assert.IsTrue(FrostMageRotation.ShouldUseColdSnap(2, true, false, true, true, true, true, 50, 80));
            Assert.IsTrue(FrostMageRotation.ShouldUseColdSnap(1, true, true, true, false, true, true, 90, 80));
            Assert.IsTrue(FrostMageRotation.ShouldUseColdSnap(1, true, true, true, true, true, false, 90, 80));
            Assert.IsFalse(FrostMageRotation.ShouldUseColdSnap(2, true, true, true, true, true, true, 50, 80));
            Assert.IsFalse(FrostMageRotation.ShouldUseColdSnap(1, true, true, true, false, true, true, 20, 80));
            Assert.IsFalse(FrostMageRotation.ShouldUseColdSnap(1, true, true, false, false, false, false, 90, 80));
        }
    }
}
