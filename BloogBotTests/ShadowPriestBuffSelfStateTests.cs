using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShadowPriestBot;

namespace BloogBotTests
{
    [TestClass]
    public class ShadowPriestBuffSelfStateTests
    {
        [TestMethod]
        public void UnknownBuffsAreSkipped()
        {
            Assert.IsFalse(ShadowPriestBuffSelfState.ShouldCastBuff(
                hasBuff: false,
                knowsSpell: false,
                isSpellReady: true));
        }

        [TestMethod]
        public void KnownReadyMissingBuffsAreCast()
        {
            Assert.IsTrue(ShadowPriestBuffSelfState.ShouldCastBuff(
                hasBuff: false,
                knowsSpell: true,
                isSpellReady: true));
        }

        [TestMethod]
        public void KnownReadyBuffsAreSkippedWithoutEnoughMana()
        {
            Assert.IsFalse(ShadowPriestBuffSelfState.ShouldCastBuff(
                hasBuff: false,
                knowsSpell: true,
                isSpellReady: true,
                hasEnoughMana: false));
        }

        [TestMethod]
        public void BuffStateCanExitWhenAllMissingBuffsAreUnknown()
        {
            Assert.IsTrue(ShadowPriestBuffSelfState.HasAllKnownBuffs(
                knowsPowerWordFortitude: true,
                hasPowerWordFortitude: true,
                knowsShadowProtection: false,
                hasShadowProtection: false));
        }
    }
}
