using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

namespace RetributionPaladinBot
{
    class CombatState : CombatStateBase, IBotState
    {
        const string DevotionAura = "Devotion Aura";
        const string Exorcism = "Exorcism";
        const string HammerOfJustice = "Hammer of Justice";
        const string HolyLight = "Holy Light";
        const string HolyShield = "Holy Shield";
        const string Judgement = "Judgement";
        const string JudgementOfLight = "Judgement of Light";
        const string JudgementOfWisdom = "Judgement of Wisdom";
        const string JudgementOfTheCrusader = "Judgement of the Crusader";
        const string Purify = "Purify";
        const string RetributionAura = "Retribution Aura";
        const string SanctityAura = "Sanctity Aura";
        const string SealOfCommand = "Seal of Command";
        const string SealOfRighteousness = "Seal of Righteousness";
        const string SealOfTheCrusader = "Seal of the Crusader";

        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly LocalPlayer player;
        readonly WoWUnit target;

        internal CombatState(
            Stack<IBotState> botStates,
            IDependencyContainer container,
            WoWUnit target,
            bool loot = true) : base(botStates, container, target, 3, loot)
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

            TryCastSpell(Purify, player.IsPoisoned || player.IsDiseased, castOnSelf: true);

            var aura = RetributionPaladinCombatRotation.SelectAura(
                player.KnowsSpell(DevotionAura),
                player.HasBuff(DevotionAura),
                player.KnowsSpell(RetributionAura),
                player.HasBuff(RetributionAura),
                player.KnowsSpell(SanctityAura),
                player.HasBuff(SanctityAura));
            if (aura != null)
                TryCastSpell(aura);

            TryCastSpell(Exorcism, target.CreatureType == CreatureType.Undead || target.CreatureType == CreatureType.Demon);

            TryCastSpell(HammerOfJustice, target.CreatureType != CreatureType.Humanoid || (target.CreatureType == CreatureType.Humanoid && target.HealthPercent < 20));

            TryCastSpell(SealOfTheCrusader, !player.HasBuff(SealOfTheCrusader) && !target.HasDebuff(JudgementOfTheCrusader));

            TryCastSpell(SealOfRighteousness, RetributionPaladinCombatRotation.ShouldUseSealOfRighteousness(
                player.HasBuff(SealOfRighteousness),
                target.HasDebuff(JudgementOfTheCrusader),
                player.KnowsSpell(SealOfCommand),
                player.KnowsSpell(JudgementOfTheCrusader)));

            TryCastSpell(SealOfCommand, !player.HasBuff(SealOfCommand) && target.HasDebuff(JudgementOfTheCrusader));

            TryCastSpell(HolyShield, !player.HasBuff(HolyShield) && target.HealthPercent > 50);

            if (ClientHelper.ClientVersion == ClientVersion.WotLK)
            {
                var judgement = RetributionPaladinCombatRotation.SelectWotlkJudgement(
                    player.KnowsSpell(JudgementOfWisdom),
                    target.HasDebuff(JudgementOfWisdom),
                    target.HasDebuff(JudgementOfLight),
                    player.Buffs.Any(b => b.Name.StartsWith("Seal of")));
                if (judgement != null)
                    TryCastSpell(judgement, 0, 10);
            }
            else
            {
                TryCastSpell(Judgement, RetributionPaladinCombatRotation.ShouldUseLegacyJudgement(
                    player.HasBuff(SealOfTheCrusader),
                    player.HasBuff(SealOfRighteousness),
                    player.HasBuff(SealOfCommand),
                    player.ManaPercent,
                    target.HealthPercent));
            }
        }
    }
}
