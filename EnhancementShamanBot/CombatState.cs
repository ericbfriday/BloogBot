using BloogBot;
using BloogBot.AI;
using BloogBot.AI.SharedStates;
using BloogBot.Game;
using BloogBot.Game.Enums;
using BloogBot.Game.Objects;
using System.Collections.Generic;
using System.Linq;

namespace EnhancementShamanBot
{
    class CombatState : CombatStateBase, IBotState
    {
        const string CallOfTheAncestors = "Call of the Ancestors";
        const string CallOfTheElements = "Call of the Elements";
        const string CallOfTheSpirits = "Call of the Spirits";
        const string ChainHeal = EnhancementShamanRotation.ChainHeal;
        const string ChainLightning = EnhancementShamanRotation.ChainLightning;
        const string Clearcasting = "Clearcasting";
        const string EarthShock = "Earth Shock";
        const string FeralSpirit = "Feral Spirit";
        const string FireElementalTotem = "Fire Elemental Totem";
        const string FireNova = "Fire Nova";
        const string FireNovaTotem = "Fire Nova Totem";
        const string FlameShock = "Flame Shock";
        const string FlametongueWeapon = EnhancementShamanRotation.FlametongueWeapon;
        const string FrostShock = "Frost Shock";
        const string GiftOfTheNaaru = "Gift of the Naaru";
        const string GroundingTotem = "Grounding Totem";
        const string HealingStreamTotem = EnhancementShamanRotation.HealingStreamTotem;
        const string HealingWave = EnhancementShamanRotation.HealingWave;
        const string LavaLash = "Lava Lash";
        const string LesserHealingWave = EnhancementShamanRotation.LesserHealingWave;
        const string LightningBolt = EnhancementShamanRotation.LightningBolt;
        const string LightningShield = EnhancementShamanRotation.LightningShield;
        const string MaelstromWeapon = "Maelstrom Weapon";
        const string MagmaTotem = "Magma Totem";
        const string ManaSpringTotem = "Mana Spring Totem";
        const string NatureSwiftness = "Nature's Swiftness";
        const string RockbiterWeapon = EnhancementShamanRotation.RockbiterWeapon;
        const string SearingTotem = "Searing Totem";
        const string ShamanisticRage = "Shamanistic Rage";
        const string StoneclawTotem = "Stoneclaw Totem";
        const string StoneskinTotem = "Stoneskin Totem";
        const string StrengthOfEarthTotem = "Strength of Earth Totem";
        const string Stormstrike = "Stormstrike";
        const string TremorTotem = "Tremor Totem";
        const string WaterShield = EnhancementShamanRotation.WaterShield;
        const string WindShear = "Wind Shear";
        const string WindfuryTotem = "Windfury Totem";
        const string WindfuryWeapon = EnhancementShamanRotation.WindfuryWeapon;
        const string WarStomp = "War Stomp";

        readonly string[] fearingCreatures = new[] { "Scorpid Terror" };
        readonly string[] fireImmuneCreatures = new[] { "Rogue Flame Spirit", "Burning Destroyer" };
        readonly string[] natureImmuneCreatures = new[] { "Swirling Vortex", "Gusting Vortex", "Dust Stormer" };

        readonly Stack<IBotState> botStates;
        readonly IDependencyContainer container;
        readonly LocalPlayer player;
        readonly WoWUnit target;

        internal CombatState(
            Stack<IBotState> botStates,
            IDependencyContainer container,
            WoWUnit target,
            bool loot = true) : base(botStates, container, target, 3, loot)
        {
            this.botStates = botStates;
            this.container = container;
            player = ObjectManager.Player;
            this.target = target;
            player.SetTarget(target.Guid);
        }

