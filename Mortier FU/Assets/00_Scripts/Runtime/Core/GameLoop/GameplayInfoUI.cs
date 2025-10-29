using MortierFu.Shared;
using TMPro;
using UnityEngine;

namespace MortierFu
{
    public class GameplayInfoUI : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private TextMeshProUGUI _countdownText;
        [SerializeField] private TextMeshProUGUI _roundText;
        [SerializeField] private ScorePanel _scorePanel;

        public ScorePanel ScorePanel => _scorePanel;
        private IGameMode _gm;
        
        public void BindToGameMode(IGameMode gameMode)
        {
            if (gameMode == null)
            {
                Logs.LogWarning("[GameplayInfoUI]: Trying to bind to a null GameMode.");
                return;
            }
            
            // Unsubscribe
            if (_gm != null)
            {
                _gm.OnRoundStarted -= UpdateRoundText;
            }
            
            _gm = gameMode;
            
            // Subscribe
            _gm.OnRoundStarted += UpdateRoundText;
            UpdateRoundText(_gm.CurrentRoundCount);
        }

        private void UpdateRoundText(int currentRound)
        {
            _roundText.text = $"Round #{currentRound}";
        }
    }
}