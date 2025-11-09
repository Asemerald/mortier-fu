using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MortierFu
{
    public class ModService : IGameService
    {
        public List<ModInfo> AllMods { get; private set; } = new();
        public Action<ModInfo> OnModToggled;

        private readonly string modsFolder;

        public ModService()
        {
            modsFolder = Path.Combine(Application.persistentDataPath, "Mods");
            if (!Directory.Exists(modsFolder))
                Directory.CreateDirectory(modsFolder);
        }

        public UniTask OnInitialize()
        {
            ScanMods();
            LoadMods();

            return UniTask.CompletedTask;
        }

        public void Tick()
        {
            foreach (var mod in AllMods)
            {
                if (mod.isLoaded && mod.instance != null)
                    mod.instance.ModUpdate();
            }
        }

        public bool IsInitialized { get; set; }

        public void Dispose()
        {
            foreach (var mod in AllMods)
            {
                if (mod.isLoaded)
                    mod.instance?.DeInitialize();
            }
        }

        public void ScanMods()
        {
            AllMods.Clear();

            foreach (var dir in Directory.GetDirectories(modsFolder))
            {
                bool isDisabled = dir.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase);
                string manifestPath = Path.Combine(dir, "manifest.json");

                try
                {
                    if (!File.Exists(manifestPath))
                    {
                        Debug.LogWarning($"[ModService] Missing manifest.json in {dir}");
                        continue;
                    }

                    var manifest = JsonUtility.FromJson<ModManifest>(File.ReadAllText(manifestPath));
                    string dllPath = manifest.dlls.FirstOrDefault(d => File.Exists(Path.Combine(dir, d)));

                    if (string.IsNullOrEmpty(dllPath))
                    {
                        Debug.LogWarning($"[ModService] No valid DLL found for {manifest.name}");
                        continue;
                    }

                    dllPath = Path.Combine(dir, dllPath);
                    var asm = Assembly.Load(File.ReadAllBytes(dllPath));
                    var modType = asm.GetTypes().FirstOrDefault(t => t.IsSubclassOf(typeof(ModBase)) && !t.IsAbstract);

                    if (modType == null)
                    {
                        Debug.LogWarning($"[ModService] No ModBase class found in {dllPath}");
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

                    AllMods.Add(info);
                    Debug.Log($"[ModService] Found mod: {manifest.name} (Enabled: {info.isEnabled})");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ModService] Error scanning mod {dir}: {e.Message}");
                }
            }
        }

        public void ToggleMod(ModInfo mod)
        {
            bool newState = !mod.isEnabled;

            string newPath = newState
                ? mod.folderPath.Replace(".disabled", "")
                : mod.folderPath + ".disabled";

            try
            {
                if (Directory.Exists(mod.folderPath))
                    Directory.Move(mod.folderPath, newPath);

                mod.folderPath = newPath;
                mod.isEnabled = newState;
                OnModToggled?.Invoke(mod);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ModService] Failed to rename mod folder: {e.Message}");
            }

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
        }

        public void LoadMods()
        {
            foreach (var mod in AllMods)
                if (mod.isEnabled && !mod.isLoaded)
                    LoadMod(mod);
        }

        private void LoadMod(ModInfo mod)
        {
            try
            {
                var instance = (ModBase)Activator.CreateInstance(mod.modType);
                instance.Initialize();
                mod.instance = instance;
                mod.isLoaded = true;
                Debug.Log($"[ModService] Loaded mod: {mod.manifest.name}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ModService] Error initializing mod {mod.manifest.name}: {e.Message}");
            }
        }
        
        public AssetReference[] GetAllModFmodBanks()
        {
            var list = new AssetReference[0];
            foreach (var mod in AllMods.Where(m => m.isEnabled))
            {
                if (mod.manifest.fmodBanks == null) continue;

                foreach (var path in mod.manifest.fmodBanks)
                {
                    var ar = new AssetReference(path);
                    Array.Resize(ref list, list.Length + 1);
                    list[list.Length - 1] = ar;
                }
            }
            return list;
        }

    }
}
