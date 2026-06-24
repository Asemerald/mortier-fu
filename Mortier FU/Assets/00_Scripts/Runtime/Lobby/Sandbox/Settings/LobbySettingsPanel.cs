using System;
using MortierFu.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MortierFu
{
    public sealed class LobbySettingsPanel : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject _root;

        [Header("References")]
        [SerializeField] private Button _returnToMainMenuButton;
        [SerializeField] private Button _addScoreButton;
        [SerializeField] private Button _removeScoreButton;
        [SerializeField] private Button _defaultSelectedButton;
        [SerializeField] private LobbyReturnToMainMenuController _returnToMainMenuController;

        [Header("Data")]
        [SerializeField] private LobbyMatchSettingsData _settingsData;

        [Header("Score Steps")]
        [SerializeField] private int[] _scoreSteps = { 500, 1000, 1500, 2000, 3000 };

        [Header("Input")]
        [SerializeField] private string _cancelActionName = "Cancel";

        [Header("Optional Feedback")]
        [SerializeField] private TextMeshProUGUI _scoreStepIndicator;

        private PlayerManager _activePlayer;
        private Action<PlayerManager> _onClosed;

        private InputAction _cancelAction;

        private int _currentScoreIndex;

        private void Awake()
        {
            if (_root)
            {
                _root.SetActive(false);
            }

            if (_returnToMainMenuButton)
            {
                _returnToMainMenuButton.onClick.AddListener(OnClickReturnToMainMenu);
            }

            if (_addScoreButton)
            {
                _addScoreButton.onClick.AddListener(AddScore);
            }

            if (_removeScoreButton)
            {
                _removeScoreButton.onClick.AddListener(RemoveScore);
            }
        }

        private void OnDisable()
        {
            UnbindInput();
        }

        private void OnDestroy()
        {
            UnbindInput();

            if (_returnToMainMenuButton)
            {
                _returnToMainMenuButton.onClick.RemoveListener(OnClickReturnToMainMenu);
            }

            if (_addScoreButton)
            {
                _addScoreButton.onClick.RemoveListener(AddScore);
            }

            if (_removeScoreButton)
            {
                _removeScoreButton.onClick.RemoveListener(RemoveScore);
            }
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

            _currentScoreIndex = FindClosestScoreIndex(_settingsData.ScoreToWin);

            if (_root)
            {
                _root.SetActive(true);
            }

            BindInput();
            ApplyCurrentScore();
            SelectDefaultButton();

            Logs.Log($"[LobbySettingsPanel] Opened for Player {player.PlayerIndex + 1}.");
        }

        public void Close()
        {
            UnbindInput();

            if (_root)
            {
                _root.SetActive(false);
            }

            _activePlayer = null;
            _onClosed = null;
        }

        private void BindInput()
        {
            if (!_activePlayer || !_activePlayer.PlayerInput)
                return;

            var actions = _activePlayer.PlayerInput.actions;

            _cancelAction = actions.FindAction(_cancelActionName, false);

            if (_cancelAction != null)
            {
                _cancelAction.performed -= OnCancel;
                _cancelAction.performed += OnCancel;
            }
        }

        private void UnbindInput()
        {
            if (_cancelAction != null)
            {
                _cancelAction.performed -= OnCancel;
            }

            _cancelAction = null;
        }

        private void OnCancel(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed)
                return;

            ConfirmAndClose();
        }

        private void ConfirmAndClose()
        {
            if (!_activePlayer)
                return;

            Logs.Log($"[LobbySettingsPanel] Closed by Player {_activePlayer.PlayerIndex + 1}.");

            _onClosed?.Invoke(_activePlayer);
            Close();
        }

        private void OnClickReturnToMainMenu()
        {
            if (!_returnToMainMenuController)
            {
                Logs.LogError("[LobbySettingsPanel] ReturnToMainMenuController reference is missing.");
                return;
            }

            _returnToMainMenuController.ReturnToMainMenu();
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

            var score = _scoreSteps[_currentScoreIndex];
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

        private void SelectDefaultButton()
        {
            var buttonToSelect = _defaultSelectedButton;

            if (!buttonToSelect)
            {
                buttonToSelect = _addScoreButton;
            }

            if (!buttonToSelect)
                return;

            EventSystem.current?.SetSelectedGameObject(buttonToSelect.gameObject);
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
    }
}