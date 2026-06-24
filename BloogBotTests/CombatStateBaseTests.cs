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

        // Helper: all gates open. Each test below flips exactly one gate closed.
        static bool CanUse(
            bool knowsSpell = true,
            bool isSpellReady = true,
            int playerResource = 100,
            int requiredResource = 0,
            double distanceToTarget = 5,
            int minRange = 0,
            int maxRange = int.MaxValue,
            bool condition = true,
            bool isStunned = false,
            bool isCasting = false,
            bool isChanneling = false,
            bool isWarrior = false) =>
            CombatStateBase.CanUseRotationAbility(
                knowsSpell, isSpellReady, playerResource, requiredResource,
                distanceToTarget, minRange, maxRange, condition,
                isStunned, isCasting, isChanneling, isWarrior);

        [TestMethod]
        public void RotationAbilityFiresWhenEveryGateIsOpen()
        {
            Assert.IsTrue(CanUse());
        }

        [TestMethod]
        public void RotationAbilityHeldWhenSpellUnknown()
        {
            Assert.IsFalse(CanUse(knowsSpell: false));
        }

        [TestMethod]
        public void RotationAbilityHeldWhenNotReady()
        {
            Assert.IsFalse(CanUse(isSpellReady: false));
        }

        [TestMethod]
        public void RotationAbilityHeldWhenUnderResourced()
        {
            Assert.IsFalse(CanUse(playerResource: 10, requiredResource: 30));
            Assert.IsTrue(CanUse(playerResource: 30, requiredResource: 30));
        }

        [TestMethod]
        public void RotationAbilityHeldWhenOutOfRange()
        {
            Assert.IsFalse(CanUse(distanceToTarget: 4, minRange: 5));
            Assert.IsFalse(CanUse(distanceToTarget: 11, maxRange: 10));
            Assert.IsTrue(CanUse(distanceToTarget: 5, minRange: 5, maxRange: 10));
            Assert.IsTrue(CanUse(distanceToTarget: 10, minRange: 5, maxRange: 10));
        }

        [TestMethod]
        public void RotationAbilityHeldWhenStunned()
        {
            Assert.IsFalse(CanUse(isStunned: true));
        }

        [TestMethod]
        public void RotationAbilityHeldWhenConditionFalse()
        {
            Assert.IsFalse(CanUse(condition: false));
        }

        [TestMethod]
        public void RotationAbilityHeldWhileCastingExceptForWarriors()
        {
            // Non-warriors cannot use abilities mid-cast/channel.
            Assert.IsFalse(CanUse(isCasting: true, isWarrior: false));
            Assert.IsFalse(CanUse(isChanneling: true, isWarrior: false));

            // Warriors keep the casting-while-moving carve-out.
            Assert.IsTrue(CanUse(isCasting: true, isWarrior: true));
            Assert.IsTrue(CanUse(isChanneling: true, isWarrior: true));
        }
    }
}
