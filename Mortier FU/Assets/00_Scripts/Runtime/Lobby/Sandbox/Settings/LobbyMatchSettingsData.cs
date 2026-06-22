using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbyMatchSettingsData : MonoBehaviour
    {
        [Header("Score")]
        [SerializeField] private int _scoreToWin = 1000;

        public int ScoreToWin => _scoreToWin;

        public void SetScoreToWin(int scoreToWin)
        {
            _scoreToWin = Mathf.Max(1, scoreToWin);

            Logs.Log($"[LobbyMatchSettingsData] ScoreToWin set to {_scoreToWin}.");
        }

        public MatchConfig ToMatchConfig()
        {
            return new MatchConfig(_scoreToWin);
        }
    }
}