using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Mortierfu
{
    [Serializable]
    public class ModInfo
    {
        public string guid;
        public string name;
        public string version;
        public string author;
        public string description;
        public string folderPath;
        public bool isEnabled;
        public bool isLoaded;
        
        public ModInfo(string guid, string name, string version, string folderPath, bool enabled)
        {
            this.guid = guid;
            this.name = name;
            this.version = version;
            this.folderPath = folderPath;
            this.isEnabled = enabled;
            this.isLoaded = false;
        }
    }

    public class ModManager : MonoBehaviour
    {
        public static ModManager Instance { get; private set; }

        public List<ModInfo> allMods = new List<ModInfo>();
        public Action<ModInfo> OnModToggled;

        private string pluginsPath;
        private string pluginsDisabledPath;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Chemins
            pluginsPath = Path.Combine(Application.dataPath, "../BepInEx/plugins");
            pluginsDisabledPath = Path.Combine(Application.dataPath, "../BepInEx/plugins_disabled");

            // Création des dossiers si manquants
            Directory.CreateDirectory(pluginsPath);
            Directory.CreateDirectory(pluginsDisabledPath);

            ScanMods();
        }

        public void ScanMods()
        {
            allMods.Clear();
            ScanFolder(pluginsPath, true);
            ScanFolder(pluginsDisabledPath, false);
        }

        private void ScanFolder(string folder, bool isEnabled)
        {
            if (!Directory.Exists(folder))
                return;

            string[] dllFiles = Directory.GetFiles(folder, "*.dll", SearchOption.AllDirectories);
            foreach (var dll in dllFiles)
            {
                try
                {
                    string folderName = Path.GetFileName(Path.GetDirectoryName(dll));
                    string dllName = Path.GetFileNameWithoutExtension(dll);

                    ModInfo mod;
                    string manifestPath = Path.Combine(Path.GetDirectoryName(dll), "manifest.json");
                    if (File.Exists(manifestPath))
                    {
                        string json = File.ReadAllText(manifestPath);
                        ManifestData data = JsonUtility.FromJson<ManifestData>(json);
                        mod = new ModInfo(
                            guid: data.guid ?? $"mod.{dllName}",
                            name: data.name ?? dllName,
                            version: data.version ?? "1.0.0",
                            folderPath: Path.GetDirectoryName(dll),
                            enabled: isEnabled
                        );
                        mod.author = data.author;
                        mod.description = data.description;
                    }
                    else
                    {
                        mod = new ModInfo(
                            guid: $"mod.{dllName}",
                            name: dllName,
                            version: "Unknown",
                            folderPath: Path.GetDirectoryName(dll),
                            enabled: isEnabled
                        );
                        mod.description = "No manifest.json";
                    }

                    allMods.Add(mod);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error scanning {dll}: {e.Message}");
                }
            }
        }

        public void ToggleMod(ModInfo mod)
        {
            if (mod.isEnabled)
            {
                // Désactivation : move vers plugins_disabled
                string dest = Path.Combine(pluginsDisabledPath, Path.GetFileName(mod.folderPath));
                Directory.Move(mod.folderPath, dest);
                mod.folderPath = dest;
                mod.isEnabled = false;
            }
            else
            {
                // Activation : move vers plugins
                string dest = Path.Combine(pluginsPath, Path.GetFileName(mod.folderPath));
                Directory.Move(mod.folderPath, dest);
                mod.folderPath = dest;
                mod.isEnabled = true;
            }

            OnModToggled?.Invoke(mod);
            ScanMods();
        }
    }

    [Serializable]
    public class ManifestData
    {
        public string guid;
        public string name;
        public string version;
        public string author;
        public string description;
    }
}
