using System;
using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public sealed class LobbyJoinController : MonoBehaviour, IPlayerUIInputHandler
    {
        [Header("References")]
        [SerializeField] private LobbySandboxController _sandboxController;

        [Header("Settings")]
        [SerializeField] private int _maxPlayers = 4;

        [Tooltip("Les PlayerManager déjà existants au chargement du lobby ne sont pas automatiquement acceptés. Ils doivent appuyer sur A.")]
        [SerializeField] private bool _existingPlayersMustPressSubmit = true;

        [Tooltip("Les nouveaux PlayerManager créés pendant le lobby sont acceptés directement. Pratique quand PlayerInputManager crée le joueur au moment de l'appui sur A.")]
        [SerializeField] private bool _autoAcceptNewPlayersCreatedInLobby = true;

        private readonly HashSet<PlayerManager> _knownPlayers = new();
        private readonly HashSet<PlayerManager> _pendingPlayers = new();
        private readonly HashSet<PlayerManager> _acceptedPlayers = new();

        private readonly List<PlayerManager> _playersBuffer = new();
        private readonly List<PlayerManager> _playersToRemoveBuffer = new();

        private LobbyService _lobbyService;
        private DeviceService _deviceService;

        private int _currentPromptCount = -1;
        private bool _initialized;

        public int CurrentPromptCount => Mathf.Max(0, _currentPromptCount);

        public event Action<PlayerManager> OnPlayerAccepted;
        public event Action OnPromptStateChanged;

        private PlayerUIInputService UIInputService =>
            ServiceManager.Instance?.Get<PlayerUIInputService>();

        private void OnEnable()
        {
            _lobbyService = ServiceManager.Instance?.Get<LobbyService>();
            _deviceService = ServiceManager.Instance?.Get<DeviceService>();

            if (!_sandboxController)
                Logs.LogError("[LobbyJoinController] SandboxController reference is missing.", this);

            if (_lobbyService is null)
                Logs.LogError("[LobbyJoinController] LobbyService is missing.", this);

            if (_deviceService is null)
                Logs.LogWarning("[LobbyJoinController] DeviceService is missing. Existing paired players may not be recovered.", this);

            InputSystem.onDeviceChange += HandleDeviceChange;

            SeedExistingPlayers();
            RefreshJoinAvailability();
            RefreshPromptCount();

            _initialized = true;
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= HandleDeviceChange;

            ClearPendingHandlers();

            _knownPlayers.Clear();
            _pendingPlayers.Clear();
            _acceptedPlayers.Clear();

            _initialized = false;
        }

        private void Update()
        {
            if (!_initialized)
                return;

            SyncPlayers();
            RefreshPromptCount();
        }

        private void SeedExistingPlayers()
        {
            if (_lobbyService is null)
                return;

            _playersBuffer.Clear();
            CopyCurrentPlayers(_playersBuffer);

            for (int i = 0; i < _playersBuffer.Count; i++)
            {
                var player = _playersBuffer[i];

                if (!player)
                    continue;

                _knownPlayers.Add(player);

                if (_existingPlayersMustPressSubmit)
                    RegisterPendingPlayer(player);
                else
                    AcceptPlayer(player);
            }
        }

        private void SyncPlayers()
        {
            if (_lobbyService is null)
                return;

            _playersBuffer.Clear();
            CopyCurrentPlayers(_playersBuffer);

            RemoveMissingPlayers(_playersBuffer);

            for (int i = 0; i < _playersBuffer.Count; i++)
            {
                var player = _playersBuffer[i];

                if (!player)
                    continue;

                if (_knownPlayers.Contains(player))
                    continue;

                _knownPlayers.Add(player);

                if (_autoAcceptNewPlayersCreatedInLobby)
                {
                    AcceptPlayer(player);
                }
                else
                {
                    RegisterPendingPlayer(player);
                }
            }
        }

        private void CopyCurrentPlayers(List<PlayerManager> result)
        {
            AddPlayersFromLobbyService(result);
            AddPlayersFromDeviceService(result);
        }
        
        private void AddPlayersFromLobbyService(List<PlayerManager> result)
        {
            if (_lobbyService is null)
                return;

            var players = _lobbyService.GetPlayers();

            for (int i = 0; i < players.Count; i++)
            {
                AddPlayerIfValid(result, players[i]);
            }
        }

        private void AddPlayersFromDeviceService(List<PlayerManager> result)
        {
            if (_deviceService is null)
                return;

            var playerInputs = _deviceService.GetAllPlayerInputs();

            for (int i = 0; i < playerInputs.Count; i++)
            {
                var playerInput = playerInputs[i];

                if (!playerInput)
                    continue;

                var player = playerInput.GetComponent<PlayerManager>();

                AddPlayerIfValid(result, player);
            }
        }

        private static void AddPlayerIfValid(List<PlayerManager> result, PlayerManager player)
        {
            if (!player)
                return;

            if (result.Contains(player))
                return;

            result.Add(player);
        }
        
        public bool ShouldShowPromptForSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _maxPlayers)
                return false;

            if (IsPlayerIndexAccepted(slotIndex))
                return false;

            int connectedGamepadCount = GetConnectedGamepadCount();

            return slotIndex < connectedGamepadCount;
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

        private void RemoveMissingPlayers(List<PlayerManager> livePlayers)
        {
            _playersToRemoveBuffer.Clear();

            foreach (var player in _knownPlayers)
            {
                if (!player || !livePlayers.Contains(player))
                    _playersToRemoveBuffer.Add(player);
            }

            for (int i = 0; i < _playersToRemoveBuffer.Count; i++)
            {
                var player = _playersToRemoveBuffer[i];

                _knownPlayers.Remove(player);
                _pendingPlayers.Remove(player);
                _acceptedPlayers.Remove(player);

                UIInputService?.Remove(player, this);
            }

            _playersToRemoveBuffer.Clear();
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

            Logs.Log($"[LobbyJoinController] Player {player.PlayerIndex + 1} is waiting to join.");
        }

        private void AcceptPlayer(PlayerManager player)
        {
            if (!player)
                return;

            if (_acceptedPlayers.Contains(player))
                return;

            if (_acceptedPlayers.Count >= _maxPlayers)
            {
                Logs.LogWarning($"[LobbyJoinController] Cannot accept Player {player.PlayerIndex + 1}: max player count reached.", this);
                return;
            }

            _pendingPlayers.Remove(player);
            UIInputService?.Remove(player, this);

            _acceptedPlayers.Add(player);

            Logs.Log($"[LobbyJoinController] Player {player.PlayerIndex + 1} joined the lobby.");

            _sandboxController?.SpawnJoinedPlayer(player);

            OnPlayerAccepted?.Invoke(player);

            RefreshJoinAvailability();
            RefreshPromptCount();
        }

        private void ClearPendingHandlers()
        {
            foreach (var player in _pendingPlayers)
            {
                if (player)
                    UIInputService?.Remove(player, this);
            }
        }

        private void RefreshJoinAvailability()
        {
            if (!PlayerInputBridge.Instance)
                return;

            bool canJoin = _acceptedPlayers.Count < _maxPlayers;
            PlayerInputBridge.Instance.CanJoin(canJoin);
        }

        private void RefreshPromptCount()
        {
            int promptCount = CalculatePromptCount();

            if (promptCount == _currentPromptCount)
            {
                OnPromptStateChanged?.Invoke();
                return;
            }

            _currentPromptCount = promptCount;

            OnPromptStateChanged?.Invoke();
        }

        private int CalculatePromptCount()
        {
            int connectedGamepadCount = GetConnectedGamepadCount();
            int acceptedGamepadPlayerCount = GetAcceptedGamepadPlayerCount();

            int promptCount = connectedGamepadCount - acceptedGamepadPlayerCount;

            return Mathf.Clamp(promptCount, 0, _maxPlayers);
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

            return count;
        }

        private int GetAcceptedGamepadPlayerCount()
        {
            int count = 0;

            foreach (var player in _acceptedPlayers)
            {
                if (!player || !player.PlayerInput)
                    continue;

                if (PlayerUsesGamepad(player))
                    count++;
            }

            return count;
        }

        private static bool PlayerUsesGamepad(PlayerManager player)
        {
            if (!player || !player.PlayerInput)
                return false;

            var devices = player.PlayerInput.devices;

            for (int i = 0; i < devices.Count; i++)
            {
                if (devices[i] is Gamepad)
                    return true;
            }

            return false;
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
                    RefreshPromptCount();
                    break;
            }
        }

        public bool IsPlayerAccepted(PlayerManager player)
        {
            return player && _acceptedPlayers.Contains(player);
        }

        public IReadOnlyCollection<PlayerManager> GetAcceptedPlayers()
        {
            return _acceptedPlayers;
        }

        public bool CanHandleUIInput(PlayerManager player)
        {
            return player &&
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