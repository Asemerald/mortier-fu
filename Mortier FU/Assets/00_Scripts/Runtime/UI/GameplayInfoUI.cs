using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine.UI;
using UnityEngine;
using System;
using PrimeTween;
using TMPro;

namespace MortierFu
{
    public class GameplayInfoUI : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private GameObject _gameplayInfoPanel;

        [SerializeField] private GameObject _readyGameObject;
        [SerializeField] private GameObject _playGameObject;
        [SerializeField] private GameObject _goldenBombshellGameObject;

        [SerializeField] private Image _countdownImage;
        [SerializeField] private List<Sprite> _countdownSprites;

        [SerializeField] private TextMeshProUGUI _roundText;

        [SerializeField] private Transform _teamInfoParent;
        [SerializeField] private GameObject _teamInfoPrefab;
        [SerializeField] private List<TextMeshProUGUI> _teamInfoTexts;

        [SerializeField] private CanvasGroup _panelGroup;

        [Header("Countdown Settings")] [SerializeField]
        private float _postPlayDelay = 0.5f;

        [SerializeField] private float _panelFadeDuration = 0.35f;

        private GameModeBase _gm;

        private void Start()
        {
            _gm = GameService.CurrentGameMode as GameModeBase;
            if (_gm == null)
            {
                Logs.LogWarning("Game mode not found");
                return;
            }

            _gm.OnGameStarted += OnGameStarted;
            _gm.OnRoundStarted += OnRoundStarted;
        }

        private void OnDestroy()
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

        private void OnRoundStarted(int currentRound)
        {
            UpdateRoundText(currentRound);
            UpdatePlayerScores();
            RunCountdown().Forget();
        }

        private async UniTaskVoid RunCountdown()
        {
            ResetCountdownUI();
            ShowPanel();

            float remaining;
            
            AnimateCountdownImage().Forget();
            
            do
            {
                remaining = _gm.CountdownRemainingTime;
                UpdateCountdownVisual(Mathf.CeilToInt(remaining));
                await UniTask.Yield();
            } while (remaining > 0f);

            ShowPlayUI();
            await UniTask.Delay(TimeSpan.FromSeconds(_postPlayDelay));

            await FadeOutPanel(_panelFadeDuration);
            HidePanel();
        }
        private async UniTaskVoid AnimateCountdownImage()
        {
            var target = _countdownImage.transform;
            await Tween.Scale(target, target.localScale * 0.525f, 1f, Ease.OutBack, cycles: Mathf.CeilToInt(_gm.CountdownRemainingTime), CycleMode.Restart);
        }

        private void ResetCountdownUI()
        {
            _readyGameObject.SetActive(true);
            _playGameObject.SetActive(false);
            _countdownImage.enabled = true;

            _panelGroup.alpha = 1f;
        }

        private void UpdateCountdownVisual(int t)
        {
            int index = t switch
            {
                <= 1 => 0,
                <= 2 => 1,
                <= 3 => 2,
                _ => 2
            };

            if (_countdownSprites == null || _countdownSprites.Count <= index) return;
            
            var newSprite = _countdownSprites[index];
            
            if (_countdownImage.sprite == newSprite) return;

            _countdownImage.sprite = newSprite;
        }

        private void ShowPlayUI()
        {
            _readyGameObject.SetActive(false);
            _countdownImage.enabled = false;
            _playGameObject.SetActive(true);
        }

        private async UniTask FadeOutPanel(float duration)
        {
            float t = 0f;
            float startAlpha = _panelGroup.alpha;

            while (t < duration)
            {
                t += Time.deltaTime;
                _panelGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / duration);
                await UniTask.Yield();
            }

            _panelGroup.alpha = 1f;
        }

        private void ShowPanel() => _gameplayInfoPanel.SetActive(true);

        private void HidePanel() => _gameplayInfoPanel.SetActive(false);

        private void InitializeUI()
        {
            HidePanel();
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

            UpdateMatchPointIndicator();
        }

        private void UpdateRoundText(int currentRound)
        {
            if (_roundText != null)
                _roundText.text = $"Round #{currentRound}";
        }

        private void UpdateMatchPointIndicator()
        {
            if (_gm == null || _goldenBombshellGameObject.activeSelf) return;

            bool isMatchPoint = false;

            for (int i = 0; i < _gm.Teams.Count; i++)
            {
                if (_gm.Teams[i].Score < _gm.Data.ScoreToWin) continue;
                isMatchPoint = true;
                break;
            }

            _goldenBombshellGameObject.SetActive(isMatchPoint);
        }
    }
}