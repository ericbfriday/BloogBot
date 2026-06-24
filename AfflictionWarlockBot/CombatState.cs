using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;

namespace AfflictionWarlockBot
{
    class CombatState : CombatStateBase, IBotState
    {
        const string WandLuaScript = "if IsAutoRepeatAction(11) == nil then CastSpellByName('Shoot') end";
        const string TurnOffWandLuaScript = "if IsAutoRepeatAction(11) ~= nil then CastSpellByName('Shoot') end";

        const string Corruption = "Corruption";
        const string CurseOfAgony = "Curse of Agony";
        const string DeathCoil = "Death Coil";
        const string DrainSoul = "Drain Soul";
        const string Immolate = "Immolate";
        const string LifeTap = "Life Tap";
        const string ShadowBolt = "Shadow Bolt";
        const string SiphonLife = "Siphon Life";

        readonly LocalPlayer player;
        readonly WoWUnit target;

        internal CombatState(
            Stack<IBotState> botStates,
            IDependencyContainer container,
            WoWUnit target,
            bool loot = true) : base(botStates, container, target, 30, loot)
        {
            player = ObjectManager.Player;
            this.target = target;
        }

        public new void Update()
        {
            if (base.Update())
                return;

            ObjectManager.Pet?.Attack();

            // Don't attempt spells without line of sight. Strafing is handled in CombatStateBase.
            if (TriggerLosRecovery())
                return;

            var wand = Inventory.GetEquippedItem(EquipSlot.Ranged);

            // Dying target: switch off the wand and drain its soul for the shard.
            if (AfflictionWarlockRotation.ShouldDrainSoul(target.HealthPercent))
            {
                player.LuaCall(TurnOffWandLuaScript);
                if (TryCastRotationSpell(DrainSoul, 0, 29))
                    return;
            }

            // Wand to conserve mana, or to let dots finish a wounded target.
            if (AfflictionWarlockRotation.ShouldUseWand(wand != null, player.ManaPercent, target.HealthPercent, player.IsCasting, player.IsChanneling))
            {
                player.LuaCall(WandLuaScript);
                return;
            }

            if (TryCastRotationSpell(DeathCoil, 0, 30, AfflictionWarlockRotation.ShouldDeathCoil(target.IsCasting, target.IsChanneling, target.HealthPercent)))
                return;

            if (TryCastRotationSpell(LifeTap, 0, int.MaxValue, AfflictionWarlockRotation.ShouldLifeTap(player.HealthPercent, player.ManaPercent)))
                return;

            if (TryCastRotationSpell(CurseOfAgony, 0, 30, AfflictionWarlockRotation.ShouldApplyDot(target.HasDebuff(CurseOfAgony), target.HealthPercent, 90)))
                return;

            if (TryCastRotationSpell(Immolate, 0, 30, AfflictionWarlockRotation.ShouldApplyDot(target.HasDebuff(Immolate), target.HealthPercent, 30)))
                return;

            if (TryCastRotationSpell(Corruption, 0, 30, AfflictionWarlockRotation.ShouldApplyDot(target.HasDebuff(Corruption), target.HealthPercent, 30)))
                return;

            if (TryCastRotationSpell(SiphonLife, 0, 30, AfflictionWarlockRotation.ShouldApplyDot(target.HasDebuff(SiphonLife), target.HealthPercent, 50)))
                return;

            if (TryCastRotationSpell(ShadowBolt, 0, 30, AfflictionWarlockRotation.ShouldShadowBolt(target.HealthPercent, wand != null)))
                return;
        }
    }
}
