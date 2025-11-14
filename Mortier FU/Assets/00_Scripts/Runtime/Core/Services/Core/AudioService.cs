using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMODUnity;
using MortierFu.Shared;
using UnityEngine.AddressableAssets;

namespace MortierFu
{
    public class AudioService : IGameService
    {
        private List<AssetReference> Banks = new List<AssetReference>();

        public void PlayMainMenuMusic()
        {
            RuntimeManager.PlayOneShot("event:/Serachan");
        }

        public async UniTask LoadBanks(AssetReference[] banksToLoad)
        {
            foreach (var bankRef in banksToLoad)
            {
                bool loaded = false;
                RuntimeManager.LoadBank(bankRef, true, () => { loaded = true; });

                while (!loaded) 
                    await UniTask.Yield();
                
                Logs.Log($"[AudioService] Loaded FMOD bank: {bankRef.Asset.name}");
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

        public UniTask OnInitialize()
        {
            return UniTask.CompletedTask;
        }

        public bool IsInitialized { get; set; }
    }
}
