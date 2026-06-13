using ElementalShamanBot;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BloogBotTests
{
    [TestClass]
    public class ElementalShamanRotationTests
    {
        [TestMethod]
        public void ImmunityListsMatchExactCreatureNames()
        {
            Assert.IsTrue(ElementalShamanRotation.IsNatureImmune("Swirling Vortex"));
            Assert.IsTrue(ElementalShamanRotation.IsFireImmune("Burning Destroyer"));
            Assert.IsTrue(ElementalShamanRotation.IsFearingCreature("Scorpid Terror"));
            Assert.IsFalse(ElementalShamanRotation.IsNatureImmune("Kobold Miner"));
            Assert.IsFalse(ElementalShamanRotation.IsFireImmune("Kobold Miner"));
        }

        [TestMethod]
        public void EarthShockInterruptsAndConsumesClearcasting()
        {
            Assert.IsTrue(ElementalShamanRotation.ShouldEarthShock(false, true, false, false));
            Assert.IsTrue(ElementalShamanRotation.ShouldEarthShock(false, false, false, true));
            Assert.IsFalse(ElementalShamanRotation.ShouldEarthShock(true, true, false, true));
            Assert.IsFalse(ElementalShamanRotation.ShouldEarthShock(false, false, false, false));
        }

        [TestMethod]
        public void LightningBoltNeedsRoomToFinishTheCast()
        {
            // Mob closing in from far away: enough time to cast.
            Assert.IsTrue(ElementalShamanRotation.ShouldLightningBolt(false, true, 16, false, 100, false));
            Assert.IsFalse(ElementalShamanRotation.ShouldLightningBolt(false, true, 15, false, 100, false));

            // Stationary mob just outside melee.
            Assert.IsTrue(ElementalShamanRotation.ShouldLightningBolt(false, false, 6, false, 100, false));
            Assert.IsFalse(ElementalShamanRotation.ShouldLightningBolt(false, false, 5, false, 100, false));

            // Focused Casting ignores pushback once the throttle elapses.
            Assert.IsTrue(ElementalShamanRotation.ShouldLightningBolt(false, true, 2, true, 50, true));
            Assert.IsFalse(ElementalShamanRotation.ShouldLightningBolt(false, true, 2, true, 50, false));

            Assert.IsFalse(ElementalShamanRotation.ShouldLightningBolt(true, true, 30, true, 100, true));
        }

        [TestMethod]
        public void FlameShockRespectsImmunitiesAndHealth()
        {
            Assert.IsTrue(ElementalShamanRotation.ShouldFlameShock(false, 50, false, false));
            Assert.IsFalse(ElementalShamanRotation.ShouldFlameShock(false, 49, false, false));
            Assert.IsTrue(ElementalShamanRotation.ShouldFlameShock(false, 10, true, false));
            Assert.IsFalse(ElementalShamanRotation.ShouldFlameShock(false, 50, false, true));
            Assert.IsFalse(ElementalShamanRotation.ShouldFlameShock(true, 50, false, false));
        }

        [TestMethod]
        public void WeaponEnchantFallsBackToRockbiterAgainstFireImmunes()
        {
            Assert.AreEqual(
                ElementalShamanRotation.RockbiterWeapon,
                ElementalShamanRotation.SelectWeaponEnchant(true, true, false, true));

            Assert.AreEqual(
                ElementalShamanRotation.FlametongueWeapon,
                ElementalShamanRotation.SelectWeaponEnchant(true, true, false, false));

            Assert.AreEqual(
                ElementalShamanRotation.RockbiterWeapon,
                ElementalShamanRotation.SelectWeaponEnchant(true, false, false, false));

            Assert.IsNull(ElementalShamanRotation.SelectWeaponEnchant(true, true, true, false));
            Assert.IsNull(ElementalShamanRotation.SelectWeaponEnchant(false, false, false, false));
        }
    }
}
