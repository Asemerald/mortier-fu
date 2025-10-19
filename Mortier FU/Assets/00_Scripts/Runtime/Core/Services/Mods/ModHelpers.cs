using System;
using System.Collections.Generic;
using System.Reflection;
using MortierFu;
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

        // Nouveaux champs pour la config de contenu
        public List<string> assetBundles = new();
        public List<string> fmodBanks = new();
        public List<string> dlls = new();
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
}