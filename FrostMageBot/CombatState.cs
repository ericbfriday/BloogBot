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

            if (TryCastNoTargetRotationSpell(Evocation, FrostMageRotation.ShouldEvocate(player.HealthPercent, PlayerHasIceBarrier, player.ManaPercent, target.HealthPercent)))
                return;

            var wand = Inventory.GetEquippedItem(EquipSlot.Ranged);
            if (FrostMageRotation.ShouldUseWand(wand != null, player.ManaPercent, player.IsCasting, player.IsChanneling))
            {
                player.LuaCall(WandLuaScript);
                return;
            }

            var aggressorCount = ObjectManager.Aggressors.Count();
            var hasWaterElemental = ObjectManager.Units.Any(u => u.Name == "Water Elemental" && u.SummonedByGuid == player.Guid);
            var targetIsFrozen = IsTargetFrozen;
            var hasFingersOfFrost = player.HasBuff(FingersOfFrostBuff);
            var isHighValueTarget = target.CreatureRank != CreatureRank.Normal || target.Level > player.Level;
            var knowsFrostNova = player.KnowsSpell(FrostNova);
            var frostNovaReady = knowsFrostNova && player.IsSpellReady(FrostNova);
            var knowsIcyVeins = player.KnowsSpell(IcyVeins);
            var icyVeinsReady = knowsIcyVeins && player.IsSpellReady(IcyVeins);
            var knowsSummonWaterElemental = player.KnowsSpell(SummonWaterElemental);
            var summonWaterElementalReady = knowsSummonWaterElemental && player.IsSpellReady(SummonWaterElemental);

            if (TryCastNoTargetRotationSpell(
                SummonWaterElemental,
                FrostMageRotation.ShouldSummonWaterElemental(hasWaterElemental, target.HealthPercent, aggressorCount)))
                return;

            if (TryCastNoTargetRotationSpell(
                IcyVeins,
                FrostMageRotation.ShouldUseIcyVeins(aggressorCount, isHighValueTarget, target.HealthPercent)))
                return;

            var shouldUseColdSnap = FrostMageRotation.ShouldUseColdSnap(
                aggressorCount,
                knowsFrostNova,
                frostNovaReady,
                knowsIcyVeins,
                icyVeinsReady,
                knowsSummonWaterElemental,
                summonWaterElementalReady,
                target.HealthPercent,
                player.HealthPercent) &&
                (!player.HasBuff(IcyVeins) || (aggressorCount > 1 && knowsFrostNova && !frostNovaReady));
            if (TryCastNoTargetRotationSpell(ColdSnap, shouldUseColdSnap))
                return;

            var ward = FrostMageRotation.SelectWard(target.Name);
            if (ward != null && TryCastRotationSpell(ward, FrostMageRotation.ShouldUseWard(target.HealthPercent, player.HealthPercent), castOnSelf: true))
                return;

            if (TryCastRotationSpell(Counterspell, 0, 30, target.Mana > 0 && target.IsCasting))
                return;

            if (TryCastRotationSpell(
                IceBarrier,
                0,
                50,
                FrostMageRotation.ShouldIceBarrier(
                    PlayerHasIceBarrier,
                    aggressorCount,
                    frostNovaReady,
                    player.HealthPercent,
                    player.ManaPercent,
                    target.HealthPercent),
                castOnSelf: true))
                return;

            if (TryCastNoTargetRotationSpell(FrostNova, 0, 9, FrostMageRotation.ShouldFrostNova(
                target.TargetGuid == player.Guid,
                target.HealthPercent,
                player.HealthPercent,
                targetIsFrozen,
                ObjectManager.Units.Any(u => u.Guid != target.Guid && u.HealthPercent > 0 && u.Guid != player.Guid && u.Position.DistanceTo(player.Position) <= 12)), callback: FrostNovaCallback))
                return;

            var shouldUseShatterSpender = FrostMageRotation.ShouldUseShatterSpender(targetIsFrozen, hasFingersOfFrost);
            if (TryCastRotationSpell(DeepFreeze, 0, range, shouldUseShatterSpender))
                return;

            if (TryCastRotationSpell(IceLance, 0, range, shouldUseShatterSpender))
                return;

            if (TryCastNoTargetRotationSpell(ConeOfCold, 0, 8, target.HealthPercent > 20 && targetIsFrozen))
                return;

            if (TryCastRotationSpell(FrostfireBolt, 0, 40, player.HasBuff(BrainFreezeBuff)))
                return;

            if (TryCastRotationSpell(Fireball, 0, 35, player.HasBuff(BrainFreezeBuff)))
                return;

            if (TryCastRotationSpell(FireBlast, 0, 20, !targetIsFrozen && (player.IsMoving || target.HealthPercent <= 20)))
                return;

            TryCastRotationSpell(nuke, 0, range);
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
