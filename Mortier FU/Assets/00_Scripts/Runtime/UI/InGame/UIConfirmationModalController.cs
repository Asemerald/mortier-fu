using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace MortierFu
{
    public sealed class UIConfirmationModalController : MonoBehaviour
    {
        private struct PlayerInputSnapshot
        {
            public PlayerManager Player;
            public PlayerControlContext Context;
            public InputSystemUIInputModule UiInputModule;
            public bool UnityEventSystemUIActive;
        }

        [Header("References")]
        [SerializeField] private UIConfirmationPanel _panel;
        [SerializeField] private UIConfirmationInputReceiver _inputReceiver;
        [SerializeField] private EventSystem _eventSystem;
        [SerializeField] private InputSystemUIInputModule _uiInputModule;

        private readonly List<PlayerInputSnapshot> _playerSnapshots = new();

        private UIConfirmationRequest _currentRequest;
        private CancellationTokenSource _lifetimeCancellation;

        private GameObject _previousSelectedObject;
        private float _previousTimeScale = 1f;
        private bool _hasPreviousTimeScale;

        private bool _isOpen;
        private bool _isProcessing;
        private bool _shouldRestoreOnDestroy;

        private void Awake()
        {
            ResolveReferences();
            _panel?.HideInstant();
        }

        private void OnDestroy()
        {
            CancelLifetime();
            ClearSelectedObject();

            if (!_shouldRestoreOnDestroy)
                return;

            RestorePlayersFromSnapshots();
            RestoreTimeScale();
        }

        public bool TryOpen(UIConfirmationRequest request)
        {
            if (_isOpen || _isProcessing)
            {
                Logs.LogWarning("[UIConfirmationModalController] Cannot open because a confirmation is already active.");
                return false;
            }

            if (!ValidateRequest(request))
                return false;

            _currentRequest = request;
            _isOpen = true;
            _isProcessing = false;
            _shouldRestoreOnDestroy = true;

            CancelLifetime();
            _lifetimeCancellation = new CancellationTokenSource();

            _previousSelectedObject = _eventSystem.currentSelectedGameObject;

            CapturePlayersIfNeeded();
            PauseGameIfNeeded();
            ApplyModalInputState();

            OpenAsync(_lifetimeCancellation.Token).Forget();

            return true;
        }

        public void RequestSubmitFromInput()
        {
            if (!_isOpen || _isProcessing)
                return;

            SubmitAsync().Forget();
        }

        public void RequestCancelFromInput()
        {
            if (!_isOpen || _isProcessing)
                return;

            CancelAsync().Forget();
        }

        public void ForceCloseInstant(bool restorePlayers = true)
        {
            CancelLifetime();
            ClearSelectedObject();

            if (_panel)
                _panel.HideInstant();

            if (restorePlayers)
            {
                RestorePlayersFromSnapshots();
                RestoreTimeScale();
            }
            else
            {
                DiscardSnapshots();
                ForgetTimeScaleSnapshot();
            }

            ClearRuntimeState();
        }

        private async UniTaskVoid OpenAsync(CancellationToken cancellationToken)
        {
            _panel.Configure(_currentRequest.Description, _currentRequest.ConfirmLabel, _currentRequest.CancelLabel);

            try
            {
                await _panel.OpenAsync(cancellationToken);

                SelectInputReceiver();

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                SelectInputReceiver();
            }
            catch (OperationCanceledException)
            { }
        }

        private async UniTaskVoid SubmitAsync()
        {
            _isProcessing = true;

            ClearSelectedObject();

            if (_panel)
                _panel.HideInstant();

            if (_currentRequest.ResumeTimeScaleOnConfirm)
                Time.timeScale = 1f;

            if (_currentRequest.RestoreContextOnConfirm)
            {
                RestorePlayersFromSnapshots();
                RestoreTimeScale();
            }
            else
            {
                DiscardSnapshots();
                ForgetTimeScaleSnapshot();
            }

            _shouldRestoreOnDestroy = false;

            try
            {
                if (_currentRequest.OnConfirmAsync != null)
                    await _currentRequest.OnConfirmAsync();
            }
            finally
            {
                ClearRuntimeState();
            }
        }

        private async UniTaskVoid CancelAsync()
        {
            _isProcessing = true;

            try
            {
                if (_lifetimeCancellation == null)
                    _lifetimeCancellation = new CancellationTokenSource();

                await _panel.CloseAsync(_lifetimeCancellation.Token);

                ClearSelectedObject();

                RestorePlayersFromSnapshots();
                RestoreTimeScale();
                RestorePreviousSelectedObject();

                if (_currentRequest.OnCancelAfterCloseAsync != null)
                    await _currentRequest.OnCancelAfterCloseAsync();
            }
            catch (OperationCanceledException)
            { }
            finally
            {
                ClearRuntimeState();
            }
        }

        private void ResolveReferences()
        {
            if (!_panel)
                _panel = GetComponent<UIConfirmationPanel>();

            if (!_inputReceiver)
                _inputReceiver = GetComponentInChildren<UIConfirmationInputReceiver>(true);

            ResolveEventSystemReferences();
        }

        private bool ValidateRequest(UIConfirmationRequest request)
        {
            if (!request.Owner)
            {
                Logs.LogError("[UIConfirmationModalController] Cannot open confirmation without owner.");
                return false;
            }

            if (!_panel || !_inputReceiver)
            {
                Logs.LogError("[UIConfirmationModalController] Confirmation panel references are missing.");
                return false;
            }

            ResolveEventSystemReferences();

            if (!_eventSystem || !_uiInputModule)
            {
                Logs.LogError("[UIConfirmationModalController] EventSystem or InputSystemUIInputModule is missing.");
                return false;
            }

            return true;
        }

        private void ResolveEventSystemReferences()
        {
            if (!_eventSystem)
                _eventSystem = EventSystem.current;

            if (!_uiInputModule && _eventSystem)
                _uiInputModule = _eventSystem.GetComponent<InputSystemUIInputModule>();
        }

        private void CapturePlayersIfNeeded()
        {
            _playerSnapshots.Clear();

            if (!_currentRequest.LockPlayersWhileOpen)
                return;

            LobbyService lobbyService = ServiceManager.Instance?.Get<LobbyService>();

            if (lobbyService == null)
            {
                Logs.LogWarning("[UIConfirmationModalController] LobbyService is missing. Only the owner input will be applied.");
                return;
            }

            IReadOnlyList<PlayerManager> players = lobbyService.GetPlayers();

            for (int i = 0; i < players.Count; i++)
            {
                PlayerManager player = players[i];

                if (!player)
                    continue;

                _playerSnapshots.Add(new PlayerInputSnapshot
                { 
                    Player = player,
                    Context = player.ControlContext,
                    UiInputModule = player.PlayerInput.uiInputModule,
                    UnityEventSystemUIActive = player.IsUnityEventSystemUIActive
                });
            }
        }

        private void ApplyModalInputState()
        {
            bool ownerApplied = false;

            if (_currentRequest.LockPlayersWhileOpen)
            {
                for (int i = 0; i < _playerSnapshots.Count; i++)
                {
                    PlayerManager player = _playerSnapshots[i].Player;

                    if (!player)
                        continue;

                    if (ReferenceEquals(player, _currentRequest.Owner))
                    {
                        ApplyOwnerModalInput(player);
                        ownerApplied = true;
                    }
                    else
                    {
                        ApplyBlockedInput(player);
                    }
                }
            }

            if (!ownerApplied)
                ApplyOwnerModalInput(_currentRequest.Owner);
        }

        private void ApplyOwnerModalInput(PlayerManager player)
        {
            if (!player)
                return;

            player.SetControlContext(PlayerControlContext.UIConfirmationOwner);
            player.PlayerInput.uiInputModule = _uiInputModule;
            player.SetUnityEventSystemUIActive(true);
        }

        private static void ApplyBlockedInput(PlayerManager player)
        {
            if (!player)
                return;

            player.SetControlContext(PlayerControlContext.UIBlocked);
            player.SetUnityEventSystemUIActive(true);
        }

        private void RestorePlayersFromSnapshots()
        {
            for (int i = 0; i < _playerSnapshots.Count; i++)
            {
                PlayerInputSnapshot snapshot = _playerSnapshots[i];

                if (!snapshot.Player)
                    continue;

                snapshot.Player.SetControlContext(snapshot.Context);
                snapshot.Player.SetUnityEventSystemUIActive(snapshot.UnityEventSystemUIActive);

                if (snapshot.UiInputModule)
                    snapshot.Player.PlayerInput.uiInputModule = snapshot.UiInputModule;
            }

            _playerSnapshots.Clear();
        }

        private void DiscardSnapshots() => _playerSnapshots.Clear();

        private void PauseGameIfNeeded()
        {
            if (!_currentRequest.PauseGameWhileOpen)
                return;

            _previousTimeScale = Time.timeScale;
            _hasPreviousTimeScale = true;

            Time.timeScale = 0f;
        }

        private void RestoreTimeScale()
        {
            if (!_hasPreviousTimeScale)
                return;

            Time.timeScale = _previousTimeScale;
            _hasPreviousTimeScale = false;
        }

        private void ForgetTimeScaleSnapshot() => _hasPreviousTimeScale = false;

        private void RestorePreviousSelectedObject()
        {
            if (!_eventSystem)
                return;

            if (!_previousSelectedObject || !_previousSelectedObject.activeInHierarchy)
                return;

            _eventSystem.SetSelectedGameObject(null);
            _eventSystem.SetSelectedGameObject(_previousSelectedObject);
        }

        private void SelectInputReceiver()
        {
            if (!_eventSystem || !_inputReceiver)
            {
                Logs.LogError("[UIConfirmationModalController] Cannot select input receiver because references are missing.");
                return;
            }

            GameObject receiverObject = _inputReceiver.gameObject;

            if (!receiverObject.activeInHierarchy)
            {
                Logs.LogError("[UIConfirmationModalController] Input receiver is not active.");
                return;
            }

            _eventSystem.SetSelectedGameObject(null);
            _eventSystem.SetSelectedGameObject(receiverObject);

            if (_eventSystem.currentSelectedGameObject != receiverObject)
                Logs.LogError("[UIConfirmationModalController] Input receiver selection failed.");
        }

        private void ClearSelectedObject()
        {
            if (!_eventSystem)
                return;

            _eventSystem.SetSelectedGameObject(null);
        }

        private void ClearRuntimeState()
        {
            _isOpen = false;
            _isProcessing = false;
            _shouldRestoreOnDestroy = false;
            _currentRequest = default;
            _previousSelectedObject = null;

            CancelLifetime();
        }

        private void CancelLifetime()
        {
            _lifetimeCancellation?.Cancel();
            _lifetimeCancellation?.Dispose();
            _lifetimeCancellation = null;
        }
    }
}