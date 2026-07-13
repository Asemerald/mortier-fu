using System;
using UnityEngine;
using Eflatun.SceneReference;

namespace MortierFu
{
    [Serializable]
    public struct ScoreRewardData
    {
        public int Score;
        public string Text;

        public static ScoreRewardData Zero => new ScoreRewardData
        {
            Score = 0,
            Text = string.Empty
        };

        public string GetDisplayText()
        {
            if (!string.IsNullOrWhiteSpace(Text))
                return Text;

            if (Score > 0)
                return $"+{Score}";

            if (Score < 0)
                return Score.ToString();

            return string.Empty;
        }

        public bool ShouldDisplay() => Score != 0 || !string.IsNullOrWhiteSpace(Text);
    }

    [Serializable]
    public struct PlacementScoreRewardsByPlayerCount
    {
        public int PlayerCount;

        public ScoreRewardData FirstRank;
        public ScoreRewardData SecondRank;
        public ScoreRewardData ThirdRank;

        public ScoreRewardData GetRewardForRank(int rank)
        {
            return rank switch
            {
                1 => FirstRank,
                2 => SecondRank,
                3 => ThirdRank,
                _ => ScoreRewardData.Zero
            };
        }
    }

    [CreateAssetMenu(fileName = "DA_GameModeData", menuName = "Mortier Fu/Game Mode Data")]
    public class SO_GameModeData : ScriptableObject
    {
        public int MinPlayerCount = 1;
        public int MaxPlayerCount = 4;
        
        [Header("Race Override")]
        public SceneReference FirstArenaRaceOverride;

        [Header("Placement Rewards")]
        public PlacementScoreRewardsByPlayerCount[] PlacementRewardsByPlayerCount = { };

        [Header("Kill Rewards")]
        public ScoreRewardData BombshellKillReward = new ScoreRewardData
        {
            Score = 30,
            Text = "+30"
        };

        public ScoreRewardData PushKillReward = new ScoreRewardData
        {
            Score = 40,
            Text = "+40"
        };

        public ScoreRewardData VehicleCrashKillReward = new ScoreRewardData
        {
            Score = 50,
            Text = "+50"
        };

        [Header("Flow")]
        public float StopShowScoreBoardDelay = 2f;

        public ScoreRewardData GetPlacementReward(int playerCount, int rank)
        {
            if (playerCount <= 0 || rank <= 0 || PlacementRewardsByPlayerCount == null)
                return ScoreRewardData.Zero;

            for (int i = 0; i < PlacementRewardsByPlayerCount.Length; i++)
            {
                if (PlacementRewardsByPlayerCount[i].PlayerCount == playerCount)
                    return PlacementRewardsByPlayerCount[i].GetRewardForRank(rank);
            }

            return ScoreRewardData.Zero;
        }

        public ScoreRewardData GetKillReward(E_DeathCause cause)
        {
            return cause switch
            {
                E_DeathCause.BombshellExplosion => BombshellKillReward,
                E_DeathCause.Fall => PushKillReward,
                E_DeathCause.VehicleCrash => VehicleCrashKillReward,
                _ => ScoreRewardData.Zero
            };
        }
    }
}