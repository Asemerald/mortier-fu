using System.Threading.Tasks;
using System;
using MortierFu.Shared;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MortierFu
{
    public static class AddressableHelpers
    {
        public static async Task<T> LoadAndInstantiate<T>(this AssetReference assetReference, Vector3 pos = default, Quaternion rotation = default) where T : Object
        {
            if (assetReference == null || string.IsNullOrEmpty(assetReference.AssetGUID))
            {
                Logs.LogError("[AddressableHelpers] AssetReference is null or has an empty GUID.");
                return null;
            }

            if (!assetReference.IsValid())
            {
                var handle = assetReference.LoadAssetAsync<GameObject>();
                await handle.Task;
            
                if(handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Logs.LogError($"[AddressableHelpers] Failed to load asset: {handle.OperationException.Message}");
                    return null;
                }
            }
            
            var prefab = assetReference.Asset as GameObject;
            if (prefab == null)
            {
                Logs.Log("TOUT WEEPIN");
                Logs.LogError("[AddressableHelpers] Loaded asset is not a GameObject.");
                return null;
            }
            
            var instance = Object.Instantiate(prefab, pos, rotation);
            var component = instance.GetComponent<T>();
            if (component == null)
            {
                Logs.LogError($"[AddressableHelpers] Loaded asset does not contain component of type {typeof(T).Name}");
                return null;
            }
            
            return component;
        }
    }
}