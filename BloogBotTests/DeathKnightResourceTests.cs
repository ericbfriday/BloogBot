using BloogBot.Game;
using BloogBot.Game.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BloogBotTests
{
    [TestClass]
    public class DeathKnightResourceTests
    {
        [TestMethod]
        public void ParseRuneStateCoercesLuaStrings()
        {
            var state = DeathKnightResources.ParseRuneState(1, "100.5", "10", "0", "4", "1");

            Assert.AreEqual(1, state.Slot);
            Assert.AreEqual(RuneType.Death, state.Type);
            Assert.IsFalse(state.Ready);
            Assert.AreEqual(1, state.Count);
            Assert.AreEqual(10f, state.CooldownRemainingSeconds);
        }

        [TestMethod]
        public void ParseRuneStateDefaultsInvalidLuaStrings()
        {
            var state = DeathKnightResources.ParseRuneState(2, "", "bad", "", "", "");

            Assert.AreEqual(2, state.Slot);
            Assert.AreEqual(RuneType.Unknown, state.Type);
            Assert.IsFalse(state.Ready);
            Assert.AreEqual(0, state.Count);
            Assert.AreEqual(0f, state.CooldownRemainingSeconds);
        }

        [TestMethod]
        public void HasReadyRunesUsesExactRunesFirst()
        {
            var runes = new[]
            {
                new RuneState(1, RuneType.Blood, true, 1, 0),
                new RuneState(2, RuneType.Frost, true, 1, 0),
                new RuneState(3, RuneType.Unholy, true, 1, 0),
            };

            var cost = new DkAbilityCost(bloodRunes: 1, frostRunes: 1, unholyRunes: 1);

            Assert.IsTrue(DeathKnightResources.HasReadyRunes(runes, cost));
        }

        [TestMethod]
        public void HasReadyRunesUsesDeathRunesAsWildcards()
        {
            var runes = new[]
            {
                new RuneState(1, RuneType.Blood, true, 1, 0),
                new RuneState(2, RuneType.Death, true, 1, 0),
                new RuneState(3, RuneType.Unholy, true, 1, 0),
            };

            var cost = new DkAbilityCost(bloodRunes: 1, frostRunes: 1, unholyRunes: 1);

            Assert.IsTrue(DeathKnightResources.HasReadyRunes(runes, cost));
        }

        [TestMethod]
        public void HasReadyRunesRejectsCoolingRunes()
        {
            var runes = new[]
            {
                new RuneState(1, RuneType.Blood, true, 1, 0),
                new RuneState(2, RuneType.Frost, false, 1, 3.5f),
                new RuneState(3, RuneType.Unholy, true, 1, 0),
                new RuneState(4, RuneType.Death, false, 1, 4.5f),
            };

            var cost = new DkAbilityCost(bloodRunes: 1, frostRunes: 1, unholyRunes: 1);

            Assert.IsFalse(DeathKnightResources.HasReadyRunes(runes, cost));
        }

        [TestMethod]
        public void HasEnoughRunicPowerChecksThreshold()
        {
            var cost = new DkAbilityCost(runicPower: 40);

            Assert.IsFalse(DeathKnightResources.HasEnoughRunicPower(39, cost));
            Assert.IsTrue(DeathKnightResources.HasEnoughRunicPower(40, cost));
        }

        [TestMethod]
        public void GetKnownCostReturnsBloodStrikeCost()
        {
            var cost = DeathKnightResources.GetKnownCost("Blood Strike");

            Assert.AreEqual(1, cost.BloodRunes);
            Assert.AreEqual(0, cost.FrostRunes);
            Assert.AreEqual(0, cost.UnholyRunes);
            Assert.AreEqual(0, cost.RunicPower);
        }

        [TestMethod]
        public void GetKnownCostReturnsRuneTapCost()
        {
            var cost = DeathKnightResources.GetKnownCost("Rune Tap");

            Assert.AreEqual(1, cost.BloodRunes);
            Assert.AreEqual(0, cost.FrostRunes);
            Assert.AreEqual(0, cost.UnholyRunes);
            Assert.AreEqual(0, cost.RunicPower);
        }

        [TestMethod]
        public void GetKnownCostReturnsZeroCostForUnknownSpell()
        {
            var cost = DeathKnightResources.GetKnownCost("A Made Up Spell");

            Assert.AreEqual(0, cost.TotalRunes);
            Assert.AreEqual(0, cost.RunicPower);
        }

        [TestMethod]
        public void RequiresGroundTargetFlagsDeathAndDecay()
        {
            Assert.IsTrue(DeathKnightResources.RequiresGroundTarget("Death and Decay"));
            Assert.IsFalse(DeathKnightResources.RequiresGroundTarget("Death Strike"));
        }
    }
}
