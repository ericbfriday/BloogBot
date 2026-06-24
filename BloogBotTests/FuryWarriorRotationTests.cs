using BloogBot.Game.Enums;
using FuryWarriorBot;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BloogBotTests
{
    [TestClass]
    public class FuryWarriorRotationTests
    {
        [TestMethod]
        public void BerserkerStanceRequiresLevelThirty()
        {
            Assert.IsFalse(FuryWarriorRotation.ShouldEnterBerserkerStance(
                29, FuryWarriorRotation.BattleStance, true, 50, CreatureType.Beast));

            Assert.IsTrue(FuryWarriorRotation.ShouldEnterBerserkerStance(
                30, FuryWarriorRotation.BattleStance, true, 50, CreatureType.Beast));
        }

        [TestMethod]
        public void BerserkerStanceOnlySwapsFromBattleStance()
        {
            Assert.IsFalse(FuryWarriorRotation.ShouldEnterBerserkerStance(
                60, FuryWarriorRotation.BerserkerStance, true, 50, CreatureType.Beast));
        }

        [TestMethod]
        public void BattleStanceIsKeptUntilRendIsAppliedToFreshTarget()
        {
            // Fresh, rendable target: stay in Battle Stance to apply Rend first.
            Assert.IsFalse(FuryWarriorRotation.ShouldEnterBerserkerStance(
                60, FuryWarriorRotation.BattleStance, false, 100, CreatureType.Beast));

            // Rend never applies to elementals/undead, so swap immediately.
            Assert.IsTrue(FuryWarriorRotation.ShouldEnterBerserkerStance(
                60, FuryWarriorRotation.BattleStance, false, 100, CreatureType.Elemental));
            Assert.IsTrue(FuryWarriorRotation.ShouldEnterBerserkerStance(
                60, FuryWarriorRotation.BattleStance, false, 100, CreatureType.Undead));
        }

        [TestMethod]
        public void PummelRequiresBerserkerStanceAndCastingManaUser()
        {
            Assert.IsTrue(FuryWarriorRotation.ShouldPummel(
                FuryWarriorRotation.BerserkerStance, 100, true, false));

            Assert.IsFalse(FuryWarriorRotation.ShouldPummel(
                FuryWarriorRotation.BattleStance, 100, true, false));

            Assert.IsFalse(FuryWarriorRotation.ShouldPummel(
                FuryWarriorRotation.BerserkerStance, 0, true, false));

            Assert.IsFalse(FuryWarriorRotation.ShouldPummel(
                FuryWarriorRotation.BerserkerStance, 100, false, false));
        }

        [TestMethod]
        public void WhirlwindRequiresMeleeRangeAndUnfearedTarget()
        {
            Assert.IsTrue(FuryWarriorRotation.ShouldWhirlwind(
                FuryWarriorRotation.BerserkerStance, 50, false, true));

            Assert.IsFalse(FuryWarriorRotation.ShouldWhirlwind(
                FuryWarriorRotation.BerserkerStance, 50, true, true));

            Assert.IsFalse(FuryWarriorRotation.ShouldWhirlwind(
                FuryWarriorRotation.BerserkerStance, 50, false, false));

            Assert.IsFalse(FuryWarriorRotation.ShouldWhirlwind(
                FuryWarriorRotation.BattleStance, 50, false, true));
        }

        [TestMethod]
        public void RetaliationIsHeldWhenCastersAreAttacking()
        {
            Assert.IsTrue(FuryWarriorRotation.ShouldUseRetaliation(
                true, 0, FuryWarriorRotation.BattleStance, true, false));

            Assert.IsFalse(FuryWarriorRotation.ShouldUseRetaliation(
                true, 1, FuryWarriorRotation.BattleStance, true, false));

            Assert.IsFalse(FuryWarriorRotation.ShouldUseRetaliation(
                true, 0, FuryWarriorRotation.BattleStance, false, false));

            Assert.IsFalse(FuryWarriorRotation.ShouldUseRetaliation(
                true, 0, FuryWarriorRotation.BattleStance, true, true));
        }

        [TestMethod]
        public void HeroicStrikeRageRequirementScalesAtLevelThirty()
        {
            Assert.AreEqual(15, FuryWarriorRotation.HeroicStrikeRageRequirement(29));
            Assert.AreEqual(45, FuryWarriorRotation.HeroicStrikeRageRequirement(30));
        }

        [TestMethod]
        public void DeathWishNeedsReadyCooldownAndHealthyTarget()
        {
            Assert.IsTrue(FuryWarriorRotation.ShouldUseDeathWish(true, 81));

            Assert.IsFalse(FuryWarriorRotation.ShouldUseDeathWish(false, 81));
            Assert.IsFalse(FuryWarriorRotation.ShouldUseDeathWish(true, 80));
        }

        [TestMethod]
        public void BloodFuryNeedsHealthyTarget()
        {
            Assert.IsTrue(FuryWarriorRotation.ShouldUseBloodFury(81));
            Assert.IsFalse(FuryWarriorRotation.ShouldUseBloodFury(80));
        }

        [TestMethod]
        public void BattleShoutIsRefreshedOnlyWhenMissing()
        {
            Assert.IsTrue(FuryWarriorRotation.ShouldUseBattleShout(false));
            Assert.IsFalse(FuryWarriorRotation.ShouldUseBattleShout(true));
        }

        [TestMethod]
        public void BloodrageIsUsedAboveHalfHealth()
        {
            Assert.IsTrue(FuryWarriorRotation.ShouldUseBloodrage(51));
            Assert.IsFalse(FuryWarriorRotation.ShouldUseBloodrage(50));
        }

        [TestMethod]
        public void ExecuteFiresBelowTwentyPercent()
        {
            Assert.IsTrue(FuryWarriorRotation.ShouldExecute(19));
            Assert.IsFalse(FuryWarriorRotation.ShouldExecute(20));
        }

        [TestMethod]
        public void BerserkerRageNeedsBerserkerStanceAndHealthyTarget()
        {
            Assert.IsTrue(FuryWarriorRotation.ShouldUseBerserkerRage(71, FuryWarriorRotation.BerserkerStance));

            Assert.IsFalse(FuryWarriorRotation.ShouldUseBerserkerRage(70, FuryWarriorRotation.BerserkerStance));
            Assert.IsFalse(FuryWarriorRotation.ShouldUseBerserkerRage(71, FuryWarriorRotation.BattleStance));
        }

        [TestMethod]
        public void OverpowerNeedsBattleStanceAndDodgeProc()
        {
            Assert.IsTrue(FuryWarriorRotation.ShouldOverpower(FuryWarriorRotation.BattleStance, true));

            Assert.IsFalse(FuryWarriorRotation.ShouldOverpower(FuryWarriorRotation.BerserkerStance, true));
            Assert.IsFalse(FuryWarriorRotation.ShouldOverpower(FuryWarriorRotation.BattleStance, false));
        }

        [TestMethod]
        public void IntimidatingShoutNeedsBunchedUnfearedNonRetaliatingPack()
        {
            Assert.IsTrue(FuryWarriorRotation.ShouldUseIntimidatingShout(false, false, true));

            Assert.IsFalse(FuryWarriorRotation.ShouldUseIntimidatingShout(true, false, true));
            Assert.IsFalse(FuryWarriorRotation.ShouldUseIntimidatingShout(false, true, true));
            Assert.IsFalse(FuryWarriorRotation.ShouldUseIntimidatingShout(false, false, false));
        }

        [TestMethod]
        public void DemoralizingShoutIsRefreshedOnlyWhenMissing()
        {
            Assert.IsTrue(FuryWarriorRotation.ShouldUseDemoralizingShout(false));
            Assert.IsFalse(FuryWarriorRotation.ShouldUseDemoralizingShout(true));
        }

        [TestMethod]
        public void SlamNeedsProcAboveExecuteRange()
        {
            Assert.IsTrue(FuryWarriorRotation.ShouldSlam(21, true));

            Assert.IsFalse(FuryWarriorRotation.ShouldSlam(20, true));
            Assert.IsFalse(FuryWarriorRotation.ShouldSlam(21, false));
        }

        [TestMethod]
        public void HamstringSnaresHumanoidsWithoutTheDebuff()
        {
            Assert.IsTrue(FuryWarriorRotation.ShouldHamstring(CreatureType.Humanoid, false));

            Assert.IsFalse(FuryWarriorRotation.ShouldHamstring(CreatureType.Beast, false));
            Assert.IsFalse(FuryWarriorRotation.ShouldHamstring(CreatureType.Humanoid, true));
        }

        [TestMethod]
        public void HeroicStrikeFiresAboveThirtyPercent()
        {
            Assert.IsTrue(FuryWarriorRotation.ShouldHeroicStrike(31));
            Assert.IsFalse(FuryWarriorRotation.ShouldHeroicStrike(30));
        }

        [TestMethod]
        public void SunderArmorIsAppliedOnDamagedTargetsWithoutTheDebuff()
        {
            Assert.IsTrue(FuryWarriorRotation.ShouldSunderArmor(79, false));

            Assert.IsFalse(FuryWarriorRotation.ShouldSunderArmor(80, false));
            Assert.IsFalse(FuryWarriorRotation.ShouldSunderArmor(79, true));
        }
    }
}
