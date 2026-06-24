using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

namespace CombatRogueBot
{
    class CombatState : CombatStateBase, IBotState
    {
        const string AdrenalineRush = "Adrenaline Rush";
        const string BladeFlurry = "Blade Flurry";
        const string Evasion = "Evasion";
        const string Eviscerate = "Eviscerate";
        const string Gouge = "Gouge";
        const string BloodFury = "Blood Fury";
        const string Kick = "Kick";
        const string Riposte = "Riposte";
        const string SinisterStrike = "Sinister Strike";
        const string SliceAndDice = "Slice and Dice";

        readonly LocalPlayer player;
        readonly WoWUnit target;

        internal CombatState(
            Stack<IBotState> botStates,
            IDependencyContainer container,
            WoWUnit target,
            bool loot = true) : base(botStates, container, target, 3, loot)
        {
            player = ObjectManager.Player;
            this.target = target;
        }

        public new void Update()
        {
            if (base.Update())
                return;

            if (TryUseRotationAbility(AdrenalineRush, 0, condition: CombatRogueRotation.ShouldAdrenalineRush(ObjectManager.Aggressors.Count(), player.HealthPercent)))
                return;

            if (TryUseRotationAbilityById(BloodFury, 3, 0, CombatRogueRotation.ShouldBloodFury(target.HealthPercent)))
                return;

            if (TryUseRotationAbility(Evasion, 0, condition: CombatRogueRotation.ShouldEvasion(ObjectManager.Aggressors.Count())))
                return;

            if (TryUseRotationAbility(BladeFlurry, 25, condition: CombatRogueRotation.ShouldBladeFlurry(ObjectManager.Aggressors.Count())))
                return;

            if (TryUseRotationAbility(SliceAndDice, 25, condition: CombatRogueRotation.ShouldSliceAndDice(player.HasBuff(SliceAndDice), target.HealthPercent, player.ComboPoints)))
                return;

            if (TryUseRotationAbility(Riposte, 10, condition: player.CanRiposte))
                return;

            if (TryUseRotationAbility(Kick, 25, condition: CombatRogueRotation.ShouldKickInterrupt(target.Mana, target.IsCasting, target.IsChanneling)))
                return;

            if (TryUseRotationAbility(Gouge, 45, condition: CombatRogueRotation.ShouldGougeInterrupt(target.Mana, target.IsCasting, target.IsChanneling, player.IsSpellReady(Kick))))
                return;

            if (TryUseRotationAbility(Eviscerate, 35, condition: CombatRogueRotation.IsReadyToEviscerate(target.HealthPercent, player.ComboPoints)))
                return;

            if (TryUseRotationAbility(SinisterStrike, 45, condition: CombatRogueRotation.ShouldSinisterStrike(player.ComboPoints)))
                return;
        }
    }
}
