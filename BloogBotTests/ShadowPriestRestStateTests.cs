using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShadowPriestBot;

namespace BloogBotTests
{
    [TestClass]
    public class ShadowPriestRestStateTests
    {
        [TestMethod]
        public void ManaIsOkWhenDrinkIsUnavailable()
        {
            Assert.IsTrue(RestState.IsManaOk(level: 30, manaPercent: 12, isDrinking: false, hasDrink: false));
        }

        [TestMethod]
        public void DrinkIsUsedUntilManaCanLeaveRest()
        {
            Assert.IsTrue(RestState.ShouldUseDrink(level: 30, hasDrink: true, isDrinking: false, manaPercent: 64));
            Assert.IsFalse(RestState.ShouldUseDrink(level: 30, hasDrink: true, isDrinking: false, manaPercent: 65));
        }

        [TestMethod]
        public void FoodIsUsedForLowHealthWithoutInterruptingCurrentFood()
        {
            Assert.IsTrue(RestState.ShouldUseFood(hasFood: true, isEating: false, healthPercent: 79));
            Assert.IsFalse(RestState.ShouldUseFood(hasFood: true, isEating: false, healthPercent: 80));
            Assert.IsFalse(RestState.ShouldUseFood(hasFood: true, isEating: true, healthPercent: 30));
        }

        [TestMethod]
        public void HealDoesNotInterruptFoodOrDrink()
        {
            Assert.IsFalse(RestState.ShouldHeal(healthOk: false, isEating: true, isDrinking: false, canCastHeal: true));
            Assert.IsFalse(RestState.ShouldHeal(healthOk: false, isEating: false, isDrinking: true, canCastHeal: true));
            Assert.IsTrue(RestState.ShouldHeal(healthOk: false, isEating: false, isDrinking: false, canCastHeal: true));
        }
    }
}
