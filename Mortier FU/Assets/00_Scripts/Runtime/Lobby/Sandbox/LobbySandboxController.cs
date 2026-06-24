using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbySandboxController : MonoBehaviour
    {
        [Header("Player Spawns")]
        [SerializeField] private Transform[] _playerSpawnPoints = new Transform[4];

        [Header("State")]
        [SerializeField] private LobbySandboxStateController _stateController;

        [Header("Startup")]
        [SerializeField] private bool _spawnPlayersOnStart;

        private LobbyService _lobbyService;
        private readonly List<PlayerManager> _spawnedPlayers = new();

        private bool _isInitialized;
        private int _lastKnownPlayerCount = -1;

        public bool IsGlobalLockActive { get; private set; }

        public event Action<PlayerManager> OnPlayerSpawned;
        public event Action OnGlobalLockStarted;
        public event Action OnGlobalLockEnded;

        private void Start()
        {
            InitializeAsync().Forget();
        }

        private void OnDestroy()
        {
            OnPlayerSpawned = null;
            OnGlobalLockStarted = null;
            OnGlobalLockEnded = null;

            _spawnedPlayers.Clear();
        }

        private void Update()
        {
          //  if (!_isInitialized)
          //      return;

         //   SyncLobbyPlayers();
        }

        private async UniTaskVoid InitializeAsync()
        {
            await EnsureLobbySandboxSystemsAsync();

            ResolveDependencies();

            _isInitialized = true;

            if (_spawnPlayersOnStart)
            {
                SyncLobbyPlayers();
            }
        }
        
        public void SpawnJoinedPlayer(PlayerManager player)
        {
            if (!player)
                return;

            if (_spawnedPlayers.Contains(player))
                return;

            SpawnPlayer(player, player.PlayerIndex);
        }

        private async UniTask EnsureLobbySandboxSystemsAsync()
        {
            if (SystemManager.Instance == null)
            {
                Logs.LogError("[LobbySandboxController] SystemManager is missing. Cannot initialize lobby sandbox systems.");
                return;
            }

            LobbySandboxSystemRegistrar.Register(SystemManager.Instance);

            await SystemManager.Instance.Initialize();
        }

        private void ResolveDependencies()
        {
            if (ServiceManager.Instance == null)
            {
                Logs.LogError("[LobbySandboxController] ServiceManager is not available.");
                return;
            }

            _lobbyService = ServiceManager.Instance.Get<LobbyService>();

            if (_lobbyService == null)
            {
                Logs.LogError("[LobbySandboxController] LobbyService is not available.");
            }
        }

        private void SyncLobbyPlayers()
        {
            var players = GetAvailablePlayers();

            if (players.Count != _lastKnownPlayerCount)
            {
                _lastKnownPlayerCount = players.Count;
                Logs.Log($"[LobbySandboxController] Available PlayerManagers: {players.Count}");
            }

            if (players.Count == 0)
                return;

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];

                if (!player)
                    continue;

                if (_spawnedPlayers.Contains(player))
                    continue;

                SpawnPlayer(player, player.PlayerIndex);
            }
        }

        private List<PlayerManager> GetAvailablePlayers()
        {
            var result = new List<PlayerManager>();

            if (_lobbyService == null)
            {
                ResolveDependencies();
            }

            var servicePlayers = _lobbyService?.GetPlayers();

            if (servicePlayers is null)
                return result;

            foreach (var player in servicePlayers)
            {
                if (!player)
                    continue;

                if (!result.Contains(player))
                    result.Add(player);
            }

            return result;
        }

        private void SpawnPlayer(PlayerManager player, int playerIndex)
        {
            if (!player)
                return;

            Transform spawnPoint = GetSpawnPoint(playerIndex);

            if (!spawnPoint)
            {
                Logs.LogError($"[LobbySandboxController] Missing spawn point for player index {playerIndex}.");
                return;
            }

            player.SpawnInGame(spawnPoint.position, spawnPoint.rotation);

            if (IsGlobalLockActive)
            {
                player.SetControlContext(PlayerControlContext.LobbyLocked);
            }
            else
            {
                ApplyCurrentContextToPlayer(player);
            }

            if (!_spawnedPlayers.Contains(player))
            {
                _spawnedPlayers.Add(player);
            }

            OnPlayerSpawned?.Invoke(player);

            Logs.Log($"[LobbySandboxController] Spawned Player {player.PlayerIndex + 1} in lobby sandbox with context {player.ControlContext}.");
        }

        public void LockAllPlayers()
        {
            if (!IsGlobalLockActive)
            {
                IsGlobalLockActive = true;
                OnGlobalLockStarted?.Invoke();
            }

            for (int i = 0; i < _spawnedPlayers.Count; i++)
            {
                var player = _spawnedPlayers[i];

                if (!player)
                    continue;

                player.SetControlContext(PlayerControlContext.LobbyLocked);
            }
        }

        public void UnlockAllPlayers()
        {
            if (!IsGlobalLockActive)
                return;

            IsGlobalLockActive = false;

            for (int i = 0; i < _spawnedPlayers.Count; i++)
            {
                ApplyCurrentContextToPlayer(_spawnedPlayers[i]);
            }

            OnGlobalLockEnded?.Invoke();
        }

        public IReadOnlyList<PlayerManager> GetSpawnedPlayers()
        {
            return _spawnedPlayers;
        }

        public bool TryGetSpawnPoint(int playerIndex, out Transform spawnPoint)
        {
            spawnPoint = GetSpawnPoint(playerIndex);
            return spawnPoint;
        }

        public PlayerControlContext GetCurrentContextForPlayer(PlayerManager player)
        {
            return _stateController
                ? _stateController.GetContextForNewPlayer(player)
                : PlayerControlContext.LobbySandbox;
        }

        public void ApplyCurrentContextToPlayer(PlayerManager player)
        {
            if (!player)
                return;

            player.SetControlContext(GetCurrentContextForPlayer(player));
        }

        private Transform GetSpawnPoint(int playerIndex)
        {
            if (_playerSpawnPoints is null)
                return null;

            if (playerIndex < 0 || playerIndex >= _playerSpawnPoints.Length)
                return null;

            return _playerSpawnPoints[playerIndex];
        }
    }
}