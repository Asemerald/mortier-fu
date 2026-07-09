using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_GameModeData", menuName = "Mortier Fu/Game Mode Data")]
    public class SO_GameModeData : ScriptableObject
    {
        public int MinPlayerCount = 1;
        public int MaxPlayerCount = 4;
        
        // Per-player-count placement bonuses. Index = player count.
        public int[] FirstRankBonusByPlayerCount  = new int[] { 0, 100, 75, 100 };
        public int[] SecondRankBonusByPlayerCount = new int[] { 0, 30, 20, 30 };
        public int[] ThirdRankBonusByPlayerCount  = new int[] { 0, 0, 10, 10 };
        public int KillBonusScore = 30;
        public int KillPushBonusScore = 10;
        public int KillCarCrashBonusScore = 20;

        public float AugmentSelectionDuration = 20;
        public float StopShowScoreBoardDelay = 2f;
        public float ShowRoundWinnerDelay = 1.6f;
        public float StormSpawnTime = 30;
    }
}