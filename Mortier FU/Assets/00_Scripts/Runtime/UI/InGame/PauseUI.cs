using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public class PauseUI : MonoBehaviour
    {
        [SerializeField] private Toggle _fullscreenToggle;

        [SerializeField] private Toggle _vSyncToggle;

        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;

        private PauseSystem _pauseSystem;

        private void Awake()
        {
            Hide();
            
            _pauseSystem = SystemManager.Instance.Get<PauseSystem>();
            _pauseSystem.RestoreSettingsFromSave();
            _pauseSystem.UpdateUIFromSave(_fullscreenToggle, _vSyncToggle, _masterVolumeSlider, _musicVolumeSlider,
                _sfxVolumeSlider);
            _pauseSystem.BindUIEvents(_fullscreenToggle, _vSyncToggle, _masterVolumeSlider, _musicVolumeSlider,
                _sfxVolumeSlider);
        }

        private void Start()
        {
            _pauseSystem.SaveSettings();
        }

        private void Resume()
        {
            Hide();
        }

        private void PauseGame()
        {
            Show();
        }

        private void Hide() => gameObject.SetActive(false);
        private void Show() => gameObject.SetActive(true);
        
    }
}