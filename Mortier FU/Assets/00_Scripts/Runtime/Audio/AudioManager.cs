using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using MortierFu.Shared;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace MortierFu
{
    public class AudioManager : MonoBehaviour
    {
        private List<EventInstance> eventInstances;
        
        //BUS
        private Bus masterBus;
        private Bus sfxBus;
        private Bus musicBus;
        private Bus ambienceBus;

        private EventInstance musicEventInstance;

        public FMODEventsSO FMODEvents;
        [SerializeField] private AnimationCurve volumeCurve;
        
        public static AudioManager instance { get; private set; }
        private void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);
            
            eventInstances = new List<EventInstance>();

            masterBus = RuntimeManager.GetBus("bus:/");
            musicBus = RuntimeManager.GetBus("bus:/MUSIC");
            sfxBus = RuntimeManager.GetBus("bus:/GAMEPLAY");
            ambienceBus = RuntimeManager.GetBus("bus:/AMBIENCE");
        }
        
        public void PlayOneShot(EventReference sound, Vector3 position)
        {
            RuntimeManager.PlayOneShot(sound, position);
        }
        
        public EventInstance CreateInstance(EventReference eventReference, bool addToList = true)
        {
            var eventInstance = RuntimeManager.CreateInstance(eventReference);

            if (addToList)
                eventInstances.Add(eventInstance);
            return eventInstance;
        }

        public void StartMusic(EventReference musicEventReference)
        {
            if (!RuntimeManager.IsInitialized)
            {
                Logs.LogWarning("[SoundManager] FMOD not initialized yet, retrying...");
                StartCoroutine(WaitForFMODAndStartMusic(musicEventReference));
                return;
            }

            musicEventInstance = CreateInstance(musicEventReference, false);
            musicEventInstance.start();
        }

        private IEnumerator WaitForFMODAndStartMusic(EventReference musicEventReference)
        {
            while (!RuntimeManager.IsInitialized) yield return new WaitForSeconds(0.1f);
            StartMusic(musicEventReference);
        }

        private void StopMusic()
        {
            if (musicEventInstance.isValid())
            {
                musicEventInstance.stop(STOP_MODE.ALLOWFADEOUT);
                musicEventInstance.release();
            }
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
    }
}
