using System.Collections.Generic;

[System.Serializable]
public class AnalyticsData
{
    public string gameId;       // ex: GF-20251218-001
    public string date;
    public int numberOfPlayers;
    public string gameVersion;  // hash du commit ou version
    public PlayerData[] players;
    public int winningScore;
    public string winner;       // Player ID
    public int roundsPlayed;
}

[System.Serializable]
public class PlayerData
{
    public string playerId;
    public int rank;
    public int score;
    public int kills;
    public float damageDealt;
    public float damageTaken;
    public int shotsFired;
    public int shotsHit;
    public int dashesPerformed;
    public int bumpsMade;
    


}

public class RoundData
{
    public int roundNumber;
    public Dictionary<string, int> playerScores; // Player ID -> Score
    public string roundWinner;                    // Player ID
}
