using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public class SettingsPanel : UIPanel
    {
        [field: SerializeField] private Toggle fullscreenToggle { get; set; }
        [field: SerializeField] private Toggle vsyncToggle { get; set; }
        [field: SerializeField] private Slider MasterVolumeSlider { get; set; }
        [field: SerializeField] private Slider MusicVolumeSlider { get; set; }
        [field: SerializeField] private Slider SFXVolumeSlider { get; set; }

        private SaveService _saveService;

        private void Awake()
        {
            // Cache SaveService reference
            _saveService = ServiceManager.Instance.Get<SaveService>();

            RestoreSettingsFromSave();
            UpdateUIFromSave();
            BindUIEvents();
        }
        private void Start()
        {
            Hide();
        }

        private void RestoreSettingsFromSave()
        {
            var s = _saveService.Settings;
            Screen.fullScreen = s.IsFullscreen;
            QualitySettings.vSyncCount = s.IsVSyncEnabled ? 1 : 0;
            
            // TODO : Apply volume settings to FMOD Bus
        }
        private void UpdateUIFromSave()
        {
            var s = _saveService.Settings;

            fullscreenToggle.isOn = s.IsFullscreen;
            vsyncToggle.isOn = s.IsVSyncEnabled;

            MasterVolumeSlider.value = s.MasterVolume;
            MusicVolumeSlider.value = s.MusicVolume;
            SFXVolumeSlider.value = s.SfxVolume;
        }
        private void BindUIEvents()
        {
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);

            MasterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            MusicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            SFXVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }
        
        private async void OnFullscreenChanged(bool value)
        {
            Screen.fullScreen = value;
            _saveService.Settings.IsFullscreen = value;
            await _saveService.SaveSettings();
        }

        private async void OnVSyncChanged(bool value)
        {
            QualitySettings.vSyncCount = value ? 1 : 0;
            _saveService.Settings.IsVSyncEnabled = value;
            await _saveService.SaveSettings();
        }

        private async void OnMasterVolumeChanged(float value)
        {
            // TODO : Apply volume to FMOD Bus
            _saveService.Settings.MasterVolume = value;
            await _saveService.SaveSettings();
        }

        private async void OnMusicVolumeChanged(float value)
        {
            // TODO : Apply volume to FMOD Bus
            _saveService.Settings.MusicVolume = value;
            await _saveService.SaveSettings();
        }

        private async void OnSfxVolumeChanged(float value)
        {
            // TODO : Apply volume to FMOD Bus
            _saveService.Settings.SfxVolume = value;
            await _saveService.SaveSettings();
        }
    }
}
