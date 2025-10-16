using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using MortierFu.Shared;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace MortierFu
{
    public class AudioService : IGameService
    {
        public List<AssetReference> Banks = new List<AssetReference>();
        
        public void Initialize()
        {
            
        }
        
        public IEnumerator LoadBank(string bankName)
        {
            // Iterate all the Studio Banks and start them loading in the background
            // including the audio sample data
            foreach (var bank in Banks)
            {
                FMODUnity.RuntimeManager.LoadBank(bank, true, () => {
                    Logs.Log($"Bank " + bank + "loaded");
                });
            }

            // Keep yielding the co-routine until all the bank loading is done
            // (for platforms with asynchronous bank loading)
            while (!FMODUnity.RuntimeManager.HaveAllBanksLoaded)
            {
                yield return null;
            }

            // Keep yielding the co-routine until all the sample data loading is done
            while (FMODUnity.RuntimeManager.AnySampleDataLoading())
            {
                yield return null;
            }
        }

        public void Tick()
        {
        }
        
        public void Dispose()
        {
            
        }

    }
}