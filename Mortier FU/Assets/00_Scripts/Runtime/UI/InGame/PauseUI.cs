using UnityEngine;
using UnityEngine.EventSystems;
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
        
        private EventSystem _eventSystem;
        
        private GamePauseSystem _gamePauseSystem;

        private void OnDestroy()
        {
            _gamePauseSystem.Paused -= Pause;
            _gamePauseSystem.Resumed -= UnPause;
        }

        private void Start()
        {
            Hide();
            _eventSystem = EventSystem.current;
            
            _gamePauseSystem = SystemManager.Instance.Get<GamePauseSystem>();
          //  _gamePauseSystem.RestoreSettingsFromSave();
           // _gamePauseSystem.UpdateUIFromSave(_fullscreenToggle, _vSyncToggle, _masterVolumeSlider, _musicVolumeSlider,
           //     _sfxVolumeSlider);
            _gamePauseSystem.BindUIEvents(_fullscreenToggle, _vSyncToggle, _masterVolumeSlider, _musicVolumeSlider,
                _sfxVolumeSlider);
            
            //_gamePauseSystem.SaveSettings();
            
            _gamePauseSystem.Paused += Pause;
            _gamePauseSystem.Resumed += UnPause;
        }

        private void UnPause()
        {
            Hide();
        }

        private void Pause()
        {
            Show();

            _eventSystem.SetSelectedGameObject(_fullscreenToggle.gameObject);
        }

        private void Hide() => _pauseMenu.SetActive(false);
        private void Show() => _pauseMenu.SetActive(true);
        
    }
}