        public new void Update()
        {
            if (player.HealthPercent < 30 && HasUsableSelfHeal())
            {
                botStates.Push(new HealSelfState(botStates, container));
                return;
            }

            if (base.Update())
                return;

            if (TriggerLosRecovery())
                return;

            // Shared callback to record where totems are placed.
            System.Action updateAnchor = () => ShamanTotemTracker.SetAnchor(player.Position);

            var targetIsFireImmune = fireImmuneCreatures.Contains(target.Name);
            var targetIsNatureImmune = natureImmuneCreatures.Contains(target.Name);
            var nearbyEnemyCount = NearbyEnemyCount(10);
            var isAoE = nearbyEnemyCount > 1;
            var hasActiveFireTotem = HasActiveFireTotem();
            var noTotemsNearby = !IsAnyTotemNearby(19);
            var maelstromStacks =
                ClientHelper.ClientVersion == ClientVersion.WotLK &&
                player.KnowsSpell(MaelstromWeapon)
                    ? GetMaelstromStacks()
                    : 0;

            var selfHeal = EnhancementShamanRotation.SelectSelfHeal(
                player.HealthPercent,
                maelstromStacks,
                CanUseSelfSpell(LesserHealingWave),
                CanUseSelfSpell(HealingWave),
                CanUseSelfSpell(ChainHeal));
            if (selfHeal != null && TryCastRotationSpell(selfHeal, castOnSelf: true))
                return;

            if (player.HealthPercent < 45 && TryCastNoTargetRotationSpell(NatureSwiftness))
                return;

            if (player.HealthPercent < 55 && TryCastRotationSpell(GiftOfTheNaaru, castOnSelf: true))
                return;

            if (player.HealthPercent < 45 && TryCastNoTargetRotationSpell(WarStomp, condition: target.Position.DistanceTo(player.Position) <= 8))
                return;

            if (player.HealthPercent < 75 && TryCastNoTargetRotationSpell(HealingStreamTotem, condition: !IsTotemNearby(HealingStreamTotem, 19), callback: updateAnchor))
                return;

            // Interrupt
            if (TryCastRotationSpell(WindShear, 0, 25, target.Mana > 0 && target.IsCasting))
                return;

            if (TryCastRotationSpell(EarthShock, 0, 20, target.Mana > 0 && (target.IsCasting || target.IsChanneling) && !targetIsNatureImmune))
                return;

            // Major cooldowns
            if (TryCastNoTargetRotationSpell(FireElementalTotem, condition: IsLongFightTarget() && !hasActiveFireTotem && !targetIsFireImmune, callback: updateAnchor))
                return;

            if (TryCastNoTargetRotationSpell(FeralSpirit, IsLongFightTarget()))
                return;

            if (TryCastNoTargetRotationSpell(ShamanisticRage, player.HealthPercent < 70 || target.HealthPercent > 20 || player.ManaPercent < 70))
                return;

            // Quick full totem redeploy via saved sets when none are present.
            // Uses the highest-rank Call spell known; falls back to lower ranks.
            if (ClientHelper.ClientVersion == ClientVersion.WotLK && noTotemsNearby)
            {
                if (TryCastNoTargetRotationSpell(CallOfTheSpirits, callback: updateAnchor))
                    return;
                if (TryCastNoTargetRotationSpell(CallOfTheAncestors, callback: updateAnchor))
                    return;
                if (TryCastNoTargetRotationSpell(CallOfTheElements, callback: updateAnchor))
                    return;
            }

            // Defensive totems
            if (TryCastNoTargetRotationSpell(GroundingTotem, condition: ObjectManager.Aggressors.Any(a => a.IsCasting && target.Mana > 0), callback: updateAnchor))
                return;
            if (TryCastNoTargetRotationSpell(TremorTotem, condition: fearingCreatures.Contains(target.Name) && !IsTotemNearby(TremorTotem, 29), callback: updateAnchor))
                return;
            if (TryCastNoTargetRotationSpell(StoneclawTotem, condition: ObjectManager.Aggressors.Count() > 1 || player.HealthPercent < 65, callback: updateAnchor))
                return;

            if (TryMaintainWeaponEnchant())
                return;

            var shield = EnhancementShamanRotation.SelectShield(
                ClientHelper.ClientVersion,
                player.KnowsSpell(WaterShield),
                player.HasBuff(WaterShield),
                player.KnowsSpell(LightningShield),
                player.HasBuff(LightningShield),
                isAoE,
                player.ManaPercent);
            if (shield != null && TryCastNoTargetRotationSpell(shield))
                return;

            // 5-stack Maelstrom Weapon proc → instant Lightning Bolt (WotLK only)
            var maelstromSpell =
                ClientHelper.ClientVersion == ClientVersion.WotLK &&
                player.KnowsSpell(MaelstromWeapon)
                    ? EnhancementShamanRotation.SelectMaelstromSpell(
                        maelstromStacks,
                        player.KnowsSpell(ChainLightning),
                        player.KnowsSpell(LightningBolt),
                        targetIsNatureImmune,
                        nearbyEnemyCount)
                    : null;
            if (maelstromSpell != null && TryCastRotationSpell(maelstromSpell, 0, 30))
                return;

            // Core melee rotation
            if (TryCastRotationSpell(Stormstrike, 0, 5))
                return;

            // Flame Shock: keep DoT on target at all times
            if (TryCastRotationSpell(FlameShock, 0, 20, !target.HasDebuff(FlameShock) && !targetIsFireImmune))
                return;

            if (TryCastNoTargetRotationSpell(MagmaTotem, condition: !hasActiveFireTotem && target.Position.DistanceTo(player.Position) <= 8 && !targetIsFireImmune, callback: updateAnchor))
                return;

            if (TryCastNoTargetRotationSpell(FireNovaTotem, condition: EnhancementShamanRotation.ShouldUseLegacyFireNovaTotem(ClientHelper.ClientVersion, player.KnowsSpell(FireNovaTotem), targetIsFireImmune, nearbyEnemyCount), callback: updateAnchor))
                return;

            if (TryCastNoTargetRotationSpell(SearingTotem, condition: !hasActiveFireTotem && target.HealthPercent > 70 && !targetIsFireImmune && target.Position.DistanceTo(player.Position) < 20, callback: updateAnchor))
                return;

            // Earth Shock: after SS debuff, on Clearcasting proc, to interrupt, or as pre-SS filler
            if (TryCastRotationSpell(EarthShock, 0, 20,
                !targetIsNatureImmune &&
                (target.HasDebuff(FlameShock) || target.HasDebuff(Stormstrike) || player.HasBuff(Clearcasting))))
                return;

            // Fire Nova: detonate active Searing Totem for AoE
            if (TryCastNoTargetRotationSpell(FireNova, 0, 30,
                EnhancementShamanRotation.ShouldUseWotlkFireNova(
                    ClientHelper.ClientVersion,
                    player.KnowsSpell(FireNova),
                    hasActiveFireTotem,
                    targetIsFireImmune,
                    player.ManaPercent)))
                return;

            // Lava Lash: weak filler, requires offhand weapon
            if (ClientHelper.ClientVersion == ClientVersion.WotLK &&
                TryCastRotationSpell(LavaLash, 0, 5, player.KnowsSpell(LavaLash) && player.OffhandHasWeapon))
                return;

            if (TryCastRotationSpell(FrostShock, 0, 20, player.IsMoving && target.Position.DistanceTo(player.Position) > 5 && !target.HasDebuff(FlameShock)))
                return;

            // Utility totems
            if (TryCastNoTargetRotationSpell(WindfuryTotem, condition: !IsTotemNearby(WindfuryTotem, 29), callback: updateAnchor))
                return;
            if (TryCastNoTargetRotationSpell(ManaSpringTotem, condition: !IsWaterTotemNearby(), callback: updateAnchor))
                return;
            if (TryCastNoTargetRotationSpell(StrengthOfEarthTotem, condition: !IsEarthBuffTotemNearby(), callback: updateAnchor))
                return;
            if (TryCastNoTargetRotationSpell(StoneskinTotem, condition: target.Mana == 0 && !IsEarthBuffTotemNearby(), callback: updateAnchor))
                return;
        }

