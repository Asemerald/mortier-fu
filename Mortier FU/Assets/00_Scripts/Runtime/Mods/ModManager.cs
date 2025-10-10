using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MortierFu
{
    [Serializable]
    public class ModInfo
    {
        public string name;
        public string folderPath;
        public bool isEnabled;
        public bool isLoaded;
        public Type modType;         // type à instancier
        public Assembly modAssembly;
        public ModBase instance;     // instance en mémoire
    }

    public interface IUpdatable
    {
        void ModUpdate();
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
            Debug.Log("Unity asm location " + typeof(IUpdatable).Assembly.Location);
        }

        void Update()
        {
            foreach (var mod in allMods)
            {
                if (mod.isLoaded && mod.instance != null)
                {
                    mod.instance.ModUpdate();
                }
            }
        }

        public void ScanMods()
        {
            allMods.Clear();
            string[] dlls = Directory.GetFiles(modsFolder, "*.dll", SearchOption.AllDirectories);

            foreach (var dllPath in dlls)
            {
                try
                {
                    var rawBytes = File.ReadAllBytes(dllPath);
                    var asm = Assembly.Load(rawBytes);
                    var modTypes = asm.GetTypes().Where(t => t.IsSubclassOf(typeof(ModBase)) && !t.IsAbstract);

                    foreach (var type in modTypes)
                    {
                        ModInfo info = new ModInfo
                        {
                            name = type.Name,
                            folderPath = Path.GetDirectoryName(dllPath),
                            modType = type,
                            modAssembly = asm,
                            isEnabled = true,
                            isLoaded = false,
                            instance = null
                        };
                        allMods.Add(info);
                        Debug.Log($"[ModManager] Found mod: {info.name} in {dllPath}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading mod {dllPath}: {e.Message}");
                }
            }


            Debug.Log($"Found {allMods.Count} mods.");
        }

        public void ToggleMod(ModInfo mod)
        {
            mod.isEnabled = !mod.isEnabled;
            OnModToggled?.Invoke(mod);

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
            foreach (var mod in allMods)
            {
                if (mod.isEnabled && !mod.isLoaded)
                {
                    LoadMod(mod);
                }
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
            }
            catch (Exception e)
            {
                Debug.LogError($"Error initializing mod {mod.name}: {e.Message}");
            }
        }

        public void RestartGame()
        {
            Debug.Log("Restarting game to apply mods...");
            Application.Quit();
        }
    }
}
