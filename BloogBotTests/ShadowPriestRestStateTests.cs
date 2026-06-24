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

        [TestMethod]
        public void HighHealthRecoveryUsesAffordableLesserHeal()
        {
            Assert.AreEqual(
                ShadowPriestRecovery.LesserHeal,
                ShadowPriestRecovery.SelectHeal(
                    healthPercent: 85,
                    canCastHeal: false,
                    canCastLesserHeal: true));
        }

        [TestMethod]
        public void SevereDamageKeepsHealPriority()
        {
            Assert.AreEqual(
                ShadowPriestRecovery.Heal,
                ShadowPriestRecovery.SelectHeal(
                    healthPercent: 40,
                    canCastHeal: true,
                    canCastLesserHeal: true));
        }

        [TestMethod]
        public void HealingLeavesShadowformFirst()
        {
            Assert.IsTrue(ShadowPriestRecovery.ShouldLeaveShadowform(
                hasShadowform: true,
                selectedHeal: ShadowPriestRecovery.LesserHeal));
            Assert.IsFalse(ShadowPriestRecovery.ShouldLeaveShadowform(
                hasShadowform: true,
                selectedHeal: null));
        }

        [TestMethod]
        public void MissingDiseaseCureDoesNotCreateAnAction()
        {
            Assert.IsNull(ShadowPriestRecovery.SelectDiseaseCure(
                knowsAbolishDisease: false,
                knowsCureDisease: false));

            Assert.AreEqual(
                ShadowPriestRecovery.CureDisease,
                ShadowPriestRecovery.SelectDiseaseCure(
                    knowsAbolishDisease: false,
                    knowsCureDisease: true));
        }

        [TestMethod]
        public void RestConsumablesDoNotInterruptEachOther()
        {
            Assert.AreEqual(
                RestConsumable.None,
                RestState.SelectConsumable(
                    level: 30,
                    hasFood: true,
                    isEating: true,
                    healthPercent: 40,
                    hasDrink: true,
                    isDrinking: false,
                    manaPercent: 20));

            Assert.AreEqual(
                RestConsumable.None,
                RestState.SelectConsumable(
                    level: 30,
                    hasFood: true,
                    isEating: false,
                    healthPercent: 40,
                    hasDrink: true,
                    isDrinking: true,
                    manaPercent: 20));
        }
    }
}
