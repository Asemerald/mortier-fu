using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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

        [field: Header("Settings References")]
        [field: SerializeField]
        public SettingsPanel SettingsPanel { get; private set; }

        [field: Header("Credits References")]
        [field: SerializeField]
        public CreditsPanel CreditsPanel { get; private set; }

        [field: Header("Lobby References")]
        [field: SerializeField]
        public LobbyPanel LobbyPanel { get; private set; }

        [field: SerializeField] 
        private LobbyCharacter[] lobbyCharacterSlots;
        
        [field: SerializeField] 
        private int minPlayers = 2;

        [Header("Utils")] 
        [field: SerializeField]
        private GameObject blackFader;

        [field: SerializeField] private MainMenuCameraManager cameraManager;

        private EventSystem _eventSystem;

        private PlayerActionInput _playerActions;
        private GameService _gameService;
        private ShakeService _shakeService;
        private LobbyService _lobbyService;

        private int _nextAvailableSlot = 0;

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

            // Create PlayerActionInput and enable Menu action map
            _playerActions = PlayerInputBridge.Instance.PlayerActionsInput;
            
            // Désactiver tous les slots au départ
            if (lobbyCharacterSlots != null)
            {
                foreach (var slot in lobbyCharacterSlots)
                {
                    if (slot != null)
                    {
                        slot.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void Start()
        {
            _eventSystem = EventSystem.current;
            if (_eventSystem == null)
            {
                Logs.LogError("[MenuManager]: No EventSystem found in the scene.", this);
            }

            _eventSystem.SetSelectedGameObject(PlayButton.gameObject);
            _shakeService = ServiceManager.Instance.Get<ShakeService>();
        }

        private void OnEnable()
        {
            _playerActions.UI.Enable();
            _playerActions.UI.Cancel.performed += OnCancel;
            //TODO: TEMP IMPLEMENTATION, TO BE REWORKED LATER
            _playerActions.UI.StartGame.performed += OnStartGame;

            // S'abonner aux événements du LobbyService
            if (_lobbyService != null)
            {
                _lobbyService.OnPlayerJoined += OnPlayerJoinedLobby;
                _lobbyService.OnPlayerLeft += OnPlayerLeftLobby;
            }
        }

        private void OnDisable()
        {
            _playerActions.UI.Disable();
            _playerActions.UI.Cancel.performed -= OnCancel;
            //TODO: TEMP IMPLEMENTATION, TO BE REWORKED LATER
            _playerActions.UI.StartGame.performed -= OnStartGame;

            // Se désabonner des événements du LobbyService
            if (_lobbyService != null)
            {
                _lobbyService.OnPlayerJoined -= OnPlayerJoinedLobby;
                _lobbyService.OnPlayerLeft -= OnPlayerLeftLobby;
            }
        }

        private void OnPlayerJoinedLobby(PlayerManager player)
        {
            if (_nextAvailableSlot < lobbyCharacterSlots.Length)
            {
                // Assigner le slot de lobby au player
                LobbyCharacter assignedSlot = lobbyCharacterSlots[_nextAvailableSlot];
                assignedSlot.gameObject.SetActive(true);
                
                // Le PlayerManager possède le LobbyCharacter
                player.PossessLobbyCharacter(assignedSlot, _nextAvailableSlot + 1);
                
                _nextAvailableSlot++;
                
                Logs.Log($"[MenuManager]: Player {player.PlayerIndex} assigned to lobby slot {_nextAvailableSlot}.");
            }
            else
            {
                Logs.LogWarning($"[MenuManager]: No available lobby slots for player {player.PlayerIndex}.");
            }
        }

        private void OnPlayerLeftLobby(PlayerManager player)
        {
            // Optionnel: gérer la libération des slots si un joueur part
            // Tu peux réorganiser les slots ou simplement décrémenter _nextAvailableSlot
            if (_nextAvailableSlot > 0)
            {
                _nextAvailableSlot--;
            }
            
            Logs.Log($"[MenuManager]: Player {player.PlayerIndex} left the lobby.");
        }

        public async UniTask StartGame()
        {
            Logs.Log("MenuManager: Starting Game...");
            
            // Libérer tous les LobbyCharacters
            foreach (var player in _lobbyService.GetPlayers())
            {
                player.ReleaseLobbyCharacter();
            }
            
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
                Logs.Log("[MenuManager]: All players ready! Starting game...");
                StartGame().Forget();
            }
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
                SwitchCameraPosition();
                LobbyPanel.Hide();
                MainMenuPanel.Show();
                _eventSystem.SetSelectedGameObject(PlayButton.gameObject);
                
                // Reset les slots quand on quitte le lobby
                ResetLobbySlots();
            }
        }

        private void ResetLobbySlots()
        {
            _nextAvailableSlot = 0;
            
            // Libérer tous les personnages et désactiver les slots
            foreach (var player in _lobbyService.GetPlayers())
            {
                player.ReleaseLobbyCharacter();
            }
            
            foreach (var slot in lobbyCharacterSlots)
            {
                if (slot != null)
                {
                    slot.gameObject.SetActive(false);
                }
            }
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
            
            if (lobbyCharacterSlots == null || lobbyCharacterSlots.Length == 0)
            {
                Logs.LogError("MenuManager: LobbyCharacterSlots array is empty or missing.", this);
            }
        }

        private void CheckActivePanels()
        {
            if (!MainMenuPanel.isActiveAndEnabled)
            {
                MainMenuPanel.gameObject.SetActive(true);
            }

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

        [ContextMenu("Trigger Black Fade")]
        public async UniTaskVoid TriggerBlackFade()
        {
            blackFader.SetActive(true);
            await UniTask.Delay(TimeSpan.FromSeconds(2));
            blackFader.SetActive(false);
        }

        public void SwitchCameraPosition()
        {
            cameraManager.MoveToNextPosition();
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