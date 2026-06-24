using System;
using MortierFu.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MortierFu
{
    public sealed class LobbySettingsPanel : MonoBehaviour, IPlayerUIInputHandler
    {
        [Header("Root")]
        [SerializeField] private GameObject _root;

        [Header("References")]
        [SerializeField] private Button _addScoreButton;
        [SerializeField] private Button _removeScoreButton;
        [SerializeField] private Button _defaultSelectedButton;

        [Header("Data")]
        [SerializeField] private LobbyMatchSettingsData _settingsData;

        [Header("Score Steps")]
        [SerializeField] private int[] _scoreSteps = { 500, 1000, 1500, 2000, 3000 };

        [Header("Optional Feedback")]
        [SerializeField] private TextMeshProUGUI _scoreStepIndicator;

        private PlayerManager _activePlayer;
        private Action<PlayerManager> _onClosed;

        private int _currentScoreIndex;
        private bool _isOpen;

        private PlayerUIInputService UIInputService =>
            ServiceManager.Instance?.Get<PlayerUIInputService>();

        private void Awake()
        {
            if (_root)
                _root.SetActive(false);

            if (_addScoreButton)
                _addScoreButton.onClick.AddListener(AddScore);

            if (_removeScoreButton)
                _removeScoreButton.onClick.AddListener(RemoveScore);
        }

        private void OnDisable()
        {
            RemoveFromUIInputService();
        }

        private void OnDestroy()
        {
            RemoveFromUIInputService();

            if (_addScoreButton)
                _addScoreButton.onClick.RemoveListener(AddScore);

            if (_removeScoreButton)
                _removeScoreButton.onClick.RemoveListener(RemoveScore);
        }

        public void Open(PlayerManager player, Action<PlayerManager> onClosed)
        {
            if (!player)
                return;

            if (!_settingsData)
            {
                Logs.LogError("[LobbySettingsPanel] Settings data reference is missing.");
                return;
            }

            _activePlayer = player;
            _onClosed = onClosed;
            _isOpen = true;

            _currentScoreIndex = FindClosestScoreIndex(_settingsData.ScoreToWin);

            if (_root)
                _root.SetActive(true);

            ApplyCurrentScore();
            RegisterToUIInputService();
            SelectDefaultButton();

            Logs.Log($"[LobbySettingsPanel] Opened for Player {player.PlayerIndex + 1}.");
        }

        public void Close()
        {
            if (!_isOpen)
                return;

            RemoveFromUIInputService();

            if (_root)
                _root.SetActive(false);

            _activePlayer = null;
            _onClosed = null;
            _isOpen = false;
        }

        private void ConfirmAndClose()
        {
            if (!_activePlayer)
                return;

            var player = _activePlayer;
            var onClosed = _onClosed;

            Logs.Log($"[LobbySettingsPanel] Confirmed and closed by Player {player.PlayerIndex + 1}.");

            Close();

            onClosed?.Invoke(player);
        }

        private void RegisterToUIInputService()
        {
            if (!_activePlayer)
                return;

            UIInputService?.Push(_activePlayer, this);
        }

        private void RemoveFromUIInputService()
        {
            if (_activePlayer)
                UIInputService?.Remove(_activePlayer, this);
            else
                UIInputService?.RemoveFromAll(this);
        }

        private void SelectDefaultButton()
        {
            var buttonToSelect = _defaultSelectedButton;

            if (!buttonToSelect)
                buttonToSelect = _addScoreButton;

            if (!buttonToSelect)
                buttonToSelect = _removeScoreButton;

            if (!buttonToSelect)
                return;

            EventSystem.current?.SetSelectedGameObject(buttonToSelect.gameObject);
        }

        private void AddScore()
        {
            SetScoreIndex(_currentScoreIndex + 1);
        }

        private void RemoveScore()
        {
            SetScoreIndex(_currentScoreIndex - 1);
        }

        private void SetScoreIndex(int index)
        {
            if (_scoreSteps is null || _scoreSteps.Length == 0)
                return;

            _currentScoreIndex = WrapIndex(index, _scoreSteps.Length);
            ApplyCurrentScore();
        }

        private void ApplyCurrentScore()
        {
            if (!_settingsData)
                return;

            if (_scoreSteps is null || _scoreSteps.Length == 0)
                return;

            int score = _scoreSteps[_currentScoreIndex];
            _settingsData.SetScoreToWin(score);

            RefreshFeedback(score);

            Logs.Log($"[LobbySettingsPanel] ScoreToWin = {score}");
        }

        private void RefreshFeedback(int currentScore)
        {
            if (!_scoreStepIndicator)
                return;

            _scoreStepIndicator.text = currentScore.ToString();
        }

        private int FindClosestScoreIndex(int score)
        {
            if (_scoreSteps is null || _scoreSteps.Length == 0)
                return 0;

            int closestIndex = 0;
            int closestDistance = Mathf.Abs(_scoreSteps[0] - score);

            for (int i = 1; i < _scoreSteps.Length; i++)
            {
                int distance = Mathf.Abs(_scoreSteps[i] - score);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        private static int WrapIndex(int value, int count)
        {
            if (count <= 0)
                return 0;

            value %= count;

            if (value < 0)
                value += count;

            return value;
        }

        public bool CanHandleUIInput(PlayerManager player)
        {
            return _isOpen &&
                   _activePlayer &&
                   ReferenceEquals(_activePlayer, player);
        }

        public bool HandleNavigate(PlayerManager player, Vector2 direction)
        {
            return false;
        }

        public bool HandleSubmit(PlayerManager player)
        {
            return false;
        }

        public bool HandleCancel(PlayerManager player)
        {
            ConfirmAndClose();
            return true;
        }
    }
}