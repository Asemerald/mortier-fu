using MortierFu.Shared;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MortierFu.Shared
{
    public static class AddressablesUtils
    {
        public static async UniTask<T> LazyLoadAsset<T>(AssetReferenceT<T> assetRef) where T : Object
        {
            var assetHandle = assetRef.LoadAssetAsync();
            try
            {
                await assetHandle;
                if (assetHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Logs.LogError("[LevelSystem]: Failed while loading settings using Addressables. Error: " + assetHandle.OperationException.Message); 
                    return null;
                }

                return assetHandle.Result;
            }
            finally
            {
                Addressables.Release(assetHandle);
            }
        }
    }
}
