using System;
using UnityEngine;

namespace MortierFu
{
    public enum MatchSettingId
    {
        ScoreToWin,
        RaceTimeLimit,
        EqualizeDropRateForAllRarities,
        HealthMultiplier,
        DisableStrikes,
        StrikeMultiplier,
        DisableGhosts,
        DisableSelfDamage
    }
    
    [Serializable]
    public struct MatchConfig
    {
        public string Subtitle;
        public int ScoreToWin;
        public float RaceTimeLimit;

        public bool EqualizeDropRateForAllRarities;

        public float HealthMultiplier;

        public bool DisableStrikes;
        public float StrikeMultiplier;

        public bool DisableGhosts;
        public bool DisableSelfDamage;

        public MatchConfig(int scoreToWin, string subtitle = "")
        {
            Subtitle = subtitle;
            ScoreToWin = scoreToWin;
            RaceTimeLimit = 20f;

            EqualizeDropRateForAllRarities = false;

            HealthMultiplier = 1f;

            DisableStrikes = false;
            StrikeMultiplier = 1f;

            DisableGhosts = false;
            DisableSelfDamage = false;

            Clamp();
        }

        public static MatchConfig Default => new()
        {
            Subtitle = "Default Config",
            ScoreToWin = 1000,
            RaceTimeLimit = 20f,

            EqualizeDropRateForAllRarities = false,

            HealthMultiplier = 1f,

            DisableStrikes = false,
            StrikeMultiplier = 1f,

            DisableGhosts = false,
            DisableSelfDamage = false,
        };

        public void Clamp()
        {
            ScoreToWin = Mathf.Clamp(ScoreToWin, 500, 3000);
            RaceTimeLimit = Mathf.Clamp(RaceTimeLimit, 10f, 30f);

            HealthMultiplier = Mathf.Clamp(HealthMultiplier, 0.5f, 3f);
            StrikeMultiplier = Mathf.Clamp(StrikeMultiplier, 0.5f, 3f);
        }
    }
}