using System;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public sealed class LobbyCustomizationPanel : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject _root;

        [Header("Customization Limits")]
        [SerializeField] private int _skinCount = 4;
        [SerializeField] private int _faceColumnCount = 4;
        [SerializeField] private int _faceRowCount = 4;

        [Header("Input")]
        [SerializeField] private string _navigateActionName = "Navigate";
        [SerializeField] private string _submitActionName = "Submit";
        [SerializeField] private string _cancelActionName = "Cancel";

        private PlayerManager _activePlayer;
        private Action<PlayerManager> _onConfirmed;

        private InputAction _navigateAction;
        private InputAction _submitAction;
        private InputAction _cancelAction;

        private int _currentSkinIndex;
        private int _currentFaceColumn;
        private int _currentFaceRow;

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

        public void Open(PlayerManager player, Action<PlayerManager> onConfirmed)
        {
            if (player == null)
                return;

            _activePlayer = player;
            _onConfirmed = onConfirmed;

            _currentSkinIndex = player.SkinIndex;
            _currentFaceColumn = player.FaceColumn;
            _currentFaceRow = player.FaceRow;

            if (_root != null)
            {
                _root.SetActive(true);
            }

            BindInput();

            ApplyCurrentCustomization();

            Logs.Log($"[LobbyCustomizationPanel] Opened for Player {player.PlayerIndex + 1}.");
        }

        public void Close()
        {
            UnbindInput();

            if (_root != null)
            {
                _root.SetActive(false);
            }

            _activePlayer = null;
            _onConfirmed = null;
            _previousNavigateInput = Vector2.zero;
        }

        public void NextSkin()
        {
            SetSkin(_currentSkinIndex + 1);
        }

        public void PreviousSkin()
        {
            SetSkin(_currentSkinIndex - 1);
        }

        public void NextFaceColumn()
        {
            SetFaceColumn(_currentFaceColumn + 1);
        }

        public void PreviousFaceColumn()
        {
            SetFaceColumn(_currentFaceColumn - 1);
        }

        public void NextFaceRow()
        {
            SetFaceRow(_currentFaceRow + 1);
        }

        public void PreviousFaceRow()
        {
            SetFaceRow(_currentFaceRow - 1);
        }

        public void Confirm()
        {
            if (_activePlayer == null)
                return;

            Logs.Log($"[LobbyCustomizationPanel] Confirmed customization for Player {_activePlayer.PlayerIndex + 1}.");

            _onConfirmed?.Invoke(_activePlayer);
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

            bool wasNotPushedY = Mathf.Abs(_previousNavigateInput.y) < Threshold;
            bool isPushedNowY = Mathf.Abs(input.y) >= Threshold;

            bool cooldownExpired = Time.time - _lastNavigateTime >= NavigateCooldown;

            bool shouldTrigger =
                ((wasNotPushedX && isPushedNowX) || (wasNotPushedY && isPushedNowY))
                || ((isPushedNowX || isPushedNowY) && cooldownExpired);

            if (!shouldTrigger)
            {
                _previousNavigateInput = input;
                return;
            }

            if (isPushedNowX)
            {
                int direction = input.x > 0f ? 1 : -1;
                SetSkin(_currentSkinIndex + direction);
            }

            if (isPushedNowY)
            {
                int direction = input.y > 0f ? 1 : -1;
                SetFaceRow(_currentFaceRow + direction);
            }

            _lastNavigateTime = Time.time;
            _previousNavigateInput = input;
        }

        private void OnSubmit(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed)
                return;

            Confirm();
        }

        private void OnCancel(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed)
                return;

            Confirm();
        }

        private void SetSkin(int skinIndex)
        {
            _currentSkinIndex = WrapIndex(skinIndex, _skinCount);
            ApplyCurrentCustomization();
        }

        private void SetFaceColumn(int faceColumn)
        {
            _currentFaceColumn = WrapIndex(faceColumn, _faceColumnCount);
            ApplyCurrentCustomization();
        }

        private void SetFaceRow(int faceRow)
        {
            _currentFaceRow = WrapIndex(faceRow, _faceRowCount);
            ApplyCurrentCustomization();
        }

        private void ApplyCurrentCustomization()
        {
            if (_activePlayer == null)
                return;

            _activePlayer.Customization.SetCustomization(
                _currentSkinIndex,
                _currentFaceColumn,
                _currentFaceRow
            );

            _activePlayer.Character?.RefreshCustomizationFromOwner();

            Logs.Log(
                $"[LobbyCustomizationPanel] Player {_activePlayer.PlayerIndex + 1} customization: " +
                $"Skin={_currentSkinIndex}, FaceColumn={_currentFaceColumn}, FaceRow={_currentFaceRow}"
            );
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