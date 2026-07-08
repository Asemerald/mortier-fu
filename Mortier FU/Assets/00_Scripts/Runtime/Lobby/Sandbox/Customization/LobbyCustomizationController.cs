using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbyCustomizationController : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject _root;

        [Header("Preview")]
        [SerializeField] private LobbyCustomizationPreview _preview;

        [Header("Navigation")]
        [SerializeField] private UINavigationPanel _navigationPanel;

        [Header("Items")]
        [SerializeField] private UIIntStepperItem _faceItem;
        [SerializeField] private UIIntStepperItem _hatItem;

        [Header("Customization Limits")]
        [SerializeField] private int _skinCount = 4;
        [SerializeField] private int _faceColumnCount = 4;
        [SerializeField] private int _faceRowCount = 3;

        private PlayerManager _activePlayer;
        private Action<PlayerManager> _onConfirmed;

        private int _currentSkinIndex;
        private int _currentFaceColumn;
        private int _currentFaceRow;

        private bool _isOpen;

        private CancellationTokenSource _panelCancellation;
        private int _visualVersion;

        private void Awake()
        {
            if (_root)
                _root.SetActive(false);

            BindItems();
        }

        private void OnEnable() => BindItems();

        private void OnDisable()
        {
            UnbindItems();

            if (_navigationPanel)
                _navigationPanel.Close();

            CancelPanelTasks();
        }

        private void OnDestroy()
        {
            UnbindItems();

            if (_navigationPanel)
                _navigationPanel.Close();

            CancelPanelTasks();
        }

        public void Open(PlayerManager player, Action<PlayerManager> onConfirmed)
        {
            if (!player)
                return;

            CancelPanelTasks();

            _panelCancellation = new CancellationTokenSource();

            _activePlayer = player;
            _onConfirmed = onConfirmed;
            _isOpen = true;

            _currentSkinIndex = player.SkinIndex;
            _currentFaceColumn = player.FaceColumn;
            _currentFaceRow = player.FaceRow;

            ConfigureItems();
            RefreshItemsFromCurrentCustomization();

            if (_root)
                _root.SetActive(true);

            ApplyCurrentCustomization();

            if (_navigationPanel)
            {
                _navigationPanel.Open(player, ConfirmFromNavigation, ConfirmFromNavigation);

                _navigationPanel.SetCanNavigate(false);
            }

            int version = ++_visualVersion;

            OpenVisualsAsync(version, _panelCancellation.Token).Forget();

            Logs.Log($"[LobbyCustomizationPanel] Opened for Player {player.PlayerIndex + 1}.", this);
        }

        private async UniTaskVoid OpenVisualsAsync(int version, CancellationToken ct)
        {
            try
            {
                if (_preview && _activePlayer)
                    await _preview.ShowAsync(_activePlayer.Customization, ct);

                if (version != _visualVersion)
                    return;

                if (_navigationPanel)
                    _navigationPanel.SetCanNavigate(true);
            }
            catch (OperationCanceledException)
            { }
        }

        public void Close() => CloseAsync(CancellationToken.None).Forget();

        public async UniTask CloseAsync(CancellationToken cancellationToken)
        {
            if (!_isOpen)
                return;

            if (_navigationPanel)
                _navigationPanel.Close();

            LobbyCustomizationPreview preview = _preview;
            GameObject root = _root;

            _panelCancellation?.Cancel();
            _panelCancellation?.Dispose();
            _panelCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            CancellationToken ct = _panelCancellation.Token;

            _activePlayer = null;
            _onConfirmed = null;
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

        private void Confirm()
        {
            if (!_activePlayer)
                return;

            PlayerManager player = _activePlayer;
            var onConfirmed = _onConfirmed;

            Logs.Log($"[LobbyCustomizationPanel] Confirmed customization for Player {player.PlayerIndex + 1}.", this);

            onConfirmed?.Invoke(player);
        }

        private void ConfirmFromNavigation(PlayerManager player)
        {
            if (!_activePlayer)
                return;

            if (!ReferenceEquals(_activePlayer, player))
                return;

            Confirm();
        }

        private void ConfigureItems()
        {
            int faceCount = Mathf.Max(1, _faceColumnCount * _faceRowCount);
            int skinCount = Mathf.Max(1, _skinCount);

            if (_faceItem)
                _faceItem.ConfigureRange(0, faceCount - 1, 1, wrapValue: true);

            if (_hatItem)
                _hatItem.ConfigureRange(0, skinCount - 1, 1, wrapValue: true);
        }

        private void RefreshItemsFromCurrentCustomization()
        {
            if (_faceItem)
            {
                _faceItem.SetValue(GetCurrentFaceLinearIndex(), notify: false);
                _faceItem.ResetUsageFeedback();
            }

            if (!_hatItem) return;
            
            _hatItem.SetValue(_currentSkinIndex, notify: false);
            _hatItem.ResetUsageFeedback();
        }

        private void BindItems()
        {
            if (_faceItem)
            {
                _faceItem.OnValueChanged -= SetFaceLinearIndex;
                _faceItem.OnValueChanged += SetFaceLinearIndex;
            }

            if (!_hatItem) return;
            
            _hatItem.OnValueChanged -= SetSkin;
            _hatItem.OnValueChanged += SetSkin;
        }

        private void UnbindItems()
        {
            if (_faceItem)
                _faceItem.OnValueChanged -= SetFaceLinearIndex;

            if (_hatItem)
                _hatItem.OnValueChanged -= SetSkin;
        }

        private int GetCurrentFaceLinearIndex() => _currentFaceRow * _faceColumnCount + _currentFaceColumn;

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
            _currentSkinIndex = WrapIndex(skinIndex, Mathf.Max(1, _skinCount));
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

        private void CancelPanelTasks()
        {
            _panelCancellation?.Cancel();
            _panelCancellation?.Dispose();
            _panelCancellation = null;
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