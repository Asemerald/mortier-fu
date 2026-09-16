using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;

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
        
        public bool IsActive => _isOpen || _isProcessing;

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

                if (!_isOpen || _isProcessing)
                    return;

                SelectInputReceiver();

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                if (!_isOpen || _isProcessing)
                    return;

                SelectInputReceiver();
            }
            catch (OperationCanceledException)
            { }
        }

        private async UniTaskVoid SubmitAsync()
        {
            _isProcessing = true;

            ClearSelectedObject();
            
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_GameplayUI_CountdownGo);

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

            if (_eventSystem && _uiInputModule) return true;
            
            Logs.LogError("[UIConfirmationModalController] EventSystem or InputSystemUIInputModule is missing.");
            return false;

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
            DetachModalInputModuleFromCapturedPlayers();

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
                        ApplyBlockedInput(player);
                }
            }

            if (!ownerApplied)
                ApplyOwnerModalInput(_currentRequest.Owner);
        }

        private void DetachModalInputModuleFromCapturedPlayers()
        {
            for (int i = 0; i < _playerSnapshots.Count; i++)
            {
                PlayerManager player = _playerSnapshots[i].Player;

                if (!player || !player.PlayerInput)
                    continue;

                // Encore un problème d'input partagé... il faut retirer toutes les anciennes attributions avant de donner le nouvel ownership.
                if (player.PlayerInput.uiInputModule == _uiInputModule)
                    player.PlayerInput.uiInputModule = null;
            }
        }
        
        private void ApplyOwnerModalInput(PlayerManager player)
        {
            if (!player || !player.PlayerInput)
                return;

            player.PlayerInput.uiInputModule = _uiInputModule;
            player.SetControlContext(_currentRequest.OwnerContext);

            BindNativeUIModuleToOwner(player);

            player.SetUnityEventSystemUIActive(true);
        }
        
        private void BindNativeUIModuleToOwner(PlayerManager player)
        {
            if (!_uiInputModule || !player || !player.PlayerInput || !player.PlayerInput.actions)
                return;

            // Voir PauseUI pour les explications
            InputActionAsset actions = player.PlayerInput.actions;
            InputActionMap uiMap = actions.FindActionMap(PlayerInputActionNames.UIMap, throwIfNotFound: false);

            if (uiMap == null)
            {
                Logs.LogError($"[UIConfirmationModalController] UI action map not found for Player {player.PlayerIndex + 1}.");
                return;
            }

            _uiInputModule.actionsAsset = actions;

            _uiInputModule.move = CreateUIActionReference(uiMap, PlayerInputActionNames.Navigate);
            _uiInputModule.submit = CreateUIActionReference(uiMap, PlayerInputActionNames.Submit);
            _uiInputModule.cancel = CreateUIActionReference(uiMap, PlayerInputActionNames.Cancel);

            _uiInputModule.enabled = false;
            _uiInputModule.enabled = true;
        }

        private static InputActionReference CreateUIActionReference(InputActionMap uiMap, string actionName)
        {
            InputAction action = uiMap.FindAction(actionName, throwIfNotFound: false);

            if (action != null) return InputActionReference.Create(action);
            Logs.LogError($"[UIConfirmationModalController] UI action '{actionName}' not found.");
            return null;

        }

        private static void ApplyBlockedInput(PlayerManager player)
        {
            if (!player)
                return;

            if (player.PlayerInput)
                player.PlayerInput.uiInputModule = null;

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

                if (snapshot.Player.PlayerInput)
                {
                    InputSystemUIInputModule moduleToRestore = snapshot.UiInputModule == _uiInputModule ? null : snapshot.UiInputModule;
                    snapshot.Player.PlayerInput.uiInputModule = moduleToRestore;
                }

                snapshot.Player.SetControlContext(snapshot.Context);
                snapshot.Player.SetUnityEventSystemUIActive(snapshot.UnityEventSystemUIActive);
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