using BloogBot.Game.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BloogBot.Game
{
    public static class DeathKnightResources
    {
        public const int RuneSlotCount = 6;

        static readonly IDictionary<string, DkAbilityCost> KnownCosts =
            new Dictionary<string, DkAbilityCost>(StringComparer.OrdinalIgnoreCase)
            {
                { "Icy Touch", new DkAbilityCost(frostRunes: 1) },
                { "Plague Strike", new DkAbilityCost(unholyRunes: 1) },
                { "Blood Strike", new DkAbilityCost(bloodRunes: 1) },
                { "Heart Strike", new DkAbilityCost(bloodRunes: 1) },
                { "Pestilence", new DkAbilityCost(bloodRunes: 1) },
                { "Blood Boil", new DkAbilityCost(bloodRunes: 1) },
                { "Death Strike", new DkAbilityCost(frostRunes: 1, unholyRunes: 1) },
                { "Obliterate", new DkAbilityCost(frostRunes: 1, unholyRunes: 1) },
                { "Scourge Strike", new DkAbilityCost(unholyRunes: 1) },
                { "Death and Decay", new DkAbilityCost(bloodRunes: 1, frostRunes: 1, unholyRunes: 1) },
                { "Death Coil", new DkAbilityCost(runicPower: 40) },
                { "Rune Strike", new DkAbilityCost(runicPower: 20) },
                { "Rune Tap", new DkAbilityCost(bloodRunes: 1) },
                { "Frost Strike", new DkAbilityCost(runicPower: 40) },
                { "Mind Freeze", new DkAbilityCost(runicPower: 20) },
                { "Icebound Fortitude", new DkAbilityCost(runicPower: 20) },
                { "Anti-Magic Shell", new DkAbilityCost(runicPower: 20) },
                { "Death Pact", new DkAbilityCost(runicPower: 40) },
            };

        static readonly ISet<string> GroundTargetedSpells =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Death and Decay",
            };

        public static RuneState ParseRuneState(
            int slot,
            string start,
            string duration,
            string ready,
            string runeType,
            string count)
        {
            return new RuneState(
                slot,
                ParseRuneType(runeType),
                ready == "1",
                ParseInt(count),
                ParseFloat(duration));
        }

        public static DkAbilityCost GetKnownCost(string spellName)
        {
            DkAbilityCost cost;
            return spellName != null && KnownCosts.TryGetValue(spellName, out cost)
                ? cost
                : new DkAbilityCost();
        }

        public static bool HasReadyRunes(IEnumerable<RuneState> runes, DkAbilityCost cost)
        {
            var readyRunes = runes
                .Where(r => r.Ready && r.Count > 0)
                .ToList();

            var deathRunes = readyRunes.Count(r => r.Type == RuneType.Death);

            return HasRunesOfType(readyRunes, RuneType.Blood, cost.BloodRunes, ref deathRunes)
                && HasRunesOfType(readyRunes, RuneType.Frost, cost.FrostRunes, ref deathRunes)
                && HasRunesOfType(readyRunes, RuneType.Unholy, cost.UnholyRunes, ref deathRunes);
        }

        public static bool HasEnoughRunicPower(int runicPower, DkAbilityCost cost)
        {
            return runicPower >= cost.RunicPower;
        }

        public static bool RequiresGroundTarget(string spellName)
        {
            return spellName != null && GroundTargetedSpells.Contains(spellName);
        }

        static bool HasRunesOfType(IList<RuneState> runes, RuneType type, int required, ref int deathRunes)
        {
            if (required <= 0)
                return true;

            var exact = runes.Count(r => r.Type == type);
            if (exact >= required)
                return true;

            var missing = required - exact;
            if (deathRunes < missing)
                return false;

            deathRunes -= missing;
            return true;
        }

        static RuneType ParseRuneType(string value)
        {
            var type = ParseInt(value);
            return Enum.IsDefined(typeof(RuneType), type) ? (RuneType)type : RuneType.Unknown;
        }

        static int ParseInt(string value)
        {
            int parsed;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                ? parsed
                : 0;
        }

        static float ParseFloat(string value)
        {
            float parsed;
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                ? parsed
                : 0f;
        }
    }
}
