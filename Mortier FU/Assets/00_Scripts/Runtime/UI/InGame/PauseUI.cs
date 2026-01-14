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

        [SerializeField] private GameObject[] _mortarImages;

        private Vector3[] _mortarInitialPositions;
        private Quaternion[] _mortarInitialRotations;

        private EventSystem _eventSystem;
        private LobbyService _lobbyService;

        private GameModeBase _gm;

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
            _gm = GameService.CurrentGameMode as GameModeBase;
            _gamePauseSystem = SystemManager.Instance.Get<GamePauseSystem>();
            _lobbyService = ServiceManager.Instance.Get<LobbyService>();
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
            _quitButton.onClick.AddListener(Application.Quit);

            if (_gm != null)
            {
                _endGameButton.onClick.AddListener(_gm.EndGame);
                _endGameButton.onClick.AddListener(_gamePauseSystem.UnPause);

                _mainMenuButton.onClick.AddListener(_gm.EndGame);
                _mainMenuButton.onClick.AddListener(_gamePauseSystem.UnPause);
            }
            _mortarInitialPositions = new Vector3[_mortarImages.Length];

            for (int i = 0; i < _mortarImages.Length; i++)
            {
                _mortarInitialPositions[i] = _mortarImages[i].transform.position;
            }
            
            _mortarInitialRotations = new Quaternion[_mortarImages.Length];

            for (int i = 0; i < _mortarImages.Length; i++)
            {
                _mortarInitialPositions[i] = _mortarImages[i].transform.position;
                _mortarInitialRotations[i] = _mortarImages[i].transform.rotation;
                _mortarImages[i].SetActive(false);
            }
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
            
            foreach (GameObject mortarImage in _mortarImages)
            {
                mortarImage.SetActive(false);
            }
        }
        
        private void RandomizeMortarPositions()
        {
            int playerCount = _lobbyService.CurrentPlayerCount;
            playerCount = Mathf.Min(playerCount, _mortarImages.Length);

            int[] indices = new int[_mortarInitialPositions.Length];
            for (int i = 0; i < indices.Length; i++)
                indices[i] = i;

            Shuffle(indices);

            for (int mortarIndex = 0; mortarIndex < playerCount; mortarIndex++)
            {
                int positionIndex = indices[mortarIndex];

                var mortar = _mortarImages[mortarIndex];

                mortar.transform.position = _mortarInitialPositions[positionIndex];

                mortar.transform.rotation = _mortarInitialRotations[mortarIndex];

                bool shouldRotate =
                    (mortarIndex % 2) != (positionIndex % 2);

                if (shouldRotate)
                {
                    mortar.transform.Rotate(0f, 0f, 180f);
                }
                
                mortar.SetActive(true);
            }
        }
        
        private void Shuffle(int[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }

        private void Show()
        {
            _pausePanel.SetActive(true);
            RandomizeMortarPositions();
        }
    }
}