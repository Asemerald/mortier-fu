using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class PortableBootstrapTool : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private GameInitializer _initializer;

        [SerializeField] private PlayerInputBridge _inputBridge;

        [Header("Gameplay Systems")] [SerializeField]
        private bool _initializeGameplaySystems = true;

        [Header("Player Spawn")] [SerializeField]
        private Transform[] _spawnPoints;

        [SerializeField] private PlayerControlContext _spawnContext = PlayerControlContext.RoundGameplay;

        [Header("Join")] [SerializeField] private bool _enableJoinWhenReady = true;
        [SerializeField] private bool _autoJoinAllGamepadsWhenReady;

        [Header("Debug")]
        [SerializeField] private bool _validateGameplayDependencies = true;

        private readonly HashSet<PlayerManager> _spawnedPlayers = new();

        private bool _isReady;
        private bool _isInitializing;
        private bool _disposeSystemManagerBeforeRegister;

        private void Awake()
        {
            if (!_initializer)
                _initializer = GetComponentInChildren<GameInitializer>(true);

            if (!_inputBridge)
                _inputBridge = GetComponentInChildren<PlayerInputBridge>(true);

            if (_inputBridge)
                _inputBridge.CanJoin(false);
        }

        private void OnEnable()
        {
            if (_inputBridge)
                _inputBridge.OnPlayerManagerJoined += HandlePlayerManagerJoined;

            InitializePortableBootstrapAsync().Forget();
        }

        private void OnDisable()
        {
            if (_inputBridge)
            {
                _inputBridge.OnPlayerManagerJoined -= HandlePlayerManagerJoined;
                _inputBridge.CanJoin(false);
            }

            _isReady = false;
            _isInitializing = false;
            _spawnedPlayers.Clear();
        }

        private async UniTaskVoid InitializePortableBootstrapAsync()
        {
            if (_isInitializing)
                return;

            _isInitializing = true;

            await WaitForGameInitializerAsync();

            if (!this)
                return;

            if (_initializeGameplaySystems)
            {
                await InitializeGameplaySystemsForDebugAsync();
            }

            if (!this)
                return;

            if (_validateGameplayDependencies)
                ValidateGameplayDependencies();

            _isReady = true;

            if (_inputBridge)
                _inputBridge.CanJoin(_enableJoinWhenReady);

            if (_autoJoinAllGamepadsWhenReady && _inputBridge)
                _inputBridge.JoinAllUnpairedGamepads();

            Logs.Log("[PortableBootstrapTool] Ready.");
        }

        private async UniTask WaitForGameInitializerAsync()
        {
            while (!_initializer || !_initializer.IsInitialized)
            {
                await UniTask.Yield();

                if (!this)
                    return;
            }
        }

        private async UniTask InitializeGameplaySystemsForDebugAsync()
        {
            if (SystemManager.Instance == null)
            {
                Logs.LogError("[PortableBootstrapTool] Cannot initialize gameplay systems because SystemManager is missing.", this);
                return;
            }

            Logs.Log("[PortableBootstrapTool] Registering gameplay systems for debug.");

            if (_disposeSystemManagerBeforeRegister)
            {
                SystemManager.Instance.Dispose();
            }

            GameplaySystemRegistrar.Register(SystemManager.Instance);

            await SystemManager.Instance.Initialize();

            Logs.Log("[PortableBootstrapTool] Gameplay systems initialized for debug.");
        }

        private void HandlePlayerManagerJoined(PlayerManager player)
        {
            if (!player)
                return;

            SpawnPlayer(player).Forget();
        }

        private async UniTaskVoid SpawnPlayer(PlayerManager player)
        {
            if (!player)
                return;

            while (!_isReady)
            {
                await UniTask.Yield();

                if (!this || !player)
                    return;
            }

            if (!_spawnedPlayers.Add(player))
                return;

            await UniTask.Yield();

            if (!player)
                return;

            Transform spawnPoint = GetSpawnPoint(player.PlayerIndex);

            player.SetControlContext(_spawnContext);

            player.SpawnInGame(
                spawnPoint.position,
                spawnPoint.rotation
            );

            await UniTask.Yield();

            if (!player)
                return;

            player.SetControlContext(_spawnContext);

            Logs.Log($"[PortableBootstrapTool] Spawned Player {player.PlayerIndex + 1} " + $"at {spawnPoint.position} with context {_spawnContext}.");
        }

        private Transform GetSpawnPoint(int playerIndex)
        {
            if (_spawnPoints == null || _spawnPoints.Length == 0)
                return transform;

            int index = Mathf.Abs(playerIndex) % _spawnPoints.Length;

            return _spawnPoints[index] ? _spawnPoints[index] : transform;
        }

        private void ValidateGameplayDependencies()
        {
            if (ServiceManager.Instance is null)
            {
                Logs.LogError("[PortableBootstrapTool] ServiceManager is missing.", this);
                return;
            }

            if (SystemManager.Instance is null)
            {
                Logs.LogError("[PortableBootstrapTool] SystemManager is missing.", this);
                return;
            }

            if (ServiceManager.Instance.Get<LobbyService>() is null)
                Logs.LogWarning("[PortableBootstrapTool] LobbyService is missing.", this);

            if (ServiceManager.Instance.Get<DeviceService>() is null)
                Logs.LogWarning("[PortableBootstrapTool] DeviceService is missing.", this);

            if (ServiceManager.Instance.Get<PlayerUIInputService>() is null)
                Logs.LogWarning("[PortableBootstrapTool] PlayerUIInputService is missing.", this);

            if (SystemManager.Instance.Get<GamePauseSystem>() is null)
                Logs.LogWarning("[PortableBootstrapTool] GamePauseSystem is missing.", this);

            if (SystemManager.Instance.Get<CameraSystem>() is null)
                Logs.LogWarning("[PortableBootstrapTool] CameraSystem is missing.", this);

            if (SystemManager.Instance.Get<LevelSystem>() is null)
                Logs.LogWarning("[PortableBootstrapTool] LevelSystem is missing.", this);

            if (SystemManager.Instance.Get<BombshellSystem>() is null)
                Logs.LogWarning("[PortableBootstrapTool] BombshellSystem is missing. Shooting will not work.", this);

            if (SystemManager.Instance.Get<AugmentProviderSystem>() is null)
                Logs.LogWarning("[PortableBootstrapTool] AugmentProviderSystem is missing.", this);

            if (SystemManager.Instance.Get<AugmentSelectionSystem>() is null)
                Logs.LogWarning("[PortableBootstrapTool] AugmentSelectionSystem is missing.", this);

            if (!Camera.main)
                Logs.LogWarning("[PortableBootstrapTool] No Camera tagged MainCamera found. Add a MainCamera for audio/camera tests.", this);
        }
    }
}