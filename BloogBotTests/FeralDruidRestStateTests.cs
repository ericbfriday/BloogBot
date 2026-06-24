using FeralDruidBot;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BloogBotTests
{
    [TestClass]
    public class FeralDruidRestStateTests
    {
        [TestMethod]
        public void HealthCutoffMatchesHealingWindow()
        {
            Assert.IsFalse(RestState.IsHealthOk(healthPercent: 79));
            Assert.IsTrue(RestState.IsHealthOk(healthPercent: 80));
        }

        [TestMethod]
        public void MissingDrinkCannotBlockRestExit()
        {
            Assert.IsTrue(RestState.IsManaOk(
                level: 30,
                manaPercent: 10,
                isDrinking: false,
                hasDrink: false));
        }

        [TestMethod]
        public void RestHealingChoosesOneAvailableSpell()
        {
            Assert.AreEqual(
                RestState.Regrowth,
                RestState.SelectRestHeal(
                    healthPercent: 50,
                    hasRegrowth: false,
                    canCastRegrowth: true,
                    hasRejuvenation: false,
                    canCastRejuvenation: true));

            Assert.AreEqual(
                RestState.Rejuvenation,
                RestState.SelectRestHeal(
                    healthPercent: 70,
                    hasRegrowth: false,
                    canCastRegrowth: false,
                    hasRejuvenation: false,
                    canCastRejuvenation: true));
        }
    }
}
