using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace FrostMageBot
{
    class CombatState : CombatStateBase, IBotState
    {
        const string WandLuaScript = "if IsAutoRepeatAction(11) == nil then CastSpellByName('Shoot') end";

        const string ColdSnap = "Cold Snap";
        const string ConeOfCold = "Cone of Cold";
        const string Counterspell = "Counterspell";
        const string Evocation = "Evocation";
        const string Fireball = FrostMageRotation.Fireball;
        const string FireBlast = "Fire Blast";
        const string FrostNova = "Frost Nova";
        const string Frostbite = "Frostbite";
        const string Frostbolt = FrostMageRotation.Frostbolt;
        const string IceBarrier = "Ice Barrier";
        const string IcyVeins = "Icy Veins";
        const string SummonWaterElemental = "Summon Water Elemental";
        const string BrainFreezeBuff = "Fireball!";
        const string FrostfireBolt = "Frostfire Bolt";
        const string DeepFreeze = "Deep Freeze";
        const string FingersOfFrostBuff = "Fingers of Frost";
        const string IceLance = "Ice Lance";
        const string ShatteredBarrier = "Shattered Barrier";

        readonly LocalPlayer player;
        readonly WoWUnit target;
        readonly string nuke;
        readonly int range;
        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;

        bool frostNovaBackpedaling;
        int frostNovaBackpedalStartTime;
        bool frostNovaJumped;
        bool frostNovaStartedMoving;
        bool unstucking;

        int combatStateStartTime;

        internal CombatState(
            Stack<IBotState> botStates,
            IDependencyContainer container,
            WoWUnit target,
            bool loot = true) :
            base(
                botStates,
                container,
                target,
                desiredRange: 29 + (ObjectManager.GetTalentRank(3, 11) * 3),
                loot)
        {
            player = ObjectManager.Player;
            this.target = target;
            this.botStates = botStates;
            this.container = container;

            nuke = FrostMageRotation.SelectNuke(player.KnowsSpell(Frostbolt), player.Level);

            range = FrostMageRotation.CalculateNukeRange(ObjectManager.GetTalentRank(3, 11));

            combatStateStartTime = Environment.TickCount;
        }

        public new void Update()
        {
            if (frostNovaBackpedaling && !frostNovaStartedMoving && Environment.TickCount - frostNovaBackpedalStartTime > 200)
            {
                player.Turn180();
                player.StartMovement(ControlBits.Front);
                frostNovaStartedMoving = true;
            }
            if (frostNovaBackpedaling && !frostNovaJumped && Environment.TickCount - frostNovaBackpedalStartTime > 500)
            {
                player.Jump();
                frostNovaJumped = true;
            }
            if (frostNovaBackpedaling && Environment.TickCount - frostNovaBackpedalStartTime > 2500)
            {
                player.StopMovement(ControlBits.Front);
                player.Face(target.Position);
                frostNovaBackpedaling = false;
            }

            if (frostNovaBackpedaling)
            {
                TryCastSpell(FrostNova); // sometimes we try to cast too early and get into this state while FrostNova is still ready.
                return;
            }

            if (base.Update())
                return;

            if (unstucking)
            {
                // Move towards the target.
                var nextWaypoint = Navigation.GetNextWaypoint(
                    ObjectManager.MapId, player.Position, target.Position, false);
                player.MoveToward(nextWaypoint);

                // Once we've dealt any damage, we are no longer stuck.
                if (target.HealthPercent < 100)
                {
                    player.StopAllMovement();
                    unstucking = false;
                }

                // If we get any threat while moving, just fight that enemy instead.
                var threat = container.FindThreat();
                if (threat != null)
                {
                    botStates.Pop();
                    botStates.Push(container.CreateMoveToTargetState(botStates, container, threat));
                    return;
                }

                // No return here. We keep trying to cast spells. Although only instant spells will
                // succeed.
            }

            // If we haven't dealt any damage to the target for 30 seconds, we're probably stuck.
            if (Environment.TickCount - combatStateStartTime > 30 * 1000 && target.HealthPercent >= 99)
            {
                unstucking = true;
                botStates.Push(new StuckState(botStates, container));

                // Reset the timer so we don't keep trying to unstuck.
                combatStateStartTime = Environment.TickCount;

                return;
            }

            // Don't attempt spells without line of sight. Strafing is handled in CombatStateBase.
            // This also applies during unstucking — movement still happens above, but spell casts
            // must not fire without LOS or they will spam indefinitely.
            if (TriggerLosRecovery())
                return;

            TryCastSpell(Evocation, 0, int.MaxValue, FrostMageRotation.ShouldEvocate(player.HealthPercent, PlayerHasIceBarrier, player.ManaPercent, target.HealthPercent));

            var wand = Inventory.GetEquippedItem(EquipSlot.Ranged);
            if (FrostMageRotation.ShouldUseWand(wand != null, player.ManaPercent, player.IsCasting, player.IsChanneling))
                player.LuaCall(WandLuaScript);
            else
            {
                TryCastSpell(SummonWaterElemental, !ObjectManager.Units.Any(u => u.Name == "Water Elemental" && u.SummonedByGuid == player.Guid));

                TryCastSpell(ColdSnap, !player.IsSpellReady(SummonWaterElemental));

                TryCastSpell(IcyVeins, ObjectManager.Aggressors.Count() > 1);

                var ward = FrostMageRotation.SelectWard(target.Name);
                if (ward != null)
                    TryCastSpell(ward, 0, int.MaxValue, FrostMageRotation.ShouldUseWard(target.HealthPercent, player.HealthPercent));

                TryCastSpell(Counterspell, 0, 30, target.Mana > 0 && target.IsCasting);

                TryCastSpell(IceBarrier, 0, 50, FrostMageRotation.ShouldIceBarrier(
                    PlayerHasIceBarrier,
                    ObjectManager.Aggressors.Count(),
                    player.IsSpellReady(FrostNova),
                    player.HealthPercent,
                    player.ManaPercent,
                    target.HealthPercent));

                TryCastSpell(FrostNova, 0, 9, FrostMageRotation.ShouldFrostNova(
                    target.TargetGuid == player.Guid,
                    target.HealthPercent,
                    player.HealthPercent,
                    IsTargetFrozen,
                    ObjectManager.Units.Any(u => u.Guid != target.Guid && u.HealthPercent > 0 && u.Guid != player.Guid && u.Position.DistanceTo(player.Position) <= 12)), callback: FrostNovaCallback);

                TryCastSpell(DeepFreeze, 0, range, IsTargetFrozen || player.HasBuff(FingersOfFrostBuff));

                TryCastSpell(IceLance, 0, range, IsTargetFrozen || player.HasBuff(FingersOfFrostBuff));

                TryCastSpell(ConeOfCold, 0, 8, player.Level >= 30 && target.HealthPercent > 20 && IsTargetFrozen);

                TryCastSpell(FireBlast, 0, 20, !IsTargetFrozen);

                TryCastSpell(FrostfireBolt, 0, 40, player.HasBuff(BrainFreezeBuff));

                TryCastSpell(Fireball, 0, 35, player.HasBuff(BrainFreezeBuff));

                // Either Frostbolt or Fireball depending on what is stronger. Will always use Frostbolt at level 8+.
                TryCastSpell(nuke, 0, range);
            }
        }

        Action FrostNovaCallback => () =>
        {
            frostNovaStartedMoving = false;
            frostNovaJumped = false;
            frostNovaBackpedaling = true;
            frostNovaBackpedalStartTime = Environment.TickCount;
        };

        // Sometimes frostbite and frostnova are considered buffs.
        bool IsTargetFrozen => target.HasDebuff(Frostbite) ||
            target.HasBuff(Frostbite) ||
            target.HasDebuff(FrostNova) ||
            target.HasBuff(FrostNova) ||
            target.HasBuff(DeepFreeze) ||
            target.HasBuff(ShatteredBarrier);

        // Sometimes ice barrier is considered a debuff.
        bool PlayerHasIceBarrier => player.HasBuff(IceBarrier) || player.HasDebuff(IceBarrier);
    }
}
