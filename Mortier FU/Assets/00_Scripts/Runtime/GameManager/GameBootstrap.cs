using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using MortierFu.Services;

namespace MortierFu
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Scene to load after init")]
        public string Scene = "MainMenu";

        private ServiceManager serviceManager;
        private ModService modService;
        private ModLoaderService loaderService;
        
        private float _progress = 0f;

        private void Awake()
        {
            DontDestroyOnLoad(this);
            StartCoroutine(InitializeRoutine());
        }

        private IEnumerator InitializeRoutine()
        {
            serviceManager = new ServiceManager();

            // --- Register core services
            modService = new ModService();
            loaderService = new ModLoaderService(modService);
            serviceManager.Register(modService);
            serviceManager.Register(loaderService);

            // --- Initialize systems
            modService.Initialize();

            // --- Begin async scene load (paused)
            AsyncOperation async = SceneManager.LoadSceneAsync(Scene);
            async.allowSceneActivation = false;

            // --- Load mod resources
            yield return loaderService.LoadAllModResources();

            // --- All ready
            async.allowSceneActivation = true;
            Debug.Log("[Bootstrap] All systems ready!");
        }
    }
}