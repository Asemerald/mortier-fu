using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine.SceneManagement;

namespace MortierFu
{
    public class GameService : IGameService
    {
        private IGameMode _currentGameMode;
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
            
            // Load gameplay scene
            var sceneHandle = SceneManager.LoadSceneAsync(k_gameplayScene);
            if (sceneHandle == null)
            {
                Logs.LogError($"Failed to load the gameplay scene {k_gameplayScene}");
                return;
            }
            
            sceneHandle.allowSceneActivation = true;
            await sceneHandle;

            // Register all game systems
            SystemManager.Instance.CreateAndRegister<LevelSystem>();
            SystemManager.Instance.CreateAndRegister<BombshellSystem>();
            SystemManager.Instance.CreateAndRegister<AugmentSelectionSystem>();

            await SystemManager.Instance.Initialize();

            // Start the game mode
        }
        
        public void Dispose()
        {
            _currentGameMode?.Dispose();
        }
        
        public bool IsInitialized { get; set; }
    }
}
