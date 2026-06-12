using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;

namespace BloodDeathKnightBot
{
    class MoveToTargetState : MoveToTargetStateBase, IBotState
    {
        const string DeathGrip = "Death Grip";

        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly WoWUnit target;
        readonly LocalPlayer player;
        readonly StuckHelper stuckHelper;

        internal MoveToTargetState(
            Stack<IBotState> botStates, IDependencyContainer container, WoWUnit target) :
            base(botStates, container, target)
        {
            this.botStates = botStates;
            this.container = container;
            this.target = target;
            player = ObjectManager.Player;
            stuckHelper = new StuckHelper(botStates, container);
        }

        public new void Update()
        {
            if (base.Update())
                return;

            if (player.IsInCombat)
            {
                EnterCombat();
                return;
            }

            stuckHelper.CheckIfStuck();

            var distanceToTarget = player.Position.DistanceTo(target.Position);
            if (CanUseDeathGrip(distanceToTarget))
            {
                player.StopAllMovement();
                player.CastSpell(DeathGrip, target.Guid);
                EnterCombat();
                return;
            }

            if (distanceToTarget < 3)
            {
                EnterCombat();
                return;
            }

            var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
            player.MoveToward(nextWaypoint);
        }

        bool CanUseDeathGrip(float distanceToTarget)
        {
            return ClientHelper.ClientVersion == ClientVersion.WotLK
                && player.Class == Class.DeathKnight
                && distanceToTarget > 8
                && distanceToTarget <= 30
                && player.InLosWith(target.Position)
                && player.IsDeathKnightAbilityUsable(DeathGrip);
        }

        void EnterCombat()
        {
            player.StopAllMovement();
            botStates.Pop();
            botStates.Push(new CombatState(botStates, container, target));
        }
    }
}
