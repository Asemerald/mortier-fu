using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MortierFu.Shared;

namespace MortierFu
{
    public class GamePauseSystem : IGameSystem
    {
        private SaveService _saveService;

        private readonly HashSet<object> _pauseBlockers = new();
        
        public bool IsPaused { get; private set; }

        public event Action<PlayerManager> Paused;
        public event Action Resumed;
        public event Action Canceled;

        public PlayerManager PauseOwner { get; private set; }
        
        public bool IsPauseBlocked => _pauseBlockers.Count > 0;

        public void SetPauseBlocked(object owner, bool blocked)
        {
            if (owner is null)
                return;

            if (blocked)
            {
                _pauseBlockers.Add(owner);
                return;
            }

            _pauseBlockers.Remove(owner);
        }

        public void TogglePause(PlayerManager player)
        {
            if (IsPaused)
            {
                Resume();
                return;
            }

            if (IsPauseBlocked)
            {
                Logs.Log("[GamePauseSystem] Pause ignored because pause is currently blocked.");
                return;
            }

            Pause(player);
        }

        public void Resume()
        {
            if (!IsPaused)
                return;

            IsPaused = false;
            PauseOwner = null;

            Time.timeScale = 1f;
            Resumed?.Invoke();
        }

        private void Pause(PlayerManager player)
        {
            if (IsPaused)
                return;

            IsPaused = true;
            PauseOwner = player;

            Time.timeScale = 0f;
            Paused?.Invoke(player);
        }

        public void Cancel() => Canceled?.Invoke();

        public void RestoreSettingsFromSave()
        {
            var s = _saveService.Settings;
            Screen.fullScreen = s.IsFullscreen;
            QualitySettings.vSyncCount = s.IsVSyncEnabled ? 1 : 0;
        }

        public void UpdateUIFromSave(Toggle fullscreenToggle, Toggle vsyncToggle, Slider masterVolumeSlider, Slider musicVolumeSlider, Slider sFXVolumeSlider)
        {
            var s = _saveService.Settings;

            fullscreenToggle.SetIsOnWithoutNotify(s.IsFullscreen);
            vsyncToggle.SetIsOnWithoutNotify(s.IsVSyncEnabled);

            masterVolumeSlider.SetValueWithoutNotify(s.MasterVolume);
            musicVolumeSlider.SetValueWithoutNotify(s.MusicVolume);
            sFXVolumeSlider.SetValueWithoutNotify(s.SfxVolume);
        }

        public void BindUIEvents(Toggle fullscreenToggle, Toggle vsyncToggle, Slider masterVolumeSlider, Slider musicVolumeSlider, Slider sfxVolumeSlider)
        {
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);

            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        private void OnFullscreenChanged(bool value)
        {
            Screen.fullScreen = value;
            _saveService.Settings.IsFullscreen = value;
        }

        private void OnVSyncChanged(bool value)
        {
            QualitySettings.vSyncCount = value ? 1 : 0;
            _saveService.Settings.IsVSyncEnabled = value;
        }

        private void OnMasterVolumeChanged(float value)
        {
            // TODO : Apply volume to FMOD Bus
            _saveService.Settings.MasterVolume = value;
        }

        private void OnMusicVolumeChanged(float value)
        {
            // TODO : Apply volume to FMOD Bus
            _saveService.Settings.MusicVolume = value;
        }

        private void OnSfxVolumeChanged(float value)
        {
            // TODO : Apply volume to FMOD Bus
            _saveService.Settings.SfxVolume = value;
        }

        public UniTask OnInitialize()
        {
            IsPaused = false;
            _saveService = ServiceManager.Instance.Get<SaveService>();
            return UniTask.CompletedTask;
        }
        
        public void SaveSettings()
        {
            _saveService.SaveSettings().Forget();
        }
        
        public void Dispose()
        {
            IsPaused = false;
            Time.timeScale = 1f;

            Paused = null;
            Resumed = null;
            Canceled = null;
            
            PauseOwner = null;
            _pauseBlockers.Clear();
        }

        public bool IsInitialized { get; set; }
    }
}