using Cysharp.Threading.Tasks;
using MortierFu.Analytics;
using MortierFu.Shared;

namespace MortierFu
{
    public class GameService : IGameService
    {
        private const string k_lobbyScene = "Lobby";
        private const string k_gameplayScene = "Gameplay";

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

            _sceneService.HideLoadingScreen();

            Logs.Log("[GameService] Gameplay pipeline done.");
        }

        public void RestartGame()
        {
            RestartGameAsync().Forget();
        }

        private async UniTaskVoid RestartGameAsync()
        {
            Logs.Log("[GameService] Restarting game.");

            _sceneService.ShowLoadingScreen();

            await CleanupCurrentGameplayRuntimeAsync();

            await InitializeGameMode<GM_FFA>();

            RegisterGameplaySystems();

            await LoadGameplaySceneAsync();

            await InitializeGameplaySystemsAsync();

            await StartCurrentGameModeAsync();

            _sceneService.HideLoadingScreen();

            Logs.Log("[GameService] Restart pipeline done.");
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

        private async UniTask CleanupCurrentGameplayRuntimeAsync()
        {
            _currentGameMode?.Dispose();
            _currentGameMode = null;
            _currentGameModeInstance = null;

            SystemManager.Instance.Dispose();

            await _sceneService.UnloadScene(k_gameplayScene);
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
            SystemManager.Instance.CreateAndRegister<GamePauseSystem>();
            SystemManager.Instance.CreateAndRegister<CameraSystem>();
            SystemManager.Instance.CreateAndRegister<LevelSystem>();
            SystemManager.Instance.CreateAndRegister<BombshellSystem>();
            SystemManager.Instance.CreateAndRegister<AugmentProviderSystem>();
            SystemManager.Instance.CreateAndRegister<AugmentSelectionSystem>();
            SystemManager.Instance.CreateAndRegister<AnalyticsSystem>();

            Logs.Log("[GameService] Gameplay systems registered.");
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
        }
    }
}