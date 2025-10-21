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
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Scene to load after init")]
        public string Scene = "MainMenu";
        
        [Expandable]
        public GameConfig config;

        private ServiceManager serviceManager;
        private ModService modService;
        private ModLoaderService loaderService;
        private AudioService audioService;
        private DevicesService devicesService;
        
        //private float _progress = 0f;

        private void Awake()
        {
            DontDestroyOnLoad(this);
            StartCoroutine(InitializeRoutine());
        }

        private IEnumerator InitializeRoutine()
        {
            serviceManager = new ServiceManager();
            
            yield return InitializeGameService();
            
            // Initialise les services de base avant les mods
            yield return serviceManager.Initialize();

            // --- Load mod resources
            yield return loaderService.LoadAllModResources();
            
            // --- Inject GameConfig banks
            audioService.RegisterBanks(config.fmodBanks);
            
            // --- Inject mods banks
            audioService.RegisterBanks(modService.GetAllModFmodBanks());
            
            // --- Load all banks 
            yield return audioService.LoadAllBanks();

            // Et enfin prefab
            var handle = config.globalPrefabs.First().LoadAssetAsync<GameObject>();
            yield return handle;
            if (handle.Status == AsyncOperationStatus.Succeeded)
                Instantiate(handle.Result);
            
            // --- Check for missing services
            yield return serviceManager.CheckForMissingServices<IGameService>();
            
            // Prépare la scène à charger
            yield return SceneManager.LoadSceneAsync(Scene);
            
            audioService.PlayMainMenuMusic();
        }


        private Task InitializeGameService()
        {
            // --- Instantiate core services
            modService = new ModService();
            loaderService = new ModLoaderService(modService);
            audioService = new AudioService();
            devicesService = new DevicesService();
            
            // --- Register services
            serviceManager.Register(modService);
            serviceManager.Register(loaderService);
            serviceManager.Register(audioService);
            serviceManager.Register(devicesService);
            
            return Task.CompletedTask;
        }
    }
}