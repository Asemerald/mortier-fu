using System;
using UnityEngine;

namespace MortierFu
{
    [Serializable]
    public struct MatchConfigByPlayerCount
    {
        [Min(1)] public int PlayerCount;
        public MatchConfig Config;
    }

    [CreateAssetMenu(fileName = "DA_MatchRuleset", menuName = "Mortier Fu/Match Settings/Ruleset")]
    public sealed class SO_MatchRuleset : ScriptableObject
    {
        [Header("Display")]
        public string DisplayName = "Classic";
        public string Subtitle = "RECOMMEND FOR 4 PLAYERS";
        [TextArea] public string Description;

        [Header("Behaviour")]
        public bool IsCustom;
        public bool AllowEditing;

        [Header("Default Config")]
        public MatchConfig DefaultConfig = MatchConfig.Default;

        [Header("Recommended By Player Count")]
        public MatchConfigByPlayerCount[] RecommendedConfigsByPlayerCount;

        public MatchConfig GetConfigForPlayerCount(int playerCount)
        {
            if (RecommendedConfigsByPlayerCount != null)
            {
                for (int i = 0; i < RecommendedConfigsByPlayerCount.Length; i++)
                {
                    if (RecommendedConfigsByPlayerCount[i].PlayerCount != playerCount)
                        continue;

                    MatchConfig config = RecommendedConfigsByPlayerCount[i].Config;
                    config.Clamp();
                    return config;
                }
            }

            MatchConfig fallback = DefaultConfig;
            fallback.Clamp();
            return fallback;
        }
    }
}