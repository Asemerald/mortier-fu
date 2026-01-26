using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Unity.Mathematics;

namespace MortierFu
{
    public class PauseUI : MonoBehaviour
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

        [SerializeField] private RawImage _pauseTopText;
        [SerializeField] private RawImage _pauseBottomText;

        [Header("Buttons")] [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _controlsButton;
        [SerializeField] private Button _endGameButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _confirmEndGameButton;
        [SerializeField] private Button _cancelEndGameButton;
        [SerializeField] private Button _confirmQuitGameButton;
        [SerializeField] private Button _cancelQuitGameButton;

        [Header("Mortars")] [SerializeField] private GameObject[] _mortarHands;
        [SerializeField] private GameObject[] _mortarHeads;

        [Header("Animation Settings")] [SerializeField]
        private float _headYOffset = 80f;

        [SerializeField] private float _headPulseOffset = 15f;
        [SerializeField] private float _headMoveDuration = 0.4f;
        [SerializeField] private float _headPulseDuration = 0.4f;
        [SerializeField] private float _minRandomDelay = 0.01f;
        [SerializeField] private float _maxRandomDelay = 0.2f;
        [SerializeField] private Ease _headMoveEase = Ease.OutCubic;
        [SerializeField] private Ease _headPulseEase = Ease.InOutSine;

        [SerializeField] private float _tilablePauseSpeed = 0.5f;

        [Header("Panel Animation Settings")] [SerializeField]
        private float _panelScaleDuration = 0.5f;

        [SerializeField] private Ease _panelScaleEase = Ease.OutElastic;

        private CancellationTokenSource _controlPanelCTS;
        private CancellationTokenSource _endGamePanelCTS;
        private CancellationTokenSource _quitPanelCTS;

        private Tween[] _activeHeadTweens;
        private CancellationTokenSource _animateCancellation;

        private EventSystem _eventSystem;
        private GamePauseSystem _gamePauseSystem;
        private GameModeBase _gm;
        private LobbyService _lobbyService;
        private ShakeService _shakeService;
        private PlayerManager _playerManager;

        private Vector3[] _mortarHandsInitialPositions;
        private Vector3[] _mortarHeadsInitialPositions;
        private Quaternion[] _mortarInitialRotations;

        private void Start()
        {
            InitReferences();
            InitUI();
            InitMortars();
            Hide();
        }

        private void Update()
        {
            if (!_gamePauseSystem.IsPaused) return;
            ScrollRawImageUV(_pauseTopText, -_tilablePauseSpeed);
            ScrollRawImageUV(_pauseBottomText, _tilablePauseSpeed);
        }

        private void OnDisable() => StopAllActiveAnimations();

        private void OnDestroy()
        {
            if (_gamePauseSystem != null)
            {
                _gamePauseSystem.Paused -= Pause;
                _gamePauseSystem.Resumed -= UnPause;
                _gamePauseSystem.Canceled -= Return;
            }

            StopAllActiveAnimations();
            _mortarHandsInitialPositions = null;
            _mortarHeadsInitialPositions = null;
            _mortarInitialRotations = null;
        }

        private void InitReferences()
        {
            _eventSystem = EventSystem.current;
            _gm = GameService.CurrentGameMode as GameModeBase;
            _gamePauseSystem = SystemManager.Instance.Get<GamePauseSystem>();
            _lobbyService = ServiceManager.Instance.Get<LobbyService>();
            _shakeService = ServiceManager.Instance.Get<ShakeService>();
            _playerManager = _lobbyService.GetPlayerByIndex(0);
        }

        private void InitUI()
        {
            _gamePauseSystem.RestoreSettingsFromSave();
            _gamePauseSystem.UpdateUIFromSave(
                _fullscreenToggle, _vSyncToggle, _masterVolumeSlider, _musicVolumeSlider, _sfxVolumeSlider);
            _gamePauseSystem.BindUIEvents(
                _fullscreenToggle, _vSyncToggle, _masterVolumeSlider, _musicVolumeSlider, _sfxVolumeSlider);

            _gamePauseSystem.Paused += Pause;
            _gamePauseSystem.Resumed += UnPause;
            _gamePauseSystem.Canceled += Return;

            _settingsButton.onClick.AddListener(OpenSettingPanel);
            _controlsButton.onClick.AddListener(OpenControlPanel);
            _endGameButton.onClick.AddListener(OpenEndGamePanel);
            _quitButton.onClick.AddListener(OpenQuitPanel);

            _fullscreenToggle.onValueChanged.AddListener(PlayToggleFeedback);
            _vSyncToggle.onValueChanged.AddListener(PlayToggleFeedback);

            _masterVolumeSlider.onValueChanged.AddListener(PlaySliderFeedback);
            _musicVolumeSlider.onValueChanged.AddListener(PlaySliderFeedback);
            _sfxVolumeSlider.onValueChanged.AddListener(PlaySliderFeedback);

            if (_gm != null)
            {
                _confirmEndGameButton.onClick.AddListener(_gamePauseSystem.TogglePause);
                _confirmEndGameButton.onClick.AddListener(_gm.ReturnToMainMenu);
            }

            _cancelEndGameButton.onClick.AddListener(Return);
            _confirmQuitGameButton.onClick.AddListener(Application.Quit);
            _cancelQuitGameButton.onClick.AddListener(Return);
        }

        private void InitMortars()
        {
            _mortarHandsInitialPositions = new Vector3[_mortarHands.Length];
            _mortarHeadsInitialPositions = new Vector3[_mortarHeads.Length];
            _mortarInitialRotations = new Quaternion[_mortarHands.Length];
            _activeHeadTweens = new Tween[_mortarHeads.Length];

            for (int i = 0; i < _mortarHands.Length; i++)
            {
                _mortarHandsInitialPositions[i] = _mortarHands[i].transform.position;
                _mortarHeadsInitialPositions[i] = _mortarHeads[i].transform.position;
                _mortarInitialRotations[i] = _mortarHands[i].transform.rotation;

                _mortarHands[i].SetActive(false);
                _mortarHeads[i].SetActive(false);
            }
        }


        private void ScrollRawImageUV(RawImage image, float speed)
        {
            var rect = image.uvRect;
            rect.x = math.frac(rect.x + speed * Time.unscaledDeltaTime);
            image.uvRect = rect;
        }

        private void StopAllActiveAnimations()
        {
            if (_activeHeadTweens != null)
            {
                foreach (var tween in _activeHeadTweens)
                    if (tween.isAlive)
                        tween.Stop();
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

        private void OpenSettingPanel()
        {
            PlayPanelSelectionFeedback();
            _settingsPanel.SetActive(true);
            _pausePanel.SetActive(false);
            _eventSystem.SetSelectedGameObject(_fullscreenToggle.gameObject);
        }

        private void OpenControlPanel()
        {
            PlayPanelSelectionFeedback();
            AnimateOpenPanel(_controlsPanel, _controlPanelCTS, null, 0.2f, Ease.OutCubic).Forget();
        }

        private void OpenEndGamePanel()
        {
            PlayPanelSelectionFeedback();
            AnimateOpenPanel(_endGameConfirmationPanel, _endGamePanelCTS, _confirmEndGameButton.gameObject,
                _panelScaleDuration, _panelScaleEase).Forget();
        }

        private void OpenQuitPanel()
        {
            PlayPanelSelectionFeedback();
            AnimateOpenPanel(_quitGameConfirmationPanel, _quitPanelCTS, _confirmQuitGameButton.gameObject, _panelScaleDuration,
                _panelScaleEase).Forget();
        }

        private async UniTask AnimateOpenPanel(GameObject panel, CancellationTokenSource cts, GameObject selectedButton,
            float duration, Ease ease)
        {
            SafeCancelCts(ref cts);
            cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;

            panel.transform.localScale = Vector3.zero;
            panel.SetActive(true);
            _eventSystem.SetSelectedGameObject(selectedButton);

            _shakeService.ShakeController(_playerManager, ShakeService.ShakeType.MID);

            try
            {
                await Tween.Scale(panel.transform, 1f, duration, ease, useUnscaledTime: true)
                    .ToUniTask(cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                // Safe cancellation
            }
        }

        private void PlayPanelSelectionFeedback()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Select);
            _shakeService.ShakeController(_playerManager, ShakeService.ShakeType.MID);
        }

        private void Show()
        {
            _pausePanel.SetActive(true);
            _pauseBackground.SetActive(true);
            _blackPanel.SetActive(true);
            _pauseTopText.transform.parent.gameObject.SetActive(true);
            _pauseBottomText.transform.parent.gameObject.SetActive(true);

            SafeCancelCts(ref _animateCancellation);
            _animateCancellation = new CancellationTokenSource();

            AnimateMortarsRandomly(_animateCancellation.Token).Forget();
        }

        private void Hide()
        {
            _pausePanel.SetActive(false);
            _pauseBackground.SetActive(false);
            _settingsPanel.SetActive(false);
            _controlsPanel.SetActive(false);
            _blackPanel.SetActive(false);
            _endGameConfirmationPanel.SetActive(false);
            _quitGameConfirmationPanel.SetActive(false);
            _pauseTopText.transform.parent.gameObject.SetActive(false);
            _pauseBottomText.transform.parent.gameObject.SetActive(false);

            StopAllActiveAnimations();
            RestoreMortarsInitialState();
        }

        private void RestoreMortarsInitialState()
        {
            for (int i = 0; i < _mortarHands.Length; i++)
            {
                _mortarHands[i].SetActive(false);
                _mortarHeads[i].SetActive(false);

                _mortarHands[i].transform.position = _mortarHandsInitialPositions[i];
                _mortarHands[i].transform.rotation = _mortarInitialRotations[i];

                _mortarHeads[i].transform.position = _mortarHeadsInitialPositions[i];
                _mortarHeads[i].transform.rotation = _mortarInitialRotations[i];
            }
        }

        private async UniTask AnimateMortarsRandomly(CancellationToken ct)
        {
            for (int i = 0; i < _activeHeadTweens.Length; i++)
                if (_activeHeadTweens[i].isAlive)
                    _activeHeadTweens[i].Stop();

            int playerCount = Mathf.Min(_lobbyService.CurrentPlayerCount, _mortarHands.Length);

            int[] indices = new int[_mortarHands.Length];
            for (int i = 0; i < indices.Length; i++) indices[i] = i;
            Shuffle(indices);

            for (int i = 0; i < playerCount; i++)
            {
                if (ct.IsCancellationRequested) return;

                int positionIndex = indices[i];
                var hand = _mortarHands[i];
                var head = _mortarHeads[i];

                bool shouldRotate = (i % 2) != (positionIndex % 2);
                ApplyMortarTransform(hand, _mortarHandsInitialPositions[positionIndex], _mortarInitialRotations[i],
                    shouldRotate);
                hand.SetActive(true);

                ApplyMortarTransform(head, _mortarHeadsInitialPositions[positionIndex], _mortarInitialRotations[i],
                    shouldRotate, _headYOffset * ((positionIndex % 2 == 0) ? -1 : 1));
                head.SetActive(true);

                await Tween.Position(head.transform, _mortarHeadsInitialPositions[positionIndex], _headMoveDuration,
                        _headMoveEase, useUnscaledTime: true)
                    .ToUniTask(cancellationToken: ct);

                if (ct.IsCancellationRequested) return;

                _activeHeadTweens[i] = Tween.Position(
                    head.transform,
                    _mortarHeadsInitialPositions[positionIndex] +
                    (_headPulseOffset * ((positionIndex % 2 == 0) ? -1 : 1) * Vector3.up),
                    _headPulseDuration,
                    _headPulseEase,
                    cycles: -1,
                    cycleMode: CycleMode.Yoyo,
                    useUnscaledTime: true
                );

                float delay = Random.Range(_minRandomDelay, _maxRandomDelay);
                await UniTask.Delay(TimeSpan.FromSeconds(delay), ignoreTimeScale: true, cancellationToken: ct);
            }
        }

        private void ApplyMortarTransform(GameObject obj, Vector3 targetPos, Quaternion baseRot, bool rotate,
            float yOffset = 0f)
        {
            obj.transform.rotation = baseRot;
            if (rotate) obj.transform.Rotate(0f, 0f, 180f);
            obj.transform.position = targetPos + new Vector3(0f, yOffset, 0f);
        }

        private void Pause()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Pause, 0);
            ServiceManager.Instance.Get<AudioService>().SetPause(1);
            _shakeService.ShakeController(_playerManager, ShakeService.ShakeType.MID);
            Show();
            _eventSystem.SetSelectedGameObject(_settingsButton.gameObject);
        }

