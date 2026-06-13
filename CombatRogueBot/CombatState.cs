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

            TryUseAbility(AdrenalineRush, 0, CombatRogueRotation.ShouldAdrenalineRush(ObjectManager.Aggressors.Count(), player.HealthPercent));

            TryUseAbilityById(BloodFury, 3, 0, target.HealthPercent > 80);

            TryUseAbility(Evasion, 0, ObjectManager.Aggressors.Count() > 1);

            TryUseAbility(BladeFlurry, 25, ObjectManager.Aggressors.Count() > 1);

            TryUseAbility(SliceAndDice, 25, CombatRogueRotation.ShouldSliceAndDice(player.HasBuff(SliceAndDice), target.HealthPercent, player.ComboPoints));

            TryUseAbility(Riposte, 10, player.CanRiposte);

            TryUseAbility(Kick, 25, ReadyToInterrupt(target));

            TryUseAbility(Gouge, 45, ReadyToInterrupt(target) && !player.IsSpellReady(Kick));

            TryUseAbility(Eviscerate, 35, CombatRogueRotation.IsReadyToEviscerate(target.HealthPercent, player.ComboPoints));

            TryUseAbility(SinisterStrike, 45, player.ComboPoints < 5);
        }

        bool ReadyToInterrupt(WoWUnit target) => CombatRogueRotation.ReadyToInterrupt(target.Mana, target.IsCasting, target.IsChanneling);
    }
}
