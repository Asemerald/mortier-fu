using System.Collections.Generic;
using UnityEngine;

namespace MortierFu
{
    public static class ModAssetLoader
    {
        private static Dictionary<string, AssetBundle> loadedBundles = new();

        public static void LoadBundle(string modFolderPath, string bundleName)
        {
            string path = System.IO.Path.Combine(modFolderPath, bundleName);
            if (loadedBundles.ContainsKey(path))
                return;

            AssetBundle bundle = AssetBundle.LoadFromFile(path);
            if (bundle != null)
                loadedBundles[path] = bundle;
            else
                Debug.LogError($"Failed to load AssetBundle: {path}");
        }

        public static AssetBundle GetBundleForAugment(DA_Augment augment)
        {
            // Chaque augment pourrait stocker dans DA_Augment le nom du bundle / modFolder
            string bundlePath = augment.ModBundlePath; 
            if (loadedBundles.TryGetValue(bundlePath, out var bundle))
                return bundle;
            return null;
        }

        public static void UnloadAll()
        {
            foreach (var b in loadedBundles.Values)
                b.Unload(true);
            loadedBundles.Clear();
        }
    }
}