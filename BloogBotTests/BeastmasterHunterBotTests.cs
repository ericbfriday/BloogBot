using BeastMasterHunterBot;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BloogBotTests
{
    [TestClass]
    public class BeastmasterHunterBotTests
    {
        [TestMethod]
        public void MendPetRequiresLivingInjuredPetWithoutExistingHot()
        {
            Assert.IsTrue(CombatState.ShouldCastMendPet(
                petAlive: true,
                petHealthPercent: 74,
                petHasMendPetBuff: false,
                mendPetReady: true));

            Assert.IsFalse(CombatState.ShouldCastMendPet(
                petAlive: true,
                petHealthPercent: 75,
                petHasMendPetBuff: false,
                mendPetReady: true));

            Assert.IsFalse(CombatState.ShouldCastMendPet(
                petAlive: true,
                petHealthPercent: 30,
                petHasMendPetBuff: true,
                mendPetReady: true));
        }

        [TestMethod]
        public void MendPetRequiresSpellToBeReady()
        {
            Assert.IsFalse(CombatState.ShouldCastMendPet(
                petAlive: true,
                petHealthPercent: 30,
                petHasMendPetBuff: false,
                mendPetReady: false));
        }

        [TestMethod]
        public void ErrandsSkipBlankConsumableNames()
        {
            Assert.IsFalse(RestState.ShouldAddErrandItem(null, amountToBuy: 1));
            Assert.IsFalse(RestState.ShouldAddErrandItem(string.Empty, amountToBuy: 1));
            Assert.IsFalse(RestState.ShouldAddErrandItem("   ", amountToBuy: 1));
            Assert.IsFalse(RestState.ShouldAddErrandItem("Ice Cold Milk", amountToBuy: 0));
            Assert.IsTrue(RestState.ShouldAddErrandItem("Ice Cold Milk", amountToBuy: 1));
        }
    }
}
