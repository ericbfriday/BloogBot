using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetributionPaladinBot;

namespace BloogBotTests
{
    [TestClass]
    public class RetributionPaladinRestStateTests
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

        [TestMethod]
        public void HighHealthRecoveryUsesRankOneAtBoundaryValues()
        {
            Assert.AreEqual(1, RestState.SelectHolyLightRank(70, true, true));
            Assert.AreEqual(1, RestState.SelectHolyLightRank(90, true, true));
        }

        [TestMethod]
        public void SevereDamagePrefersHighestAffordableHolyLight()
        {
            Assert.AreEqual(-1, RestState.SelectHolyLightRank(69, true, true));
            Assert.AreEqual(1, RestState.SelectHolyLightRank(69, false, true));
            Assert.IsNull(RestState.SelectHolyLightRank(69, false, false));
        }

        [TestMethod]
        public void RestConsumablesDoNotInterruptEachOther()
        {
            Assert.AreEqual(
                RestConsumable.None,
                RestState.SelectConsumable(30, true, true, 40, true, false, 20));
            Assert.AreEqual(
                RestConsumable.None,
                RestState.SelectConsumable(30, true, false, 40, true, true, 20));
        }

        [TestMethod]
        public void OptionalRecoverySpellsMustBeKnownReadyAndAffordable()
        {
            Assert.IsFalse(HealSelfState.CanCastSpell(false, true, 100, 10));
            Assert.IsFalse(HealSelfState.CanCastSpell(true, false, 100, 10));
            Assert.IsFalse(HealSelfState.CanCastSpell(true, true, 9, 10));
            Assert.IsTrue(HealSelfState.CanCastSpell(true, true, 10, 10));
        }
    }
}
