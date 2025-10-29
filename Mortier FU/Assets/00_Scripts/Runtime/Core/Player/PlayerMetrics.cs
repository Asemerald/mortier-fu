namespace MortierFu
{
    public struct PlayerMetrics
    {
        public int TotalKills;
        public int TotalDeaths;
        public int TotalAssists;
        
        public int RoundKills;
        public int RoundAssists;

        public void ResetMetrics() => this = default;

        public void ResetRoundMetrics()
        {
            RoundKills = 0;
            RoundAssists = 0;
        }
    }
}