using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public sealed class LobbyCustomizationController : MonoBehaviour, IPlayerUIInputHandler
    {
        private enum CustomizationRow
        {
            Face,
            Hat
        }

        private enum CustomizationSide
        {
            Left,
            Right
        }

        private enum NavigationAxis
        {
            None,
            Horizontal,
            Vertical
        }

        [Header("Root")]
        [SerializeField] private GameObject _root;

        [Header("Preview")]
        [SerializeField] private LobbyCustomizationPreview _preview;

        [Header("Arrows - Face")]
        [SerializeField] private Graphic _faceLeftArrow;
        [SerializeField] private Graphic _faceRightArrow;

        [Header("Arrows - Hat")]
        [SerializeField] private Graphic _hatLeftArrow;
        [SerializeField] private Graphic _hatRightArrow;

        [Header("Arrow Visuals")]
        [SerializeField] private Color _normalArrowColor = Color.white;
        [SerializeField] private Color _highlightArrowColor = Color.yellow;

        [Header("Customization Limits")]
        [SerializeField] private int _skinCount = 4;
        [SerializeField] private int _faceColumnCount = 4;
        [SerializeField] private int _faceRowCount = 3;

        [Header("Navigation")]
        [SerializeField] private float _navigationThreshold = 0.3f;
        [SerializeField] private float _navigationCooldown = 0.2f;

        private PlayerManager _activePlayer;
        private Action<PlayerManager> _onConfirmed;

        private int _currentSkinIndex;
        private int _currentFaceColumn;
        private int _currentFaceRow;

        private CustomizationRow _activeRow = CustomizationRow.Face;
        private CustomizationSide _activeSide = CustomizationSide.Left;

        private NavigationAxis _previousAxis = NavigationAxis.None;
        private int _previousDirection;
        private float _lastNavigateTime;

        private bool _isOpen;
        private bool _canNavigate;

        private CancellationTokenSource _panelCancellation;
        private int _visualVersion;

        private PlayerUIInputService UIInputService =>
            ServiceManager.Instance?.Get<PlayerUIInputService>();

        private void Awake()
        {
            if (_root)
                _root.SetActive(false);

            EnableArrows(false);
            UpdateArrowHighlights();
        }

        private void OnDisable()
        {
            RemoveFromUIInputService();
            CancelPanelTasks();
        }

        private void OnDestroy()
        {
            RemoveFromUIInputService();
            CancelPanelTasks();
        }

        public void Open(PlayerManager player, Action<PlayerManager> onConfirmed)
        {
            if (!player)
                return;

            CancelPanelTasks();
            EnableArrows(true);

            _panelCancellation = new CancellationTokenSource();

            _activePlayer = player;
            _onConfirmed = onConfirmed;
            _isOpen = true;
            _canNavigate = false;

            _currentSkinIndex = player.SkinIndex;
            _currentFaceColumn = player.FaceColumn;
            _currentFaceRow = player.FaceRow;

            _activeRow = CustomizationRow.Face;
            _activeSide = CustomizationSide.Left;

            _previousAxis = NavigationAxis.None;
            _previousDirection = 0;
            _lastNavigateTime = 0f;

            if (_root)
                _root.SetActive(true);

            ApplyCurrentCustomization();
            UpdateArrowHighlights();

            RegisterToUIInputService();

            int version = ++_visualVersion;

            OpenVisualsAsync(version, _panelCancellation.Token).Forget();

            Logs.Log($"[LobbyCustomizationPanel] Opened for Player {player.PlayerIndex + 1}.");
        }

        private async UniTaskVoid OpenVisualsAsync(int version, CancellationToken ct)
        {
            try
            {
                if (_preview && _activePlayer)
                {
                    await _preview.ShowAsync(
                        _activePlayer.Customization,
                        ct
                    );
                }

                if (version != _visualVersion)
                    return;

                _canNavigate = true;
            }
            catch (OperationCanceledException)
            { }
        }
        
        public void Close() => CloseAsync(CancellationToken.None).Forget();
        
        public async UniTask CloseAsync(CancellationToken cancellationToken)
        {
            if (!_isOpen)
                return;

            RemoveFromUIInputService();
            EnableArrows(false);

            _canNavigate = false;

            LobbyCustomizationPreview preview = _preview;
            GameObject root = _root;

            _panelCancellation?.Cancel();
            _panelCancellation?.Dispose();
            _panelCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            CancellationToken ct = _panelCancellation.Token;

            _activePlayer = null;
            _onConfirmed = null;
            _previousAxis = NavigationAxis.None;
            _previousDirection = 0;
            _lastNavigateTime = 0f;
            _isOpen = false;

            try
            {
                if (preview)
                    await preview.HideAsync(ct);
            }
            catch (OperationCanceledException)
            { }

            if (root)
                root.SetActive(false);
        }

        public void Confirm()
        {
            if (!_activePlayer)
                return;

            PlayerManager player = _activePlayer;
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

        private void CancelPanelTasks()
        {
            _panelCancellation?.Cancel();
            _panelCancellation?.Dispose();
            _panelCancellation = null;
        }

        private bool TryProcessNavigation(Vector2 input)
        {
            if (!TryGetNavigation(input, out var axis, out int direction))
            {
                _previousAxis = NavigationAxis.None;
                _previousDirection = 0;
                return false;
            }

            bool isSameDirection =
                axis == _previousAxis &&
                direction == _previousDirection;

            bool cooldownExpired =
                Time.unscaledTime - _lastNavigateTime >= _navigationCooldown;

            if (isSameDirection && !cooldownExpired)
                return false;

            if (axis == NavigationAxis.Horizontal)
                HandleHorizontalNavigation(direction);
            else if (axis == NavigationAxis.Vertical)
                HandleVerticalNavigation(direction);

            _previousAxis = axis;
            _previousDirection = direction;
            _lastNavigateTime = Time.unscaledTime;

            return true;
        }

        private bool TryGetNavigation(Vector2 input, out NavigationAxis axis, out int direction)
        {
            axis = NavigationAxis.None;
            direction = 0;

            float absX = Mathf.Abs(input.x);
            float absY = Mathf.Abs(input.y);

            if (absX < _navigationThreshold && absY < _navigationThreshold)
                return false;

            if (absX >= absY)
            {
                axis = NavigationAxis.Horizontal;
                direction = input.x > 0f ? 1 : -1;
                return true;
            }

            axis = NavigationAxis.Vertical;
            direction = input.y > 0f ? 1 : -1;
            return true;
        }

        private void HandleHorizontalNavigation(int direction)
        {
            _activeSide = direction < 0
                ? CustomizationSide.Left
                : CustomizationSide.Right;

            if (_activeRow == CustomizationRow.Face)
            {
                int currentFaceIndex = GetCurrentFaceLinearIndex();
                SetFaceLinearIndex(currentFaceIndex + direction);
            }
            else
            {
                SetSkin(_currentSkinIndex + direction);
            }

            UpdateArrowHighlights();
        }

        private void HandleVerticalNavigation(int direction)
        {
            _activeRow = direction > 0
                ? CustomizationRow.Hat
                : CustomizationRow.Face;

            UpdateArrowHighlights();
        }

        private int GetCurrentFaceLinearIndex()
        {
            return _currentFaceRow * _faceColumnCount + _currentFaceColumn;
        }

        private void SetFaceLinearIndex(int faceIndex)
        {
            int faceCount = Mathf.Max(1, _faceColumnCount * _faceRowCount);
            faceIndex = WrapIndex(faceIndex, faceCount);

            _currentFaceColumn = faceIndex % _faceColumnCount;
            _currentFaceRow = faceIndex / _faceColumnCount;

            ApplyCurrentCustomization();
        }

        private void SetSkin(int skinIndex)
        {
            _currentSkinIndex = WrapIndex(skinIndex, _skinCount);
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

            if (_preview)
                _preview.Apply(_activePlayer.Customization);
        }

        private void EnableArrows(bool enable)
        {
            _faceLeftArrow.enabled = enable;
            _faceRightArrow.enabled = enable;
            _hatLeftArrow.enabled = enable;
            _hatRightArrow.enabled = enable;
        }

        private void UpdateArrowHighlights()
        {
            SetArrowHighlighted(
                _faceLeftArrow,
                _activeRow == CustomizationRow.Face &&
                _activeSide == CustomizationSide.Left
            );

            SetArrowHighlighted(
                _faceRightArrow,
                _activeRow == CustomizationRow.Face &&
                _activeSide == CustomizationSide.Right
            );

            SetArrowHighlighted(
                _hatLeftArrow,
                _activeRow == CustomizationRow.Hat &&
                _activeSide == CustomizationSide.Left
            );

            SetArrowHighlighted(
                _hatRightArrow,
                _activeRow == CustomizationRow.Hat &&
                _activeSide == CustomizationSide.Right
            );
        }

        private void SetArrowHighlighted(Graphic arrow, bool highlighted)
        {
            if (!arrow)
                return;

            arrow.color = highlighted
                ? _highlightArrowColor
                : _normalArrowColor;
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

            if (!_canNavigate)
                return true;

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