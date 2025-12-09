using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.Exceptions;
using Cysharp.Threading.Tasks;

namespace MortierFu.Shared
{
    public static class AddressablesUtils
    {
        public static async UniTask<AsyncOperationHandle<T>> LazyLoadAsset<T>(object key) where T : Object
        {
            var assetHandle = Addressables.LoadAssetAsync<T>(key);
            await assetHandle;
            
            if (assetHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(assetHandle);
                throw new OperationException("Failed to load asset: " + key);
            }
            
            return assetHandle;
        }
        
        public static async UniTask<AsyncOperationHandle<T>> LazyLoadAssetRef<T>(this AssetReferenceT<T> assetRef) where T : Object
        {
            var assetHandle = assetRef.LoadAssetAsync();
            await assetHandle;
            
            if (assetHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(assetHandle);
                throw new OperationException("Failed to load asset reference: " + assetRef.RuntimeKey);
            }
            
            return assetHandle;
        }
    }
}
