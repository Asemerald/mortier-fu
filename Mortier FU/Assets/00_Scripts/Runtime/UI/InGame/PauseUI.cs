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
        
        [SerializeField] private GameObject _pauseMenu;

        private GamePauseSystem _gamePauseSystem;

        private void OnDestroy()
        {
            _gamePauseSystem.Paused -= Show;
            _gamePauseSystem.Resumed -= Hide;
        }
        
        public void OnResumeButton()
        {
            _gamePauseSystem.Resume();
        }

        private void Start()
        {
            Hide();
            
            _gamePauseSystem = SystemManager.Instance.Get<GamePauseSystem>();
          //  _gamePauseSystem.RestoreSettingsFromSave();
           // _gamePauseSystem.UpdateUIFromSave(_fullscreenToggle, _vSyncToggle, _masterVolumeSlider, _musicVolumeSlider,
           //     _sfxVolumeSlider);
            _gamePauseSystem.BindUIEvents(_fullscreenToggle, _vSyncToggle, _masterVolumeSlider, _musicVolumeSlider,
                _sfxVolumeSlider);
            
            //_gamePauseSystem.SaveSettings();
            
            _gamePauseSystem.Paused += Show;
            _gamePauseSystem.Resumed += Hide;
        }

        private void Resume()
        {
            Hide();
        }

        private void PauseGame()
        {
            Show();
        }

        private void Hide() => _pauseMenu.SetActive(false);
        private void Show() => _pauseMenu.SetActive(true);
        
    }
}