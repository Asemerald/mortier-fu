using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace MortierFu
{
    public class AudioService : IGameService
    {
        private readonly List<AssetReference> _bankRefs = new();
        private bool _isInitialized = false;

        public void Initialize() { }

        /// <summary>
        /// Loads all FMOD banks labeled as "fmodBanks" from Addressables.
        /// </summary>
        public IEnumerator LoadAllBanksFromAddressables()
        {
            Logs.Log("[AudioService] Loading FMOD banks from Addressables...");

            // Charge toutes les locations avec le label "fmodBanks"
            AsyncOperationHandle<IList<IResourceLocation>> handle = 
                Addressables.LoadResourceLocationsAsync("FMODBank", typeof(AssetReference));
            yield return handle;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Logs.LogError("[AudioService] Failed to locate FMOD banks Addressables.");
                yield break;
            }

            List<AssetReference> bankRefs = new List<AssetReference>();
            foreach (var location in handle.Result)
            {
                var bankRef = new AssetReference(location.PrimaryKey);
                bankRefs.Add(bankRef);

                Logs.Log($"[AudioService] Loading FMOD Bank: {location.PrimaryKey}");
                FMODUnity.RuntimeManager.LoadBank(bankRef, true, () =>
                {
                    Logs.Log($"[AudioService] Bank loaded: {location.PrimaryKey}");
                });
            }

            // Wait until FMOD finished loading all banks and sample data
            while (!FMODUnity.RuntimeManager.HaveAllBanksLoaded || FMODUnity.RuntimeManager.AnySampleDataLoading())
                yield return null;

            Logs.Log("[AudioService] All FMOD banks successfully loaded.");
        }

        public void Tick() { }

        public void Dispose()
        {
            Logs.Log("[AudioService] Unloading FMOD banks...");
            foreach (var bankRef in _bankRefs)
            {
                try
                {
                    RuntimeManager.UnloadBank(bankRef);
                }
                catch { /* ignore */ }
            }
            _bankRefs.Clear();
        }
    }
}
