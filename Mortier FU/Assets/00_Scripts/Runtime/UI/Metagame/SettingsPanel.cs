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
            OnVSyncSelected(false);
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
        
        private void SaveSettings()
        {
            _saveService.SaveSettings().Forget();
        }

        public override void Hide()
        {
            base.Hide();
            SaveSettings();
        }
        
        #region UiPanel Feedbacks

        [Header("Assets Feedbacks")]
        [SerializeField] private Image[] fullScreenSelectedAssets;
        [SerializeField] private Image[] vsyncSelectedAssets;
        

        public void OnFullScreenSelected(bool selected)
        {
            if (fullScreenSelectedAssets == null) return;
            
            // If selected, enable assets 1 and 2 and disable asset 0, else disable them and enable assets 0
            if (selected)
            {
                fullScreenSelectedAssets[0].enabled = false;
                fullScreenSelectedAssets[1].enabled = true;
                fullScreenSelectedAssets[2].enabled = true;
            }
            else
            {
                fullScreenSelectedAssets[0].enabled = true;
                fullScreenSelectedAssets[1].enabled = false;
                fullScreenSelectedAssets[2].enabled = false;
            }
        }
        
        public void OnVSyncSelected(bool selected)
        {
            if (vsyncSelectedAssets == null) return;
            
            // If selected, enable assets 1 and 2 and disable asset 0, else disable them and enable assets 0
            if (selected)
            {
                vsyncSelectedAssets[0].enabled = false;
                vsyncSelectedAssets[1].enabled = true;
                vsyncSelectedAssets[2].enabled = true;
            }
            else
            {
                vsyncSelectedAssets[0].enabled = true;
                vsyncSelectedAssets[1].enabled = false;
                vsyncSelectedAssets[2].enabled = false;
            }
        }

        #endregion
    }
}
