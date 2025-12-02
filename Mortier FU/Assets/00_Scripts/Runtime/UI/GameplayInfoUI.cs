using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;
using MortierFu.Shared;

namespace MortierFu
{
    public class GameplayInfoUI : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private GameObject _gameplayInfoPanel;
        [SerializeField] private TextMeshProUGUI _countdownText;
        [SerializeField] private TextMeshProUGUI _roundText;
        [SerializeField] private Transform _teamInfoParent;
        [SerializeField] private GameObject _teamInfoPrefab;
        [SerializeField] private List<TextMeshProUGUI> _teamInfoText;
        
        private GameModeBase _gm;

        private void Start()
        {
            _gm = GameService.CurrentGameMode as GameModeBase;
            if (_gm == null)
            {
                Logs.LogWarning("Game mode not found !");
                return;
            }
            
            _gm.OnGameStarted += OnGameStarted;
            _gm.OnRoundStarted += OnRoundStarted; // No unscubscription
        }

        private void OnGameStarted()
        {
            _gm.OnGameStarted -= OnGameStarted;
            Initialize();
        }
        
        private void OnRoundStarted(int currentRound)
        {
            UpdateRoundText(currentRound);
            UpdatePlayerScores();
            HandleCountdown().Forget();
            Debug.LogWarning("ROUND STARTED");
        }

        private async UniTaskVoid HandleCountdown()
        {
            ShowCountdown();
            float countdownTime;
            do
            {
                countdownTime = _gm.CountdownRemainingTime;
                #if UNITY_EDITOR
                countdownTime *= 4f;
                #endif
                UpdateCountdownText(Mathf.FloorToInt(countdownTime));
                await UniTask.Yield();
            } while (countdownTime > 0f);
            
            UpdateCountdownText(0);
            HideCountdown();
        }

        private void Initialize()
        {
            HideCountdown();
            PopulateTeamInfo();
        }

        private void PopulateTeamInfo()
        {
            if (_gm == null || _gm.Teams == null)
            {
                Logs.LogError("[GameplayInfoUI] GameMode or Teams not initialized.");
                return;
            }

            for (int i = 0; i < _gm.Teams.Count; i++)
            {
                var iconGO = Instantiate(_teamInfoPrefab, _teamInfoParent);
                var text = iconGO.GetComponentInChildren<TextMeshProUGUI>();
                
                if (text != null)
                {
                    text.text = $"Player {_gm.Teams[i].Index + 1}: {_gm.Teams[i].Score}";
                }
                
                _teamInfoText.Add(text);
            }
        }

        private void UpdatePlayerScores()
        {
            for (int i = 0; i < _gm.Teams.Count; i++)
            {
                if (i < _teamInfoText.Count)
                {
                    _teamInfoText[i].text = $"Player {_gm.Teams[i].Index + 1}: {_gm.Teams[i].Score}";
                }
            }
        }
        
        private void UpdateRoundText(int currentRound) => _roundText.text = $"Round #{currentRound}";

        private void UpdateCountdownText(int timeRemaining) => _countdownText.text = timeRemaining <= 0 ? "Fight!" : $"{timeRemaining}";
        
        private void HideCountdown() => _gameplayInfoPanel.SetActive(false);

        private void ShowCountdown() => _gameplayInfoPanel.SetActive(true);
    }
}