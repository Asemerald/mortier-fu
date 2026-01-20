using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Unity.Mathematics;


//TODO : Je ferai une énorme passe sur tout le script il est affreux
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
            _eventSystem = EventSystem.current;
            _gm = GameService.CurrentGameMode as GameModeBase;
            _gamePauseSystem = SystemManager.Instance.Get<GamePauseSystem>();
            _lobbyService = ServiceManager.Instance.Get<LobbyService>();
            _shakeService = ServiceManager.Instance.Get<ShakeService>();

            _playerManager = _lobbyService.GetPlayerByIndex(0);

            _gamePauseSystem.RestoreSettingsFromSave();
            _gamePauseSystem.UpdateUIFromSave(_fullscreenToggle, _vSyncToggle, _masterVolumeSlider, _musicVolumeSlider,
                _sfxVolumeSlider);
            _gamePauseSystem.BindUIEvents(_fullscreenToggle, _vSyncToggle, _masterVolumeSlider, _musicVolumeSlider,
                _sfxVolumeSlider);

            _gamePauseSystem.Paused += Pause;
            _gamePauseSystem.Resumed += UnPause;
            _gamePauseSystem.Canceled += Return;

            _settingsButton.onClick.AddListener(ShowSettingsPanel);
            _controlsButton.onClick.AddListener(ShowControlsPanel);
            _endGameButton.onClick.AddListener(ShowEndGameConfirmationPanel);
            _quitButton.onClick.AddListener(ShowQuitConfirmationPanel);
            _fullscreenToggle.onValueChanged.AddListener(SelectedToggleFeedback);
            _vSyncToggle.onValueChanged.AddListener(SelectedToggleFeedback);

            _masterVolumeSlider.onValueChanged.AddListener(SliderValueChange);
            _musicVolumeSlider.onValueChanged.AddListener(SliderValueChange);
            _sfxVolumeSlider.onValueChanged.AddListener(SliderValueChange);

            if (_gm != null)
            {
                // TODO: Mettre le son sur endgame main menu et quit
                _confirmEndGameButton.onClick.AddListener(_gm.EndGame);
                _confirmEndGameButton.onClick.AddListener(_gamePauseSystem.TogglePause);
            }
            
            _cancelEndGameButton.onClick.AddListener(Return);
            _confirmQuitGameButton.onClick.AddListener(Application.Quit);
            _cancelQuitGameButton.onClick.AddListener(Return);
            
            _mortarHandsInitialPositions = new Vector3[_mortarHands.Length];
            _mortarHeadsInitialPositions = new Vector3[_mortarHeads.Length];
            _mortarInitialRotations = new Quaternion[_mortarHands.Length];
            _activeHeadTweens = new Tween[_mortarHeads.Length];

            for (int i = 0; i < _mortarHands.Length; i++)
            {
                _mortarHandsInitialPositions[i] = _mortarHands[i].transform.position;
                _mortarHeadsInitialPositions[i] = _mortarHeads[i].transform.position;
                _mortarInitialRotations[i] = _mortarHands[i].transform.rotation;

                _mortarHeads[i].SetActive(false);
                _mortarHands[i].SetActive(false);
            }

            Hide();
        }

        private void Update()
        {
            if (!_gamePauseSystem.IsPaused) return;

            AnimateTilableImage(_pauseTopText, -_tilablePauseSpeed);
            AnimateTilableImage(_pauseBottomText, _tilablePauseSpeed);
        }

        private void OnDisable()
        {
            StopAllAnimations();
        }

        private void OnDestroy()
        {
            if (_gamePauseSystem != null)
            {
                _gamePauseSystem.Paused -= Pause;
                _gamePauseSystem.Resumed -= UnPause;
                _gamePauseSystem.Canceled -= Return;
            }

            _animateCancellation?.Cancel();
            _animateCancellation?.Dispose();

            _mortarHandsInitialPositions = null;
            _mortarHeadsInitialPositions = null;
            _mortarInitialRotations = null;

            if (_activeHeadTweens == null) return;

            foreach (var tween in _activeHeadTweens)
            {
                if (tween.isAlive)
                    tween.Stop();
            }
        }

        private void AnimateTilableImage(RawImage image, float speed)
        {
            var rect = image.uvRect;
            rect.x = math.frac(rect.x + speed * Time.unscaledDeltaTime);
            image.uvRect = rect;
        }

        private void StopAllAnimations()
        {
            if (_activeHeadTweens != null)
            {
                foreach (var tween in _activeHeadTweens)
                {
                    if (tween.isAlive)
                        tween.Stop();
                }
            }

            _animateCancellation?.Cancel();
            _animateCancellation?.Dispose();
            _animateCancellation = null;
        }

        private void Pause()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Pause);
            _shakeService.ShakeController(_playerManager, ShakeService.ShakeType.MID);
            Show();
            _eventSystem.SetSelectedGameObject(_settingsButton.gameObject);
        }

        private void UnPause()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Return);
            _shakeService.ShakeController(_playerManager, ShakeService.ShakeType.MID);
            Hide();
        }

        private void ShowSettingsPanel()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Select);
            _shakeService.ShakeController(_playerManager, ShakeService.ShakeType.MID);
            _settingsPanel.SetActive(true);
            _controlsPanel.SetActive(false);
            _endGameConfirmationPanel.SetActive(false);
            _quitGameConfirmationPanel.SetActive(false);
            _pausePanel.SetActive(false);

            _eventSystem.SetSelectedGameObject(_fullscreenToggle.gameObject);
        }

        private void ShowControlsPanel()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Select);
            _shakeService.ShakeController(_playerManager, ShakeService.ShakeType.MID);
            _controlsPanel.SetActive(true);
            _pausePanel.SetActive(true);
            _endGameConfirmationPanel.SetActive(false);
            _quitGameConfirmationPanel.SetActive(false);
            _blackPanel.SetActive(false);
            _settingsPanel.SetActive(false);
            _eventSystem.SetSelectedGameObject(null);
        }
        
        private void ShowEndGameConfirmationPanel()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Select);
            _shakeService.ShakeController(_playerManager, ShakeService.ShakeType.MID);
            _endGameConfirmationPanel.SetActive(true);
            _pausePanel.SetActive(true);
            _blackPanel.SetActive(false);
            _quitGameConfirmationPanel.SetActive(false);
            _controlsPanel.SetActive(false);
            _settingsPanel.SetActive(false);

            _eventSystem.SetSelectedGameObject(_confirmEndGameButton.gameObject);
        }
        
        private void ShowQuitConfirmationPanel()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Select);
            _shakeService.ShakeController(_playerManager, ShakeService.ShakeType.MID);
            _pausePanel.SetActive(true);
            _blackPanel.SetActive(false);
            _quitGameConfirmationPanel.SetActive(true);
            _endGameConfirmationPanel.SetActive(false);
            _controlsPanel.SetActive(false);
            _settingsPanel.SetActive(false);

            _eventSystem.SetSelectedGameObject(_confirmQuitGameButton.gameObject);
        }

        private void Show()
        {
            _pausePanel.SetActive(true);
            _pauseBackground.SetActive(true);
            _blackPanel.SetActive(true);
            _pauseTopText.transform.parent.gameObject.SetActive(true);
            _pauseBottomText.transform.parent.gameObject.SetActive(true);

            _animateCancellation?.Cancel();
            _animateCancellation?.Dispose();
            _animateCancellation = new CancellationTokenSource();

            RandomizeAndAnimateMortars(_animateCancellation.Token).Forget();
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

            StopAllAnimations();

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

        private async UniTask RandomizeAndAnimateMortars(CancellationToken ct)
        {
            for (int i = 0; i < _activeHeadTweens.Length; i++)
                if (_activeHeadTweens[i].isAlive)
                    _activeHeadTweens[i].Stop();

            int playerCount = Mathf.Min(_lobbyService.CurrentPlayerCount, _mortarHands.Length);

            int[] indices = new int[_mortarHandsInitialPositions.Length];
            for (int i = 0; i < indices.Length; i++) indices[i] = i;
            Shuffle(indices);

            for (int i = 0; i < playerCount; i++)
            {
                if (ct.IsCancellationRequested) return;

                int positionIndex = indices[i];
                var hand = _mortarHands[i];
                var head = _mortarHeads[i];

                Vector3 handTargetPos = _mortarHandsInitialPositions[positionIndex];
                hand.transform.position = handTargetPos;
                hand.transform.rotation = _mortarInitialRotations[i];
                bool shouldRotate = (i % 2) != (positionIndex % 2);
                if (shouldRotate) hand.transform.Rotate(0f, 0f, 180f);
                hand.SetActive(true);

                Vector3 headTargetPos = _mortarHeadsInitialPositions[positionIndex];
                head.transform.rotation = _mortarInitialRotations[i];
                if (shouldRotate) head.transform.Rotate(0f, 0f, 180f);

                Vector3 wallNormal = (positionIndex % 2 == 0) ? Vector3.down : Vector3.up;
                Vector3 headStartPos = headTargetPos + wallNormal * _headYOffset;
                head.transform.position = headStartPos;
                head.SetActive(true);

                await Tween.Position(
                    head.transform,
                    headTargetPos,
                    _headMoveDuration,
                    _headMoveEase,
                    useUnscaledTime: true
                ).ToUniTask(cancellationToken: ct);

                if (ct.IsCancellationRequested) return;

                _activeHeadTweens[i] = Tween.Position(
                    head.transform,
                    headTargetPos + wallNormal * _headPulseOffset,
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

        private void Return()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Return);
            
            if (_settingsPanel.activeSelf)
            {
                _eventSystem.SetSelectedGameObject(_settingsButton.gameObject);
            }
            if (_controlsPanel.activeSelf)
            {
                _eventSystem.SetSelectedGameObject(_controlsButton.gameObject);
                _blackPanel.SetActive(true);
            }
            if (_endGameConfirmationPanel.activeSelf)
            {
                _eventSystem.SetSelectedGameObject(_endGameButton.gameObject);
                _blackPanel.SetActive(true);
            }
            if (_quitGameConfirmationPanel.activeSelf)
            {
                _eventSystem.SetSelectedGameObject(_quitButton.gameObject);
                _blackPanel.SetActive(true);
            }

            _settingsPanel.SetActive(false);
            _controlsPanel.SetActive(false);
            _endGameConfirmationPanel.SetActive(false);
            _quitGameConfirmationPanel.SetActive(false);
            _pausePanel.SetActive(true);
        }

        private void SelectedToggleFeedback(bool value)
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Select);
            _shakeService.ShakeController(_playerManager, ShakeService.ShakeType.LITTLE);
        }

        private void SliderValueChange(float value)
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Slider);
            _shakeService.ShakeController(_playerManager, ShakeService.ShakeType.LITTLE);
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