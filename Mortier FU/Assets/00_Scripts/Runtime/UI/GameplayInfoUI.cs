using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace MortierFu
{
    public class GameplayInfoUI : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private GameObject _gameplayInfoPanel;
        
        [SerializeField] private GameObject _readyGameObject;
        [SerializeField] private GameObject _playGameObject;
        
        [SerializeField] private Image _countdownSpriteRenderer;
        [SerializeField] private List<Sprite> _countdownSprites;

        [SerializeField] private TextMeshProUGUI _roundText;

        [SerializeField] private Transform _teamInfoParent;
        [SerializeField] private GameObject _teamInfoPrefab;
        [SerializeField] private List<TextMeshProUGUI> _teamInfoText;

        private CanvasGroup _panelGroup;
        private GameModeBase _gm;

        private void Awake()
        {
            _panelGroup = _gameplayInfoPanel.GetComponent<CanvasGroup>();
            if (_panelGroup == null)
                _panelGroup = _gameplayInfoPanel.AddComponent<CanvasGroup>();
        }

        private void Start()
        {
            _gm = GameService.CurrentGameMode as GameModeBase;
            if (_gm == null)
            {
                Logs.LogWarning("Game mode not found !");
                return;
            }

            _gm.OnGameStarted += OnGameStarted;
            _gm.OnRoundStarted += OnRoundStarted;
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
            RunCountdown().Forget();
        }

        private async UniTaskVoid RunCountdown()
        {
            ResetCountdownUI();
            ShowPanel();

            float remaining;

            do
            {
                remaining = _gm.CountdownRemainingTime;
                UpdateCountdownVisual(Mathf.CeilToInt(remaining));
                await UniTask.Yield();
            }
            while (remaining > 0f);

            ShowPlayUI();
            await UniTask.Delay(500);

            await FadeOutPanel(0.35f);
            HidePanel();
        }

        private void ResetCountdownUI()
        {
            _readyGameObject.SetActive(true);
            _playGameObject.SetActive(false);
            _countdownSpriteRenderer.enabled = true;

            foreach (Transform child in _gameplayInfoPanel.transform)
                child.gameObject.SetActive(true);

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

            _countdownSpriteRenderer.sprite = _countdownSprites[index];
        }

        private void ShowPlayUI()
        {
            _readyGameObject.SetActive(false);
            _countdownSpriteRenderer.enabled = false;
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

            _panelGroup.alpha = 0f;
        }

        private void ShowPanel() => _gameplayInfoPanel.SetActive(true);
        private void HidePanel() => _gameplayInfoPanel.SetActive(false);

        private void Initialize()
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
                _teamInfoText.Add(txt);

                txt.text = $"Player {_gm.Teams[i].Index + 1}: {_gm.Teams[i].Score}";
            }
        }

        private void UpdatePlayerScores()
        {
            for (int i = 0; i < _gm.Teams.Count; i++)
                _teamInfoText[i].text = $"Player {_gm.Teams[i].Index + 1}: {_gm.Teams[i].Score}";
        }

        private void UpdateRoundText(int currentRound)
            => _roundText.text = $"Round #{currentRound}";
    }
}
