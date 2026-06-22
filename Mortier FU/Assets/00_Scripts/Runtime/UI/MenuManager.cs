using System;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MortierFu
{
    public class MenuManager : MonoBehaviour
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

        [field: Header("Settings References")]
        [field: SerializeField]
        public SettingsPanel SettingsPanel { get; private set; }

        [field: Header("Credits References")]
        [field: SerializeField]
        public CreditsPanel CreditsPanel { get; private set; }

        [Header("Utils")]
        [SerializeField] private MainMenuCameraManager _cameraManager;

        [SerializeField] private float _delayBeforeMainMenuShow = 2f;

        private EventSystem _eventSystem;
        private bool _isLoadingLobby;

        public PlayerInput Player1InputAction { get; private set; }

        public static MenuManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Logs.LogWarning("[MenuManager] Multiple instances detected. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;

            CheckReferences();
            CheckActivePanels();

            ServiceManager.Instance
                .Get<AudioService>()
                .StartMusic(AudioService.FMODEvents.MUS_MainMenu)
                .Forget();
        }

        private void Start()
        {
            _eventSystem = EventSystem.current;

            if (_eventSystem == null)
            {
                Logs.LogError("[MenuManager] No EventSystem found in the scene.", this);
                return;
            }

            EnableUiInputModule();

            if (PlayButton != null)
            {
                _eventSystem.SetSelectedGameObject(PlayButton.gameObject);
                PlayButton.onClick.AddListener(LoadLobbyScene);
            }

            ShowMainMenuAfterDelay(_delayBeforeMainMenuShow).Forget();

            if (PlayerInputBridge.Instance != null)
            {
                PlayerInputBridge.Instance.CanJoin(true);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (PlayButton != null)
            {
                PlayButton.onClick.RemoveListener(LoadLobbyScene);
            }

            UnbindPlayer1Input();
        }

        private void EnableUiInputModule()
        {
            if (_eventSystem == null)
                return;

            var inputModule = _eventSystem.GetComponent<InputSystemUIInputModule>();

            if (inputModule == null || inputModule.actionsAsset == null)
            {
                Logs.LogWarning("[MenuManager] InputSystemUIInputModule or actions asset is missing.", this);
                return;
            }

            foreach (var actionMap in inputModule.actionsAsset.actionMaps)
            {
                actionMap.Enable();
            }
        }

        private async UniTask ShowMainMenuAfterDelay(float delay)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay));

            if (MainMenuPanel != null)
            {
                MainMenuPanel.gameObject.SetActive(true);
            }

            if (_eventSystem != null && PlayButton != null)
            {
                _eventSystem.SetSelectedGameObject(PlayButton.gameObject);
            }
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

            if (sceneService == null)
            {
                Logs.LogError("[MenuManager] Cannot load lobby because SceneService is missing.", this);
                _isLoadingLobby = false;
                return;
            }

            if (PlayerInputBridge.Instance != null)
            {
                PlayerInputBridge.Instance.CanJoin(false);
            }

            await sceneService.LoadScene("Lobby", setAsActiveScene: true);

            await sceneService.UnloadScene("MainMenu");
        }

        public void SetPlayer1InputAction(PlayerInput playerInput)
        {
            UnbindPlayer1Input();

            Player1InputAction = playerInput;

            if (Player1InputAction == null)
                return;

            var cancelAction = Player1InputAction.actions.FindAction("Cancel", false);

            if (cancelAction != null)
            {
                cancelAction.performed += OnCancel;
            }

            if (PlayerInputBridge.Instance != null)
            {
                PlayerInputBridge.Instance.CanJoin(false);
            }

            Logs.Log("[MenuManager] Player 1 input assigned.");
        }

        private void UnbindPlayer1Input()
        {
            if (Player1InputAction == null)
                return;

            var cancelAction = Player1InputAction.actions.FindAction("Cancel", false);

            if (cancelAction != null)
            {
                cancelAction.performed -= OnCancel;
            }

            Player1InputAction = null;
        }

        private void OnCancel(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            Logs.Log("[MenuManager] Cancel triggered.");

            if (SettingsPanel != null && SettingsPanel.IsVisible())
            {
                SettingsPanel.Hide();
                MainMenuPanel.Show();
                _animatedCharacter.SetActive(true);
                _animatedOutlineCharacter.SetActive(true);
                
                if (_eventSystem != null && SettingsButton != null)
                {
                    _eventSystem.SetSelectedGameObject(SettingsButton.gameObject);
                }

                return;
            }

            if (CreditsPanel != null && CreditsPanel.IsVisible())
            {
                CreditsPanel.Hide();
                MainMenuPanel.Show();
                _animatedCharacter.SetActive(true);
                _animatedOutlineCharacter.SetActive(true);
                
                if (_eventSystem != null && CreditsButton != null)
                {
                    _eventSystem.SetSelectedGameObject(CreditsButton.gameObject);
                }
            }
        }

        private void CheckReferences()
        {
            if (MainMenuPanel == null)
            {
                Logs.LogError("[MenuManager] MainMenuPanel reference is missing.", this);
            }

            if (PlayButton == null)
            {
                Logs.LogError("[MenuManager] PlayButton reference is missing.", this);
            }

            if (SettingsButton == null)
            {
                Logs.LogError("[MenuManager] SettingsButton reference is missing.", this);
            }

            if (CreditsButton == null)
            {
                Logs.LogError("[MenuManager] CreditsButton reference is missing.", this);
            }

            if (SettingsPanel == null)
            {
                Logs.LogError("[MenuManager] SettingsPanel reference is missing.", this);
            }

            if (CreditsPanel == null)
            {
                Logs.LogError("[MenuManager] CreditsPanel reference is missing.", this);
            }

            if (_cameraManager == null)
            {
                Logs.LogWarning("[MenuManager] MainMenuCameraManager reference is missing.", this);
            }
        }

        private void CheckActivePanels()
        {
            if (SettingsPanel != null && !SettingsPanel.isActiveAndEnabled)
            {
                SettingsPanel.gameObject.SetActive(true);
            }

            if (CreditsPanel != null && !CreditsPanel.isActiveAndEnabled)
            {
                CreditsPanel.gameObject.SetActive(true);
            }
        }
    }
}