using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MortierFu
{
    [Serializable]
    public class ModManifest
    {
        public string name = "Unnamed Mod";
        public string version = "1.0";
        public string author = "Unknown";
        public string description = "";
    }

    [Serializable]
    public class ModInfo
    {
        public ModManifest manifest;
        public string folderPath;
        public bool isEnabled;
        public bool isLoaded;
        public Type modType;
        public Assembly modAssembly;
        public ModBase instance;
    }

    public class ModManager : MonoBehaviour
    {
        public static ModManager Instance { get; private set; }
        public List<ModInfo> allMods = new List<ModInfo>();
        public Action<ModInfo> OnModToggled;

        private string modsFolder;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            modsFolder = Path.Combine(Application.dataPath, "Mods");
            if (!Directory.Exists(modsFolder))
                Directory.CreateDirectory(modsFolder);

            ScanMods();
            LoadMods();
        }

        void Update()
        {
            foreach (var mod in allMods)
            {
                if (mod.isLoaded && mod.instance != null)
                    mod.instance.ModUpdate();
            }
        }

        public void ScanMods()
        {
            allMods.Clear();

            foreach (var dir in Directory.GetDirectories(modsFolder))
            {
                bool isDisabled = dir.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase);
                string manifestPath = Path.Combine(dir, "manifest.json");
                ModManifest manifest = new ModManifest();

                try
                {
                    if (File.Exists(manifestPath))
                        manifest = JsonUtility.FromJson<ModManifest>(File.ReadAllText(manifestPath));
                    else
                        Debug.LogWarning($"[ModManager] No manifest.json found in {dir}");

                    string[] dlls = Directory.GetFiles(dir, "*.dll", SearchOption.TopDirectoryOnly);
                    if (dlls.Length == 0)
                    {
                        Debug.LogWarning($"[ModManager] No DLL found in {dir}");
                        continue;
                    }

                    string dllPath = dlls[0];
                    var asm = Assembly.Load(File.ReadAllBytes(dllPath));
                    var modType = asm.GetTypes().FirstOrDefault(t => t.IsSubclassOf(typeof(ModBase)) && !t.IsAbstract);

                    if (modType == null)
                    {
                        Debug.LogWarning($"[ModManager] No ModBase class found in {dllPath}");
                        continue;
                    }

                    ModInfo info = new ModInfo
                    {
                        manifest = manifest,
                        folderPath = dir,
                        modType = modType,
                        modAssembly = asm,
                        isEnabled = !isDisabled,
                        isLoaded = false,
                        instance = null
                    };

                    allMods.Add(info);
                    Debug.Log($"[ModManager] Found mod: {manifest.name} (Enabled: {info.isEnabled})");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ModManager] Error loading mod in {dir}: {e.Message}");
                }
            }

            Debug.Log($"[ModManager] Total mods found: {allMods.Count}");
        }

        public void ToggleMod(ModInfo mod)
        {
            bool newState = !mod.isEnabled;

            // On renomme le dossier
            string newPath = newState
                ? mod.folderPath.Replace(".disabled", "")
                : mod.folderPath + ".disabled";

            try
            {
                if (Directory.Exists(mod.folderPath))
                    Directory.Move(mod.folderPath, newPath);

                Debug.Log($"[ModManager] Mod '{mod.manifest.name}' renamed to {newPath}");
                mod.folderPath = newPath;
                mod.isEnabled = newState;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ModManager] Failed to rename mod folder: {e.Message}");
            }

            // On applique l’état immédiatement si nécessaire
            if (!mod.isEnabled && mod.isLoaded)
            {
                mod.instance?.DeInitialize();
                mod.instance = null;
                mod.isLoaded = false;
            }
            else if (mod.isEnabled && !mod.isLoaded)
            {
                LoadMod(mod);
            }
            OnModToggled?.Invoke(mod);
        }

        public void LoadMods()
        {
            foreach (var mod in allMods)
            {
                if (mod.isEnabled && !mod.isLoaded)
                    LoadMod(mod);
            }
        }

        private void LoadMod(ModInfo mod)
        {
            try
            {
                var instance = (ModBase)Activator.CreateInstance(mod.modType);
                instance.Initialize();
                mod.instance = instance;
                mod.isLoaded = true;
                Debug.Log($"[ModManager] Loaded mod: {mod.manifest.name}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ModManager] Error initializing mod {mod.manifest.name}: {e.Message}");
            }
        }

        public void RestartGame()
        {
            Debug.Log("Restarting game to apply mods...");
            DebugManager.RestartGame();
        }
    }
}
