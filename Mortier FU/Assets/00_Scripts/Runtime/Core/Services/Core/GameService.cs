using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class GameService : IGameService
    {
        private const string k_mainMenuScene = "MainMenu";
        private const string k_lobbyScene = "Lobby";
        private const string k_gameplayScene = "Gameplay";

        private bool _isSceneTransitionInProgress;
        
        private IGameMode _currentGameMode;
        private SceneService _sceneService;

        private MatchConfig? _pendingMatchConfig;
        private MatchConfig _lastMatchConfig = MatchConfig.Default;

        private static IGameMode _currentGameModeInstance;

        public static IGameMode CurrentGameMode
        {
            get
            {
                var gameService = ServiceManager.Instance.Get<GameService>();
                _currentGameModeInstance = gameService?._currentGameMode;
                return _currentGameModeInstance;
            }
        }

        public bool IsInitialized { get; set; }

        public UniTask OnInitialize()
        {
            _sceneService = ServiceManager.Instance.Get<SceneService>();

            if (_sceneService == null)
            {
                Logs.LogError("[GameService] SceneService could not be found.");
            }

            return UniTask.CompletedTask;
        }

        public void SetPendingMatchConfig(MatchConfig config)
        {
            _pendingMatchConfig = config;
        }

        public async UniTask InitializeGameMode<T>() where T : class, IGameMode, new()
        {
            MatchConfig matchConfig = ConsumeMatchConfig();

            _currentGameMode = new T();
            _currentGameMode.SetMatchConfig(matchConfig);

            await _currentGameMode.Initialize();

            Logs.Log($"[GameService] Game mode initialized with ScoreToWin={matchConfig.ScoreToWin}.");
        }

        public async UniTaskVoid ExecuteGameplayPipeline()
        {
            if (_currentGameMode == null)
            {
                Logs.LogError("[GameService] Cannot execute gameplay pipeline with a null game mode.");
                return;
            }

            _sceneService.ShowLoadingScreen();

            await CleanupSandboxRuntimeAsync();

            RegisterGameplaySystems();

            await LoadGameplaySceneAsync();

            await InitializeGameplaySystemsAsync();

            await StartCurrentGameModeAsync();

            Logs.Log("[GameService] Gameplay pipeline done.");
        }

        public void RestartGame()
        {
            RestartGameAsync().Forget();
        }

        private async UniTaskVoid RestartGameAsync()
        {
            _isSceneTransitionInProgress = true;

            Logs.Log("[GameService] Restarting game.");

            RestoreRuntimeBeforeSceneTransition();

            _sceneService.ShowLoadingScreen();

            await CleanupGameplaySessionAsync();

            await InitializeGameMode<GM_FFA>();

            RegisterGameplaySystems();

            await LoadGameplaySceneAsync();

            await InitializeGameplaySystemsAsync();

            await StartCurrentGameModeAsync();

            //_sceneService.HideLoadingScreen();

            _isSceneTransitionInProgress = false;

            Logs.Log("[GameService] Restart pipeline done.");
        }
        
        public async UniTaskVoid LoadLobbySceneAsync()
        {
            var sceneService = ServiceManager.Instance.Get<SceneService>();

            if (sceneService is null)
            {
                Logs.LogError("[MenuManager] Cannot load lobby because SceneService is missing.");
                return;
            }

            if (PlayerInputBridge.Instance)
                PlayerInputBridge.Instance.CanJoin(false);

            _sceneService.ShowLoadingScreen();
            
            
            await sceneService.LoadScene("Lobby", setAsActiveScene: true);
            await sceneService.UnloadScene("MainMenu");
            
            _sceneService.HideLoadingScreen();
            await CircleTransition.Instance.OpenAsync(1f);
        }
        
        public void ReturnToLobby()
        {
            if (_isSceneTransitionInProgress)
                return;

            ReturnToLobbyAsync().Forget();
        }

        private async UniTaskVoid ReturnToLobbyAsync()
        {
            _isSceneTransitionInProgress = true;

            Logs.Log("[GameService] Returning to lobby.");

            RestoreRuntimeBeforeSceneTransition();

            _sceneService.ShowLoadingScreen();

            await CleanupGameplaySessionAsync();

            await _sceneService.LoadScene(
                k_lobbyScene,
                setAsActiveScene: true
            );

            _sceneService.HideLoadingScreen();

            _isSceneTransitionInProgress = false;
            
            Logs.Log("[GameService] Returned to lobby.");
        }
        
        public void ReturnToMainMenu()
        {
            if (_isSceneTransitionInProgress)
                return;

            ReturnToMainMenuAsync().Forget();
        }

        public async UniTask ReturnToMainMenuAsync()
        {
            _isSceneTransitionInProgress = true;

            Logs.Log("[GameService] Returning to main menu.");

            RestoreRuntimeBeforeSceneTransition();

            _sceneService.ShowLoadingScreen();

            await CleanupGameplaySessionAsync();

            await ResetPlayerSessionForMainMenuAsync();

            await _sceneService.LoadScene(
                k_mainMenuScene,
                setAsActiveScene: true
            );

            _sceneService.HideLoadingScreen();

            _isSceneTransitionInProgress = false;

            Logs.Log("[GameService] Returned to main menu.");
        }
        
        public void ReturnLobbyToMainMenu()
        {
            if (_isSceneTransitionInProgress)
                return;

            ReturnLobbyToMainMenuAsync().Forget();
        }

        public async UniTask ReturnLobbyToMainMenuAsync()
        {
            if (_isSceneTransitionInProgress)
                return;

            _isSceneTransitionInProgress = true;

            Logs.Log("[GameService] Returning to main menu from lobby.");

            RestoreRuntimeBeforeSceneTransition();

            _sceneService.ShowLoadingScreen();

            await CleanupLobbyRuntimeForMainMenuAsync();

            await _sceneService.LoadScene(
                k_mainMenuScene,
                setAsActiveScene: true
            );

            _sceneService.HideLoadingScreen();

            _isSceneTransitionInProgress = false;

            Logs.Log("[GameService] Returned to main menu from lobby.");
        }
        
        private async UniTask CleanupLobbyRuntimeForMainMenuAsync()
        {
            await ResetPlayerSessionForMainMenuAsync();

            SystemManager.Instance.Dispose();

            await _sceneService.UnloadScene(k_lobbyScene);
        }

        private async UniTask ResetPlayerSessionForMainMenuAsync()
        {
            PlayerInputBridge.Instance?.CanJoin(false);

            var lobbyService = ServiceManager.Instance.Get<LobbyService>();
            lobbyService?.ClearPlayers(destroyPlayerObjects: true);

            var deviceService = ServiceManager.Instance.Get<DeviceService>();
            deviceService?.ClearPlayers();

            await UniTask.Yield();
            

            Logs.Log("[GameService] Player session reset for main menu.");
        }
        
        private void RestoreRuntimeBeforeSceneTransition()
        {
            Time.timeScale = 1f;

            var audioService = ServiceManager.Instance.Get<AudioService>();

            if (audioService != null)
            {
                audioService.SetPause(0);
            }
        }
        
        private async UniTask InitializeGameplaySystemsAsync()
        {
            await SystemManager.Instance.Initialize();
        }

        private MatchConfig ConsumeMatchConfig()
        {
            if (_pendingMatchConfig.HasValue)
            {
                _lastMatchConfig = _pendingMatchConfig.Value;
                _pendingMatchConfig = null;
                return _lastMatchConfig;
            }

            Logs.LogWarning("[GameService] No pending MatchConfig found. Reusing last MatchConfig.");

            return _lastMatchConfig;
        }

        private async UniTask CleanupSandboxRuntimeAsync()
        {
            SystemManager.Instance.Dispose();

            await _sceneService.UnloadScene(k_lobbyScene);
        }
        
        private async UniTask CleanupGameplaySessionAsync()
        {
            RestoreRuntimeBeforeSceneTransition();

            _currentGameMode?.Dispose();
            _currentGameMode = null;
            _currentGameModeInstance = null;

            ResetPersistentGameplayServices();

            await UnloadCurrentMapIfNeededAsync();

            SystemManager.Instance.Dispose();

            await _sceneService.UnloadScene(k_gameplayScene);
        }

        private void ResetPersistentGameplayServices()
        {
            ServiceManager.Instance.Get<ConfirmationService>()?.ResetRuntimeState();
            ServiceManager.Instance.Get<PlayerUIInputService>()?.ClearAllHandlers();
        }
        
        private async UniTask UnloadCurrentMapIfNeededAsync()
        {
            var levelSystem = SystemManager.Instance?.Get<LevelSystem>();

            if (levelSystem == null)
            {
                Logs.LogWarning("[GameService] No LevelSystem found while cleaning gameplay runtime.");
                return;
            }

            await levelSystem.UnloadCurrentMap();

            Logs.Log("[GameService] Current map unloaded.");
        }

        private async UniTask LoadGameplaySceneAsync()
        {
            await _sceneService.LoadScene(
                k_gameplayScene,
                setAsActiveScene: true
            );
        }

        private void RegisterGameplaySystems()
        {
            GameplaySystemRegistrar.Register(SystemManager.Instance);
        }

        private async UniTask StartCurrentGameModeAsync()
        {
            if (_currentGameMode == null)
            {
                Logs.LogError("[GameService] Cannot start a null game mode.");
                return;
            }

            await _currentGameMode.StartGame();
        }

        public void Dispose()
        {
            _currentGameMode?.Dispose();
            _currentGameMode = null;
            _currentGameModeInstance = null;

            _pendingMatchConfig = null;
            _lastMatchConfig = MatchConfig.Default;

            _isSceneTransitionInProgress = false;
            RestoreRuntimeBeforeSceneTransition();
        }
    }
}