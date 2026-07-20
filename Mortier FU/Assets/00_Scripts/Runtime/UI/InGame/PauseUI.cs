using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using PrimeTween;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace MortierFu
{
    public class PauseUI : MonoBehaviour, IPlayerUIInputHandler
    {
        [Header("Toggles & Sliders")] [SerializeField]
        private Toggle _fullscreenToggle;

        [SerializeField] private Toggle _vSyncToggle;
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;

        [Header("Panels")] [SerializeField] private GameObject _pausePanel;
        [SerializeField] private GameObject _pauseBackground;
        [SerializeField] private GameObject _blackPanel;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameObject _controlsPanel;
        [SerializeField] private GameObject _endGameConfirmationPanel;
        [SerializeField] private GameObject _quitGameConfirmationPanel;

        //[SerializeField] private RawImage _pauseTopText;
        //[SerializeField] private RawImage _pauseBottomText;

        [Header("Buttons")] [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _controlsButton;
        [SerializeField] private Button _endGameButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _confirmEndGameButton;
        [SerializeField] private Button _cancelEndGameButton;
        [SerializeField] private Button _confirmQuitGameButton;
        [SerializeField] private Button _cancelQuitGameButton;

        /*[Header("Animation Settings")] [SerializeField]
        private float _tilablePauseSpeed = 0.5f;*/

        [Header("Panel Animation Settings")] [SerializeField]
        private float _panelScaleDuration = 0.5f;

        [SerializeField] private Ease _panelScaleEase = Ease.OutElastic;

        [SerializeField] private LobbyReturnToMainMenuController  _lobbyReturnToMainMenuController;
        
        private CancellationTokenSource _controlPanelCTS;
        private CancellationTokenSource _endGamePanelCTS;
        private CancellationTokenSource _quitPanelCTS;

        private Tween[] _activeHeadTweens;
        private CancellationTokenSource _animateCancellation;

        private EventSystem _eventSystem;
        private GamePauseSystem _gamePauseSystem;
        private LobbyService _lobbyService;
        private ShakeService _shakeService;
        private PlayerManager _playerManager;
        private GameService _gameService;

        private PlayerControlContext _previousPlayerContext;
        private bool _hasPreviousPlayerContext;

        private PlayerUIInputService UIInputService =>
            ServiceManager.Instance?.Get<PlayerUIInputService>();

        private void Start()
        {
            InitReferences();
            InitUI();
            BindPauseSystemEvents();
            Hide();
        }

        private void OnDisable()
        {
            RemoveFromUIInputService();
            ExitPauseInputContext();

            UnbindPauseSystemEvents();
            StopAllActiveAnimations();
        }

        private void OnDestroy()
        {
            RemoveFromUIInputService();
            ExitPauseInputContext();

            UnbindPauseSystemEvents();
            UnbindUIEvents();
            StopAllActiveAnimations();
        }

        /*
        private void Update()
        {
            if (_gamePauseSystem is null || !_gamePauseSystem.IsPaused)
                return;

       //     if (_pauseTopText)
          //      ScrollRawImageUV(_pauseTopText, -_tilablePauseSpeed);

      //      if (_pauseBottomText)
         //       ScrollRawImageUV(_pauseBottomText, _tilablePauseSpeed);
        }
        */

        private void InitReferences()
        {
            _eventSystem = EventSystem.current;
            _gameService = ServiceManager.Instance.Get<GameService>();
            _gamePauseSystem = SystemManager.Instance.Get<GamePauseSystem>();
            _lobbyService = ServiceManager.Instance.Get<LobbyService>();
            _shakeService = ServiceManager.Instance.Get<ShakeService>();

            _playerManager = TryGetPlayerByIndex(0);

            if (!_playerManager)
            {
                Logs.LogWarning("[PauseUI] Player 1 was not found during initialization.", this);
            }
        }

        private PlayerManager TryGetPlayerByIndex(int playerIndex)
        {
            if (_lobbyService is null)
                return null;

            var players = _lobbyService.GetPlayers();

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];

                if (!player)
                    continue;

                if (player.PlayerIndex == playerIndex)
                    return player;
            }

            return null;
        }

        private void InitUI()
        {
            if (_gamePauseSystem is null)
            {
                Logs.LogError("[PauseUI] GamePauseSystem was not found.]");
                return;
            }

            _gamePauseSystem.RestoreSettingsFromSave();

            _gamePauseSystem.UpdateUIFromSave(
                _fullscreenToggle,
                _vSyncToggle,
                _masterVolumeSlider,
                _musicVolumeSlider,
                _sfxVolumeSlider
            );

            _gamePauseSystem.BindUIEvents(
                _fullscreenToggle,
                _vSyncToggle,
                _masterVolumeSlider,
                _musicVolumeSlider,
                _sfxVolumeSlider
            );

            if (_settingsButton)
                _settingsButton.onClick.AddListener(OpenSettingPanel);

            if (_controlsButton)
                _controlsButton.onClick.AddListener(OpenControlPanel);

            if (_endGameButton)
            {
                _endGameButton.onClick.AddListener(OpenEndGamePanel);
            }

            if (_quitButton)
                _quitButton.onClick.AddListener(OpenQuitPanel);

            if (_fullscreenToggle)
                _fullscreenToggle.onValueChanged.AddListener(PlayToggleFeedback);

            if (_vSyncToggle)
                _vSyncToggle.onValueChanged.AddListener(PlayToggleFeedback);

            if (_masterVolumeSlider)
                _masterVolumeSlider.onValueChanged.AddListener(PlaySliderFeedback);

            if (_musicVolumeSlider)
                _musicVolumeSlider.onValueChanged.AddListener(PlaySliderFeedback);

            if (_sfxVolumeSlider)
                _sfxVolumeSlider.onValueChanged.AddListener(PlaySliderFeedback);

            if (_confirmEndGameButton)
                _confirmEndGameButton.onClick.AddListener(_lobbyReturnToMainMenuController ? OnConfirmReturnToMainMenu : OnConfirmEndGame);

            if (_cancelEndGameButton)
                _cancelEndGameButton.onClick.AddListener(Return);

            if (_confirmQuitGameButton)
                _confirmQuitGameButton.onClick.AddListener(Application.Quit);

            if (_cancelQuitGameButton)
                _cancelQuitGameButton.onClick.AddListener(Return);
        }

        private void UnbindUIEvents()
        {
            if (_settingsButton)
                _settingsButton.onClick.RemoveListener(OpenSettingPanel);

            if (_controlsButton)
                _controlsButton.onClick.RemoveListener(OpenControlPanel);

            if (_endGameButton)
                _endGameButton.onClick.RemoveListener(OpenEndGamePanel);

            if (_quitButton)
                _quitButton.onClick.RemoveListener(OpenQuitPanel);

            if (_fullscreenToggle)
                _fullscreenToggle.onValueChanged.RemoveListener(PlayToggleFeedback);

            if (_vSyncToggle)
                _vSyncToggle.onValueChanged.RemoveListener(PlayToggleFeedback);

            if (_masterVolumeSlider)
                _masterVolumeSlider.onValueChanged.RemoveListener(PlaySliderFeedback);

            if (_musicVolumeSlider)
                _musicVolumeSlider.onValueChanged.RemoveListener(PlaySliderFeedback);

            if (_sfxVolumeSlider)
                _sfxVolumeSlider.onValueChanged.RemoveListener(PlaySliderFeedback);

            if (_confirmEndGameButton)
                _confirmEndGameButton.onClick.RemoveListener(_lobbyReturnToMainMenuController ? OnConfirmReturnToMainMenu : OnConfirmEndGame);

            if (_cancelEndGameButton)
                _cancelEndGameButton.onClick.RemoveListener(Return);

            if (_confirmQuitGameButton)
                _confirmQuitGameButton.onClick.RemoveListener(Application.Quit);

            if (_cancelQuitGameButton)
                _cancelQuitGameButton.onClick.RemoveListener(Return);
        }

        private void OnConfirmEndGame()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Select);

            if (_gamePauseSystem is not null && _gamePauseSystem.IsPaused)
            {
                _gamePauseSystem.TogglePause();
            }

            _gameService?.ReturnToLobby();
        }

        private void OnConfirmReturnToMainMenu()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Select);

            if (_gamePauseSystem is not null && _gamePauseSystem.IsPaused)
            {
                _gamePauseSystem.TogglePause();
            }

            _lobbyReturnToMainMenuController.ConfirmReturnToMainMenu();
        }

        private void ScrollRawImageUV(RawImage image, float speed)
        {
            if (!image)
                return;

            var rect = image.uvRect;
            rect.x = math.frac(rect.x + speed * Time.unscaledDeltaTime);
            image.uvRect = rect;
        }

        private void StopAllActiveAnimations()
        {
            if (_activeHeadTweens is not null)
            {
                for (int i = 0; i < _activeHeadTweens.Length; i++)
                {
                    if (_activeHeadTweens[i].isAlive)
                        _activeHeadTweens[i].Stop();
                }
            }

            _animateCancellation?.Cancel();
            _animateCancellation?.Dispose();
            _animateCancellation = null;

            SafeCancelCts(ref _controlPanelCTS);
            SafeCancelCts(ref _endGamePanelCTS);
            SafeCancelCts(ref _quitPanelCTS);
        }

        private void SafeCancelCts(ref CancellationTokenSource cts)
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        private CancellationTokenSource CreatePanelCts(CancellationTokenSource current)
        {
            current?.Cancel();
            current?.Dispose();
            return new CancellationTokenSource();
        }

        private void OpenSettingPanel()
        {
            PlayPanelSelectionFeedback();

            if (_settingsPanel)
                _settingsPanel.SetActive(true);

            if (_pausePanel)
                _pausePanel.SetActive(false);

            if (_eventSystem && _fullscreenToggle)
                _eventSystem.SetSelectedGameObject(_fullscreenToggle.gameObject);
        }

        private void OpenControlPanel()
        {
            PlayPanelSelectionFeedback();
            AnimateOpenControlPanel().Forget();
        }

        private void OpenEndGamePanel()
        {
            PlayPanelSelectionFeedback();
            AnimateOpenEndGamePanel().Forget();
        }

        private void OpenQuitPanel()
        {
            PlayPanelSelectionFeedback();
            AnimateOpenQuitPanel().Forget();
        }

        private async UniTask AnimateOpenControlPanel()
        {
            _controlPanelCTS = CreatePanelCts(_controlPanelCTS);

            await AnimateOpenPanel(
                _controlsPanel,
                _controlPanelCTS,
                null,
                0.2f,
                Ease.OutCubic
            );
        }

        private async UniTask AnimateOpenEndGamePanel()
        {
            _endGamePanelCTS = CreatePanelCts(_endGamePanelCTS);

            await AnimateOpenPanel(
                _endGameConfirmationPanel,
                _endGamePanelCTS,
                _confirmEndGameButton ? _confirmEndGameButton.gameObject : null,
                _panelScaleDuration,
                _panelScaleEase
            );
        }

        private async UniTask AnimateOpenQuitPanel()
        {
            _quitPanelCTS = CreatePanelCts(_quitPanelCTS);

            await AnimateOpenPanel(
                _quitGameConfirmationPanel,
                _quitPanelCTS,
                _confirmQuitGameButton ? _confirmQuitGameButton.gameObject : null,
                _panelScaleDuration,
                _panelScaleEase
            );
        }

        private async UniTask AnimateOpenPanel(
            GameObject panel,
            CancellationTokenSource cts,
            GameObject selectedButton,
            float duration,
            Ease ease
        )
        {
            if (!panel || cts is null)
                return;

            CancellationToken ct = cts.Token;

            panel.transform.localScale = Vector3.zero;
            panel.SetActive(true);

            if (_eventSystem && selectedButton)
                _eventSystem.SetSelectedGameObject(selectedButton);

            if (_shakeService is not null && _playerManager)
                _shakeService.ShakeController(_playerManager, ShakeService.ShakeType.MID);

            try
            {
                await Tween.Scale(panel.transform, 1f, duration, ease, useUnscaledTime: true)
                    .ToUniTask(cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                // Safe cancellation.
            }
        }

        private void PlayPanelSelectionFeedback()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Select);

            if (_shakeService is not null && _playerManager)
                _shakeService.ShakeController(_playerManager, ShakeService.ShakeType.MID);
        }

        private void Show()
        {
            if (_pausePanel)
            {
                AnimatePausePanel().Forget();
            }

            if (_pauseBackground)
                _pauseBackground.SetActive(true);

            if (_blackPanel)
                _blackPanel.SetActive(true);

            // if (_pauseTopText && _pauseTopText.transform.parent)
            //   _pauseTopText.transform.parent.gameObject.SetActive(true);

//            if (_pauseBottomText && _pauseBottomText.transform.parent)
            //              _pauseBottomText.transform.parent.gameObject.SetActive(true);

            SafeCancelCts(ref _animateCancellation);
            _animateCancellation = new CancellationTokenSource();
        }

        private void Hide()
        {
            if (_pausePanel)
                _pausePanel.SetActive(false);

            if (_pauseBackground)
                _pauseBackground.SetActive(false);

            if (_settingsPanel)
                _settingsPanel.SetActive(false);

            if (_controlsPanel)
                _controlsPanel.SetActive(false);

            if (_blackPanel)
                _blackPanel.SetActive(false);

            if (_endGameConfirmationPanel)
                _endGameConfirmationPanel.SetActive(false);

            if (_quitGameConfirmationPanel)
                _quitGameConfirmationPanel.SetActive(false);

            /*if (_pauseTopText && _pauseTopText.transform.parent)
                _pauseTopText.transform.parent.gameObject.SetActive(false);

            if (_pauseBottomText && _pauseBottomText.transform.parent)
                _pauseBottomText.transform.parent.gameObject.SetActive(false);*/

            StopAllActiveAnimations();
        }

        private async UniTask AnimatePausePanel()
        {
            _pausePanel.SetActive(true);

            //  await Tween.Position(_pausePanel.transform, )
        }

        private void Pause()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Pause, 0);

            ServiceManager.Instance.Get<AudioService>().SetPause(1);

            if (_shakeService is not null && _playerManager)
                _shakeService.ShakeController(_playerManager, ShakeService.ShakeType.MID);

            EnterPauseInputContext();
            RegisterToUIInputService();

            Show();

            if (_eventSystem && _settingsButton)
                _eventSystem.SetSelectedGameObject(_settingsButton.gameObject);
        }

        private void UnPause()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Return, 0);

            ServiceManager.Instance.Get<AudioService>().SetPause(0);

            if (_shakeService is not null && _playerManager)
                _shakeService.ShakeController(_playerManager, ShakeService.ShakeType.MID);

            RemoveFromUIInputService();
            ExitPauseInputContext();

            Hide();
        }

        private void EnterPauseInputContext()
        {
            if (!_playerManager)
                _playerManager = TryGetPlayerByIndex(0);

            if (!_playerManager)
                return;

            if (_playerManager.ControlContext != PlayerControlContext.PauseMenu)
            {
                _previousPlayerContext = _playerManager.ControlContext;
                _hasPreviousPlayerContext = true;
            }

            _playerManager.SetControlContext(PlayerControlContext.PauseMenu);
        }

        private void ExitPauseInputContext()
        {
            if (!_playerManager)
                return;

            if (_hasPreviousPlayerContext)
            {
                _playerManager.SetControlContext(_previousPlayerContext);
            }

            _hasPreviousPlayerContext = false;
        }

        private void RegisterToUIInputService()
        {
            if (!_playerManager)
                _playerManager = TryGetPlayerByIndex(0);

            if (!_playerManager)
                return;

            UIInputService?.Push(_playerManager, this);
        }

        private void RemoveFromUIInputService()
        {
            if (_playerManager)
            {
                UIInputService?.Remove(_playerManager, this);
                return;
            }

            UIInputService?.RemoveFromAll(this);
        }

        private void PlayToggleFeedback(bool value)
        {
            PlayMinorUIFeedback();
        }

        private void PlaySliderFeedback(float value)
        {
            PlayMinorUIFeedback();
        }

        private void PlayMinorUIFeedback()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Slider);

            if (_shakeService is not null && _playerManager)
                _shakeService.ShakeController(_playerManager, ShakeService.ShakeType.LITTLE);
        }

        private void Return()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Return);

            if (_controlsPanel && _controlsPanel.activeSelf && _controlsButton && _eventSystem)
                _eventSystem.SetSelectedGameObject(_controlsButton.gameObject);

            if (_endGameConfirmationPanel && _endGameConfirmationPanel.activeSelf && _endGameButton && _eventSystem)
                _eventSystem.SetSelectedGameObject(_endGameButton.gameObject);

            if (_quitGameConfirmationPanel && _quitGameConfirmationPanel.activeSelf && _quitButton && _eventSystem)
                _eventSystem.SetSelectedGameObject(_quitButton.gameObject);

            if (_settingsPanel && _settingsPanel.activeSelf && _settingsButton && _eventSystem)
                _eventSystem.SetSelectedGameObject(_settingsButton.gameObject);

            if (_settingsPanel)
                _settingsPanel.SetActive(false);

            if (_controlsPanel)
                _controlsPanel.SetActive(false);

            if (_endGameConfirmationPanel)
                _endGameConfirmationPanel.SetActive(false);

            if (_quitGameConfirmationPanel)
                _quitGameConfirmationPanel.SetActive(false);

            if (_pausePanel)
                _pausePanel.SetActive(true);

            if (_blackPanel)
                _blackPanel.SetActive(true);
        }

        private bool IsAnySubPanelOpen()
        {
            return (_settingsPanel && _settingsPanel.activeSelf) ||
                   (_controlsPanel && _controlsPanel.activeSelf) ||
                   (_endGameConfirmationPanel && _endGameConfirmationPanel.activeSelf) ||
                   (_quitGameConfirmationPanel && _quitGameConfirmationPanel.activeSelf);
        }

        private void BindPauseSystemEvents()
        {
            if (_gamePauseSystem is null)
            {
                Logs.LogError("[PauseUI] GamePauseSystem is null]");
                return;
            }

            _gamePauseSystem.Paused -= Pause;
            _gamePauseSystem.Resumed -= UnPause;
            _gamePauseSystem.Canceled -= Return;

            _gamePauseSystem.Paused += Pause;
            _gamePauseSystem.Resumed += UnPause;
            _gamePauseSystem.Canceled += Return;
        }

        private void UnbindPauseSystemEvents()
        {
            if (_gamePauseSystem is null)
                return;

            _gamePauseSystem.Paused -= Pause;
            _gamePauseSystem.Resumed -= UnPause;
            _gamePauseSystem.Canceled -= Return;
        }

        public bool CanHandleUIInput(PlayerManager player)
        {
            return _gamePauseSystem is not null &&
                   _gamePauseSystem.IsPaused &&
                   _playerManager &&
                   ReferenceEquals(_playerManager, player);
        }

        public bool HandleNavigate(PlayerManager player, Vector2 direction)
        {
            return false;
        }

        public bool HandleSubmit(PlayerManager player)
        {
            return false;
        }

        public bool HandleCancel(PlayerManager player)
        {
            if (!CanHandleUIInput(player))
                return false;

            if (IsAnySubPanelOpen())
            {
                Return();
                return true;
            }

            _gamePauseSystem.TogglePause();
            return true;
        }

        private void Shuffle(int[] array)
        {
            if (array is null)
                return;

            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }
    }
}