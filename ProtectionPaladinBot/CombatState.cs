using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

namespace ProtectionPaladinBot
{
    class CombatState : CombatStateBase, IBotState
    {
        const string Consecration = "Consecration";
        const string DevotionAura = "Devotion Aura";
        const string Exorcism = "Exorcism";
        const string HammerOfJustice = "Hammer of Justice";
        const string HolyLight = "Holy Light";
        const string HolyShield = "Holy Shield";
        const string Judgement = "Judgement";
        const string JudgementOfLight = "Judgement of Light";
        const string JudgementOfWisdom = "Judgement of Wisdom";
        const string JudgementOfTheCrusader = "Judgement of the Crusader";
        const string LayOnHands = "Lay on Hands";
        const string Purify = "Purify";
        const string RetributionAura = "Retribution Aura";
        const string RighteousFury = "Righteous Fury";
        const string SealOfRighteousness = "Seal of Righteousness";
        const string SealOfTheCrusader = "Seal of the Crusader";
        const string AvengersShield = "Avenger's Shield";
        const string HammerOfTheRighteous = "Hammer of the Righteous";
        const string DivinePlea = "Divine Plea";

        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly LocalPlayer player;
        readonly WoWUnit target;

        internal CombatState(
            Stack<IBotState> botStates,
            IDependencyContainer container,
            WoWUnit target,
            bool loot = true) : base(botStates, container, target, 4, loot)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
            this.target = target;
        }

        public new void Update()
        {
            if (player.HealthPercent < 30 && target.HealthPercent > 50 && player.Mana >= player.GetManaCost(HolyLight))
            {
                botStates.Push(new HealSelfState(botStates, container));
                return;
            }

            if (base.Update())
                return;

            // Melee hybrid — no proactive LOS gate; the base closes the distance.

            if (TryCastNoTargetRotationSpell(DivinePlea, ProtectionPaladinRotation.ShouldDivinePlea(player.ManaPercent)))
                return;

            if (TryCastRotationSpell(LayOnHands, ProtectionPaladinRotation.ShouldLayOnHands(player.Mana >= player.GetManaCost(HolyLight), player.HealthPercent), castOnSelf: true))
                return;

            if (TryCastRotationSpell(Purify, ProtectionPaladinRotation.ShouldPurify(player.IsPoisoned, player.IsDiseased), castOnSelf: true))
                return;

            if (TryCastNoTargetRotationSpell(RighteousFury, !player.HasBuff(RighteousFury)))
                return;

            var aura = ProtectionPaladinRotation.SelectAura(
                player.KnowsSpell(RetributionAura),
                player.HasBuff(DevotionAura),
                player.HasBuff(RetributionAura));
            if (aura != null && TryCastNoTargetRotationSpell(aura))
                return;

            if (TryCastRotationSpell(Exorcism, 0, 30, ProtectionPaladinRotation.ShouldExorcism(
                target.CreatureType == CreatureType.Undead || target.CreatureType == CreatureType.Demon)))
                return;

            if (TryCastRotationSpell(HammerOfJustice, 0, 10, ProtectionPaladinRotation.ShouldHammerOfJustice(
                target.CreatureType == CreatureType.Humanoid, target.HealthPercent)))
                return;

            if (TryCastRotationSpell(HammerOfTheRighteous, 0, 4))
                return;

            if (TryCastNoTargetRotationSpell(Consecration, ProtectionPaladinRotation.ShouldConsecration(ObjectManager.Aggressors.Count())))
                return;

            // for judgements - in WotLK they reworked Paladins to have "Judgement of Light" and "Judgement of Wisdom" instead of "Judgement".
            // we may want different bot .dlls for each client?
            if (ClientHelper.ClientVersion == ClientVersion.WotLK)
            {
                var hasActiveSeal = player.Buffs.Any(b => b.Name.StartsWith("Seal of"));
                if (TryCastRotationSpell(JudgementOfWisdom, 0, 10, ProtectionPaladinRotation.ShouldJudgementOfWisdom(target.HasDebuff(JudgementOfWisdom), hasActiveSeal)))
                    return;
                if (TryCastRotationSpell(JudgementOfLight, 0, 10, ProtectionPaladinRotation.ShouldJudgementOfLight(target.HasDebuff(JudgementOfLight), hasActiveSeal, player.KnowsSpell(JudgementOfWisdom))))
                    return;
            }
            else if (TryCastRotationSpell(Judgement, 0, 10, ProtectionPaladinRotation.ShouldUseLegacyJudgement(
                player.HasBuff(SealOfTheCrusader),
                player.HasBuff(SealOfRighteousness),
                player.ManaPercent,
                target.HealthPercent)))
                return;

            if (TryCastNoTargetRotationSpell(SealOfTheCrusader, ProtectionPaladinRotation.ShouldSealOfTheCrusader(
                player.HasBuff(SealOfTheCrusader), target.HasDebuff(JudgementOfTheCrusader))))
                return;

            if (TryCastNoTargetRotationSpell(SealOfRighteousness, ProtectionPaladinRotation.ShouldSealOfRighteousness(
                player.HasBuff(SealOfRighteousness),
                target.HasDebuff(JudgementOfTheCrusader),
                player.KnowsSpell(JudgementOfTheCrusader))))
                return;

            if (TryCastNoTargetRotationSpell(HolyShield, ProtectionPaladinRotation.ShouldHolyShield(
                player.HasBuff(HolyShield), target.HealthPercent)))
                return;
        }
    }
}
