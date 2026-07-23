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
        DisableSelfDamage,
        DisableAugments
    }
    
    [Serializable]
    public struct MatchConfig
    {
        public int ScoreToWin;
        public float RaceTimeLimit;

        public bool EqualizeDropRateForAllRarities;

        public float HealthMultiplier;

        public bool DisableStrikes;
        public float StrikeMultiplier;

        public bool DisableGhosts;
        public bool DisableSelfDamage;
        [HideInInspector] public bool DisableAugments;

        public MatchConfig(int scoreToWin)
        {
            ScoreToWin = scoreToWin;
            RaceTimeLimit = 20f;

            EqualizeDropRateForAllRarities = false;

            HealthMultiplier = 1f;

            DisableStrikes = false;
            StrikeMultiplier = 1f;

            DisableGhosts = false;
            DisableSelfDamage = false;
            DisableAugments = false;

            Clamp();
        }

        public static MatchConfig Default => new()
        {
            ScoreToWin = 1000,
            RaceTimeLimit = 20f,

            EqualizeDropRateForAllRarities = false,

            HealthMultiplier = 1f,

            DisableStrikes = false,
            StrikeMultiplier = 1f,

            DisableGhosts = false,
            DisableSelfDamage = false,
            DisableAugments = false
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