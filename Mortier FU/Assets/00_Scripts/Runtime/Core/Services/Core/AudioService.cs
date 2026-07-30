using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FMOD;
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
        private List<AssetReference> Banks = new ();

        public static FMODEventsSO FMODEvents;

        [SerializeField] private static AnimationCurve volumeCurve;
        private List<EventInstance> eventInstances;
        private List<EventInstance> mapEventInstances;
        
        //BUS
        private static Bus masterBus;
        private static Bus sfxBus;
        private static Bus musicBus;
        private static Bus ambienceBus;

        protected EventInstance musicEventInstance, ambienceEventInstance;
        private static bool breakPlayed;

        public enum PhaseType
        {
            MUSIC,
            AMBIENCE,
            TWICE
        }

        #region EventInstance functions

        public static EventInstance PlayOneShot(EventReference eventRef, float panning = 0)
        {
            EventInstance instance = RuntimeManager.CreateInstance(eventRef);
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(Vector3.zero));

            TrySetParameterIfExists(instance, "Pan", panning);

            instance.start();
            instance.release();

            return instance;
        }
        
        private static bool TrySetParameterIfExists(EventInstance instance, string parameterName, float value)
        {
            if (!instance.isValid())
                return false;

            FMOD.RESULT descriptionResult = instance.getDescription(out EventDescription eventDescription);

            if (descriptionResult != FMOD.RESULT.OK)
                return false;

            FMOD.RESULT countResult = eventDescription.getParameterDescriptionCount(out int parameterCount);

            if (countResult != FMOD.RESULT.OK)
                return false;

            for (int i = 0; i < parameterCount; i++)
            {
                FMOD.RESULT parameterResult = eventDescription.getParameterDescriptionByIndex(
                    i,
                    out PARAMETER_DESCRIPTION parameterDescription
                );

                if (parameterResult != FMOD.RESULT.OK)
                    continue;

                if (!string.Equals(parameterDescription.name, parameterName, StringComparison.Ordinal))
                    continue;

                FMOD.RESULT setResult = instance.setParameterByName(parameterName, value);

                if (setResult != FMOD.RESULT.OK)
                {
                    Logs.LogWarning($"[AudioService] Failed to set FMOD parameter '{parameterName}'. Result: {setResult}");
                    return false;
                }

                return true;
            }

            return false;
        }
        
        public static EventInstance PlayOneShot(EventReference eventRef, Vector3 position)
        {
            EventInstance instance = RuntimeManager.CreateInstance(eventRef);
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));

            float panning = GetPanningFromWorldSpace(position);

            TrySetParameterIfExists(instance, "Pan", panning);

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
            if (Camera.main == null)
                return 0;

            var screenPos = Camera.main.WorldToScreenPoint(position);
            float pan = (screenPos.x - (Screen.width/2)) / Screen.width * 2;
            
            return pan;
        }
        
        private static float GetPowerFromBombshell(Bombshell bombshell)
        {
            float rangeValue = FMODEvents.rangeCurve.Evaluate(bombshell.AoeRange);
            float damageValue = FMODEvents.damageCurve.Evaluate(bombshell.Damage);

            var finalValue = Mathf.Clamp((rangeValue + damageValue) * 0.5f, 0, 1);

            return finalValue;
        }
        
        #endregion
        
        public EventInstance CreateInstance(EventReference eventReference, bool addToList = true)
        {
            var eventInstance = RuntimeManager.CreateInstance(eventReference);

            if (addToList)
                eventInstances.Add(eventInstance);
            return eventInstance;
        }
        
        public EventInstance CreateMapInstance(EventReference eventReference, Vector3 position)
        {
            var eventInstance = CreateInstance(eventReference);

            TrySetParameterIfExists(eventInstance, "Pan", GetPanningFromWorldSpace(position));
            mapEventInstances.Add(eventInstance);
            eventInstance.start();

            return eventInstance;
        }

        public void DestroyInstance(EventInstance eventInstance)
        {
            if (!eventInstance.isValid()) return;

                eventInstances.Remove(eventInstance);
            eventInstance.stop(STOP_MODE.ALLOWFADEOUT);
            eventInstance.release();
        }
        
        public void DestroyMapInstance(EventInstance eventInstance)
        {
            if (!mapEventInstances.Contains(eventInstance)) return;
            
            mapEventInstances.Remove(eventInstance);
            eventInstance.stop(STOP_MODE.ALLOWFADEOUT);
            eventInstance.release();
        }

        public void ClearAllInstances()
        {
            foreach (var eventInstance in eventInstances)
            {
                eventInstance.stop(STOP_MODE.IMMEDIATE);
                eventInstance.release();
            }
            
            eventInstances.Clear();
        }

        public void ClearAllMapInstances()
        {
            foreach (var eventInstance in mapEventInstances)
            {
                eventInstance.stop(STOP_MODE.IMMEDIATE);
                eventInstance.release();
            }
            
            mapEventInstances.Clear();
        }
        
        public async UniTask StartMusic(EventReference eventReference)
        {
            if (!RuntimeManager.IsInitialized)
            {
                Logs.LogWarning("[SoundManager] FMOD not initialized yet, retrying...");
                await WaitForFMODAndStartMusic(eventReference);
                return;
            }

            if (musicEventInstance.isValid())
            {
                await StopMusic();
            }

            musicEventInstance = CreateInstance(eventReference, false);
            musicEventInstance.start();
        }

        private async UniTask WaitForFMODAndStartMusic(EventReference eventReference)
        {
            while (!RuntimeManager.IsInitialized) await Task.Delay(TimeSpan.FromSeconds(0.1f)) ;
            StartMusic(eventReference).Forget();
        }

        public UniTask StopMusic()
        {
            if (musicEventInstance.isValid())
            {
                musicEventInstance.stop(STOP_MODE.ALLOWFADEOUT);
                musicEventInstance.release();
            }
            return UniTask.CompletedTask;
        }

        public void SetPhase(int value, PhaseType type)
        {
            switch (type)
            {
                case PhaseType.MUSIC :
                    RuntimeManager.StudioSystem.setParameterByName("MusicPhase", value);
                    break;
                case PhaseType.AMBIENCE :
                    RuntimeManager.StudioSystem.setParameterByName("AmbiPhase", value);
                    break;
                case PhaseType.TWICE :
                    RuntimeManager.StudioSystem.setParameterByName("MusicPhase", value);
                    RuntimeManager.StudioSystem.setParameterByName("AmbiPhase", value);
                    break;
            }
        }
        
        public void SetPause(int value)
        {
            RuntimeManager.StudioSystem.setParameterByName("Pause", value);
        }
        
        public async UniTask StartAmbience(bool night, bool rain)
        {
            if (!RuntimeManager.IsInitialized)
            {
                Logs.LogWarning("[SoundManager] FMOD not initialized yet, retrying...");
                await WaitForFMODAndStartAmbience(night, rain);
                return;
            }

            if (musicEventInstance.isValid())
            {
                await StopAmbiance();
            }

            EventReference eventRef;
            if (night) 
                eventRef = FMODEvents.AMBI_Night;
            else
                eventRef = FMODEvents.AMBI_Day;

            ambienceEventInstance = CreateInstance(eventRef, false);
            ambienceEventInstance.setParameterByName("Rain", rain? 1 : 0);
            ambienceEventInstance.start();
        }

        private async UniTask WaitForFMODAndStartAmbience(bool night, bool rain)
        {
            while (!RuntimeManager.IsInitialized) await Task.Delay(TimeSpan.FromSeconds(0.1f)) ;
            StartAmbience(night, rain).Forget();
        }

        public UniTask StopAmbiance()
        {
            if (ambienceEventInstance.isValid())
            {
                ambienceEventInstance.stop(STOP_MODE.ALLOWFADEOUT);
                ambienceEventInstance.release();
            }
            return UniTask.CompletedTask;
        }

        public static void SetVolume(BusEnum bus, float vol)
        {
            switch (bus)
            {
                case BusEnum.MASTER:
                    masterBus.setVolume(vol);
                    break;
                case BusEnum.MUSIC:
                    musicBus.setVolume(vol);
                    break;
                case BusEnum.SFX:
                    sfxBus.setVolume(vol);
                    break;
                case BusEnum.AMBIENCE:
                    ambienceBus.setVolume(vol);
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
            mapEventInstances = new List<EventInstance>();

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
