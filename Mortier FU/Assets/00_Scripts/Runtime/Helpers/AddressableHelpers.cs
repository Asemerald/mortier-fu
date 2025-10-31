using System.Threading.Tasks;
using System;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MortierFu
{
    public static class AddressableHelpers
    {
        public static Task<T> LoadAndInstantiate<T>(this AssetReference assetReference, Vector3 pos = default, Quaternion rotation = default) where T : Object
        {
            var tcs = new TaskCompletionSource<T>();

            var handle = assetReference.LoadAssetAsync<T>();
            handle.Completed += op =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    try
                    {
                        Object instantiatedObj = Object.Instantiate(op.Result as Object, pos, rotation);

                        T finalResult = instantiatedObj as T;
                        if (finalResult == null && instantiatedObj is GameObject go)
                        {
                            finalResult = go.GetComponent<T>();
                        }

                        if (finalResult != null)
                        {
                            tcs.SetResult(finalResult);
                        }
                        else
                        {
                            tcs.SetException(new InvalidCastException($"Instantiated object could not be cast to {typeof(T)}"));
                        }
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                    finally
                    {
                        Addressables.Release(handle);
                    }
                }
                else
                {
                    tcs.SetException(new Exception($"Failed to load asset reference '{assetReference.RuntimeKey}': {op.OperationException?.Message}"));
                }
            };

            return tcs.Task;
        }
    }
}