using BloogBot.Game.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetributionPaladinBot;

namespace BloogBotTests
{
    [TestClass]
    public class RetributionPaladinBuffSelfStateTests
    {
        [TestMethod]
        public void LevelTwelveMissingBlessingOfMightDoesNotExitBuffState()
        {
            Assert.IsFalse(RetributionPaladinBuffSelfState.HasRequiredBlessing(
                knowsBlessingOfMight: true,
                hasBlessingOfMight: false,
                hasBlessingOfKings: false,
                hasBlessingOfSanctuary: false));
        }

        [TestMethod]
        public void SelectsBlessingOfMightWhenNoBetterBlessingIsKnown()
        {
            Assert.AreEqual(
                RetributionPaladinBuffSelfState.BlessingOfMight,
                RetributionPaladinBuffSelfState.SelectBlessing(
                    knowsBlessingOfMight: true,
                    knowsBlessingOfKings: false,
                    knowsBlessingOfSanctuary: false));
        }

        [TestMethod]
        public void NonVanillaClientsCastSelfBuffsOnPlayerGuid()
        {
            Assert.AreEqual(
                SelfBuffCastMode.CastSpellOnPlayer,
                RetributionPaladinBuffSelfState.GetSelfBuffCastMode(ClientVersion.TBC));

            Assert.AreEqual(
                SelfBuffCastMode.CastSpellOnPlayer,
                RetributionPaladinBuffSelfState.GetSelfBuffCastMode(ClientVersion.WotLK));
        }

        [TestMethod]
        public void VanillaClientsUseLuaSelfCast()
        {
            Assert.AreEqual(
                SelfBuffCastMode.LuaCastOnSelf,
                RetributionPaladinBuffSelfState.GetSelfBuffCastMode(ClientVersion.Vanilla));
        }
    }
}
