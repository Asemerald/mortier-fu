using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using MortierFu.Services;
using MortierFu.Shared;
using NaughtyAttributes;

namespace MortierFu
{
    public class GameInitializer : MonoBehaviour
    {
        [Header("Scene to load after init")]
        public string scene = "MainMenu";
        
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
        
#if UNITY_EDITOR
        [Header("Debug")]
        public bool isPortableBootstrap = false;
#endif  
        
        //private float _progress = 0f;

        private void Awake()
        {
            InitializeAsync().Forget();
            DontDestroyOnLoad(gameObject);
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
            var sceneHandle = SceneManager.LoadSceneAsync(scene);
            if (sceneHandle == null)
            {
                Logs.LogError($"Couldn't load {scene} !");
                return;
            }
            
            await sceneHandle.ToUniTask();
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
            
            // --- Register services
            _serviceManager.Register(_modService);
            _serviceManager.Register(_loaderService);
            _serviceManager.Register(_audioService);
            _serviceManager.Register(_deviceService);
            _serviceManager.Register(_gameService);
            _serviceManager.Register(_lobbyService);
            _serviceManager.Register(_discordService);
            _serviceManager.Register(_confirmationService);
            
            return UniTask.CompletedTask;
        }

        private void OnDestroy()
        {
            _serviceManager?.Dispose();
            _systemManager?.Dispose();
        }
    }
}