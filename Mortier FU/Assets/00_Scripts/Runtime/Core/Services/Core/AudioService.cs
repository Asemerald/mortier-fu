using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.AddressableAssets;


namespace MortierFu
{
    public class AudioService : IGameService
    {
        private List<AssetReference> Banks = new List<AssetReference>();

        public static FMODEventsSO FMODEvents;

        public void PlayMainMenuMusic()
        {
            RuntimeManager.PlayOneShot("event:/Serachan");
        }
        
        public static void PlayOneShot(EventReference eventRef)
        {
            RuntimeManager.PlayOneShot(eventRef);
        }

        #region EventInstance functions
        
        public static EventInstance PlayOneShot(EventReference eventRef, float panning = 0)
        {
            EventInstance instance = RuntimeManager.CreateInstance(eventRef);
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(Vector3.zero));
            instance.setParameterByName("Pan", panning);
            instance.start();
            instance.release();

            return instance;
        }
        
        public static EventInstance PlayOneShot(EventReference eventRef, Vector3 position)
        {
            EventInstance instance = RuntimeManager.CreateInstance(eventRef);
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(Vector3.zero));
            var panning = GetPanningFromWorldSpace(position);
            instance.setParameterByName("Pan", panning);
            instance.start();
            instance.release();

            return instance;
        }
        
        public static void PlayBombshellAudio(EventReference eventRef, Bombshell bombshell, Vector3 position)
        {
            float panning = GetPanningFromWorldSpace(position);
            float power = GetPowerFromBombshell(bombshell);
            
            var instance = PlayOneShot(eventRef, panning);
            instance.setParameterByName("ShotPower", power);
            instance.release();
        }

        private static float GetPanningFromWorldSpace(Vector3 position)
        {
            //AJOUTER LOGIQUE
            var screenPos = Camera.main.WorldToScreenPoint(position);
            float pan = (screenPos.x - (Screen.width/2)) / Screen.width * 2;
            
            Debug.LogWarning($"{pan}");
            
            return pan;
        }
        
        private static float GetPowerFromBombshell(Bombshell bombshell)
        {
            float rangeValue = FMODEvents.rangeCurve.Evaluate(bombshell.AoeRange);
            float damageValue = FMODEvents.damageCurve.Evaluate(bombshell.Damage);

            return (rangeValue + damageValue) * 0.5f;
        }
        
        #endregion

        public async UniTask LoadBanks(AssetReference[] banksToLoad)
        {
            foreach (var bankRef in banksToLoad)
            {
                bool loaded = false;
                RuntimeManager.LoadBank(bankRef, true, () => { loaded = true; });

                while (!loaded) 
                    await UniTask.Yield();
                
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

        public UniTask OnInitialize()
        {
            FMODEvents = Resources.Load<FMODEventsSO>("FMODEvents");
            return UniTask.CompletedTask;
        }

        public bool IsInitialized { get; set; }
    }
}
