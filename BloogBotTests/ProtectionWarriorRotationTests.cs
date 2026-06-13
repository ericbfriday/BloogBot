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
    }
}
