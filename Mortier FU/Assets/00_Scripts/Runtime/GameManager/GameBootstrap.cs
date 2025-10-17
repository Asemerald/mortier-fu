using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using FMODUnity;
using MortierFu.Shared;

namespace MortierFu
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Scene to load after init")]
        public string Scene = "Lobby";

        private ModManager modManager;
        private List<AssetBundle> loadedBundles = new();
        private bool isInitialized = false;
        
        private ServiceManager serviceManager;

        private void Awake()
        {
            DontDestroyOnLoad(this);

            serviceManager = new ServiceManager();
            StartCoroutine(InitializeRoutine());
            
        }

        private IEnumerator InitializeRoutine()
        {
            Logs.Log("[Bootstrap] Starting initialization...");
            
            // Step 1 — Load next scene (async but blocked)
            StartCoroutine(LoadMainScene());

            // Step 2 — Initialize core systems
            yield return InitializeCoreSystems();

            // Step 3 — Initialize and scan mods
            yield return InitializeMods();

            // Step 4 — Load asset bundles from mods
            yield return LoadModAssetBundles();

            // Step 5 — Load FMOD banks from bundles
            yield return LoadFmodBanksFromBundles();

            isInitialized = true;
            Logs.Log("[Bootstrap] Initialization complete!");
        }

        private IEnumerator InitializeCoreSystems()
        {
            Logs.Log("[Bootstrap] Initializing core systems...");
            serviceManager.RegisterAll<IGameService>();
            yield return null;
        }

        private IEnumerator InitializeMods()
        {
            Logs.Log("[Bootstrap] Scanning and loading mods...");
            modManager = ModManager.Instance;
            modManager.ScanMods();
            modManager.LoadMods();
            yield return null;
        }

        private IEnumerator LoadModAssetBundles()
        {
            Logs.Log("[Bootstrap] Searching for AssetBundles in Mods...");

            foreach (var mod in modManager.allMods)
            {
                string modFolder = mod.folderPath;
                var bundleFiles = Directory.GetFiles(modFolder, "*.bundle", SearchOption.AllDirectories);

                foreach (var path in bundleFiles)
                {
                    Logs.Log($"[Bootstrap] Loading AssetBundle: {path}");
                    var bundleLoadRequest = AssetBundle.LoadFromFileAsync(path);
                    yield return bundleLoadRequest;

                    var bundle = bundleLoadRequest.assetBundle;
                    if (bundle != null)
                    {
                        loadedBundles.Add(bundle);
                        Logs.Log($"[Bootstrap] Loaded bundle: {bundle.name}");

                        // Optional: Inspect contents
                        string[] assetNames = bundle.GetAllAssetNames();
                        foreach (string a in assetNames)
                            Logs.Log($"    > Contains: {a}");
                    }
                    else
                    {
                        Logs.LogError($"[Bootstrap] Failed to load bundle at {path}");
                    }
                }
            }
        }

        private IEnumerator LoadFmodBanksFromBundles()
        {
            Logs.Log("[Bootstrap] Loading FMOD banks from bundles...");

            foreach (var bundle in loadedBundles)
            {
                var assets = bundle.LoadAllAssets<TextAsset>();
                foreach (var asset in assets)
                {
                    if (asset.name.EndsWith(".bank") || asset.name.Contains("Bank"))
                    {
                        string tempPath = Path.Combine(Application.persistentDataPath, asset.name);
                        File.WriteAllBytes(tempPath, asset.bytes);

                        var result = RuntimeManager.StudioSystem.loadBankFile(tempPath, FMOD.Studio.LOAD_BANK_FLAGS.NORMAL, out var bank);
                        Logs.Log($"[Bootstrap] Loaded FMOD bank {asset.name}: {result}");
                    }
                }
            }

            yield return null;
        }

        private IEnumerator LoadMainScene()
        {
            Debug.Log($"[Bootstrap] Loading main scene: {Scene}");
            AsyncOperation async = SceneManager.LoadSceneAsync(Scene);
            async.allowSceneActivation = false;

            // Wait until everything is ready
            while (!isInitialized)
                yield return null;

            Logs.Log("[Bootstrap] All systems ready, activating scene.");
            async.allowSceneActivation = true;
        }
    }
}