        // WotLK UnitBuff returns: name, rank, icon, count, ...
        int GetMaelstromStacks()
        {
            var result = player.LuaCallWithResults("{0}, {1}, {2}, {3} = UnitBuff('player', 'Maelstrom Weapon')");
            if (result != null && result.Length >= 4 && int.TryParse(result[3], out var stacks))
                return stacks;
            return 0;
        }

        bool TryMaintainWeaponEnchant()
        {
            if (!player.MainhandIsEnchanted)
            {
                var mainhandEnchant = EnhancementShamanRotation.SelectMainhandWeaponEnchant(
                    ClientHelper.ClientVersion,
                    player.KnowsSpell(FlametongueWeapon),
                    player.KnowsSpell(WindfuryWeapon),
                    player.KnowsSpell(RockbiterWeapon));

                if (mainhandEnchant != null && TryCastNoTargetRotationSpell(mainhandEnchant))
                    return true;
            }

            if (ClientHelper.ClientVersion == ClientVersion.Vanilla || !player.OffhandHasWeapon || player.OffhandIsEnchanted)
                return false;

            var offhandEnchant = EnhancementShamanRotation.SelectOffhandWeaponEnchant(
                player.KnowsSpell(FlametongueWeapon),
                player.KnowsSpell(WindfuryWeapon));

            return offhandEnchant != null && TryCastNoTargetRotationSpell(offhandEnchant);
        }

