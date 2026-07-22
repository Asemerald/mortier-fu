using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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

        private void OnEnable()
        {
            EventSystem.current.SetSelectedGameObject(fullscreenToggle.gameObject);
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
        
        private void OnFullscreenChanged(bool value)
        {
            Screen.fullScreen = value;
            _saveService.Settings.IsFullscreen = value;
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Tick);
        }

        private void OnVSyncChanged(bool value)
        {
            QualitySettings.vSyncCount = value ? 1 : 0;
            _saveService.Settings.IsVSyncEnabled = value;
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Tick);
        }

        private void OnMasterVolumeChanged(float value)
        {
            // TODO : Apply volume to FMOD Bus
            _saveService.Settings.MasterVolume = value;
            AudioService.SetVolume(AudioService.BusEnum.MASTER, value);
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Slider);
        }

        private void OnMusicVolumeChanged(float value)
        {
            // TODO : Apply volume to FMOD Bus
            _saveService.Settings.MusicVolume = value;
            AudioService.SetVolume(AudioService.BusEnum.MUSIC, value);
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Slider);
        }

        private void OnSfxVolumeChanged(float value)
        {
            // TODO : Apply volume to FMOD Bus
            _saveService.Settings.SfxVolume = value;
            AudioService.SetVolume(AudioService.BusEnum.SFX, value);
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Slider);
        }
        
        private void SaveSettings()
        {
            _saveService.SaveSettings().Forget();
        }

        public override void Hide()
        {
            base.Hide();
            SaveSettings();
        }
    }
}
