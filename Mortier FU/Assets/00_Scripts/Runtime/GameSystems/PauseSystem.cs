using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public class PauseSystem : IGameSystem
    {
        private SaveService _saveService;

        public void RestoreSettingsFromSave()
        {
            var s = _saveService.Settings;
            Screen.fullScreen = s.IsFullscreen;
            QualitySettings.vSyncCount = s.IsVSyncEnabled ? 1 : 0;

            // TODO : Apply volume settings to FMOD Bus
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
        
        public void Dispose()
        {
        }

        public UniTask OnInitialize()
        {
            _saveService = ServiceManager.Instance.Get<SaveService>();

            return UniTask.CompletedTask;
        }

        public bool IsInitialized { get; set; }
    }
}