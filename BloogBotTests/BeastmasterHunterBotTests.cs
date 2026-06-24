using BeastMasterHunterBot;
using BloogBot.Game.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HunterPetManagerState = BeastmasterHunterBot.PetManagerState;

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
                playerKnowsMendPet: true,
                mendPetReady: true,
                playerMana: 100,
                mendPetManaCost: 50));

            Assert.IsFalse(CombatState.ShouldCastMendPet(
                petAlive: true,
                petHealthPercent: 75,
                petHasMendPetBuff: false,
                playerKnowsMendPet: true,
                mendPetReady: true,
                playerMana: 100,
                mendPetManaCost: 50));

            Assert.IsFalse(CombatState.ShouldCastMendPet(
                petAlive: true,
                petHealthPercent: 30,
                petHasMendPetBuff: true,
                playerKnowsMendPet: true,
                mendPetReady: true,
                playerMana: 100,
                mendPetManaCost: 50));
        }

        [TestMethod]
        public void MendPetRequiresKnownReadySpellAndMana()
        {
            Assert.IsFalse(CombatState.ShouldCastMendPet(
                petAlive: true,
                petHealthPercent: 30,
                petHasMendPetBuff: false,
                playerKnowsMendPet: false,
                mendPetReady: true,
                playerMana: 100,
                mendPetManaCost: 50));

            Assert.IsFalse(CombatState.ShouldCastMendPet(
                petAlive: true,
                petHealthPercent: 30,
                petHasMendPetBuff: false,
                playerKnowsMendPet: true,
                mendPetReady: false,
                playerMana: 100,
                mendPetManaCost: 50));

            Assert.IsFalse(CombatState.ShouldCastMendPet(
                petAlive: true,
                petHealthPercent: 30,
                petHasMendPetBuff: false,
                playerKnowsMendPet: true,
                mendPetReady: true,
                playerMana: 49,
                mendPetManaCost: 50));
        }

        [TestMethod]
        public void TargetAurasTreatBuffsAndDebuffsAsExistingEffects()
        {
            Assert.IsTrue(BeastmasterHunterRotation.TargetHasAura(targetHasDebuff: false, targetHasBuff: true));
            Assert.IsTrue(BeastmasterHunterRotation.TargetHasAura(targetHasDebuff: true, targetHasBuff: false));
            Assert.IsFalse(BeastmasterHunterRotation.TargetHasAura(targetHasDebuff: false, targetHasBuff: false));

            Assert.IsFalse(BeastmasterHunterRotation.ShouldApplySerpentSting(
                targetHasSerpentStingDebuff: false,
                targetHasSerpentStingBuff: true,
                targetHealthPercent: 80,
                manaPercent: 100));

            Assert.IsFalse(BeastmasterHunterRotation.ShouldApplySerpentSting(
                targetHasSerpentStingDebuff: true,
                targetHasSerpentStingBuff: false,
                targetHealthPercent: 80,
                manaPercent: 100));

            Assert.IsTrue(BeastmasterHunterRotation.ShouldApplySerpentSting(
                targetHasSerpentStingDebuff: false,
                targetHasSerpentStingBuff: false,
                targetHealthPercent: 80,
                manaPercent: 100));
        }

        [TestMethod]
        public void TargetMaintenanceSkipsDyingTargetsAndLowMana()
        {
            Assert.IsFalse(BeastmasterHunterRotation.ShouldApplyHuntersMark(
                targetHasHuntersMarkDebuff: false,
                targetHasHuntersMarkBuff: false,
                targetHealthPercent: 19,
                manaPercent: 100));

            Assert.IsFalse(BeastmasterHunterRotation.ShouldApplyHuntersMark(
                targetHasHuntersMarkDebuff: false,
                targetHasHuntersMarkBuff: false,
                targetHealthPercent: 80,
                manaPercent: 9));

            Assert.IsFalse(BeastmasterHunterRotation.ShouldApplySerpentSting(
                targetHasSerpentStingDebuff: false,
                targetHasSerpentStingBuff: false,
                targetHealthPercent: 34,
                manaPercent: 100));

            Assert.IsFalse(BeastmasterHunterRotation.ShouldApplySerpentSting(
                targetHasSerpentStingDebuff: false,
                targetHasSerpentStingBuff: false,
                targetHealthPercent: 80,
                manaPercent: 24));
        }

        [TestMethod]
        public void AspectSelectionUsesViperForManaRecoveryAndDragonhawkForDamage()
        {
            Assert.AreEqual(BeastmasterHunterRotation.AspectOfTheViper, BeastmasterHunterRotation.SelectAspect(
                ClientVersion.WotLK,
                knowsAspectOfViper: true,
                hasAspectOfViper: false,
                knowsAspectOfDragonhawk: true,
                hasAspectOfDragonhawk: false,
                knowsAspectOfHawk: true,
                hasAspectOfHawk: true,
                manaPercent: 19));

            Assert.AreEqual(BeastmasterHunterRotation.AspectOfTheDragonhawk, BeastmasterHunterRotation.SelectAspect(
                ClientVersion.WotLK,
                knowsAspectOfViper: true,
                hasAspectOfViper: true,
                knowsAspectOfDragonhawk: true,
                hasAspectOfDragonhawk: false,
                knowsAspectOfHawk: true,
                hasAspectOfHawk: false,
                manaPercent: 80));

            Assert.AreEqual(BeastmasterHunterRotation.AspectOfTheHawk, BeastmasterHunterRotation.SelectAspect(
                ClientVersion.WotLK,
                knowsAspectOfViper: true,
                hasAspectOfViper: true,
                knowsAspectOfDragonhawk: false,
                hasAspectOfDragonhawk: false,
                knowsAspectOfHawk: true,
                hasAspectOfHawk: false,
                manaPercent: 80));

            Assert.IsNull(BeastmasterHunterRotation.SelectAspect(
                ClientVersion.Vanilla,
                knowsAspectOfViper: true,
                hasAspectOfViper: false,
                knowsAspectOfDragonhawk: true,
                hasAspectOfDragonhawk: false,
                knowsAspectOfHawk: true,
                hasAspectOfHawk: false,
                manaPercent: 19));
        }

        [TestMethod]
        public void WotlkRangedPriorityFollowsLevelingGuide()
        {
            Assert.AreEqual(BeastmasterHunterRotation.KillShot, BeastmasterHunterRotation.SelectRangedShot(
                ClientVersion.WotLK,
                targetHealthPercent: 20,
                manaPercent: 15,
                canKillShot: true,
                canAimedShot: true,
                canMultiShot: true,
                canSteadyShot: true,
                canArcaneShot: true,
                isMoving: false));

            Assert.AreEqual(BeastmasterHunterRotation.AimedShot, BeastmasterHunterRotation.SelectRangedShot(
                ClientVersion.WotLK,
                targetHealthPercent: 80,
                manaPercent: 80,
                canKillShot: false,
                canAimedShot: true,
                canMultiShot: true,
                canSteadyShot: true,
                canArcaneShot: true,
                isMoving: false));

            Assert.AreEqual(BeastmasterHunterRotation.MultiShot, BeastmasterHunterRotation.SelectRangedShot(
                ClientVersion.WotLK,
                targetHealthPercent: 80,
                manaPercent: 80,
                canKillShot: false,
                canAimedShot: false,
                canMultiShot: true,
                canSteadyShot: true,
                canArcaneShot: true,
                isMoving: false));

            Assert.AreEqual(BeastmasterHunterRotation.SteadyShot, BeastmasterHunterRotation.SelectRangedShot(
                ClientVersion.WotLK,
                targetHealthPercent: 80,
                manaPercent: 30,
                canKillShot: false,
                canAimedShot: false,
                canMultiShot: true,
                canSteadyShot: true,
                canArcaneShot: true,
                isMoving: false));
        }

        [TestMethod]
        public void RangedPriorityPreservesManaAndSkipsCastTimeShotsWhileMoving()
        {
            Assert.AreEqual(BeastmasterHunterRotation.ArcaneShot, BeastmasterHunterRotation.SelectRangedShot(
                ClientVersion.WotLK,
                targetHealthPercent: 80,
                manaPercent: 80,
                canKillShot: false,
                canAimedShot: false,
                canMultiShot: true,
                canSteadyShot: true,
                canArcaneShot: true,
                isMoving: true));

            Assert.IsNull(BeastmasterHunterRotation.SelectRangedShot(
                ClientVersion.WotLK,
                targetHealthPercent: 80,
                manaPercent: 14,
                canKillShot: false,
                canAimedShot: false,
                canMultiShot: true,
                canSteadyShot: true,
                canArcaneShot: true,
                isMoving: false));
        }

        [TestMethod]
        public void OffensiveCooldownsRequireMeaningfulFightsAndNoExistingBuff()
        {
            Assert.IsTrue(BeastmasterHunterRotation.ShouldUsePetOffensiveCooldown(
                petAlive: true,
                petHasCooldownBuff: false,
                targetHealthPercent: 30,
                aggressorCount: 1));

            Assert.IsTrue(BeastmasterHunterRotation.ShouldUsePetOffensiveCooldown(
                petAlive: true,
                petHasCooldownBuff: false,
                targetHealthPercent: 10,
                aggressorCount: 2));

            Assert.IsFalse(BeastmasterHunterRotation.ShouldUsePetOffensiveCooldown(
                petAlive: false,
                petHasCooldownBuff: false,
                targetHealthPercent: 80,
                aggressorCount: 2));

            Assert.IsFalse(BeastmasterHunterRotation.ShouldUsePetOffensiveCooldown(
                petAlive: true,
                petHasCooldownBuff: true,
                targetHealthPercent: 80,
                aggressorCount: 2));

            Assert.IsTrue(BeastmasterHunterRotation.ShouldUseHunterOffensiveCooldown(
                hunterHasCooldownBuff: false,
                targetHealthPercent: 50,
                aggressorCount: 1));

            Assert.IsFalse(BeastmasterHunterRotation.ShouldUseHunterOffensiveCooldown(
                hunterHasCooldownBuff: true,
                targetHealthPercent: 80,
                aggressorCount: 2));
        }

        [TestMethod]
        public void UtilityCooldownsRequireActualPressure()
        {
            Assert.IsTrue(BeastmasterHunterRotation.ShouldUseMisdirection(
                petAlive: true,
                targetIsTargetingPlayer: true,
                targetHealthPercent: 80,
                aggressorCount: 1));

            Assert.IsTrue(BeastmasterHunterRotation.ShouldUseMisdirection(
                petAlive: true,
                targetIsTargetingPlayer: false,
                targetHealthPercent: 80,
                aggressorCount: 2));

            Assert.IsFalse(BeastmasterHunterRotation.ShouldUseMisdirection(
                petAlive: false,
                targetIsTargetingPlayer: true,
                targetHealthPercent: 80,
                aggressorCount: 2));

            Assert.IsFalse(BeastmasterHunterRotation.ShouldUseMisdirection(
                petAlive: true,
                targetIsTargetingPlayer: true,
                targetHealthPercent: 20,
                aggressorCount: 2));

            Assert.IsTrue(BeastmasterHunterRotation.ShouldUseDefensiveCooldown(
                targetIsTargetingPlayer: true,
                playerHealthPercent: 29,
                playerHasDefensiveBuff: false));

            Assert.IsFalse(BeastmasterHunterRotation.ShouldUseDefensiveCooldown(
                targetIsTargetingPlayer: false,
                playerHealthPercent: 10,
                playerHasDefensiveBuff: false));

            Assert.IsFalse(BeastmasterHunterRotation.ShouldUseDefensiveCooldown(
                targetIsTargetingPlayer: true,
                playerHealthPercent: 29,
                playerHasDefensiveBuff: true));
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

        [TestMethod]
        public void RestConsumablesDoNotInterruptEachOther()
        {
            Assert.AreEqual(
                RestConsumable.None,
                RestState.SelectConsumable(
                    hasFood: true,
                    isEating: true,
                    healthPercent: 50,
                    hasDrink: true,
                    isDrinking: false,
                    manaPercent: 20));

            Assert.AreEqual(
                RestConsumable.None,
                RestState.SelectConsumable(
                    hasFood: true,
                    isEating: false,
                    healthPercent: 50,
                    hasDrink: true,
                    isDrinking: true,
                    manaPercent: 20));
        }

        [TestMethod]
        public void UnknownMendPetCannotBlockRestExit()
        {
            Assert.IsTrue(RestState.IsPetHealthOk(
                petExists: true,
                petHealthPercent: 50,
                playerKnowsMendPet: false));

            Assert.IsFalse(RestState.IsPetHealthOk(
                petExists: true,
                petHealthPercent: 50,
                playerKnowsMendPet: true));
        }

        [TestMethod]
        public void RestMendPetRequiresKnownReadyAffordableSpell()
        {
            Assert.IsFalse(RestState.ShouldCastMendPet(true, 50, false, false, true, 100, 50));
            Assert.IsFalse(RestState.ShouldCastMendPet(true, 50, false, true, false, 100, 50));
            Assert.IsFalse(RestState.ShouldCastMendPet(true, 50, false, true, true, 49, 50));
            Assert.IsTrue(RestState.ShouldCastMendPet(true, 50, false, true, true, 50, 50));
        }

        [TestMethod]
        public void PetManagerSelectsCallOrReviveWithoutLoopingOnUnknownSpells()
        {
            Assert.AreEqual(
                HunterPetManagerState.CallPet,
                HunterPetManagerState.SelectRecoverySpell(
                    petExists: false,
                    petAlive: false,
                    knowsCallPet: true,
                    knowsRevivePet: true));

            Assert.AreEqual(
                HunterPetManagerState.RevivePet,
                HunterPetManagerState.SelectRecoverySpell(
                    petExists: true,
                    petAlive: false,
                    knowsCallPet: true,
                    knowsRevivePet: true));

            Assert.IsNull(HunterPetManagerState.SelectRecoverySpell(
                petExists: true,
                petAlive: false,
                knowsCallPet: true,
                knowsRevivePet: false));
        }
    }
}
