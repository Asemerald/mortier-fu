using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MortierFu
{
    [System.Serializable]
    public struct PlayerCountRecommendedScore
    {
        [Min(1)] public int PlayerCount;
        public int ScoreToWin;
    }

    public sealed class TEMP_LobbyRecommendedScoreDisplay : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private TMP_Text _recommendedForText;

        [Header("Recommended Score (affichage uniquement)")]
        [SerializeField] private List<PlayerCountRecommendedScore> _recommendedScoreByPlayerCount = new();

        public void Refresh(int currentScore)
        {
            if (!_recommendedForText)
                return;

            for (var i = 0; i < _recommendedScoreByPlayerCount.Count; i++)
            {
                if (_recommendedScoreByPlayerCount[i].ScoreToWin != currentScore)
                    continue;

                _recommendedForText.gameObject.SetActive(true);
                _recommendedForText.text = $"Recommended for {_recommendedScoreByPlayerCount[i].PlayerCount} players";
                return;
            }

            _recommendedForText.gameObject.SetActive(false);
        }
    }
}