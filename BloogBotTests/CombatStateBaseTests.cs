using BloogBot.AI.SharedStates;
using BloogBot.Game.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BloogBotTests
{
    [TestClass]
    public class CombatStateBaseTests
    {
        [TestMethod]
        public void RangedSlotRelicsDoNotSuppressMeleeAutoAttack()
        {
            Assert.IsFalse(CombatStateBase.ShouldSuppressMeleeAutoAttack(ItemSubclass.Totem));
            Assert.IsFalse(CombatStateBase.ShouldSuppressMeleeAutoAttack(ItemSubclass.Idol));
            Assert.IsFalse(CombatStateBase.ShouldSuppressMeleeAutoAttack(ItemSubclass.Sigil));
        }

        [TestMethod]
        public void WandSuppressesMeleeAutoAttack()
        {
            Assert.IsTrue(CombatStateBase.ShouldSuppressMeleeAutoAttack(ItemSubclass.Wand));
        }
    }
}
