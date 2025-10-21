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
        private GameInstance gameInstance;
        
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
            
            // --- Load GameConfig banks
            yield return audioService.LoadBanks(config.fmodBanks);
            
            // --- Load mods banks TODO FIX PARCE QUE ÇA MARCHE PAS
            yield return audioService.LoadBanks(modService.GetAllModFmodBanks());
            
            // --- Check for missing services
            yield return serviceManager.CheckForMissingServices<IGameService>();
            
            // --- Load MainMenu Scene
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
            gameInstance = new GameInstance();
            
            // --- Register services
            serviceManager.Register(modService);
            serviceManager.Register(loaderService);
            serviceManager.Register(audioService);
            serviceManager.Register(devicesService);
            serviceManager.Register(gameInstance);
            
            return Task.CompletedTask;
        }
    }
}