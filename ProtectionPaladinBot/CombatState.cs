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

            TryCastSpell(DivinePlea, player.ManaPercent < 50);

            TryCastSpell(LayOnHands, ProtectionPaladinRotation.ShouldLayOnHands(player.Mana >= player.GetManaCost(HolyLight), player.HealthPercent), castOnSelf: true);

            TryCastSpell(Purify, player.IsPoisoned || player.IsDiseased, castOnSelf: true);

            TryCastSpell(RighteousFury, !player.HasBuff(RighteousFury));

            var aura = ProtectionPaladinRotation.SelectAura(
                player.KnowsSpell(RetributionAura),
                player.HasBuff(DevotionAura),
                player.HasBuff(RetributionAura));
            if (aura != null)
                TryCastSpell(aura);

            TryCastSpell(Exorcism, 0, 30, target.CreatureType == CreatureType.Undead || target.CreatureType == CreatureType.Demon);

            TryCastSpell(HammerOfJustice, 0, 10, (target.CreatureType != CreatureType.Humanoid || (target.CreatureType == CreatureType.Humanoid && target.HealthPercent < 20)));

            TryCastSpell(HammerOfTheRighteous, 0, 4);

            TryCastSpell(Consecration, ObjectManager.Aggressors.Count() > 1);

            // for judgements - in WotLK they reworked Paladins to have "Judgement of Light" and "Judgement of Wisdom" instead of "Judgement".
            // we may want different bot .dlls for each client?
            if (ClientHelper.ClientVersion == ClientVersion.WotLK)
            {
                var hasActiveSeal = player.Buffs.Any(b => b.Name.StartsWith("Seal of"));
                TryCastSpell(JudgementOfWisdom, 0, 10, ProtectionPaladinRotation.ShouldJudgementOfWisdom(target.HasDebuff(JudgementOfWisdom), hasActiveSeal));
                TryCastSpell(JudgementOfLight, 0, 10, ProtectionPaladinRotation.ShouldJudgementOfLight(target.HasDebuff(JudgementOfLight), hasActiveSeal, player.KnowsSpell(JudgementOfWisdom)));
            }
            else
            {
                TryCastSpell(Judgement, 0, 10, ProtectionPaladinRotation.ShouldUseLegacyJudgement(
                    player.HasBuff(SealOfTheCrusader),
                    player.HasBuff(SealOfRighteousness),
                    player.ManaPercent,
                    target.HealthPercent));
            }

            TryCastSpell(SealOfTheCrusader, !player.HasBuff(SealOfTheCrusader) && !target.HasDebuff(JudgementOfTheCrusader));

            TryCastSpell(SealOfRighteousness, ProtectionPaladinRotation.ShouldSealOfRighteousness(
                player.HasBuff(SealOfRighteousness),
                target.HasDebuff(JudgementOfTheCrusader),
                player.KnowsSpell(JudgementOfTheCrusader)));

            TryCastSpell(HolyShield, !player.HasBuff(HolyShield) && target.HealthPercent > 50);
        }
    }
}
