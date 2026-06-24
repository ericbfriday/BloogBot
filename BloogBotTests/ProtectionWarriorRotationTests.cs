using BloogBot.Game.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtectionWarriorBot;

namespace BloogBotTests
{
    [TestClass]
    public class ProtectionWarriorRotationTests
    {
        [TestMethod]
        public void MultiTargetAbilitiesRunUntilAoeThreatIsEstablished()
        {
            Assert.IsTrue(ProtectionWarriorRotation.ShouldUseMultiTargetAbilities(2, false, false));
            Assert.IsTrue(ProtectionWarriorRotation.ShouldUseMultiTargetAbilities(2, true, false));
            Assert.IsFalse(ProtectionWarriorRotation.ShouldUseMultiTargetAbilities(2, true, true));
            Assert.IsFalse(ProtectionWarriorRotation.ShouldUseMultiTargetAbilities(1, false, false));
        }

        [TestMethod]
        public void SingleTargetAbilitiesRunForOneAggressorOrEstablishedThreat()
        {
            Assert.IsTrue(ProtectionWarriorRotation.ShouldUseSingleTargetAbilities(1, false, false));
            Assert.IsTrue(ProtectionWarriorRotation.ShouldUseSingleTargetAbilities(3, true, true));
            Assert.IsFalse(ProtectionWarriorRotation.ShouldUseSingleTargetAbilities(3, true, false));
        }

        [TestMethod]
        public void ModesAreMutuallyExclusive()
        {
            for (var aggressors = 1; aggressors <= 3; aggressors++)
                foreach (var demo in new[] { false, true })
                    foreach (var clap in new[] { false, true })
                        Assert.IsFalse(
                            ProtectionWarriorRotation.ShouldUseMultiTargetAbilities(aggressors, demo, clap) &&
                            ProtectionWarriorRotation.ShouldUseSingleTargetAbilities(aggressors, demo, clap));
        }

        [TestMethod]
        public void RendSkipsElementalsAndUndead()
        {
            Assert.IsTrue(ProtectionWarriorRotation.ShouldRend(80, false, CreatureType.Beast));
            Assert.IsFalse(ProtectionWarriorRotation.ShouldRend(80, false, CreatureType.Elemental));
            Assert.IsFalse(ProtectionWarriorRotation.ShouldRend(80, false, CreatureType.Undead));
            Assert.IsFalse(ProtectionWarriorRotation.ShouldRend(80, true, CreatureType.Beast));
            Assert.IsFalse(ProtectionWarriorRotation.ShouldRend(50, false, CreatureType.Beast));
        }

        [TestMethod]
        public void RetaliationRequiresThreeOrMoreAggressors()
        {
            Assert.IsTrue(ProtectionWarriorRotation.ShouldUseRetaliation(3));
            Assert.IsTrue(ProtectionWarriorRotation.ShouldUseRetaliation(4));
            Assert.IsFalse(ProtectionWarriorRotation.ShouldUseRetaliation(2));
        }

        [TestMethod]
        public void DemoralizingShoutRefreshesWhenMissingAndAggressorsAreClose()
        {
            Assert.IsTrue(ProtectionWarriorRotation.ShouldUseDemoralizingShout(false, true));
            Assert.IsFalse(ProtectionWarriorRotation.ShouldUseDemoralizingShout(true, true));
            Assert.IsFalse(ProtectionWarriorRotation.ShouldUseDemoralizingShout(false, false));
        }

        [TestMethod]
        public void ThunderClapRefreshesWhenMissingAndAggressorsAreClose()
        {
            Assert.IsTrue(ProtectionWarriorRotation.ShouldUseThunderClap(false, true));
            Assert.IsFalse(ProtectionWarriorRotation.ShouldUseThunderClap(true, true));
            Assert.IsFalse(ProtectionWarriorRotation.ShouldUseThunderClap(false, false));
        }

        [TestMethod]
        public void LastStandFiresAtEightPercentOrLower()
        {
            Assert.IsTrue(ProtectionWarriorRotation.ShouldUseLastStand(8));
            Assert.IsTrue(ProtectionWarriorRotation.ShouldUseLastStand(5));
            Assert.IsFalse(ProtectionWarriorRotation.ShouldUseLastStand(9));
        }

        [TestMethod]
        public void ShieldBashInterruptsCastersWithMana()
        {
            Assert.IsTrue(ProtectionWarriorRotation.ShouldUseShieldBash(true, 100));
            Assert.IsFalse(ProtectionWarriorRotation.ShouldUseShieldBash(false, 100));
            Assert.IsFalse(ProtectionWarriorRotation.ShouldUseShieldBash(true, 0));
        }

        [TestMethod]
        public void ShieldSlamFiresAboveThirtyPercent()
        {
            Assert.IsTrue(ProtectionWarriorRotation.ShouldUseShieldSlam(31));
            Assert.IsFalse(ProtectionWarriorRotation.ShouldUseShieldSlam(30));
            Assert.IsFalse(ProtectionWarriorRotation.ShouldUseShieldSlam(20));
        }
    }
}
