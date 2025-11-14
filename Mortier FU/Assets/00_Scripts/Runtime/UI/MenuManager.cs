using MortierFu;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
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
    
        private void Awake()
        {
            CheckReferences();
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
    }
}
