using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu;
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
        [field: Header("MainMenu References")]
        [field: SerializeField]
        public MainMenuPanel MainMenuPanel { get; private set; }

        [field: SerializeField] public Button PlayButton { get; private set; }
        [field: SerializeField] public Button SettingsButton { get; private set; }
        [field: SerializeField] public Button CreditsButton { get; private set; }
        [field: SerializeField] public Button QuitButton { get; private set; }

        [Header("Lobby UI Selection")] [SerializeField]
        private Button _player1ReadySelectedButton;

        [SerializeField] private GameObject _playGameObject;
        [SerializeField] private GameObject _gameMaxScoreObject;

        [field: Header("Settings References")]
        [field: SerializeField]
        public SettingsPanel SettingsPanel { get; private set; }

        [field: Header("Credits References")]
        [field: SerializeField]
        public CreditsPanel CreditsPanel { get; private set; }

        [field: Header("Lobby References")]
        [field: SerializeField]
        public LobbyPanel LobbyPanel { get; private set; }

        [field: SerializeField] private int minPlayers = 2;

        [Header("Utils")] [field: SerializeField]
        public MainMenuCameraManager cameraManager;
        [SerializeField] private float delayBeforeMainMenuShow = 2f;

        [Header("References")] public GameObject playerGO;

        private EventSystem _eventSystem;

        public PlayerInput Player1InputAction { get; private set; }
        private GameService _gameService;
        private ShakeService _shakeService;
        private LobbyService _lobbyService;

        public static MenuManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Logs.LogWarning("[MenuManager]: Multiple instances detected. Destroying duplicate.", this);
                Destroy(this.gameObject);
                return;
            }

            Instance = this;

            _gameService = ServiceManager.Instance.Get<GameService>();
            if (_gameService == null)
            {
                Logs.LogError("[MenuManager]: GameService could not be found in ServiceManager.", this);
            }

            _lobbyService = ServiceManager.Instance.Get<LobbyService>();
            if (_lobbyService == null)
            {
                Logs.LogError("[MenuManager]: LobbyService could not be found in ServiceManager.", this);
            }

            CheckReferences();
            CheckActivePanels();

            ServiceManager.Instance.Get<AudioService>().StartMusic(AudioService.FMODEvents.MUS_MainMenu).Forget();
        }

        private void Start()
        {
            _eventSystem = EventSystem.current;
            if (_eventSystem == null)
            {
                Logs.LogError("[MenuManager]: No EventSystem found in the scene.", this);
            }

            _eventSystem.SetSelectedGameObject(PlayButton.gameObject);
            _eventSystem.GetComponent<InputSystemUIInputModule>().actionsAsset.actionMaps[0].Enable();
            _eventSystem.GetComponent<InputSystemUIInputModule>().actionsAsset.actionMaps[1].Enable();
            _shakeService = ServiceManager.Instance.Get<ShakeService>();
            
            ShowMainMenuAfterDelay(delayBeforeMainMenuShow).Forget();
            
            PlayerInputBridge.Instance.CanJoin(true);
        }
        
        private async UniTask ShowMainMenuAfterDelay(float delay)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
            MainMenuPanel.gameObject.SetActive(true);
            _eventSystem.SetSelectedGameObject(PlayButton.gameObject);
        }

        private void OnEnable()
        {
            _playGameObject.SetActive(false);
            _gameMaxScoreObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (Player1InputAction == null) return;
            Player1InputAction.actions.FindAction("Cancel").performed -= OnCancel;
            Player1InputAction.actions.FindAction("StartGame").performed -=
                OnStartGame; //TODO: TEMP IMPLEMENTATION, TO BE REWORKED LATER
        }

        public void SetPlayer1InputAction(PlayerInput playerInput)
        {
            Player1InputAction = playerInput;
            Player1InputAction.actions.FindAction("Cancel").performed += OnCancel;
            
            PlayerInputBridge.Instance.CanJoin(false);
        }

        public async UniTask StartGame()
        {
            Logs.Log("MenuManager: Starting Game...");

            // When game mode is selected
            await _gameService.InitializeGameMode<GM_FFA>();

            // Should handle game mode teams

            _gameService.ExecuteGameplayPipeline().Forget();

            _shakeService.ShakeControllers(ShakeService.ShakeType.MID);
        }

        //TODO: TEMP IMPLEMENTATION, TO BE REWORKED LATER
        private void OnStartGame(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            Logs.Log("[MenuManager]: OnStartGame triggered via TEMP implementation.");
            if (LobbyPanel.IsVisible())
            {
                StartGame().Forget();
            }
        }

        public void CheckAllPlayersReady()
        {
            if (_lobbyService.CurrentPlayerCount < minPlayers)
            {
                Logs.Log($"[MenuManager]: Not enough players ({_lobbyService.CurrentPlayerCount}/{minPlayers}).");
                return;
            }

            bool allReady = true;
            foreach (var player in _lobbyService.GetPlayers())
            {
                if (!player.IsReady)
                {
                    allReady = false;
                    break;
                }
            }

            if (allReady)
            {
                _gameMaxScoreObject.SetActive(true);
                SelectPlayer1ReadyButton();
                _playGameObject.SetActive(true);
                Player1InputAction.actions.FindAction("StartGame").performed += OnStartGame;
            }
        }

        public bool AllPlayersReady()
        {
            foreach (var player in _lobbyService.GetPlayers())
            {
                if (!player.IsReady)
                {
                    return false;
                }
            }

            return true;
        }

        public void OnCancel(InputAction.CallbackContext context)
        {
            Logs.Log("[MenuManager]: OnCancel triggered.");
            if (!context.performed) return;

            // Hide Current Panel and go back to Main Menu
            if (SettingsPanel.IsVisible())
            {
                SettingsPanel.Hide();
                MainMenuPanel.Show();
                _eventSystem.SetSelectedGameObject(SettingsButton.gameObject);
            }
            else if (CreditsPanel.IsVisible())
            {
                CreditsPanel.Hide();
                MainMenuPanel.Show();
                _eventSystem.SetSelectedGameObject(CreditsButton.gameObject);
            }
            else if (LobbyPanel.IsVisible())
            {
                TransitionFromLobbyToMainMenu().Forget();
            }
        }
        
        private UniTask TransitionFromLobbyToMainMenu()
        {
            LobbyPanel.Hide();
            MainMenuPanel.Show();
            cameraManager.TeleportToPosition(1);
            _eventSystem.SetSelectedGameObject(PlayButton.gameObject);
            PlayerInputBridge.Instance.CanJoin(false);
            
            return UniTask.CompletedTask;
        }

        private void SelectPlayer1ReadyButton()
        {
            if (_eventSystem == null || _player1ReadySelectedButton == null)
                return;

            _eventSystem.SetSelectedGameObject(_player1ReadySelectedButton.gameObject);
        }

        private void CheckReferences()
        {
            if (MainMenuPanel == null)
            {
                Logs.LogError("MenuManager: MainMenuPanel reference is missing.", this);
            }

            if (SettingsPanel == null)
            {
                Logs.LogError("MenuManager: SettingsPanel reference is missing.", this);
            }

            if (CreditsPanel == null)
            {
                Logs.LogError("MenuManager: CreditsPanel reference is missing.", this);
            }

            if (LobbyPanel == null)
            {
                Logs.LogError("MenuManager: LobbyPanel reference is missing.", this);
            }

            if (cameraManager == null)
            {
                Logs.LogError("MenuManager: MainMenuCameraManager reference is missing.", this);
            }
        }

        private void CheckActivePanels()
        {
            // off to disable tween
            /*if (!MainMenuPanel.isActiveAndEnabled)
            {
                MainMenuPanel.gameObject.SetActive(true);
            }*/

            if (!SettingsPanel.isActiveAndEnabled)
            {
                SettingsPanel.gameObject.SetActive(true);
            }

            if (!CreditsPanel.isActiveAndEnabled)
            {
                CreditsPanel.gameObject.SetActive(true);
            }

            if (!LobbyPanel.isActiveAndEnabled)
            {
                LobbyPanel.gameObject.SetActive(true);
            }
        }

        public void ChangeSelectedButton(Button newSelected)
        {
            _eventSystem.SetSelectedGameObject(newSelected.gameObject);
        }
    }

    public enum MenuPanelType
    {
        MainMenu,
        Settings,
        Credits,
        Lobby
    }
}