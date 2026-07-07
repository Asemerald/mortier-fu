using System;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MortierFu
{
    public sealed class MenuManager : MonoBehaviour, IPlayerUIInputHandler
    {
        [field: Header("Main Menu References")]
        [field: SerializeField]
        public MainMenuPanel MainMenuPanel { get; private set; }

        [field: SerializeField] public Button PlayButton { get; private set; }
        [field: SerializeField] public Button SettingsButton { get; private set; }
        [field: SerializeField] public Button CreditsButton { get; private set; }
        [field: SerializeField] public Button QuitButton { get; private set; }

        [SerializeField] private GameObject _animatedCharacter;
        [SerializeField] private GameObject _animatedOutlineCharacter;

        [Header("Animation")] [SerializeField] private Image _background;
        [SerializeField] private Image _logo;

        [SerializeField] private Ease _backgroundEase = Ease.OutQuad;
        [SerializeField] private Ease _logoEase = Ease.OutQuad;
        [SerializeField] private Ease _playButtonEase = Ease.OutQuad;
        [SerializeField] private Ease _creditsButtonEase = Ease.OutQuad;
        [SerializeField] private Ease _settingsButtonEase = Ease.OutQuad;
        [SerializeField] private Ease _quitButtonEase = Ease.OutQuad;

        [SerializeField] private float _backgroundEaseDuration = 1.5f;
        [SerializeField] private float _logoEaseDuration = 1.5f;
        [SerializeField] private float _playButtonEaseDuration = 0.7f;
        [SerializeField] private float _creditsButtonEaseDuration = 0.7f;
        [SerializeField] private float _settingsButtonEaseDuration = 0.7f;
        [SerializeField] private float _quitButtonEaseDuration = 0.7f;

        [field: Header("Settings References")]
        [field: SerializeField]
        public SettingsPanel SettingsPanel { get; private set; }

        [field: Header("Credits References")]
        [field: SerializeField]
        public CreditsPanel CreditsPanel { get; private set; }

        [Header("Utils")] [SerializeField] private MainMenuCameraManager _cameraManager;
        [SerializeField] private float _delayBeforeMainMenuShow = 2f;

        private EventSystem _eventSystem;
        private PlayerManager _player1;
        private InputSystemUIInputModule _uiInputModule;
        private InputAction _cancelAction;

        private bool _isLoadingLobby;

        public static MenuManager Instance { get; private set; }

        private PlayerUIInputService UIInputService =>
            ServiceManager.Instance?.Get<PlayerUIInputService>();

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Logs.LogWarning("[MenuManager] Multiple instances detected. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;

            CheckReferences();
            CheckActivePanels();

            var audioService = ServiceManager.Instance?.Get<AudioService>();
            audioService?.StartMusic(AudioService.FMODEvents.MUS_MainMenu).Forget();
        }

        private void Start()
        {
            _eventSystem = EventSystem.current;

            if (!_eventSystem)
            {
                Logs.LogError("[MenuManager] No EventSystem found in the scene.", this);
                return;
            }

            EnableUiInputModule();
            BindGlobalCancelAction();
            BindButtons();

            ShowMainMenuAfterDelay(_delayBeforeMainMenuShow).Forget();

            if (PlayerInputBridge.Instance)
            {
                PlayerInputBridge.Instance.CanJoin(false);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            UnbindGlobalCancelAction();
            UnregisterPlayer1();
            UnbindButtons();
        }

        private async UniTask AnimateMainMenu()
        {
            _background.gameObject.SetActive(true);
            _logo.gameObject.SetActive(true);

            PlayButton.transform.localScale = Vector3.zero;
            CreditsButton.transform.localScale = Vector3.zero;
            SettingsButton.transform.localScale = Vector3.zero;
            QuitButton.transform.localScale = Vector3.zero;

            await Tween.Alpha(_background, 1f, _backgroundEaseDuration, _backgroundEase)
                .Group(Tween.Alpha(_logo, 1f, _logoEaseDuration, _logoEase));

            await Tween.Scale(PlayButton.transform, 1.5f, _playButtonEaseDuration, _playButtonEase)
                .Group(Tween.Scale(CreditsButton.transform, 1.5f, _creditsButtonEaseDuration, _creditsButtonEase))
                .Group(Tween.Scale(SettingsButton.transform, 1.5f, _settingsButtonEaseDuration, _settingsButtonEase))
                .Group(Tween.Scale(QuitButton.transform, 1.5f, _quitButtonEaseDuration, _quitButtonEase));

            if (_eventSystem && PlayButton)
                _eventSystem.SetSelectedGameObject(PlayButton.gameObject);
        }

        private void BindButtons()
        {
            if (PlayButton)
                PlayButton.onClick.AddListener(LoadLobbyScene);

            if (SettingsButton)
                SettingsButton.onClick.AddListener(OpenSettingsPanel);

            if (CreditsButton)
                CreditsButton.onClick.AddListener(OpenCreditsPanel);

            if (QuitButton)
                QuitButton.onClick.AddListener(QuitGame);
        }

        private void UnbindButtons()
        {
            if (PlayButton)
                PlayButton.onClick.RemoveListener(LoadLobbyScene);

            if (SettingsButton)
                SettingsButton.onClick.RemoveListener(OpenSettingsPanel);

            if (CreditsButton)
                CreditsButton.onClick.RemoveListener(OpenCreditsPanel);

            if (QuitButton)
                QuitButton.onClick.RemoveListener(QuitGame);
        }

        private void EnableUiInputModule()
        {
            if (!_eventSystem)
                return;

            _uiInputModule = _eventSystem.GetComponent<InputSystemUIInputModule>();

            if (!_uiInputModule || !_uiInputModule.actionsAsset)
            {
                Logs.LogWarning("[MenuManager] InputSystemUIInputModule or actions asset is missing.", this);
                return;
            }

            foreach (var actionMap in _uiInputModule.actionsAsset.actionMaps)
            {
                actionMap.Enable();
            }
        }

        private void BindGlobalCancelAction()
        {
            UnbindGlobalCancelAction();

            if (!_uiInputModule)
                return;

            _cancelAction = _uiInputModule.cancel.action;

            if (_cancelAction == null)
            {
                Logs.LogWarning("[MenuManager] UI cancel action is missing on InputSystemUIInputModule.", this);
                return;
            }

            _cancelAction.performed += HandleGlobalCancelPerformed;
            _cancelAction.Enable();
        }

        private void UnbindGlobalCancelAction()
        {
            if (_cancelAction == null)
                return;

            _cancelAction.performed -= HandleGlobalCancelPerformed;
            _cancelAction = null;
        }

        private void HandleGlobalCancelPerformed(InputAction.CallbackContext context)
        {
            if (_isLoadingLobby)
                return;

            TryCancelCurrentPanel();
        }

        private async UniTask ShowMainMenuAfterDelay(float delay)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay));

            if (MainMenuPanel)
                MainMenuPanel.Show();

            await AnimateMainMenu();
        }

        private void UnregisterPlayer1()
        {
            if (_player1)
                UIInputService?.Remove(_player1, this);
            else
                UIInputService?.RemoveFromAll(this);

            _player1 = null;
        }

        private void LoadLobbyScene()
        {
            if (_isLoadingLobby)
                return;

            LoadLobbySceneAsync().Forget();
        }

        private async UniTaskVoid LoadLobbySceneAsync()
        {
            _isLoadingLobby = true;

            var sceneService = ServiceManager.Instance.Get<SceneService>();

            if (sceneService is null)
            {
                Logs.LogError("[MenuManager] Cannot load lobby because SceneService is missing.", this);
                _isLoadingLobby = false;
                return;
            }

            UIInputService?.RemoveFromAll(this);

            if (PlayerInputBridge.Instance)
                PlayerInputBridge.Instance.CanJoin(false);

            await sceneService.LoadScene("Lobby", setAsActiveScene: true);
            await sceneService.UnloadScene("MainMenu");
        }

        public void OpenSettingsPanel()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Select);

            if (MainMenuPanel)
                MainMenuPanel.Hide();

            if (SettingsPanel)
                SettingsPanel.Show();

            SetCharactersVisible(false);
        }

        private void CloseSettingsPanel()
        {
            if (SettingsPanel)
                SettingsPanel.Hide();

            if (MainMenuPanel)
                MainMenuPanel.Show();

            SetCharactersVisible(true);

            if (_eventSystem && SettingsButton)
                _eventSystem.SetSelectedGameObject(SettingsButton.gameObject);
        }

        public void OpenCreditsPanel()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Select);

            if (MainMenuPanel)
                MainMenuPanel.Hide();

            if (CreditsPanel)
                CreditsPanel.Show();

            SetCharactersVisible(false);
        }

        private void CloseCreditsPanel()
        {
            if (CreditsPanel)
                CreditsPanel.Hide();

            if (MainMenuPanel)
                MainMenuPanel.Show();

            SetCharactersVisible(true);

            if (_eventSystem && CreditsButton)
                _eventSystem.SetSelectedGameObject(CreditsButton.gameObject);
        }

        public void QuitGame()
        {
            Logs.Log("[MenuManager] Quitting game.");
            
            Application.Quit();

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#endif
        }

        private void SetCharactersVisible(bool visible)
        {
            if (_animatedCharacter)
                _animatedCharacter.SetActive(visible);

            if (_animatedOutlineCharacter)
                _animatedOutlineCharacter.SetActive(visible);
        }

        private bool TryCancelCurrentPanel()
        {
            Logs.Log("[MenuManager] Cancel triggered.");

            if (SettingsPanel && SettingsPanel.IsVisible())
            {
                CloseSettingsPanel();
                return true;
            }

            if (CreditsPanel && CreditsPanel.IsVisible())
            {
                CloseCreditsPanel();
                return true;
            }

            return false;
        }

        public bool CanHandleUIInput(PlayerManager player)
        {
            return _player1 &&
                   ReferenceEquals(_player1, player) &&
                   player.CurrentPermissions.CanCancelUI;
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
            return TryCancelCurrentPanel();
        }

        private void CheckReferences()
        {
            if (!MainMenuPanel)
                Logs.LogError("[MenuManager] MainMenuPanel reference is missing.", this);

            if (!PlayButton)
                Logs.LogError("[MenuManager] PlayButton reference is missing.", this);

            if (!SettingsButton)
                Logs.LogError("[MenuManager] SettingsButton reference is missing.", this);

            if (!CreditsButton)
                Logs.LogError("[MenuManager] CreditsButton reference is missing.", this);

            if (!QuitButton)
                Logs.LogError("[MenuManager] QuitButton reference is missing.", this);

            if (!SettingsPanel)
                Logs.LogError("[MenuManager] SettingsPanel reference is missing.", this);

            if (!CreditsPanel)
                Logs.LogError("[MenuManager] CreditsPanel reference is missing.", this);

            if (!_cameraManager)
                Logs.LogWarning("[MenuManager] MainMenuCameraManager reference is missing.", this);
        }

        private void CheckActivePanels()
        {
            if (SettingsPanel && !SettingsPanel.isActiveAndEnabled)
                SettingsPanel.gameObject.SetActive(true);

            if (CreditsPanel && !CreditsPanel.isActiveAndEnabled)
                CreditsPanel.gameObject.SetActive(true);
        }
    }
}