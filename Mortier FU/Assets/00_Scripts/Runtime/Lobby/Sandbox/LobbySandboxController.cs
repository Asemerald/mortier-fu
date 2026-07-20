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
        [SerializeField] private bool _spawnPlayersOnStart;
        
        [Header("Tutorial")]
        [SerializeField] private List<SO_Tutorial> _tutorials;

        private LobbyService _lobbyService;
        private readonly List<PlayerManager> _spawnedPlayers = new();
        
        private static readonly List<PlayerManager> _playerFirstJoin = new();

        private int _lastKnownPlayerCount = -1;

        private bool _isGlobalLockActive;

        private List<PlayerLobbyTutorial> _playerLobbyTutorial = new List<PlayerLobbyTutorial>();

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
            
            foreach (PlayerLobbyTutorial tuto in _playerLobbyTutorial)
                tuto?.Disconnect();
        }

        private async UniTaskVoid InitializeAsync()
        {
            await EnsureLobbySandboxSystemsAsync();

            ResolveDependencies();

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

            for (var i = 0; i < players.Count; i++)
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

            var spawnPoint = GetSpawnPoint(playerIndex);

            if (!spawnPoint)
            {
                Logs.LogError($"[LobbySandboxController] Missing spawn point for player index {playerIndex}.");
                return;
            }

            player.SpawnInGame(spawnPoint.position, spawnPoint.rotation);

            if (_isGlobalLockActive)
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

            if (!_playerFirstJoin.Contains(player))
            {
                _playerFirstJoin.Add(player);


                PlayerLobbyTutorial tempTuto = new PlayerLobbyTutorial(_tutorials, player);
                _playerLobbyTutorial.Add(tempTuto);
            }
            
            Logs.Log($"[LobbySandboxController] Spawned Player {player.PlayerIndex + 1} in lobby sandbox with context {player.ControlContext}.");
        }

        public void LockAllPlayers()
        {
            if (!_isGlobalLockActive)
            {
                _isGlobalLockActive = true;
                OnGlobalLockStarted?.Invoke();
            }

            for (var i = 0; i < _spawnedPlayers.Count; i++)
            {
                var player = _spawnedPlayers[i];

                if (!player)
                    continue;

                player.SetControlContext(PlayerControlContext.LobbyLocked);
            }
        }

        public void UnlockAllPlayers()
        {
            if (!_isGlobalLockActive)
                return;

            _isGlobalLockActive = false;

            for (var i = 0; i < _spawnedPlayers.Count; i++)
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

        private PlayerControlContext GetCurrentContextForPlayer(PlayerManager player)
        {
            return PlayerControlContext.LobbySandbox;
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

        private void OnApplicationQuit()
        {
            _playerFirstJoin.Clear();
        }
    }
}