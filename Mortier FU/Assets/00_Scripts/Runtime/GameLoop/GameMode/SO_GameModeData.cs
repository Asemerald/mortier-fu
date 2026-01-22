using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_GameModeData", menuName = "Mortier Fu/Game Mode Data")]
    public class SO_GameModeData : ScriptableObject
    {
        public int MinPlayerCount = 1;
        public int MaxPlayerCount = 4;
        
        public int ScoreToWin = 1000;
        public int FirstRankBonusScore = 100;
        public int SecondRankBonusScore = 30;   
        public int ThirdRankBonusScore = 10;
        public int KillBonusScore = 30;
        public int KillPushBonusScore = 10;
        public int KillCarCrashBonusScore = 20;

        public float AugmentSelectionDuration = 20;
        public float RoundStartCountdown = 5f;
        public float DisplayScoresDuration = 15f;
        public float ShowRoundWinnerDelay = 1.6f;
        public float StormSpawnTime = 30;
    }
}