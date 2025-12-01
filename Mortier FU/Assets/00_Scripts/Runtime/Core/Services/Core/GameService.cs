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

        /// Kinda expansive, should cache the result if used multiple time per case.
        public static IGameMode CurrentGameMode
        {
            get
            {
                var gameService = ServiceManager.Instance.Get<GameService>();
                return gameService._currentGameMode;
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
            SystemManager.Instance.CreateAndRegister<BombshellSystem>();
            SystemManager.Instance.CreateAndRegister<AugmentProviderSystem>();
            SystemManager.Instance.CreateAndRegister<AugmentSelectionSystem>();
            SystemManager.Instance.CreateAndRegister<CameraSystem>();

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
