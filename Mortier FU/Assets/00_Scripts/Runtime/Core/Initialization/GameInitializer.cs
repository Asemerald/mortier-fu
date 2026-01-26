using Cysharp.Threading.Tasks;
using UnityEngine;
using MortierFu.Services;
using NaughtyAttributes;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace MortierFu
{
    public class GameInitializer : MonoBehaviour
    {
        [FormerlySerializedAs("scene")] [Header("Scene to load after init")]
        public string sceneName = "MainMenu";

        [Expandable] public SO_GameConfig config;
        
        [Header("Warmup Manager")]
        public WarmupManager warmupManager;
        
        private float _progress = 0f;

        public float GetInitializationProgress() => _progress;

        private ServiceManager _serviceManager;
        private SystemManager _systemManager;
        private ModService _modService;
        private ModLoaderService _loaderService;
        private AudioService _audioService;
        private DeviceService _deviceService;
        private ConfirmationService _confirmationService;
        private GameService _gameService;
        private LobbyService _lobbyService;
        private DiscordService _discordService;
        private SceneService _sceneService;
        private SaveService _saveService;
        private ShakeService _shakeService;
        
#if UNITY_EDITOR
        [Header("Debug")] public bool isPortableBootstrap = false;
#endif

        //private float _progress = 0f;

        private void Awake()
        {
            // Lock and hide cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            InitializeAsync().Forget();
        }

        private void Update()
        {
            _serviceManager?.Tick();
            _systemManager?.Tick();
        }

        private async UniTaskVoid InitializeAsync()
        {
            _serviceManager = new ServiceManager(this);
            _systemManager = new SystemManager(this);

            config.shaderVariantsToPreload.WarmUp();

            while (!config.shaderVariantsToPreload.isWarmedUp)
            {
                await UniTask.Yield();
            }
            _progress = 0.1f;

            await InitializeGameService();
            _progress = 0.2f;
            
            // Initialise les services de base avant les mods
            await _serviceManager.Initialize();
            _progress = 0.3f;
            
            // Initialise les systèmes de base avant les mods
            await _systemManager.Initialize();
            _progress = 0.4f;
            
            // --- Initialize Addressables
            await Addressables.InitializeAsync();
            _progress = 0.5f;
            
            // --- Load mod resources
            await _loaderService.LoadAllModResources();
            _progress = 0.6f;
            
            // --- Load GameConfig banks
            await _audioService.LoadBanks(config.fmodBanks);
            _progress = 0.7f;
            
            // --- Load mods banks TODO FIX PARCE QUE ÇA MARCHE PAS
            await _audioService.LoadBanks(_modService.GetAllModFmodBanks());
            _progress = 0.8f;
            
#if UNITY_EDITOR
            // --- Check for missing services (only in editor)
            await _serviceManager.CheckForMissingServices<IGameService>();
            
            if (isPortableBootstrap)
            {
                // Stay in current Scene
                return;
            }
#endif
            await warmupManager.WarmupAllAsync();
            _progress = 0.9f;
            
            // --- Load MainMenu Scene
            await _sceneService.LoadScene(sceneName, true);
            
            _progress = 1f;
            _sceneService.HideLoadingScreen();
        }


        private UniTask InitializeGameService()
        {
            // --- Instantiate core services
            _modService = new ModService();
            _loaderService = new ModLoaderService();
            _audioService = new AudioService();
            _deviceService = new DeviceService();
            _gameService = new GameService();
            _lobbyService = new LobbyService();
            _discordService = new DiscordService();
            _confirmationService = new ConfirmationService();
            _sceneService = new SceneService();
            _saveService = new SaveService();
            _shakeService = new ShakeService();
            
            // --- Register services
            _serviceManager.Register(_modService);
            _serviceManager.Register(_loaderService);
            _serviceManager.Register(_audioService);
            _serviceManager.Register(_deviceService);
            _serviceManager.Register(_gameService);
            _serviceManager.Register(_lobbyService);
            _serviceManager.Register(_discordService);
            _serviceManager.Register(_confirmationService);
            _serviceManager.Register(_sceneService);
            _serviceManager.Register(_saveService);
            _serviceManager.Register(_shakeService);
            
            return UniTask.CompletedTask;
        }

        private void OnApplicationQuit()
        {
            _serviceManager?.Dispose();
            _systemManager?.Dispose();
        }
    }
}