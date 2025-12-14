using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;
using TMPro;

namespace MortierFu
{
    public class GameplayInfoUI : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private TextMeshProUGUI _roundText;

        [SerializeField] private Transform _teamInfoParent;
        [SerializeField] private GameObject _teamInfoPrefab;
        [SerializeField] private List<TextMeshProUGUI> _teamInfoTexts;

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
            InitializeUI();
        }
        
        private void OnRoundStarted(RoundInfo currentRound)
        {
            UpdateRoundText(currentRound.RoundIndex);
            UpdatePlayerScores();
        }

        private void InitializeUI()
        {
            PopulateTeamInfo();
        }

        private void PopulateTeamInfo()
        {
            for (int i = 0; i < _gm.Teams.Count; i++)
            {
                var iconGO = Instantiate(_teamInfoPrefab, _teamInfoParent);
                var txt = iconGO.GetComponentInChildren<TextMeshProUGUI>();
                _teamInfoTexts.Add(txt);

                txt.text = $"Player {_gm.Teams[i].Index + 1}: {_gm.Teams[i].Score}";
            }
        }

        private void UpdatePlayerScores()
        {
            for (int i = 0; i < _gm.Teams.Count; i++)
            {
                if (i < _teamInfoTexts.Count)
                    _teamInfoTexts[i].text = $"Player {_gm.Teams[i].Index + 1}: {_gm.Teams[i].Score}";
            }
        }

        private void UpdateRoundText(int currentRound)
        {
            if (_roundText != null)
                _roundText.text = $"Round #{currentRound}";
        }
    }
}