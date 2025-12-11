using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine.SceneManagement;

namespace MortierFu
{
    public class GameService : IGameService
    {
        private IGameMode _currentGameMode;
        private SceneService _sceneService;
        
        private const string k_mainMenuScene = "MainMenu";
        private const string k_gameplayScene = "Gameplay";
        
        private static IGameMode _currentGameModeInstance;
        
        public static IGameMode CurrentGameMode
        {
            get
            {
                // return the cached _currentGameModeInstance if not null, else get it from the GameService and cache it
                if (_currentGameModeInstance != null) return _currentGameModeInstance;
                var gameService = ServiceManager.Instance.Get<GameService>();
                _currentGameModeInstance = gameService?._currentGameMode;
                return _currentGameModeInstance;
            }
        }
        
        public UniTask OnInitialize()
        {
            _sceneService = ServiceManager.Instance.Get<SceneService>();
            return UniTask.CompletedTask;
        }

        public async UniTask InitializeGameMode<T>() where T : class, IGameMode, new()
        {
            _currentGameMode = new T(); 
            await _currentGameMode.Initialize();
        }

        public async UniTaskVoid ExecuteGameplayPipeline()
        {
            if (_currentGameMode == null)
            {
                Logs.LogError("Cannot execute the gameplay pipeline with a null or invalid game mode !");
                return;
            }
            
            _sceneService.ShowLoadingScreen();
            
            // Unload main menu scene
            await _sceneService.UnloadScene(k_mainMenuScene);
            
            // Load gameplay scene
            await _sceneService.LoadScene(k_gameplayScene, true);
            
            // Register all game systems
            SystemManager.Instance.CreateAndRegister<LevelSystem>();
            SystemManager.Instance.CreateAndRegister<CameraSystem>();
            SystemManager.Instance.CreateAndRegister<BombshellSystem>();
            SystemManager.Instance.CreateAndRegister<PuddleSystem>();
            SystemManager.Instance.CreateAndRegister<AugmentProviderSystem>();
            SystemManager.Instance.CreateAndRegister<AugmentSelectionSystem>();

            await SystemManager.Instance.Initialize();
            

            // Start the game mode
            await _currentGameMode.StartGame();

            _sceneService.HideLoadingScreen();
            
            Logs.Log("Gameplay pipeline done !");
        }
        
        public void Dispose()
        {
            _currentGameMode?.Dispose();
        }
        
        public bool IsInitialized { get; set; }
    }
}
