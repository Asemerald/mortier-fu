using System;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public class GamePauseSystem : IGameSystem
    {
        private SaveService _saveService;

        public bool IsPaused { get; private set; }

        public event Action<PlayerManager> Paused;
        public event Action Resumed;
        public event Action Canceled;

        public void TogglePause(PlayerManager player)
        {
            if (IsPaused)
            {
                UnPause();
            }
            else
            {
                Logs.LogWarning(player.PlayerIndex.ToString());
                Pause(player);
            }
        }

        private void UnPause()
        {
            if (!IsPaused) return;

            IsPaused = false;
            Time.timeScale = 1f;
            Resumed?.Invoke();
        }

        private void Pause(PlayerManager player)
        {
            if (IsPaused) return;

            Logs.LogWarning(player.PlayerIndex.ToString());
            IsPaused = true;
            Time.timeScale = 0f;
            Paused?.Invoke(player);
        }

        public void Cancel()
        {
            Canceled?.Invoke();
        }

        public void RestoreSettingsFromSave()
        {
            var s = _saveService.Settings;
            Screen.fullScreen = s.IsFullscreen;
            QualitySettings.vSyncCount = s.IsVSyncEnabled ? 1 : 0;
        }

        public void UpdateUIFromSave(Toggle fullscreenToggle, Toggle vsyncToggle, Slider masterVolumeSlider,
            Slider musicVolumeSlider, Slider sFXVolumeSlider)
        {
            var s = _saveService.Settings;

            fullscreenToggle.isOn = s.IsFullscreen;
            vsyncToggle.isOn = s.IsVSyncEnabled;

            masterVolumeSlider.value = s.MasterVolume;
            musicVolumeSlider.value = s.MusicVolume;
            sFXVolumeSlider.value = s.SfxVolume;
        }

        public void BindUIEvents(Toggle fullscreenToggle, Toggle vsyncToggle, Slider MasterVolumeSlider,
            Slider MusicVolumeSlider, Slider SFXVolumeSlider)
        {
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);

            MasterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            MusicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            SFXVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
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

        public void SaveSettings()
        {
            _saveService.SaveSettings().Forget();
        }
        
        public UniTask OnInitialize()
        {
            IsPaused = false;
            _saveService = ServiceManager.Instance.Get<SaveService>();
            return UniTask.CompletedTask;
        }
        
        public void Dispose()
        {
            IsPaused = false;
            Time.timeScale = 1f;

            Paused = null;
            Resumed = null;
            Canceled = null;
        }

        public bool IsInitialized { get; set; }
    }
}