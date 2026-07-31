using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using MortierFu.Services;
using NaughtyAttributes;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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
        private SceneService _sceneService;
        private SaveService _saveService;
        private ShakeService _shakeService;
        
        private readonly List<AsyncOperationHandle> _preloadedBundleHandles = new();

        private void Awake()
        {
            PlayerLobbyTutorialSession.Clear();

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

            _progress = 0f;

            await InitializeGameService();
            _progress = 0.1f;

            // Initialise les services de base avant les mods
            await _serviceManager.Initialize();
            _progress = 0.2f;

            // Initialise les systèmes de base avant les mods
            await _systemManager.Initialize();
            _progress = 0.3f;

            // Initialise Addressables
            await Addressables.InitializeAsync();
            _progress = 0.4f;
            
            // 🚀 PRÉCHARGEMENT DES MAPS EN RAM
            List<string> labelsToPreload = GetMapLabelsToPreload();
            await PreloadMapBundlesAsync(labelsToPreload);
            _progress = 0.5f;

            // Load mod resources
            await _loaderService.LoadAllModResources();
            _progress = 0.6f;

            // Load GameConfig banks
            if (config != null && config.fmodBanks != null)
            {
                await _audioService.LoadBanks(config.fmodBanks);
            }
            _progress = 0.7f;

            // Load mods banks
            await _audioService.LoadBanks(_modService.GetAllModFmodBanks());
            _progress = 0.8f;

            if (isPortableBootstrap)
            {
                _progress = 1f;
                IsInitialized = true;
                _sceneService.HideLoadingScreen();
                return;
            }

            if (warmupManager != null)
            {
                await warmupManager.WarmupAllAsync();
            }
            _progress = 0.9f;

            await _sceneService.LoadScene(sceneName, true);

            _progress = 1f;
            _sceneService.HideLoadingScreen();

            IsInitialized = true;
        }

        /// <summary>
        /// Récupère la liste des labels à précharger (avec fallback de sécurité si la config n'est pas remplie)
        /// </summary>
        private List<string> GetMapLabelsToPreload()
        {
            if (config != null && config.bundleLabelsToLoad != null && config.bundleLabelsToLoad.Count > 0)
            {
                return config.bundleLabelsToLoad;
            }

            // Fallback par défaut si bundleLabelsToLoad n'est pas renseigné dans le ScriptableObject
            return new List<string> { "ArenaMaps", "RaceMaps" };
        }
        
        /// <summary>
        /// Précharge en RAM les dépendances/bundles d'une liste de labels ou clés Addressables.
        /// </summary>
        private async UniTask PreloadMapBundlesAsync<T>(List<T> targets)
        {
            if (targets == null || targets.Count == 0) 
                return;

            var tasks = new List<UniTask>();

            for (int i = 0; i < targets.Count; i++)
            {
                T target = targets[i];
                if (target == null) 
                    continue;

                var handle = Addressables.DownloadDependenciesAsync(target, autoReleaseHandle: false);
        
                _preloadedBundleHandles.Add(handle);
                tasks.Add(handle.ToUniTask());
            }

            await UniTask.WhenAll(tasks);
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
            _serviceManager.Register(_confirmationService);
            _serviceManager.Register(_sceneService);
            _serviceManager.Register(_saveService);
            _serviceManager.Register(_shakeService);

            return UniTask.CompletedTask;
        }

        private void OnDestroy()
        {
            ReleasePreloadedBundles();
        }

        private void OnApplicationQuit()
        {
            ReleasePreloadedBundles();
            _serviceManager?.Dispose();
            _systemManager?.Dispose();
        }

        private void ReleasePreloadedBundles()
        {
            for (int i = 0; i < _preloadedBundleHandles.Count; i++)
            {
                if (_preloadedBundleHandles[i].IsValid())
                {
                    Addressables.Release(_preloadedBundleHandles[i]);
                }
            }
            _preloadedBundleHandles.Clear();
        }
    }
}