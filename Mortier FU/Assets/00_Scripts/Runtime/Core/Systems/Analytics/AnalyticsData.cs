using System.Collections.Generic;
using JetBrains.Annotations;
using MortierFu;

namespace MortierFu
{
    [System.Serializable]
    public class AnalyticsData
    {
        public string gameId;       // ex: GF-20251218-001
        public string date;
        public int numberOfPlayers;
        public string gameVersion;  // hash du commit ou version
        public string officialGameVersion; // nom de la version release (pour Steam)
        public int scoreToWin;
        public AnalyticsRoundData[] rounds;
        public string winner;       // Player ID
        public int roundsPlayed;
        public int durationSeconds;
        
        public AnalyticsFinalPlayerStats[] finalPlayerStats;

        public int totalBombshellKills;
        public int totalSuicides;
        public int totalPushKills;
        public int totalSelfFalls;
    }
    
    [System.Serializable]
    public class AnalyticsFinalPlayerStats
    {
        public string playerId;
        public int score;
        public int kills;
        public int dashesPerformed;
        public int bumpsMade;
        public int stunsPerformed;
        public int stunsUnderwented;
        public int shotsFired;
        public int shotsHit;
        public float damageDealt;
        public float damageTaken;
    }
    public class AnalyticsRoundData
    {
        public int roundNumber;
        public string roundWinner;
        public List<AnalyticsPlayerData> players;
    }


    [System.Serializable]
    public class AnalyticsPlayerData
    {
        public string playerId;
        public int rank;
        public int score;
        public int kills;
        [CanBeNull] public SO_Augment selectedAugment;
        public float damageDealt;
        public float damageTaken;
        public int shotsFired;
        public int shotsHit;
        public int dashesPerformed;
        public int bumpsMade;
        public int stunsPerformed;
        public int stunsUnderwented;
        public int killerId;
        public E_DeathCause deathCause;
    }

    public enum DeathCause
    {
        None,
        Player1,
        Player2,
        Player3,
        Player4,
        Fall,
        VehicleCrash
    }
}