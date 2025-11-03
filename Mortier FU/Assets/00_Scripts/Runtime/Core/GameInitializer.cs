using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using MortierFu.Services;
using NaughtyAttributes;

namespace MortierFu
{
    public class GameInitializer : MonoBehaviour
    {
        [Header("Scene to load after init")]
        public string scene = "MainMenu";
        
        [Expandable]
        public GameConfig config;

        private ServiceManager _serviceManager;
        private SystemManager _systemManager;
        private ModService _modService;
        private ModLoaderService _loaderService;
        private AudioService _audioService;
        private DeviceService _deviceService;
        private GameInstance _gameInstance;
        private LobbyService _lobbyService;
        private DiscordService _discordService;
        
#if UNITY_EDITOR
        [Header("Debug")]
        public bool isPortableBootstrap = false;
#endif  
        
        //private float _progress = 0f;

        private void Awake()
        {
            StartCoroutine(InitializeRoutine());
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            _serviceManager?.Tick();
            _systemManager?.Tick();
        }

        private IEnumerator InitializeRoutine()
        {
            _serviceManager = new ServiceManager(this);
            _systemManager = new SystemManager(this);
            
            yield return InitializeGameService();
            
            // Initialise les services de base avant les mods
            yield return _serviceManager.Initialize();
            
            // Initialise les systèmes de base avant les mods
            yield return _systemManager.Initialize();
            
#if UNITY_EDITOR
            if (isPortableBootstrap)
            {
                _systemManager.CreateAndRegister<AugmentSelectionSystem>();
                yield return _systemManager.Initialize();
            }
#endif
            // --- Load mod resources
            yield return _loaderService.LoadAllModResources();
            
            // --- Load GameConfig banks
            yield return _audioService.LoadBanks(config.fmodBanks);
            
            // --- Load mods banks TODO FIX PARCE QUE ÇA MARCHE PAS
            yield return _audioService.LoadBanks(_modService.GetAllModFmodBanks());
            
#if UNITY_EDITOR
            // --- Check for missing services (only in editor)
            yield return _serviceManager.CheckForMissingServices<IGameService>();
            
            if (isPortableBootstrap)
            {
                // Stay in current Scene
                yield break;
            }
#endif
            // --- Load MainMenu Scene
            yield return SceneManager.LoadSceneAsync(scene);
        }


        private Task InitializeGameService()
        {
            // --- Instantiate core services
            _modService = new ModService();
            _loaderService = new ModLoaderService();
            _audioService = new AudioService();
            _deviceService = new DeviceService();
            _gameInstance = new GameInstance();
            _lobbyService = new LobbyService();
            _discordService = new DiscordService();
            
            // --- Register services
            _serviceManager.Register(_modService);
            _serviceManager.Register(_loaderService);
            _serviceManager.Register(_audioService);
            _serviceManager.Register(_deviceService);
            _serviceManager.Register(_gameInstance);
            _serviceManager.Register(_lobbyService);
            _serviceManager.Register(_discordService);
            
            return Task.CompletedTask;
        }
    }
}