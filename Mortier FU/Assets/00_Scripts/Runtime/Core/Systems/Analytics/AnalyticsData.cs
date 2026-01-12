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
        public AnalyticsRoundData[] rounds;
        public string winner;       // Player ID
        public int roundsPlayed;
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
        public DeathCause deathCause;
    }

    public enum DeathCause
    {
        None,
        Player1,
        Player2,
        Player3,
        Player4,
        Fall
    }
}