using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MortierFu
{
    public class MainMenuPanel : UIPanel
    {
        [Header("Panels")]
        [SerializeField] private MainMenuPanel mainMenuPanel;
        [SerializeField] private LobbyPanel lobbyPanel;
        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private CreditsPanel creditsPanel;
    
        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;
    
        private void Start()
        {
            Show();
            InitializeButtons();
        }

        private void InitializeButtons()
        {
            playButton.onClick.AddListener(OpenLobbyPanel);
            settingsButton.onClick.AddListener(OpenSettingsPanel);
            creditsButton.onClick.AddListener(OpenCreditsPanel);
            quitButton.onClick.AddListener(QuitGame);
        }
        
        
        private void OpenLobbyPanel()
        {
            Hide();
            lobbyPanel.Show();
        }
        private void OpenSettingsPanel()
        {
            Hide();
            settingsPanel.Show();
        }
        private void OpenCreditsPanel()
        {
            Hide();
            creditsPanel.Show();
        }
        private void QuitGame()
        {
            Logs.Log("[MainMenuPanel]: Quitting game...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            return;
#endif
            Application.Quit();
        }
        private void OnDestroy()
        {
            playButton.onClick.RemoveListener(OpenLobbyPanel);
        }
    }
}