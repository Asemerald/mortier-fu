using MortierFu.Shared;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MortierFu
{
    public static class GameSystemHelpers
    {
        public static async UniTask<T> LoadSystemSettings<T>(AssetReferenceT<T> settingsRef) where T : SO_SystemSettings
        {
            var settingsHandle = settingsRef.LoadAssetAsync();
            try
            {
                // Load the system settings
                await settingsHandle;

                if (settingsHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Logs.LogError("[LevelSystem]: Failed while loading settings using Addressables. Error: " + settingsHandle.OperationException.Message); 
                    return null;
                }

                return settingsHandle.Result;
            }
            finally
            {
                Addressables.Release(settingsHandle);
            }
        }
    }
}
