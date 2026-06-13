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
    }
}
