using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

namespace ShadowPriestBot
{
    class CombatState : CombatStateBase, IBotState
    {
        const string WandLuaScript = "if IsAutoRepeatAction(12) == nil then CastSpellByName('Shoot') end";
        const string TurnOffWandLuaScript = "if IsAutoRepeatAction(12) ~= nil then CastSpellByName('Shoot') end";

        const string AbolishDisease = "Abolish Disease";
        const string CureDisease = "Cure Disease";
        const string DispelMagic = "Dispel Magic";
        const string InnerFire = "Inner Fire";
        const string LesserHeal = "Lesser Heal";
        const string MindBlast = "Mind Blast";
        const string MindFlay = "Mind Flay";
        const string PowerWordShield = "Power Word: Shield";
        const string PsychicScream = "Psychic Scream";
        const string ShadowForm = "Shadowform";
        const string ShadowWordPain = "Shadow Word: Pain";
        const string Smite = "Smite";
        const string VampiricEmbrace = "Vampiric Embrace";
        const string WeakenedSoul = "Weakened Soul";

        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly WoWUnit target;
        readonly LocalPlayer player;

        internal CombatState(
            Stack<IBotState> botStates,
            IDependencyContainer container,
            WoWUnit target,
            bool loot = true) : base(botStates, container, target, 30, loot)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
            this.target = target;
        }

        public new void Update()
        {
            if (player.HealthPercent < 30 && target.HealthPercent > 50 && player.Mana >= player.GetManaCost(LesserHeal))
            {
                botStates.Push(new HealSelfState(botStates, container));
                return;
            }

            if (base.Update())
                return;

            // Don't attempt spells without line of sight. Strafing is handled in CombatStateBase.
            if (TriggerLosRecovery())
                return;

            var hasWand = Inventory.GetEquippedItem(EquipSlot.Ranged) != null;
            if (ShadowPriestPowerlevelCombatRotation.ShouldUseWand(
                hasWand, player.IsCasting, player.IsChanneling, player.ManaPercent,
                target.CreatureType == CreatureType.Totem, target.HealthPercent))
            {
                player.LuaCall(WandLuaScript);
                return;
            }

            var distanceToTarget = target.Position.DistanceTo(player.Position);

            if (TryCastNoTargetRotationSpell(ShadowForm, !player.HasBuff(ShadowForm)))
                return;

            if (TryCastRotationSpell(VampiricEmbrace, 0, 29, ShadowPriestPowerlevelCombatRotation.ShouldVampiricEmbrace(
                player.HealthPercent, target.HasDebuff(VampiricEmbrace), target.HealthPercent)))
                return;

            if (TryCastRotationSpell(PsychicScream, 0, 7, ShadowPriestPowerlevelCombatRotation.ShouldPsychicScream(
                distanceToTarget, player.HasBuff(PowerWordShield), ObjectManager.Aggressors.Count(), target.CreatureType == CreatureType.Elemental)))
                return;

            if (TryCastRotationSpell(ShadowWordPain, 0, 29, ShadowPriestPowerlevelCombatRotation.ShouldShadowWordPain(
                target.HealthPercent, target.HasDebuff(ShadowWordPain))))
                return;

            if (TryCastRotationSpell(DispelMagic, 0, int.MaxValue, player.HasMagicDebuff, castOnSelf: true))
                return;

            var shouldCureDisease = ShadowPriestPowerlevelCombatRotation.ShouldCureDisease(player.IsDiseased, player.HasBuff(ShadowForm));
            if (player.KnowsSpell(AbolishDisease))
            {
                if (TryCastRotationSpell(AbolishDisease, 0, int.MaxValue, shouldCureDisease, castOnSelf: true))
                    return;
            }
            else if (TryCastRotationSpell(CureDisease, 0, int.MaxValue, shouldCureDisease, castOnSelf: true))
                return;

            if (TryCastNoTargetRotationSpell(InnerFire, !player.HasBuff(InnerFire)))
                return;

            if (TryCastRotationSpell(PowerWordShield, 0, int.MaxValue, ShadowPriestPowerlevelCombatRotation.ShouldPowerWordShield(
                player.HasDebuff(WeakenedSoul), player.HasBuff(PowerWordShield), target.HealthPercent, player.HealthPercent), castOnSelf: true))
                return;

            if (TryCastRotationSpell(MindBlast, 0, 29))
                return;

            if (ShadowPriestPowerlevelCombatRotation.ShouldMindFlay(
                player.KnowsSpell(MindFlay), distanceToTarget, player.KnowsSpell(PowerWordShield), player.HasBuff(PowerWordShield)))
            {
                if (TryCastRotationSpell(MindFlay, 0, 19))
                    return;
            }
            else if (TryCastRotationSpell(Smite, 0, 29, !player.HasBuff(ShadowForm)))
                return;
        }
    }
}
