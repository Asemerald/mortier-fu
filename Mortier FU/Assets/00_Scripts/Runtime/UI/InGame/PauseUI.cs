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
        
        [SerializeField] private GameObject _pausePanel;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameObject _controlsPanel;
        
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _controlsButton;
        [SerializeField] private Button _endGameButton;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _quitButton;
        
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
            
            _settingsButton.onClick.AddListener(ShowSettingsPanel);
            _controlsButton.onClick.AddListener(ShowControlsPanel);
        }

        private void UnPause()
        {
            Hide();
        }

        private void Pause()
        {
            Show();

            _eventSystem.SetSelectedGameObject(_settingsButton.gameObject);
        }
        
        private void ShowSettingsPanel()
        {
            _settingsPanel.SetActive(true);
            _controlsPanel.SetActive(false);
            _pausePanel.SetActive(false);
            
            _eventSystem.SetSelectedGameObject(_fullscreenToggle.gameObject);
        }
        
        private void ShowControlsPanel()
        {
            _controlsPanel.SetActive(true);
            _pausePanel.SetActive(true);
            _settingsPanel.SetActive(false);
            
            _eventSystem.SetSelectedGameObject(null);
        }

        private void Hide()
        {
            _settingsPanel.SetActive(false);
            _controlsPanel.SetActive(false);
            _pausePanel.SetActive(false);
        }

        private void Show() => _pausePanel.SetActive(true);
        
    }
}