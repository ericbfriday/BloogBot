using ArmsWarriorBot;
using BloogBot.Game.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BloogBotTests
{
    [TestClass]
    public class ArmsWarriorRotationTests
    {
        [TestMethod]
        public void HeroicStrikeRageRequirementScalesAtLevelThirty()
        {
            Assert.AreEqual(15, ArmsWarriorRotation.HeroicStrikeRageRequirement(29));
            Assert.AreEqual(45, ArmsWarriorRotation.HeroicStrikeRageRequirement(30));
        }

        [TestMethod]
        public void SunderArmorIsUsedOnSunderTargetWithoutFullStacks()
        {
            Assert.IsTrue(ArmsWarriorRotation.ShouldSunderArmor(
                sunderStackCount: null,
                targetLevel: 20,
                playerLevel: 20,
                targetHealth: 500,
                targetIsSunderTarget: true));

            Assert.IsTrue(ArmsWarriorRotation.ShouldSunderArmor(
                sunderStackCount: 4,
                targetLevel: 20,
                playerLevel: 20,
                targetHealth: 500,
                targetIsSunderTarget: true));
        }

        [TestMethod]
        public void SunderArmorStopsAtFiveStacks()
        {
            Assert.IsFalse(ArmsWarriorRotation.ShouldSunderArmor(
                sunderStackCount: 5,
                targetLevel: 20,
                playerLevel: 20,
                targetHealth: 500,
                targetIsSunderTarget: true));
        }

        [TestMethod]
        public void SunderArmorSkipsLowLevelAndNonSunderTargets()
        {
            Assert.IsFalse(ArmsWarriorRotation.ShouldSunderArmor(
                sunderStackCount: null,
                targetLevel: 15,
                playerLevel: 20,
                targetHealth: 500,
                targetIsSunderTarget: true));

            Assert.IsFalse(ArmsWarriorRotation.ShouldSunderArmor(
                sunderStackCount: null,
                targetLevel: 20,
                playerLevel: 20,
                targetHealth: 500,
                targetIsSunderTarget: false));
        }

        [TestMethod]
        public void SunderTargetMatchesHighArmorMobFamilies()
        {
            Assert.IsTrue(ArmsWarriorRotation.IsSunderTarget("Elder Mottled Boar Grizzly"));
            Assert.IsTrue(ArmsWarriorRotation.IsSunderTarget("Sarkoth Scorpid"));
            Assert.IsFalse(ArmsWarriorRotation.IsSunderTarget("Kobold Miner"));
            Assert.IsFalse(ArmsWarriorRotation.IsSunderTarget(null));
        }

        [TestMethod]
        public void HamstringIsUsedOnFleeingHumanoids()
        {
            Assert.IsTrue(ArmsWarriorRotation.ShouldHamstring(
                targetIsHumanoid: true,
                targetName: "Defias Thug",
                targetHealthPercent: 29,
                targetHasHamstring: false));
        }

        [TestMethod]
        public void HamstringIsNotRefreshedOrUsedEarly()
        {
            Assert.IsFalse(ArmsWarriorRotation.ShouldHamstring(
                targetIsHumanoid: true,
                targetName: "Defias Thug",
                targetHealthPercent: 29,
                targetHasHamstring: true));

            Assert.IsFalse(ArmsWarriorRotation.ShouldHamstring(
                targetIsHumanoid: true,
                targetName: "Defias Thug",
                targetHealthPercent: 30,
                targetHasHamstring: false));
        }

        [TestMethod]
        public void HamstringTreatsPlainstridersLikeHumanoids()
        {
            Assert.IsTrue(ArmsWarriorRotation.ShouldHamstring(
                targetIsHumanoid: false,
                targetName: "Greater Plainstrider",
                targetHealthPercent: 20,
                targetHasHamstring: false));
        }

        [TestMethod]
        public void RendSkipsElementalsAndUndead()
        {
            Assert.IsTrue(ArmsWarriorRotation.ShouldRend(80, false, CreatureType.Beast));
            Assert.IsFalse(ArmsWarriorRotation.ShouldRend(80, false, CreatureType.Elemental));
            Assert.IsFalse(ArmsWarriorRotation.ShouldRend(80, false, CreatureType.Undead));
            Assert.IsFalse(ArmsWarriorRotation.ShouldRend(80, true, CreatureType.Beast));
            Assert.IsFalse(ArmsWarriorRotation.ShouldRend(50, false, CreatureType.Beast));
        }

        [TestMethod]
        public void AoeFillersWaitUntilAoeToolsAreSpent()
        {
            // Thunder Clap still pending on a healthy target: keep working the AoE tools.
            Assert.IsFalse(ArmsWarriorRotation.ShouldUseSingleTargetFillersInAoe(
                targetHasThunderClap: false,
                knowsThunderClap: true,
                targetHasDemoralizingShout: true,
                knowsDemoralizingShout: true,
                targetHealthPercent: 80,
                hasSweepingStrikes: true,
                sweepingStrikesReady: false));

            // Sweeping Strikes is ready but not active: cast it before filler.
            Assert.IsFalse(ArmsWarriorRotation.ShouldUseSingleTargetFillersInAoe(
                targetHasThunderClap: true,
                knowsThunderClap: true,
                targetHasDemoralizingShout: true,
                knowsDemoralizingShout: true,
                targetHealthPercent: 80,
                hasSweepingStrikes: false,
                sweepingStrikesReady: true));

            // Everything applied: fall through to single-target fillers.
            Assert.IsTrue(ArmsWarriorRotation.ShouldUseSingleTargetFillersInAoe(
                targetHasThunderClap: true,
                knowsThunderClap: true,
                targetHasDemoralizingShout: true,
                knowsDemoralizingShout: true,
                targetHealthPercent: 80,
                hasSweepingStrikes: true,
                sweepingStrikesReady: false));

            // Low-level warrior who knows neither shout: fillers immediately.
            Assert.IsTrue(ArmsWarriorRotation.ShouldUseSingleTargetFillersInAoe(
                targetHasThunderClap: false,
                knowsThunderClap: false,
                targetHasDemoralizingShout: false,
                knowsDemoralizingShout: false,
                targetHealthPercent: 80,
                hasSweepingStrikes: false,
                sweepingStrikesReady: false));
        }
    }
}