        private void UnPause()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Return, 0);
            ServiceManager.Instance.Get<AudioService>().SetPause(0);
            _shakeService.ShakeController(_playerManager, ShakeService.ShakeType.MID);
            Hide();
        }

        private void PlayToggleFeedback(bool value) => PlayMinorUIFeedback();
        private void PlaySliderFeedback(float value) => PlayMinorUIFeedback();

        private void PlayMinorUIFeedback()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Slider);
            _shakeService.ShakeController(_playerManager, ShakeService.ShakeType.LITTLE);
        }

        private void Return()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Return);

            if (_controlsPanel.activeSelf) _eventSystem.SetSelectedGameObject(_controlsButton.gameObject);
            if (_endGameConfirmationPanel.activeSelf) _eventSystem.SetSelectedGameObject(_endGameButton.gameObject);
            if (_quitGameConfirmationPanel.activeSelf) _eventSystem.SetSelectedGameObject(_quitButton.gameObject);
            if (_settingsPanel.activeSelf) _eventSystem.SetSelectedGameObject(_settingsButton.gameObject);

            _settingsPanel.SetActive(false);
            _controlsPanel.SetActive(false);
            _endGameConfirmationPanel.SetActive(false);
            _quitGameConfirmationPanel.SetActive(false);
            _pausePanel.SetActive(true);
            _blackPanel.SetActive(true);
        }

        private void Shuffle(int[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }
    }
}