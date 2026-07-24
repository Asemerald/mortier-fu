using Cysharp.Threading.Tasks;
using UnityEngine;
using MortierFu.Services;
using NaughtyAttributes;
using UnityEngine.AddressableAssets;

namespace MortierFu
{
    public class GameInitializer : MonoBehaviour
    {
        [Header("Scene to load after init")]
        public string sceneName = "MainMenu";

        [Expandable] public SO_GameConfig config;

        [Header("Warmup Manager")] public WarmupManager warmupManager;

        private float _progress = 0f;

        [Header("Debug")] public bool isPortableBootstrap = false;

        public float GetInitializationProgress() => _progress;
        
        public bool IsInitialized { get; private set; }

        private ServiceManager _serviceManager;
        private SystemManager _systemManager;
        private ModService _modService;
        private ModLoaderService _loaderService;
        private AudioService _audioService;
        private DeviceService _deviceService;
        private FXService _fxService;
        private ConfirmationService _confirmationService;
        private GameService _gameService;
        private LobbyService _lobbyService;
        private PlayerUIInputService _playerUIInputService;
        //private DiscordService _discordService;
        private SceneService _sceneService;
        private SaveService _saveService;
        private ShakeService _shakeService;

        private void Awake()
        {
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

            // Initialise Addressables
            await Addressables.InitializeAsync();
            _progress = 0.5f;

            // Load mod resources
            await _loaderService.LoadAllModResources();
            _progress = 0.6f;

            // Load GameConfig banks
            await _audioService.LoadBanks(config.fmodBanks);
            _progress = 0.7f;

            // Load mods banks
            await _audioService.LoadBanks(_modService.GetAllModFmodBanks());
            _progress = 0.8f;

            // Check les services manquants
            //await _serviceManager.CheckForMissingServices<IGameService>();

            if (isPortableBootstrap)
            {
                _progress = 1f;
                IsInitialized = true;
                _sceneService.HideLoadingScreen();
                return;
            }

            await warmupManager.WarmupAllAsync();
            _progress = 0.9f;

            await _sceneService.LoadScene(sceneName, true);

            _progress = 1f;
            _sceneService.HideLoadingScreen();

            IsInitialized = true;
        }


        private UniTask InitializeGameService()
        {
            // Création des core services
            _modService = new ModService();
            _loaderService = new ModLoaderService();
            _audioService = new AudioService();
            _deviceService = new DeviceService();
            _gameService = new GameService();
            _lobbyService = new LobbyService();
            _playerUIInputService = new PlayerUIInputService();
            //_discordService = new DiscordService();
            _confirmationService = new ConfirmationService();
            _sceneService = new SceneService();
            _saveService = new SaveService();
            _fxService = new FXService();
            _shakeService = new ShakeService();

            // Register des services
            _serviceManager.Register(_modService);
            _serviceManager.Register(_loaderService);
            _serviceManager.Register(_audioService);
            _serviceManager.Register(_fxService);
            _serviceManager.Register(_deviceService);
            _serviceManager.Register(_gameService);
            _serviceManager.Register(_lobbyService);
            _serviceManager.Register(_playerUIInputService);
            //_serviceManager.Register(_discordService);
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