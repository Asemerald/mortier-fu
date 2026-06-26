using System;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbyCustomizationPanel : MonoBehaviour, IPlayerUIInputHandler
    {
        [Header("Root")]
        [SerializeField] private GameObject _root;

        [Header("Customization Limits")]
        [SerializeField] private int _skinCount = 4;
        [SerializeField] private int _faceColumnCount = 4;
        [SerializeField] private int _faceRowCount = 4;

        [Header("Navigation")]
        [SerializeField] private float _navigationThreshold = 0.7f;
        [SerializeField] private float _navigationCooldown = 0.25f;

        private PlayerManager _activePlayer;
        private Action<PlayerManager> _onConfirmed;

        private int _currentSkinIndex;
        private int _currentFaceColumn;
        private int _currentFaceRow;

        private Vector2 _previousNavigateInput = Vector2.zero;
        private float _lastNavigateTime;

        private bool _isOpen;

        private PlayerUIInputService UIInputService =>
            ServiceManager.Instance?.Get<PlayerUIInputService>();

        private void Awake()
        {
            if (_root)
                _root.SetActive(false);
        }

        private void OnDisable()
        {
            RemoveFromUIInputService();
        }

        private void OnDestroy()
        {
            RemoveFromUIInputService();
        }

        public void Open(PlayerManager player, Action<PlayerManager> onConfirmed)
        {
            if (!player)
                return;

            _activePlayer = player;
            _onConfirmed = onConfirmed;
            _isOpen = true;

            _currentSkinIndex = player.SkinIndex;
            _currentFaceColumn = player.FaceColumn;
            _currentFaceRow = player.FaceRow;

            _previousNavigateInput = Vector2.zero;
            _lastNavigateTime = 0f;

            if (_root)
                _root.SetActive(true);

            ApplyCurrentCustomization();
            RegisterToUIInputService();

            Logs.Log($"[LobbyCustomizationPanel] Opened for Player {player.PlayerIndex + 1}.");
        }

        public void Close()
        {
            if (!_isOpen)
                return;

            RemoveFromUIInputService();

            if (_root)
                _root.SetActive(false);

            _activePlayer = null;
            _onConfirmed = null;
            _previousNavigateInput = Vector2.zero;
            _lastNavigateTime = 0f;
            _isOpen = false;
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
            if (!_activePlayer)
                return;

            var player = _activePlayer;
            var onConfirmed = _onConfirmed;

            Logs.Log($"[LobbyCustomizationPanel] Confirmed customization for Player {player.PlayerIndex + 1}.");

            onConfirmed?.Invoke(player);
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

        private bool TryProcessNavigation(Vector2 input)
        {
            bool wasNotPushedX = Mathf.Abs(_previousNavigateInput.x) < _navigationThreshold;
            bool isPushedNowX = Mathf.Abs(input.x) >= _navigationThreshold;

            bool wasNotPushedY = Mathf.Abs(_previousNavigateInput.y) < _navigationThreshold;
            bool isPushedNowY = Mathf.Abs(input.y) >= _navigationThreshold;

            bool cooldownExpired = Time.unscaledTime - _lastNavigateTime >= _navigationCooldown;

            bool shouldTrigger =
                ((wasNotPushedX && isPushedNowX) || (wasNotPushedY && isPushedNowY))
                || ((isPushedNowX || isPushedNowY) && cooldownExpired);

            if (!shouldTrigger)
            {
                _previousNavigateInput = input;
                return false;
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

            _lastNavigateTime = Time.unscaledTime;
            _previousNavigateInput = input;

            return true;
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
            if (!_activePlayer)
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

        public bool CanHandleUIInput(PlayerManager player)
        {
            return _isOpen &&
                   _activePlayer &&
                   ReferenceEquals(_activePlayer, player);
        }

        public bool HandleNavigate(PlayerManager player, Vector2 direction)
        {
            if (!CanHandleUIInput(player))
                return false;

            TryProcessNavigation(direction);
            return true;
        }

        public bool HandleSubmit(PlayerManager player)
        {
            if (!CanHandleUIInput(player))
                return false;

            Confirm();
            return true;
        }

        public bool HandleCancel(PlayerManager player)
        {
            if (!CanHandleUIInput(player))
                return false;

            Confirm();
            return true;
        }
    }
}