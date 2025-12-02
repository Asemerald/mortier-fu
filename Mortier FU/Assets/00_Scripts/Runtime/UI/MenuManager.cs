using System;
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
        [field: SerializeField] public MainMenuPanel MainMenuPanel { get; private set; }
        [field: SerializeField] public Button PlayButton { get; private set; }
        [field: SerializeField] public Button SettingsButton { get; private set; }
        [field: SerializeField] public Button CreditsButton { get; private set; }
        [field: SerializeField] public Button QuitButton { get; private set; }
    
        [field: Header("Settings References")]
        [field: SerializeField] public SettingsPanel SettingsPanel { get; private set; }
    
        [field: Header("Credits References")]
        [field: SerializeField] public CreditsPanel CreditsPanel { get; private set; }
    
        [field: Header("Lobby References")]
        [field: SerializeField] public LobbyPanel LobbyPanel { get; private set; }
        
        private EventSystem _eventSystem;
        
        private PlayerActionInput _playerActions; 
    
        private void Awake()
        {
            CheckReferences();
            CheckActivePanels();
            
            // Create PlayerActionInput and enable Menu action map
            _playerActions = new PlayerActionInput();
        }
    
        private void Start()
        {
            _eventSystem = EventSystem.current;
            if (_eventSystem == null)
            {
                Logs.LogError("[MenuManager]: No EventSystem found in the scene.", this);
            }
        
            _eventSystem.SetSelectedGameObject(PlayButton.gameObject);
        }

        private void OnEnable()
        {
            _playerActions.UI.Enable();
            _playerActions.UI.Cancel.performed += OnCancel;
        }
        
        private void OnDisable()
        {
            _playerActions.UI.Disable();
            _playerActions.UI.Cancel.performed -= OnCancel;
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
                LobbyPanel.Hide();
                MainMenuPanel.Show();
                _eventSystem.SetSelectedGameObject(PlayButton.gameObject);
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
        }

        private void CheckActivePanels()
        {
            if (!MainMenuPanel.isActiveAndEnabled)
            {
                //Logs.LogWarning("[MenuManager]: MainMenuPanel is not active!", this);
                MainMenuPanel.gameObject.SetActive(true);
            }
            if (!SettingsPanel.isActiveAndEnabled)
            {
                //Logs.LogWarning("[MenuManager]: SettingsPanel is not active!", this);
                SettingsPanel.gameObject.SetActive(true);
            }

            if (!CreditsPanel.isActiveAndEnabled)
            {
                //Logs.LogWarning("[MenuManager]: CreditsPanel is not active!", this);
                CreditsPanel.gameObject.SetActive(true);
            }

            if (!LobbyPanel.isActiveAndEnabled)
            {
                //Logs.LogWarning("[MenuManager]: LobbyPanel is not active!", this);
                LobbyPanel.gameObject.SetActive(true);
            }
        }
    }
}
