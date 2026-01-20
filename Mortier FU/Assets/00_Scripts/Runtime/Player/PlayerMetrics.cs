using System.Collections.Generic;
namespace MortierFu
{
    public struct PlayerMetrics
    {
        public List<E_DeathCause> TotalKills;
        public int TotalDeaths;
        public int TotalAssists;
        
        public List<E_DeathCause> RoundKills;
        public int RoundAssists;

        public void ResetMetrics() => this = default;

        public void ResetRoundMetrics()
        {
            RoundKills = new List<E_DeathCause>();
            RoundAssists = 0;
        }
    }
}