using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public sealed class LobbyJoinController : MonoBehaviour, IPlayerUIInputHandler
    {
        [Header("References")]
        [SerializeField] private LobbySandboxController _sandboxController;

        [Header("Join")]
        [SerializeField] private int _maxPlayers = 4;
        [SerializeField] private bool _enableJoiningOnLobbyEnter = true;

        [Tooltip("Si true, les PlayerManager déjà présents quand le lobby se charge doivent appuyer sur A pour entrer dans le sandbox.")]
        [SerializeField] private bool _existingPlayersMustPressSubmit;

        [Tooltip("Si true, les nouveaux PlayerManager créés dans le lobby sont acceptés directement.")]
        [SerializeField] private bool _autoAcceptNewPlayersCreatedInLobby = true;

        [Header("Testing")]
        [Tooltip("Mode debug/test : crée automatiquement un joueur pour chaque manette déjà branchée en entrant dans le lobby.")]
        [SerializeField] private bool _autoJoinConnectedGamepads;

        [Header("Debug")]
        [SerializeField] private bool _showDebugLogs;
        [SerializeField] private bool _debugInputState;

        private readonly HashSet<PlayerManager> _knownPlayers = new();
        private readonly HashSet<PlayerManager> _pendingPlayers = new();
        private readonly HashSet<PlayerManager> _acceptedPlayers = new();

        private readonly List<PlayerManager> _playersBuffer = new();

        private LobbyService _lobbyService;
        private PlayerInputBridge _bridge;
        private CancellationTokenSource _initializeCancellation;

        private bool _isInitialized;
        private bool _eventsSubscribed;

        public event Action<PlayerManager> OnPlayerAccepted;
        public event Action OnPromptStateChanged;

        private PlayerUIInputService UIInputService =>
            ServiceManager.Instance?.Get<PlayerUIInputService>();

        private void OnEnable()
        {
            StartInitialization();
        }

        private void OnDisable()
        {
            CancelInitialization();
            UnsubscribeEvents();
            ClearRuntimeState();

            _isInitialized = false;

            if (_bridge)
                _bridge.CanJoin(false);
        }

        private void OnDestroy()
        {
            CancelInitialization();
            UnsubscribeEvents();
            ClearRuntimeState();

            OnPlayerAccepted = null;
            OnPromptStateChanged = null;
        }

        private void StartInitialization()
        {
            CancelInitialization();

            _initializeCancellation = new CancellationTokenSource();
            InitializeAsync(_initializeCancellation.Token).Forget();
        }

        private void CancelInitialization()
        {
            if (_initializeCancellation is null)
                return;

            _initializeCancellation.Cancel();
            _initializeCancellation.Dispose();
            _initializeCancellation = null;
        }

        private async UniTaskVoid InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                bool resolved = await WaitForDependenciesAsync(cancellationToken);

                if (!resolved)
                {
                    Logs.LogError("[LobbyJoinController] Could not resolve required dependencies.", this);
                    return;
                }

                ValidateReferences();
                SubscribeEvents();

                _isInitialized = true;

                SeedExistingPlayers();

                RefreshJoinAvailability();

                if (_debugInputState)
                    _bridge.DebugLogInputState();

                if (_autoJoinConnectedGamepads)
                    AutoJoinConnectedGamepads();

                NotifyPromptStateChanged();

                DebugLog("[LobbyJoinController] Initialized.");
            }
            catch (OperationCanceledException)
            {
                // Safe cancellation when the object is disabled/destroyed.
            }
        }

        private async UniTask<bool> WaitForDependenciesAsync(CancellationToken cancellationToken)
        {
            const int maxFramesToWait = 120;

            for (int i = 0; i < maxFramesToWait; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (ResolveDependencies())
                    return true;

                await UniTask.Yield();

                cancellationToken.ThrowIfCancellationRequested();
            }

            return ResolveDependencies();
        }

        private bool ResolveDependencies()
        {
            _bridge = PlayerInputBridge.Instance;
            _lobbyService = ServiceManager.Instance?.Get<LobbyService>();

            return _bridge && _lobbyService is not null;
        }

        private void ValidateReferences()
        {
            if (!_sandboxController)
                Logs.LogError("[LobbyJoinController] SandboxController reference is missing.", this);

            if (_bridge)
                _bridge.ValidateMaxPlayers(_maxPlayers);
        }

        private void SubscribeEvents()
        {
            if (_eventsSubscribed)
                return;

            if (_lobbyService is not null)
            {
                _lobbyService.OnPlayerJoined += HandlePlayerJoined;
                _lobbyService.OnPlayerLeft += HandlePlayerLeft;
            }

            InputSystem.onDeviceChange += HandleDeviceChange;

            _eventsSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_eventsSubscribed)
                return;

            if (_lobbyService is not null)
            {
                _lobbyService.OnPlayerJoined -= HandlePlayerJoined;
                _lobbyService.OnPlayerLeft -= HandlePlayerLeft;
            }

            InputSystem.onDeviceChange -= HandleDeviceChange;

            _eventsSubscribed = false;
        }

        private void SeedExistingPlayers()
        {
            if (_lobbyService is null)
                return;

            _playersBuffer.Clear();

            var players = _lobbyService.GetPlayers();

            for (int i = 0; i < players.Count; i++)
            {
                AddPlayerIfValid(_playersBuffer, players[i]);
            }

            for (int i = 0; i < _playersBuffer.Count; i++)
            {
                RegisterKnownPlayer(_playersBuffer[i], isExistingPlayer: true);
            }

            _playersBuffer.Clear();
        }

        private void HandlePlayerJoined(PlayerManager player)
        {
            RegisterKnownPlayer(player, isExistingPlayer: false);

            RefreshJoinAvailability();
            NotifyPromptStateChanged();
            
            player.Character.ActivateRoundAugments();
        }

        private void HandlePlayerLeft(PlayerManager player)
        {
            RemovePlayerFromRuntimeState(player);

            RefreshJoinAvailability();
            NotifyPromptStateChanged();
        }

        private void RegisterKnownPlayer(PlayerManager player, bool isExistingPlayer)
        {
            if (!player)
                return;

            if (!_knownPlayers.Add(player))
                return;

            if (isExistingPlayer)
            {
                if (_existingPlayersMustPressSubmit)
                    RegisterPendingPlayer(player);
                else
                    AcceptPlayer(player);

                return;
            }

            if (_autoAcceptNewPlayersCreatedInLobby)
                AcceptPlayer(player);
            else
                RegisterPendingPlayer(player);
        }

        private void RegisterPendingPlayer(PlayerManager player)
        {
            if (!player)
                return;

            if (_acceptedPlayers.Contains(player))
                return;

            if (!_pendingPlayers.Add(player))
                return;

            player.SetControlContext(PlayerControlContext.Lobby);

            UIInputService?.Push(player, this);

            DebugLog($"[LobbyJoinController] Player {GetSafeDisplayIndex(player)} is waiting to join.");
        }

        private void AcceptPlayer(PlayerManager player)
        {
            if (!player)
                return;

            if (_acceptedPlayers.Contains(player))
                return;

            if (_acceptedPlayers.Count >= _maxPlayers)
            {
                Logs.LogWarning($"[LobbyJoinController] Cannot accept Player {GetSafeDisplayIndex(player)}: max player count reached.", this);
                return;
            }

            _knownPlayers.Add(player);
            _pendingPlayers.Remove(player);

            UIInputService?.Remove(player, this);

            _acceptedPlayers.Add(player);

            DebugLog($"[LobbyJoinController] Player {GetSafeDisplayIndex(player)} joined the lobby.");

            if (!_sandboxController)
            {
                Logs.LogError("[LobbyJoinController] Cannot spawn accepted player because SandboxController reference is missing.", this);
                return;
            }

            _sandboxController.SpawnJoinedPlayer(player);
            _sandboxController.ApplyCurrentContextToPlayer(player);

            OnPlayerAccepted?.Invoke(player);

            RefreshJoinAvailability();
            NotifyPromptStateChanged();
        }

        private void RemovePlayerFromRuntimeState(PlayerManager player)
        {
            if (!player)
                return;

            _knownPlayers.Remove(player);
            _pendingPlayers.Remove(player);
            _acceptedPlayers.Remove(player);

            UIInputService?.Remove(player, this);
        }

        private void ClearRuntimeState()
        {
            foreach (var player in _pendingPlayers)
            {
                if (player)
                    UIInputService?.Remove(player, this);
            }

            _knownPlayers.Clear();
            _pendingPlayers.Clear();
            _acceptedPlayers.Clear();
            _playersBuffer.Clear();
        }

        private void RefreshJoinAvailability()
        {
            if (!_bridge)
                return;

            if (!_enableJoiningOnLobbyEnter)
            {
                _bridge.CanJoin(false);
                return;
            }

            bool canJoin = GetKnownPlayerCount() < _maxPlayers;
            _bridge.CanJoin(canJoin);
        }

        private void AutoJoinConnectedGamepads()
        {
            if (!_bridge)
                return;

            DebugLog("[LobbyJoinController] Auto-joining connected gamepads for testing.");

            _bridge.JoinAllUnpairedGamepads();

            if (_debugInputState)
                _bridge.DebugLogInputState();
        }

        public bool ShouldShowPromptForSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _maxPlayers) return false;
            
            if (slotIndex == 0 && !IsPlayerIndexAccepted(slotIndex)) return true;
            
            if (slotIndex >= GetConnectedGamepadCount()) return false;
                

            return !IsPlayerIndexAccepted(slotIndex);
        }

        private bool IsPlayerIndexAccepted(int playerIndex)
        {
            foreach (var player in _acceptedPlayers)
            {
                if (!player)
                    continue;

                if (player.PlayerIndex == playerIndex)
                    return true;
            }

            return false;
        }

        private int GetConnectedGamepadCount()
        {
            int count = 0;

            for (int i = 0; i < Gamepad.all.Count; i++)
            {
                var gamepad = Gamepad.all[i];

                if (gamepad is null)
                    continue;

                if (!gamepad.added)
                    continue;

                count++;
            }

            return Mathf.Min(count, _maxPlayers);
        }
        

        private int GetKnownPlayerCount()
        {
            int count = 0;

            foreach (var player in _knownPlayers)
            {
                if (player)
                    count++;
            }

            return count;
        }

        private void HandleDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is not Gamepad)
                return;

            switch (change)
            {
                case InputDeviceChange.Added:
                case InputDeviceChange.Removed:
                case InputDeviceChange.Disconnected:
                case InputDeviceChange.Reconnected:
                    RefreshJoinAvailability();
                    NotifyPromptStateChanged();
                    break;
            }
        }

        private void NotifyPromptStateChanged()
        {
            OnPromptStateChanged?.Invoke();
        }

        private static void AddPlayerIfValid(List<PlayerManager> result, PlayerManager player)
        {
            if (!player)
                return;

            if (result.Contains(player))
                return;

            result.Add(player);
        }

        private static string GetSafeDisplayIndex(PlayerManager player)
        {
            if (!player)
                return "?";

            int playerIndex = player.PlayerIndex;

            if (playerIndex < 0)
                return "?";

            return (playerIndex + 1).ToString();
        }

        private void DebugLog(string message)
        {
            if (!_showDebugLogs)
                return;

            Logs.Log(message, this);
        }

        public bool CanHandleUIInput(PlayerManager player)
        {
            return _isInitialized &&
                   player &&
                   _pendingPlayers.Contains(player) &&
                   player.CurrentPermissions.CanConfirmUI;
        }

        public bool HandleNavigate(PlayerManager player, Vector2 direction)
        {
            return false;
        }

        public bool HandleSubmit(PlayerManager player)
        {
            if (!CanHandleUIInput(player))
                return false;

            AcceptPlayer(player);
            return true;
        }

        public bool HandleCancel(PlayerManager player)
        {
            return false;
        }
    }
}