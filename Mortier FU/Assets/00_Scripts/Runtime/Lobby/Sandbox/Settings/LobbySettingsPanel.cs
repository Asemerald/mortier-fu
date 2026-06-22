using System;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public sealed class LobbySettingsPanel : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject _root;

        [Header("Data")]
        [SerializeField] private LobbyMatchSettingsData _settingsData;

        [Header("Score Steps")]
        [SerializeField] private int[] _scoreSteps = { 500, 1000, 1500, 2000, 3000 };

        [Header("Input")]
        [SerializeField] private string _navigateActionName = "Navigate";
        [SerializeField] private string _submitActionName = "Submit";
        [SerializeField] private string _cancelActionName = "Cancel";

        [Header("Optional Feedback")]
        [SerializeField] private GameObject[] _scoreStepIndicators;

        private PlayerManager _activePlayer;
        private Action<PlayerManager> _onClosed;

        private InputAction _navigateAction;
        private InputAction _submitAction;
        private InputAction _cancelAction;

        private int _currentScoreIndex;

        private Vector2 _previousNavigateInput = Vector2.zero;
        private float _lastNavigateTime;

        private const float Threshold = 0.7f;
        private const float NavigateCooldown = 0.25f;

        private void Awake()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private void OnDisable()
        {
            UnbindInput();
        }

        public void Open(PlayerManager player, Action<PlayerManager> onClosed)
        {
            if (player == null)
                return;

            if (_settingsData == null)
            {
                Logs.LogError("[LobbySettingsPanel] Settings data reference is missing.");
                return;
            }

            _activePlayer = player;
            _onClosed = onClosed;

            _currentScoreIndex = FindClosestScoreIndex(_settingsData.ScoreToWin);

            if (_root != null)
            {
                _root.SetActive(true);
            }

            BindInput();
            ApplyCurrentScore();

            Logs.Log($"[LobbySettingsPanel] Opened for Player {player.PlayerIndex + 1}.");
        }

        public void Close()
        {
            UnbindInput();

            if (_root != null)
            {
                _root.SetActive(false);
            }

            _activePlayer = null;
            _onClosed = null;
            _previousNavigateInput = Vector2.zero;
        }

        private void BindInput()
        {
            if (_activePlayer == null || _activePlayer.PlayerInput == null)
                return;

            var actions = _activePlayer.PlayerInput.actions;

            _navigateAction = actions.FindAction(_navigateActionName, false);
            _submitAction = actions.FindAction(_submitActionName, false);
            _cancelAction = actions.FindAction(_cancelActionName, false);

            if (_navigateAction != null)
                _navigateAction.performed += OnNavigate;

            if (_submitAction != null)
                _submitAction.performed += OnSubmit;

            if (_cancelAction != null)
                _cancelAction.performed += OnCancel;
        }

        private void UnbindInput()
        {
            if (_navigateAction != null)
                _navigateAction.performed -= OnNavigate;

            if (_submitAction != null)
                _submitAction.performed -= OnSubmit;

            if (_cancelAction != null)
                _cancelAction.performed -= OnCancel;

            _navigateAction = null;
            _submitAction = null;
            _cancelAction = null;
        }

        private void OnNavigate(InputAction.CallbackContext ctx)
        {
            if (_activePlayer == null)
                return;

            Vector2 input = ctx.ReadValue<Vector2>();

            bool wasNotPushedX = Mathf.Abs(_previousNavigateInput.x) < Threshold;
            bool isPushedNowX = Mathf.Abs(input.x) >= Threshold;
            bool cooldownExpired = Time.time - _lastNavigateTime >= NavigateCooldown;

            bool shouldTrigger =
                (wasNotPushedX && isPushedNowX)
                || (isPushedNowX && cooldownExpired);

            if (!shouldTrigger)
            {
                _previousNavigateInput = input;
                return;
            }

            int direction = input.x > 0f ? 1 : -1;

            SetScoreIndex(_currentScoreIndex + direction);

            _lastNavigateTime = Time.time;
            _previousNavigateInput = input;
        }

        private void OnSubmit(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed)
                return;

            ConfirmAndClose();
        }

        private void OnCancel(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed)
                return;

            ConfirmAndClose();
        }

        private void ConfirmAndClose()
        {
            if (_activePlayer == null)
                return;

            Logs.Log($"[LobbySettingsPanel] Closed by Player {_activePlayer.PlayerIndex + 1}.");

            _onClosed?.Invoke(_activePlayer);
        }

        private void SetScoreIndex(int index)
        {
            if (_scoreSteps == null || _scoreSteps.Length == 0)
                return;

            _currentScoreIndex = WrapIndex(index, _scoreSteps.Length);
            ApplyCurrentScore();
        }

        private void ApplyCurrentScore()
        {
            if (_settingsData == null)
                return;

            if (_scoreSteps == null || _scoreSteps.Length == 0)
                return;

            int score = _scoreSteps[_currentScoreIndex];
            _settingsData.SetScoreToWin(score);

            RefreshFeedback();

            Logs.Log($"[LobbySettingsPanel] ScoreToWin = {score}");
        }

        private void RefreshFeedback()
        {
            if (_scoreStepIndicators == null)
                return;

            for (int i = 0; i < _scoreStepIndicators.Length; i++)
            {
                if (_scoreStepIndicators[i] == null)
                    continue;

                _scoreStepIndicators[i].SetActive(i == _currentScoreIndex);
            }
        }

        private int FindClosestScoreIndex(int score)
        {
            if (_scoreSteps == null || _scoreSteps.Length == 0)
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