using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using BepInEx;

namespace Mortierfu
{
    [Serializable]
    public class ModInfo
    {
        public string guid;           // com.author.modname
        public string name;           // Nom du mod
        public string version;        // 1.0.0
        public string author;         // Nom de l'auteur
        public string description;    // Description
        public string folderPath;     // Chemin du dossier
        public bool isEnabled;        // Activé ou non
        public bool isLoaded;         // Chargé en mémoire
        
        public ModInfo(string guid, string name, string version, string folderPath)
        {
            this.guid = guid;
            this.name = name;
            this.version = version;
            this.folderPath = folderPath;
            this.isEnabled = true;
            this.isLoaded = false;
        }
    }

    public class ModManager : MonoBehaviour
    {
        public static ModManager Instance { get; private set; }
        
        public List<ModInfo> allMods = new List<ModInfo>();
        public Action<ModInfo> OnModToggled;
        
        private string pluginsPath;
        private string configPath;
        
        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            pluginsPath = Path.Combine(Paths.BepInExRootPath, "plugins");
            configPath = Path.Combine(Paths.ConfigPath, "ModManager.json");
            
            ScanMods();
            LoadModStates();
        }
        
        /// <summary>
        /// Scan tous les dossiers dans BepInEx/plugins pour trouver les mods
        /// </summary>
        public void ScanMods()
        {
            allMods.Clear();
            
            if (!Directory.Exists(pluginsPath))
            {
                Debug.LogWarning("Plugins folder not found!");
                return;
            }
            
            // Récupère tous les DLLs dans plugins/ (récursif)
            string[] dllFiles = Directory.GetFiles(pluginsPath, "*.dll", SearchOption.AllDirectories);
            
            Debug.Log($"Found {dllFiles.Length} DLL files");
            
            foreach (string dllPath in dllFiles)
            {
                try
                {
                    // Récupère le dossier parent du DLL
                    string folderPath = Path.GetDirectoryName(dllPath);
                    string folderName = Path.GetFileName(folderPath);
                    
                    // Skip si c'est directement dans plugins/ (pas dans un sous-dossier) TODO suremenet a tej
                    if (folderPath == pluginsPath)
                        continue;
                    
                    // Check si un manifest.json existe
                    string manifestPath = Path.Combine(folderPath, "manifest.json");
                    
                    ModInfo modInfo;
                    
                    if (File.Exists(manifestPath))
                    {
                        // Charge depuis le manifest
                        modInfo = LoadFromManifest(manifestPath, folderPath);
                    }
                    else
                    {
                        // Crée un ModInfo basique depuis le nom du DLL
                        string dllName = Path.GetFileNameWithoutExtension(dllPath);
                        modInfo = new ModInfo(
                            guid: $"unknown.{dllName}",
                            name: dllName,
                            version: "Unknown",
                            folderPath: folderPath
                        );
                        modInfo.description = "No manifest.json found";
                    }
                    
                    // Check si le dossier est désactivé (.disabled)
                    modInfo.isEnabled = !folderName.EndsWith(".disabled");
                    
                    // Check si le mod est actuellement chargé par BepInEx
                    modInfo.isLoaded = IsModLoaded(modInfo.guid);
                    
                    allMods.Add(modInfo);
                    Debug.Log($"Found mod: {modInfo.name} ({modInfo.version})");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error scanning {dllPath}: {e.Message}");
                }
            }
            
            Debug.Log($"Total mods found: {allMods.Count}");
        }
        
        /// <summary>
        /// Charge les infos depuis un manifest.json
        /// </summary>
        private ModInfo LoadFromManifest(string manifestPath, string folderPath)
        {
            string json = File.ReadAllText(manifestPath);
            ManifestData data = JsonUtility.FromJson<ManifestData>(json);
            
            ModInfo modInfo = new ModInfo(
                guid: data.guid ?? "unknown",
                name: data.name ?? "Unknown Mod",
                version: data.version ?? "1.0.0",
                folderPath: folderPath
            );
            
            modInfo.author = data.author;
            modInfo.description = data.description;
            
            return modInfo;
        }
        
        /// <summary>
        /// Check si un mod est actuellement chargé par BepInEx
        /// </summary>
        private bool IsModLoaded(string guid)
        {
            // BepInEx garde une liste des plugins chargés
            var loadedPlugins = BepInEx.Bootstrap.Chainloader.PluginInfos;
            return loadedPlugins.ContainsKey(guid);
        }
        
        /// <summary>
        /// Active/Désactive un mod en renommant son dossier
        /// </summary>
        public void ToggleMod(ModInfo mod)
        {
            string currentPath = mod.folderPath;
            string newPath;
            
            if (mod.isEnabled)
            {
                // Désactive le mod
                newPath = currentPath + ".disabled";
                mod.isEnabled = false;
            }
            else
            {
                // Active le mod (enlève .disabled)
                newPath = currentPath.Replace(".disabled", "");
                mod.isEnabled = true;
            }
            
            try
            {
                Directory.Move(currentPath, newPath);
                mod.folderPath = newPath;
                
                Debug.Log($"Mod {mod.name} {(mod.isEnabled ? "enabled" : "disabled")}");
                
                OnModToggled?.Invoke(mod);
                SaveModStates();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error toggling mod {mod.name}: {e.Message}");
            }
        }
        
        /// <summary>
        /// Sauvegarde l'état des mods (pour se souvenir après redémarrage)
        /// </summary>
        private void SaveModStates()
        {
            ModStatesSave save = new ModStatesSave();
            save.modStates = allMods.Select(m => new ModState 
            { 
                guid = m.guid, 
                isEnabled = m.isEnabled 
            }).ToList();
            
            string json = JsonUtility.ToJson(save, true);
            File.WriteAllText(configPath, json);
        }
        
        /// <summary>
        /// Charge l'état sauvegardé des mods
        /// </summary>
        private void LoadModStates()
        {
            if (!File.Exists(configPath))
                return;
            
            try
            {
                string json = File.ReadAllText(configPath);
                ModStatesSave save = JsonUtility.FromJson<ModStatesSave>(json);
                
                // Applique les états sauvegardés
                foreach (var state in save.modStates)
                {
                    var mod = allMods.FirstOrDefault(m => m.guid == state.guid);
                    if (mod != null && mod.isEnabled != state.isEnabled)
                    {
                        // Si l'état a changé, applique le changement
                        ToggleMod(mod);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading mod states: {e.Message}");
            }
        }
        
        /// <summary>
        /// Force le rechargement de tous les mods
        /// </summary>
        public void RefreshMods()
        {
            ScanMods();
        }
    }
    
    // Classes pour la sérialisation JSON
    [Serializable]
    public class ManifestData
    {
        public string guid;
        public string name;
        public string version;
        public string author;
        public string description;
    }
    
    [Serializable]
    public class ModState
    {
        public string guid;
        public bool isEnabled;
    }
    
    [Serializable]
    public class ModStatesSave
    {
        public List<ModState> modStates = new List<ModState>();
    }
}