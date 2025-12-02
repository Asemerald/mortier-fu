using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_GameModeData", menuName = "Mortier Fu/Game Mode Data")]
    public class SO_GameModeData : ScriptableObject
    {
        public int MinPlayerCount = 2;
        public int MaxPlayerCount = 4;
        
        public int ScoreToWin = 1000;
        public int FirstRankBonusScore = 100;
        public int SecondRankBonusScore = 30;
        public int ThirdRankBonusScore = 10;
        public int KillBonusScore = 50;

        public float AugmentSelectionDuration = 15;
        public float RoundStartCountdown = 4f;
        public float DisplayScoresDuration = 6f;

        public float StormSpawnTime = 30;
    }
}