        bool HasUsableSelfHeal() =>
            CanUseSelfSpell(LesserHealingWave) ||
            CanUseSelfSpell(HealingWave) ||
            CanUseSelfSpell(ChainHeal);

        bool CanUseSelfSpell(string name) =>
            player.KnowsSpell(name) &&
            player.IsSpellReady(name) &&
            player.Mana >= player.GetManaCost(name) &&
            !player.IsStunned &&
            !player.IsCasting &&
            !player.IsChanneling;

        bool IsLongFightTarget() =>
            target.HealthPercent > 80 &&
            (target.CreatureRank != CreatureRank.Normal || ObjectManager.Aggressors.Count() >= 3) &&
            target.Position.DistanceTo(player.Position) < 20;

        int NearbyEnemyCount(float radius)
        {
            var nearby = new HashSet<ulong>();

            if (target.Health > 0 && target.Position.DistanceTo(player.Position) <= radius)
                nearby.Add(target.Guid);

            foreach (var aggressor in ObjectManager.Aggressors.Where(a => a.Health > 0 && a.Position.DistanceTo(player.Position) <= radius))
                nearby.Add(aggressor.Guid);

            return nearby.Count;
        }

        bool HasActiveFireTotem() =>
            IsTotemNearby(MagmaTotem, 19) ||
            IsTotemNearby(SearingTotem, 19) ||
            IsTotemNearby(FireElementalTotem, 30) ||
            IsTotemNearby(FireNovaTotem, 19);

        bool IsEarthBuffTotemNearby() =>
            IsTotemNearby(StoneclawTotem, 19) ||
            IsTotemNearby(StoneskinTotem, 19) ||
            IsTotemNearby(StrengthOfEarthTotem, 19) ||
            IsTotemNearby(TremorTotem, 29);

        bool IsWaterTotemNearby() =>
            IsTotemNearby(HealingStreamTotem, 19) ||
            IsTotemNearby(ManaSpringTotem, 19);

        bool IsAnyTotemNearby(float range) =>
            ObjectManager.Units.Any(u =>
                u.HealthPercent > 0 &&
                u.Position.DistanceTo(player.Position) < range &&
                u.Name != null &&
                u.Name.Contains("Totem"));

        bool IsTotemNearby(string name, float range) =>
            ObjectManager.Units.Any(u =>
                u.HealthPercent > 0 &&
                u.Position.DistanceTo(player.Position) < range &&
                u.Name != null &&
                u.Name.Contains(name));
    }
}
