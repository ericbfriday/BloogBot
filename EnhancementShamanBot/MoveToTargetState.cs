using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Objects;
using System.Collections.Generic;

namespace EnhancementShamanBot
{
    class MoveToTargetState : MoveToTargetStateBase, IBotState
    {
        const string LightningBolt = "Lightning Bolt";
        const string TotemicRecall = "Totemic Recall";

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
            if (player.IsCasting)
                return;

            // Recall stale totems before committing to movement so mana is refunded.
            // Instant cast; clears anchor so this only fires once per movement phase.
            if (ShamanTotemTracker.AreStale(player.Position)
                && player.KnowsSpell(TotemicRecall)
                && player.IsSpellReady(TotemicRecall)
                && player.Mana >= player.GetManaCost(TotemicRecall))
            {
                player.LuaCall($"CastSpellByName('{TotemicRecall}')");
                ShamanTotemTracker.ClearAnchor();
                return;
            }

            if (base.Update())
            {
                return;
            }

            stuckHelper.CheckIfStuck();

            if (player.TargetGuid != target.Guid)
                player.SetTarget(target.Guid);

            if (player.Position.DistanceTo(target.Position) < 27 && player.InLosWith(target.Position))
            {
                if (player.IsMoving)
                    player.StopAllMovement();

                if (Wait.For("PullWithLightningBoltDelay", 250))
                {
                    if (!player.IsInCombat &&
                        player.KnowsSpell(LightningBolt) &&
                        player.IsSpellReady(LightningBolt) &&
                        player.Mana >= player.GetManaCost(LightningBolt))
                    {
                        player.LuaCall($"CastSpellByName('{LightningBolt}')");
                    }

                    botStates.Pop();
                    botStates.Push(new CombatState(botStates, container, target));
                    return;
                }
            }
            else
            {
                var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
                player.MoveToward(nextWaypoint);
            }
        }
    }
}
