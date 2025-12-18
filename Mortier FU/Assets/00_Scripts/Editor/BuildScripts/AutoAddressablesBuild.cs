using UnityEditor;
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
            AddressableAssetSettings.CleanPlayerContent();
            AddressableAssetSettings.BuildPlayerContent();
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