using System;
using System.Collections.Generic;
using UnityEngine;

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
        public AnalyticsAugmentEntry[] augmentStats;

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
        public AnalyticsPlayerStats playerStats;
    }
    
    [System.Serializable]
    public class AnalyticsRoundData
    {
        public int roundNumber;
        public string roundWinner;
        public int roundDurationSeconds;
        public List<AnalyticsPlayerData> players;
    }


    [System.Serializable]
    public class AnalyticsPlayerData
    {
        public string playerId;
        public int rank;
        public int score;
        public int kills;
        [System.NonSerialized] public SO_Augment selectedAugment;
        public string selectedAugmentName;
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
        public string deathCauseName;
    }

    [System.Serializable]
    public class AnalyticsAugmentEntry
    {
        public int augmentId;
        public string augmentName;
        public int timesShown;
        public int timesPicked;

        public int timesPickedByWinner;
        public bool winnerHadIt;
    }

    [System.Serializable]
    public class AnalyticsPlayerStats
    {
        public float maxHealth;
        public float moveSpeed;
        public float bombshellDamage;
        public float bombshellImpactRadius;
        public float bombshellSpeed;
        public float fireRate;
        public float shotRange;
        public float dashCharges;
        public float dashCooldown;
        public float dashDistance;
        public float strikePushForce;
        public float strikeStunDuration;
    }
}