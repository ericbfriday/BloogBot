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

        [TestMethod]
        public void IntimidatingShoutFearsAClusteredPack()
        {
            Assert.IsTrue(ArmsWarriorRotation.ShouldIntimidatingShout(
                targetHasIntimidatingShout: false,
                hasRetaliation: false,
                allAggressorsInRange: true,
                neutralBystanderInRange: false));
        }

        [TestMethod]
        public void IntimidatingShoutIsHeldBackWhenUnsafe()
        {
            // Already feared.
            Assert.IsFalse(ArmsWarriorRotation.ShouldIntimidatingShout(
                targetHasIntimidatingShout: true,
                hasRetaliation: false,
                allAggressorsInRange: true,
                neutralBystanderInRange: false));

            // Retaliation is up: don't scatter the pack.
            Assert.IsFalse(ArmsWarriorRotation.ShouldIntimidatingShout(
                targetHasIntimidatingShout: false,
                hasRetaliation: true,
                allAggressorsInRange: true,
                neutralBystanderInRange: false));

            // An aggressor is out of range.
            Assert.IsFalse(ArmsWarriorRotation.ShouldIntimidatingShout(
                targetHasIntimidatingShout: false,
                hasRetaliation: false,
                allAggressorsInRange: false,
                neutralBystanderInRange: false));

            // A neutral mob nearby would get pulled.
            Assert.IsFalse(ArmsWarriorRotation.ShouldIntimidatingShout(
                targetHasIntimidatingShout: false,
                hasRetaliation: false,
                allAggressorsInRange: true,
                neutralBystanderInRange: true));
        }

        [TestMethod]
        public void RetaliationPopsOnAClusteredUnfearedPack()
        {
            Assert.IsTrue(ArmsWarriorRotation.ShouldRetaliation(
                retaliationReady: true,
                allAggressorsInRange: true,
                anyAggressorHasIntimidatingShout: false));
        }

        [TestMethod]
        public void RetaliationIsHeldBackWhenUnsafe()
        {
            // Not off cooldown.
            Assert.IsFalse(ArmsWarriorRotation.ShouldRetaliation(
                retaliationReady: false,
                allAggressorsInRange: true,
                anyAggressorHasIntimidatingShout: false));

            // An aggressor is out of range.
            Assert.IsFalse(ArmsWarriorRotation.ShouldRetaliation(
                retaliationReady: true,
                allAggressorsInRange: false,
                anyAggressorHasIntimidatingShout: false));

            // Something is feared and would run out of cleave range.
            Assert.IsFalse(ArmsWarriorRotation.ShouldRetaliation(
                retaliationReady: true,
                allAggressorsInRange: true,
                anyAggressorHasIntimidatingShout: true));
        }

        [TestMethod]
        public void DemoralizingShoutFillsWhenIntimidatingShoutIsUnavailable()
        {
            Assert.IsTrue(ArmsWarriorRotation.ShouldDemoralizingShout(
                anyAggressorNeedsDemoralizingShout: true,
                allAggressorsInRange: true,
                intimidatingShoutReady: false,
                hasRetaliation: false,
                neutralOrFearedUnitInRange: false));

            // Intimidating Shout still on cooldown but Retaliation is up: still fine to shout.
            Assert.IsTrue(ArmsWarriorRotation.ShouldDemoralizingShout(
                anyAggressorNeedsDemoralizingShout: true,
                allAggressorsInRange: true,
                intimidatingShoutReady: true,
                hasRetaliation: true,
                neutralOrFearedUnitInRange: false));
        }

        [TestMethod]
        public void DemoralizingShoutIsHeldBackWhenUnsafe()
        {
            // Nobody needs the debuff.
            Assert.IsFalse(ArmsWarriorRotation.ShouldDemoralizingShout(
                anyAggressorNeedsDemoralizingShout: false,
                allAggressorsInRange: true,
                intimidatingShoutReady: false,
                hasRetaliation: false,
                neutralOrFearedUnitInRange: false));

            // An aggressor is out of range.
            Assert.IsFalse(ArmsWarriorRotation.ShouldDemoralizingShout(
                anyAggressorNeedsDemoralizingShout: true,
                allAggressorsInRange: false,
                intimidatingShoutReady: false,
                hasRetaliation: false,
                neutralOrFearedUnitInRange: false));

            // Intimidating Shout is ready (and we're not retaliating): prefer the fear.
            Assert.IsFalse(ArmsWarriorRotation.ShouldDemoralizingShout(
                anyAggressorNeedsDemoralizingShout: true,
                allAggressorsInRange: true,
                intimidatingShoutReady: true,
                hasRetaliation: false,
                neutralOrFearedUnitInRange: false));

            // A neutral or feared mob is nearby.
            Assert.IsFalse(ArmsWarriorRotation.ShouldDemoralizingShout(
                anyAggressorNeedsDemoralizingShout: true,
                allAggressorsInRange: true,
                intimidatingShoutReady: false,
                hasRetaliation: false,
                neutralOrFearedUnitInRange: true));
        }

        [TestMethod]
        public void ThunderClapFillsWhenIntimidatingShoutIsUnavailable()
        {
            Assert.IsTrue(ArmsWarriorRotation.ShouldThunderClap(
                anyAggressorNeedsThunderClap: true,
                allAggressorsInRange: true,
                intimidatingShoutReady: false,
                hasRetaliation: false,
                neutralOrFearedUnitInRange: false));

            // Intimidating Shout on cooldown but Retaliation is up.
            Assert.IsTrue(ArmsWarriorRotation.ShouldThunderClap(
                anyAggressorNeedsThunderClap: true,
                allAggressorsInRange: true,
                intimidatingShoutReady: true,
                hasRetaliation: true,
                neutralOrFearedUnitInRange: false));
        }

        [TestMethod]
        public void ThunderClapIsHeldBackWhenUnsafe()
        {
            // Nobody needs the debuff.
            Assert.IsFalse(ArmsWarriorRotation.ShouldThunderClap(
                anyAggressorNeedsThunderClap: false,
                allAggressorsInRange: true,
                intimidatingShoutReady: false,
                hasRetaliation: false,
                neutralOrFearedUnitInRange: false));

            // An aggressor is out of range.
            Assert.IsFalse(ArmsWarriorRotation.ShouldThunderClap(
                anyAggressorNeedsThunderClap: true,
                allAggressorsInRange: false,
                intimidatingShoutReady: false,
                hasRetaliation: false,
                neutralOrFearedUnitInRange: false));

            // Intimidating Shout is ready (and we're not retaliating): prefer the fear.
            Assert.IsFalse(ArmsWarriorRotation.ShouldThunderClap(
                anyAggressorNeedsThunderClap: true,
                allAggressorsInRange: true,
                intimidatingShoutReady: true,
                hasRetaliation: false,
                neutralOrFearedUnitInRange: false));

            // A neutral or feared mob is nearby.
            Assert.IsFalse(ArmsWarriorRotation.ShouldThunderClap(
                anyAggressorNeedsThunderClap: true,
                allAggressorsInRange: true,
                intimidatingShoutReady: false,
                hasRetaliation: false,
                neutralOrFearedUnitInRange: true));
        }

        [TestMethod]
        public void SweepingStrikesIsRefreshedWhileTargetIsHealthy()
        {
            Assert.IsTrue(ArmsWarriorRotation.ShouldSweepingStrikes(
                hasSweepingStrikes: false,
                targetHealthPercent: 31));
        }

        [TestMethod]
        public void SweepingStrikesIsNotRefreshedOrUsedLate()
        {
            // Already active.
            Assert.IsFalse(ArmsWarriorRotation.ShouldSweepingStrikes(
                hasSweepingStrikes: true,
                targetHealthPercent: 80));

            // Target too low to be worth it.
            Assert.IsFalse(ArmsWarriorRotation.ShouldSweepingStrikes(
                hasSweepingStrikes: false,
                targetHealthPercent: 30));
        }
    }
}
