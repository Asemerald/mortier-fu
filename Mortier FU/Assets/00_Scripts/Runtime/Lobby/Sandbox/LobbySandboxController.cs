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

        [Header("Startup")]
        [SerializeField] private bool _spawnPlayersOnStart = true;

        [Header("Debug")]
        [SerializeField] private bool _allowScenePlayerFallback = true;

        private LobbyService _lobbyService;
        private readonly List<PlayerManager> _spawnedPlayers = new();

        private bool _isInitialized;
        private int _lastKnownPlayerCount = -1;
        
        public bool IsGlobalLockActive { get; private set; }

        public event Action OnGlobalLockStarted;
        public event Action OnGlobalLockEnded;

        private void Start()
        {
            InitializeAsync().Forget();
        }

        private void Update()
        {
            if (!_isInitialized)
                return;

            SyncLobbyPlayers();
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

        private async UniTask EnsureLobbySandboxSystemsAsync()
        {
            if (SystemManager.Instance == null)
            {
                Logs.LogError("[LobbySandboxController] SystemManager is not available.");
                return;
            }

            SystemManager.Instance.CreateAndRegisterIfMissing<GamePauseSystem>();
            SystemManager.Instance.CreateAndRegisterIfMissing<CameraSystem>();
            SystemManager.Instance.CreateAndRegisterIfMissing<BombshellSystem>();

            await SystemManager.Instance.Initialize();

            Logs.Log("[LobbySandboxController] Lobby sandbox systems initialized.");
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

        public void SyncLobbyPlayers()
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

                if (player == null)
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

            if (servicePlayers != null)
            {
                foreach (var player in servicePlayers)
                {
                    if (player == null)
                        continue;

                    if (!result.Contains(player))
                        result.Add(player);
                }
            }

            if (_allowScenePlayerFallback)
            {
                var scenePlayers = FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);

                foreach (var player in scenePlayers)
                {
                    if (player == null)
                        continue;

                    if (!result.Contains(player))
                        result.Add(player);
                }
            }

            return result;
        }

        public void SpawnPlayer(PlayerManager player, int playerIndex)
        {
            if (player == null)
                return;

            Transform spawnPoint = GetSpawnPoint(playerIndex);

            if (spawnPoint == null)
            {
                Logs.LogError($"[LobbySandboxController] Missing spawn point for player index {playerIndex}.");
                return;
            }

            player.SpawnInGame(spawnPoint.position, spawnPoint.rotation);
            player.SetControlContext(PlayerControlContext.LobbySandbox);

            if (!_spawnedPlayers.Contains(player))
            {
                _spawnedPlayers.Add(player);
            }

            Logs.Log($"[LobbySandboxController] Spawned Player {player.PlayerIndex + 1} in lobby sandbox.");
        }

        public void SetSandboxEnabled(bool enabled)
        {
            var context = enabled
                ? PlayerControlContext.LobbySandbox
                : PlayerControlContext.LobbyLocked;

            foreach (var player in _spawnedPlayers)
            {
                if (player == null)
                    continue;

                player.SetControlContext(context);
            }
        }

        public void LockAllPlayers()
        {
            if (!IsGlobalLockActive)
            {
                IsGlobalLockActive = true;
                OnGlobalLockStarted?.Invoke();
            }

            foreach (var player in _spawnedPlayers)
            {
                if (player == null)
                    continue;

                player.SetControlContext(PlayerControlContext.LobbyLocked);
            }
        }

        public void UnlockAllPlayers()
        {
            IsGlobalLockActive = false;

            foreach (var player in _spawnedPlayers)
            {
                if (player == null)
                    continue;

                player.SetControlContext(PlayerControlContext.LobbySandbox);
            }

            OnGlobalLockEnded?.Invoke();
        }

        public IReadOnlyList<PlayerManager> GetSpawnedPlayers()
        {
            return _spawnedPlayers;
        }

        private Transform GetSpawnPoint(int playerIndex)
        {
            if (_playerSpawnPoints == null)
                return null;

            if (playerIndex < 0 || playerIndex >= _playerSpawnPoints.Length)
                return null;

            return _playerSpawnPoints[playerIndex];
        }
    }
}