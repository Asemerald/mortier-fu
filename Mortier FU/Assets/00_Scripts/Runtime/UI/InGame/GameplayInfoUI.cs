using MortierFu.Shared;
using UnityEngine;
using TMPro;

namespace MortierFu
{
    public class GameplayInfoUI : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private TextMeshProUGUI _roundText;

        private GameModeBase _gm;

        private void Awake()
        {
            _gm = GameService.CurrentGameMode as GameModeBase;
        }
        
        private void OnEnable()
        {
            if (_gm == null)
            {
                Logs.LogWarning("[GameplayInfoUI]: Game mode not found");
                return;
            }

            _gm.OnGameStarted += OnGameStarted;
            _gm.OnRoundStarted += OnRoundStarted;
        }

        private void OnDisable()
        {
            if (_gm == null) return;

            _gm.OnGameStarted -= OnGameStarted;
            _gm.OnRoundStarted -= OnRoundStarted;
        }

        private void OnGameStarted()
        {
            _gm.OnGameStarted -= OnGameStarted;
        }
        
        private void OnRoundStarted(RoundInfo currentRound)
        {
            UpdateRoundText(currentRound.RoundIndex);
        }

        private void UpdateRoundText(int currentRound)
        {
            if (_roundText != null)
                _roundText.text = $"Round #{currentRound}";
        }
    }
}