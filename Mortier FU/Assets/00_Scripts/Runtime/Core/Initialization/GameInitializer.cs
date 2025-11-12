using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using MortierFu.Services;
using MortierFu.Shared;
using NaughtyAttributes;
using UnityEngine.Serialization;

namespace MortierFu
{
    public class GameInitializer : MonoBehaviour
    {
        [FormerlySerializedAs("scene")] [Header("Scene to load after init")]
        public string sceneName = "MainMenu";
        
        [Expandable]
        public SO_GameConfig config;
        
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
        
#if UNITY_EDITOR
        [Header("Debug")]
        public bool isPortableBootstrap = false;
#endif  
        
        //private float _progress = 0f;

        private void Awake()
        {
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

            await InitializeGameService();
            
            // Initialise les services de base avant les mods
            await _serviceManager.Initialize();
            
            // Initialise les systèmes de base avant les mods
            await _systemManager.Initialize();
            
            // --- Load mod resources
            await _loaderService.LoadAllModResources();
            
            // --- Load GameConfig banks
            await _audioService.LoadBanks(config.fmodBanks);
            
            // --- Load mods banks TODO FIX PARCE QUE ÇA MARCHE PAS
            await _audioService.LoadBanks(_modService.GetAllModFmodBanks());
            
#if UNITY_EDITOR
            // --- Check for missing services (only in editor)
            await _serviceManager.CheckForMissingServices<IGameService>();
            
            if (isPortableBootstrap)
            {
                // Stay in current Scene
                return;
            }
#endif
            // --- Load MainMenu Scene
            await _sceneService.LoadScene(sceneName);
            
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
            
            return UniTask.CompletedTask;
        }

        private void OnDestroy()
        {
            _serviceManager?.Dispose();
            _systemManager?.Dispose();
        }
    }
}