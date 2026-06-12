using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

namespace BloodDeathKnightBot
{
    class CombatState : CombatStateBase, IBotState
    {
        const string BloodBoil = "Blood Boil";
        const string BloodPlague = "Blood Plague";
        const string BloodStrike = "Blood Strike";
        const string DeathCoil = "Death Coil";
        const string DeathStrike = "Death Strike";
        const string FrostFever = "Frost Fever";
        const string HeartStrike = "Heart Strike";
        const string IceboundFortitude = "Icebound Fortitude";
        const string IcyTouch = "Icy Touch";
        const string MindFreeze = "Mind Freeze";
        const string Pestilence = "Pestilence";
        const string PlagueStrike = "Plague Strike";
        const string RuneTap = "Rune Tap";

        readonly Stack<IBotState> botStates;
        readonly LocalPlayer player;
        readonly WoWUnit target;

        internal CombatState(
            Stack<IBotState> botStates,
            IDependencyContainer container,
            WoWUnit target,
            bool loot = true) : base(botStates, container, target, 3, loot)
        {
            this.botStates = botStates;
            player = ObjectManager.Player;
            this.target = target;
        }

        public new void Update()
        {
            if (!CanRunDeathKnightCombat)
            {
                player.StopAllMovement();
                player.LuaCall("message('BloodDeathKnightBot requires a WotLK Death Knight.')");
                botStates.Pop();
                return;
            }

            if (base.Update())
                return;

            var acted = false;
            var aggressorCount = ObjectManager.Aggressors.Count();
            var hasFrostFever = target.HasDebuff(FrostFever);
            var hasBloodPlague = target.HasDebuff(BloodPlague);

            TryUseDeathKnightAbility(MindFreeze, condition: ReadyToInterrupt, callback: () => acted = true);
            if (acted) return;

            TryUseDeathKnightAbility(RuneTap, condition: player.HealthPercent < 45, callback: () => acted = true);
            if (acted) return;

            TryUseDeathKnightAbility(IceboundFortitude, condition: player.HealthPercent < 35, callback: () => acted = true);
            if (acted) return;

            TryUseDeathKnightAbility(IcyTouch, condition: !hasFrostFever, callback: () => acted = true);
            if (acted) return;

            TryUseDeathKnightAbility(PlagueStrike, condition: !hasBloodPlague, callback: () => acted = true);
            if (acted) return;

            TryUseDeathKnightAbility(Pestilence, condition: aggressorCount >= 2 && hasFrostFever && hasBloodPlague, callback: () => acted = true);
            if (acted) return;

            TryUseDeathKnightAbility(DeathStrike, condition: player.HealthPercent < 70, callback: () => acted = true);
            if (acted) return;

            TryUseDeathKnightAbility(BloodBoil, condition: aggressorCount >= 3, callback: () => acted = true);
            if (acted) return;

            TryUseDeathKnightAbility(HeartStrike, condition: player.KnowsSpell(HeartStrike), callback: () => acted = true);
            if (acted) return;

            TryUseDeathKnightAbility(BloodStrike, condition: !player.KnowsSpell(HeartStrike), callback: () => acted = true);
            if (acted) return;

            TryUseDeathKnightAbility(DeathCoil, condition: player.RunicPower >= 80);
        }

        bool CanRunDeathKnightCombat =>
            ClientHelper.ClientVersion == ClientVersion.WotLK &&
            player.Class == Class.DeathKnight;

        bool ReadyToInterrupt => target.IsCasting || target.IsChanneling;
    }
}
