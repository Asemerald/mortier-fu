using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.AddressableAssets;
using STOP_MODE = FMOD.Studio.STOP_MODE;


namespace MortierFu
{
    public class AudioService : IGameService
    {
        private List<AssetReference> Banks = new List<AssetReference>();

        public static FMODEventsSO FMODEvents;

        [SerializeField] private AnimationCurve volumeCurve;
        private List<EventInstance> eventInstances;
        
        //BUS
        private Bus masterBus;
        private Bus sfxBus;
        private Bus musicBus;
        private Bus ambienceBus;

        private EventInstance musicEventInstance, ambienceEventInstance;
        private static bool breakPlayed;

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

        public static async UniTask PlayBreakAudio(EventReference eventRef, Vector3 position)
        {
            if (breakPlayed) return;
            
            PlayOneShot(eventRef, position);
            breakPlayed = true;
            await Task.Delay(TimeSpan.FromSeconds(0.1f));
            breakPlayed = false;
        }

        private static float GetPanningFromWorldSpace(Vector3 position)
        {
            //AJOUTER LOGIQUE
            var screenPos = Camera.main.WorldToScreenPoint(position);
            float pan = (screenPos.x - (Screen.width/2)) / Screen.width * 2;
            
            return pan;
        }
        
        private static float GetPowerFromBombshell(Bombshell bombshell)
        {
            float rangeValue = FMODEvents.rangeCurve.Evaluate(bombshell.AoeRange);
            float damageValue = FMODEvents.damageCurve.Evaluate(bombshell.Damage);

            return (rangeValue + damageValue) * 0.5f;
        }
        
        #endregion
        
        public EventInstance CreateInstance(EventReference eventReference, bool addToList = true)
        {
            var eventInstance = RuntimeManager.CreateInstance(eventReference);

            if (addToList)
                eventInstances.Add(eventInstance);
            return eventInstance;
        }

        public async UniTask StartMusic()
        {
            if (!RuntimeManager.IsInitialized)
            {
                Logs.LogWarning("[SoundManager] FMOD not initialized yet, retrying...");
                await WaitForFMODAndStartMusic();
                return;
            }

            musicEventInstance = CreateInstance(FMODEvents.MUS_Gameplay, false);
            musicEventInstance.start();
        }

        private async UniTask WaitForFMODAndStartMusic()
        {
            while (!RuntimeManager.IsInitialized) await Task.Delay(TimeSpan.FromSeconds(0.1f)) ;
            StartMusic().Forget();
        }

        private UniTask StopMusic()
        {
            if (musicEventInstance.isValid())
            {
                musicEventInstance.stop(STOP_MODE.ALLOWFADEOUT);
                musicEventInstance.release();
            }
            return UniTask.CompletedTask;
        }

        public void SetPhase(int value)
        {
            RuntimeManager.StudioSystem.setParameterByName("Phase", value);
        }
        
        public void SetPause(int value)
        {
            RuntimeManager.StudioSystem.setParameterByName("Pause", value);
        }
        
        public void StartAmbience()
        {
            ambienceEventInstance = CreateInstance(FMODEvents.MUS_Gameplay, false);
            ambienceEventInstance.start();
        }
        
        private UniTask StopAmbience()
        {
            if (ambienceEventInstance.isValid())
            {
                ambienceEventInstance.stop(STOP_MODE.ALLOWFADEOUT);
                ambienceEventInstance.release();
            }
            return UniTask.CompletedTask;
        }

        public void SetVolume(BusEnum bus, float vol)
        {
            switch (bus)
            {
                case BusEnum.MASTER:
                    masterBus.setVolume(volumeCurve.Evaluate(vol));
                    break;
                case BusEnum.MUSIC:
                    musicBus.setVolume(volumeCurve.Evaluate(vol));
                    break;
                case BusEnum.SFX:
                    sfxBus.setVolume(volumeCurve.Evaluate(vol));
                    break;
                case BusEnum.AMBIENCE:
                    ambienceBus.setVolume(volumeCurve.Evaluate(vol));
                    break;
            }
        }

        public enum BusEnum
        {
            MASTER,
            MUSIC,
            SFX,
            AMBIENCE
        }

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

            OnPostBankLoad().Forget();
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

        private UniTask OnPostBankLoad()
        {
            eventInstances = new List<EventInstance>();

            masterBus = RuntimeManager.GetBus("bus:/");
            musicBus = RuntimeManager.GetBus("bus:/MUSIC");
            sfxBus = RuntimeManager.GetBus("bus:/GAMEPLAY");
            ambienceBus = RuntimeManager.GetBus("bus:/AMBIENCE");
            
            FMODEvents = Resources.Load<FMODEventsSO>("FMODEvents");
            
            return UniTask.CompletedTask;
        }

        public bool IsInitialized { get; set; }
    }
}
