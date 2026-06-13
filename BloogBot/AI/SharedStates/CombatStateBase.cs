using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using BloogBot.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BloogBot.AI.SharedStates
{
    public abstract class CombatStateBase
    {
        const string FacingErrorMessage = "You are facing the wrong way!";
        const string LosErrorMessage = "Target not in line of sight";
        const string BattleStance = "Battle Stance";

        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly int desiredRange;
        readonly LocalPlayer player;
        readonly WoWUnit target;
        readonly bool loot;

        bool backpedaling;
        int backpedalStartTime;
        bool noLos;
        int noLosStartTime;
        int losFirstFailTime;
        const int LosStrafe1Ms = 2000;
        const int LosTimeoutMs = 4000;
        const int LosAbandonMs = 15000;

        int combatStateStartTime;

        public static bool ShouldSuppressMeleeAutoAttack(ItemSubclass? rangedItemSubclass) =>
            rangedItemSubclass == ItemSubclass.Wand;

        public CombatStateBase(
            Stack<IBotState> botStates,
            IDependencyContainer container,
            WoWUnit target,
            int desiredRange,
            bool loot = true)
        {
            player = ObjectManager.Player;
            this.target = target;
            player.Target = target;

            this.botStates = botStates;
            this.container = container;
            this.desiredRange = desiredRange;
            this.loot = loot;

            combatStateStartTime = Environment.TickCount;

            WoWEventHandler.OnErrorMessage += OnErrorMessageCallback;
        }

        public bool Update()
        {
            // melee classes occasionally end up in a weird state where they are too close to hit the mob,
            // so we backpedal a bit to correct the position
            if (backpedaling && Environment.TickCount - backpedalStartTime > 500)
            {
                player.StopMovement(ControlBits.Back);
                backpedaling = false;
            }
            if (backpedaling)
                return true;

            // When LOS is blocked, strafe laterally (left then right) instead of walking into the
            // obstacle. Works even when navigation has no mmap/tile data for the current map.
            if (noLos)
            {
                // Abandon target if LOS has been broken long enough that the mob is unreachable.
                if (losFirstFailTime != 0 && Environment.TickCount - losFirstFailTime > LosAbandonMs)
                {
                    container.Probe.BlacklistedMobIds.Add(target.Guid);
                    if (container.BotSettings.PermanentlyBlacklistUnreachableTargets)
                        Repository.AddBlacklistedMob(target.Guid);
                    CleanUp();
                    return true;
                }

                var losRecovered = player.InLosWith(target.Position);
                if (losRecovered || Environment.TickCount - noLosStartTime > LosTimeoutMs)
                {
                    player.StopMovement(ControlBits.StrafeLeft);
                    player.StopMovement(ControlBits.StrafeRight);
                    noLos = false;
                    if (losRecovered)
                        losFirstFailTime = 0;
                }
                else
                {
                    player.Face(target.Position);
                    player.StopMovement(ControlBits.Front);
                    var elapsed = Environment.TickCount - noLosStartTime;
                    if (elapsed < LosStrafe1Ms)
                    {
                        player.StopMovement(ControlBits.StrafeRight);
                        player.StartMovement(ControlBits.StrafeLeft);
                    }
                    else
                    {
                        player.StopMovement(ControlBits.StrafeLeft);
                        player.StartMovement(ControlBits.StrafeRight);
                    }
                    return true;
                }
            }

            // If we haven't dealt any damage to the target for 30 seconds, we're probably stuck.
            if (Environment.TickCount - combatStateStartTime > 30 * 1000 && target.HealthPercent >= 99)
            {
                // Add the target to the blacklist and stop fighting it.
                container.Probe.BlacklistedMobIds.Add(target.Guid);
                if (container.BotSettings.PermanentlyBlacklistUnreachableTargets)
                {
                    Repository.AddBlacklistedMob(target.Guid);
                }

                botStates.Pop();
                return true;
            }

            // see if somebody else stole the mob we were targeting
            if (target.TappedByOther)
            {
                CleanUp();
                return true;
            }

            // when killing certain summoned units (like totems), our local reference to target will still have 100% health even after the totem is destroyed
            // so we need to lookup the target again in the object manager, and if it's null, we can assume it's dead and leave combat.
            var checkTarget = ObjectManager.Units.FirstOrDefault(u => u.Guid == target.Guid);
            if (target.Health == 0 || target.TappedByOther || checkTarget == null)
            {
                player.StopAllMovement();

                if (ObjectManager.Aggressors.Count() == 0 && player.Class == Class.Warrior && player.CurrentStance != BattleStance) TryUseAbility(BattleStance);

                if (Wait.For("PopCombatState", 1500))
                {
                    CleanUp();
                    if (loot)
                    {
                        botStates.Push(new LootState(botStates, container, target));
                    }
                }

                var threat = container.FindThreat();

                if (threat != null)
                {
                    // We also need to do the same check against the threat we found.
                    var checkThreat = ObjectManager.Units.FirstOrDefault(u => u.Guid == threat.Guid);
                    if (threat.Health == 0 || threat.TappedByOther || checkThreat == null)
                    {
                        return true;
                    }

                    botStates.Push(container.CreateCombatState(botStates, container, threat, loot));
                }

                return true;
            }

            // ensure the correct target is set
            if (player.TargetGuid != target.Guid)
                player.SetTarget(target.Guid);

            // ensure we're facing the target
            if (!player.IsFacing(target.Position))
                player.Face(target.Position);

            // make sure casters don't move or anything while they're casting by returning here
            if ((player.IsCasting || player.IsChanneling) && player.Class != Class.Warrior)
                return true;

            // ensure we're in range of the target
            if (player.Position.DistanceTo(target.Position) > desiredRange)
            {
                var nextWaypoint = Navigation.GetNextWaypoint(ObjectManager.MapId, player.Position, target.Position, false);
                player.MoveToward(nextWaypoint);
            }
            else if (player.IsMoving && player.Position.DistanceTo(target.Position) < desiredRange - 1)
                player.StopAllMovement();

            // Only true wand users should suppress melee auto-attack; relics share the ranged slot.
            var rangedItem = Inventory.GetEquippedItem(EquipSlot.Ranged);
            if (!ShouldSuppressMeleeAutoAttack(rangedItem?.Info?.ItemSubclass))
            {
                if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                {
                    var autoAttackAction = player.Class == Class.Warrior ? 84 : 12;

                    // first check if auto attack is in the correct action slot
                    var isInCorrectSpot = player.LuaCallWithResults($"{{0}} = IsAttackAction({autoAttackAction})");
                    if (isInCorrectSpot.Length == 0 || isInCorrectSpot[0] != "1")
                    {
                        var error = "You must place the <Attack> action from your spellbook on the last slot on your primary action bar.";
                        player.LuaCall($"message('{error}')");
                        return false;
                    }

                    var autoAttackLuaScript = $"if IsCurrentAction('{autoAttackAction}') == nil then CastSpellByName('Attack') end";
                    player.LuaCall(autoAttackLuaScript);
                }
                else
                {
                    player.LuaCall("StartAttack()");
                }
            }

            return false;
        }

        public void TryCastSpell(string name, int minRange, int maxRange, bool condition = true, Action callback = null, bool castOnSelf = false) =>
            TryCastSpellInternal(name, minRange, maxRange, condition, callback, castOnSelf);

        public void TryCastSpell(string name, bool condition = true, Action callback = null, bool castOnSelf = false) =>
            TryCastSpellInternal(name, 0, int.MaxValue, condition, callback, castOnSelf);

        void TryCastSpellInternal(string name, int minRange, int maxRange, bool condition = true, Action callback = null, bool castOnSelf = false)
        {
            var distanceToTarget = player.Position.DistanceTo(target.Position);

            if (player.IsSpellReady(name) && player.Mana >= player.GetManaCost(name) && distanceToTarget >= minRange && distanceToTarget <= maxRange && condition && !player.IsStunned && ((!player.IsCasting && !player.IsChanneling) || player.Class == Class.Warrior))
            {
                if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                {
                    var castOnSelfString = castOnSelf ? ",1" : "";
                    player.LuaCall($"CastSpellByName(\"{name}\"{castOnSelfString})");
                    callback?.Invoke();
                }
                else
                {
                    var targetGuid = castOnSelf ? player.Guid : target.Guid;
                    player.CastSpell(name, targetGuid);
                    callback?.Invoke();
                }
            }
        }

        // shared by 
        public void TryUseAbility(string name, int requiredResource = 0, bool condition = true, Action callback = null)
        {
            int playerResource = 0;

            if (player.Class == Class.Warrior)
                playerResource = player.Rage;
            else if (player.Class == Class.Rogue)
                playerResource = player.Energy;
            // todo: feral druids (bear/cat form)

            if (player.IsSpellReady(name) && playerResource >= requiredResource && condition && !player.IsStunned && !player.IsCasting)
            {
                if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                {
                    player.LuaCall($"CastSpellByName('{name}')");
                    callback?.Invoke();
                }
                else
                {
                    player.CastSpell(name, target.Guid);
                    callback?.Invoke();
                }
            }
        }

        // https://vanilla-wow.fandom.com/wiki/API_CastSpell
        // The id is counted from 1 through all spell types (tabs on the right side of SpellBookFrame).
        public void TryUseAbilityById(string name, int id, int requiredRage = 0, bool condition = true, Action callback = null)
        {
            if (player.IsSpellReady(name) && player.Rage >= requiredRage && condition && !player.IsStunned && !player.IsCasting)
            {
                if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                {
                    player.LuaCall($"CastSpell({id}, 'spell')");
                    callback?.Invoke();
                }
                else
                {
                    player.CastSpell(name, target.Guid);
                    callback?.Invoke();
                }
            }
        }

        // Rotation casting helpers: like TryCastSpell, but they also require the spell to be known
        // and report whether the cast was attempted, so rotations can be written as
        // "if (TryCastRotationSpell(...)) return;" priority chains.
        protected bool TryCastRotationSpell(
            string name,
            bool condition = true,
            Action callback = null,
            bool castOnSelf = false) =>
            TryCastRotationSpell(name, 0, int.MaxValue, condition, callback, castOnSelf);

        protected bool TryCastRotationSpell(
            string name,
            int minRange,
            int maxRange,
            bool condition = true,
            Action callback = null,
            bool castOnSelf = false)
        {
            if (!CanCastRotationSpell(name, minRange, maxRange, condition))
                return false;

            TryCastSpell(name, minRange, maxRange, condition, callback, castOnSelf);
            return true;
        }

        // Casts without retargeting; used for totems, self-buffs, and other untargeted spells.
        protected bool TryCastNoTargetRotationSpell(
            string name,
            bool condition = true,
            Action callback = null) =>
            TryCastNoTargetRotationSpell(name, 0, int.MaxValue, condition, callback);

        protected bool TryCastNoTargetRotationSpell(
            string name,
            int minRange,
            int maxRange,
            bool condition = true,
            Action callback = null)
        {
            if (!CanCastRotationSpell(name, minRange, maxRange, condition))
                return false;

            player.LuaCall($"CastSpellByName(\"{name}\")");
            callback?.Invoke();
            return true;
        }

        protected bool CanCastRotationSpell(string name, int minRange, int maxRange, bool condition)
        {
            if (!condition || player.IsStunned || player.IsCasting || player.IsChanneling)
                return false;

            var distanceToTarget = player.Position.DistanceTo(target.Position);
            return player.KnowsSpell(name) &&
                player.IsSpellReady(name) &&
                player.Mana >= player.GetManaCost(name) &&
                distanceToTarget >= minRange &&
                distanceToTarget <= maxRange;
        }

        void CleanUp()
        {
            player.StopAllMovement();
            botStates.Pop();
            WoWEventHandler.OnErrorMessage -= OnErrorMessageCallback;
        }

        // Proactive LOS check for ranged subclasses. Call before attempting spells.
        // Returns true when LOS is blocked and strafing has been triggered — callers should return immediately.
        protected bool TriggerLosRecovery()
        {
            if (player.InLosWith(target.Position))
                return false;
            if (!noLos)
            {
                noLos = true;
                noLosStartTime = Environment.TickCount;
                if (losFirstFailTime == 0)
                    losFirstFailTime = Environment.TickCount;
            }
            return true;
        }

        void OnErrorMessageCallback(object sender, OnUiMessageArgs e)
        {
            if (e.Message == FacingErrorMessage && !backpedaling)
            {
                backpedaling = true;
                backpedalStartTime = Environment.TickCount;
                player.StartMovement(ControlBits.Back);
            }
            else if (e.Message == LosErrorMessage && !noLos)
            {
                noLos = true;
                noLosStartTime = Environment.TickCount;
                if (losFirstFailTime == 0)
                    losFirstFailTime = Environment.TickCount;
            }
        }
    }
}
