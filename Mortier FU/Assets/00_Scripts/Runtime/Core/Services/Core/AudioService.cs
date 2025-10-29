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
        private List<AssetReference> Banks = new List<AssetReference>();

        public void PlayMainMenuMusic()
        {
            RuntimeManager.PlayOneShot("event:/Serachan");
        }

        public IEnumerator LoadBanks(AssetReference[] banksToLoad)
        {
            foreach (var bankRef in banksToLoad)
            {
                bool loaded = false;
                RuntimeManager.LoadBank(bankRef, true, () => { loaded = true; });

                while (!loaded) yield return null;
                //Logs.Log($"[AudioService] Loaded FMOD bank: {bankRef.Asset.name}");
                Banks.Add(bankRef);
            }
        }
        
        public void Dispose()
        {
            Logs.Log("[AudioService] Unloading FMOD banks...");
            foreach (var bankRef in Banks)
            {
                try
                {
                    RuntimeManager.UnloadBank(bankRef);
                }
                catch { /* ignore */ }
            }
            Banks.Clear();
        }
    }
}
