namespace BloogBot.Game
{
    public class DkAbilityCost
    {
        public DkAbilityCost(int bloodRunes = 0, int frostRunes = 0, int unholyRunes = 0, int runicPower = 0)
        {
            BloodRunes = bloodRunes;
            FrostRunes = frostRunes;
            UnholyRunes = unholyRunes;
            RunicPower = runicPower;
        }

        public int BloodRunes { get; }

        public int FrostRunes { get; }

        public int UnholyRunes { get; }

        public int RunicPower { get; }

        public int TotalRunes
        {
            get { return BloodRunes + FrostRunes + UnholyRunes; }
        }
    }
}
