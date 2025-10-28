using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using MortierFu.Services;
using MortierFu.Shared;
using NaughtyAttributes;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MortierFu
{
    public class GameInitializer : MonoBehaviour
    {
        [Header("Scene to load after init")]
        public string scene = "MainMenu";
        
        [Expandable]
        public GameConfig config;

        private ServiceManager _serviceManager;
        private ModService _modService;
        private ModLoaderService _loaderService;
        private AudioService _audioService;
        private DeviceService _deviceService;
        private GameInstance _gameInstance;
        private LobbyService _lobbyService;
        
        //private float _progress = 0f;

        private void Awake()
        {
            StartCoroutine(InitializeRoutine());
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            _serviceManager?.Tick();
        }

        private IEnumerator InitializeRoutine()
        {
            _serviceManager = new ServiceManager();
            
            yield return InitializeGameService();
            
            // Initialise les services de base avant les mods
            yield return _serviceManager.Initialize();

            // --- Load mod resources
            yield return _loaderService.LoadAllModResources();
            
            // --- Load GameConfig banks
            yield return _audioService.LoadBanks(config.fmodBanks);
            
            // --- Load mods banks TODO FIX PARCE QUE ÇA MARCHE PAS
            yield return _audioService.LoadBanks(_modService.GetAllModFmodBanks());
            
#if UNITY_EDITOR
            // --- Check for missing services (only in editor)
            yield return _serviceManager.CheckForMissingServices<IGameService>();
#endif
            // --- Load MainMenu Scene
            yield return SceneManager.LoadSceneAsync(scene);
        }


        private Task InitializeGameService()
        {
            // --- Instantiate core services
            _modService = new ModService();
            _loaderService = new ModLoaderService(_modService);
            _audioService = new AudioService();
            _deviceService = new DeviceService();
            _gameInstance = new GameInstance();
            _lobbyService = new LobbyService();
            
            // --- Register services
            _serviceManager.Register(_modService);
            _serviceManager.Register(_loaderService);
            _serviceManager.Register(_audioService);
            _serviceManager.Register(_deviceService);
            _serviceManager.Register(_gameInstance);
            _serviceManager.Register(_lobbyService);
            
            return Task.CompletedTask;
        }
    }
}