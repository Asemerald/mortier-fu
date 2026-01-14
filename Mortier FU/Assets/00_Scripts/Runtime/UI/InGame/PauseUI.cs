using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

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
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameObject _controlsPanel;

        [Header("Buttons")] [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _controlsButton;
        [SerializeField] private Button _endGameButton;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _quitButton;

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

        private Vector3[] _mortarHandsInitialPositions;
        private Vector3[] _mortarHeadsInitialPositions;
        private Quaternion[] _mortarInitialRotations;

        private Tween[] _activeHeadTweens;

        private EventSystem _eventSystem;
        private LobbyService _lobbyService;
        private GameModeBase _gm;
        private GamePauseSystem _gamePauseSystem;

        private CancellationTokenSource _animateCancellation;

        private void OnDestroy()
        {
            if (_gamePauseSystem != null)
            {
                _gamePauseSystem.Paused -= Pause;
                _gamePauseSystem.Resumed -= UnPause;
            }
            
            _mortarHandsInitialPositions = null;
            _mortarHeadsInitialPositions = null;
            _mortarInitialRotations = null;
        }

        private void Start()
        {
            _eventSystem = EventSystem.current;
            _gm = GameService.CurrentGameMode as GameModeBase;
            _gamePauseSystem = SystemManager.Instance.Get<GamePauseSystem>();
            _lobbyService = ServiceManager.Instance.Get<LobbyService>();

            _gamePauseSystem.BindUIEvents(_fullscreenToggle, _vSyncToggle, _masterVolumeSlider, _musicVolumeSlider,
                _sfxVolumeSlider);

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

            _mortarHandsInitialPositions = new Vector3[_mortarHands.Length];
            _mortarHeadsInitialPositions = new Vector3[_mortarHeads.Length];
            _mortarInitialRotations = new Quaternion[_mortarHands.Length];

            for (int i = 0; i < _mortarHands.Length; i++)
            {
                _mortarHandsInitialPositions[i] = _mortarHands[i].transform.position;
                _mortarHeadsInitialPositions[i] = _mortarHeads[i].transform.position;
                _mortarInitialRotations[i] = _mortarHands[i].transform.rotation;

                _mortarHeads[i].SetActive(false);
                _mortarHands[i].SetActive(false);
            }

            _activeHeadTweens = new Tween[_mortarHeads.Length];

            Hide();
        }

        private void UnPause() => Hide();

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
            _pauseBackground.SetActive(false);
            _pausePanel.SetActive(false);

            if (_activeHeadTweens != null)
            {
                foreach (var tween in _activeHeadTweens)
                {
                    tween.Stop();
                }
            }

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
            _pauseBackground.SetActive(true);

            _animateCancellation?.Cancel();
            _animateCancellation?.Dispose();
            _animateCancellation = new CancellationTokenSource();

            RandomizeAndAnimateMortars(_animateCancellation.Token).Forget();
        }
    }
}