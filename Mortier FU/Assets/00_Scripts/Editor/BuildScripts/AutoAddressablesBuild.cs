using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEngine;
using UnityEditor.AddressableAssets.Settings;

namespace MortierFu.Editor
{
    public class AutoAddressablesBuild
    {
        /// <summary>
        /// Run a clean build before export.
        /// </summary>
        static public void PreExport()
        {
            Debug.Log("BuildAddressablesProcessor.PreExport start");
            
            // Update previous addressable build
            string contentStateDataPath = ContentUpdateScript.GetContentStateDataPath(false);
            if (!File.Exists(contentStateDataPath))
            {
                throw new Exception("Previous Content State Data missing");
            }
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            List<AddressableAssetEntry> modifiedEntries = ContentUpdateScript.GatherModifiedEntries(settings, contentStateDataPath);
            ContentUpdateScript.CreateContentUpdateGroup(settings, modifiedEntries, "Content_Update");
            ContentUpdateScript.BuildContentUpdate(settings, contentStateDataPath);
            
            Debug.Log("BuildAddressablesProcessor.PreExport done");
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayerHandler);
        }

        private static void BuildPlayerHandler(BuildPlayerOptions options)
        {
            PreExport();
            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
        }
    }

}