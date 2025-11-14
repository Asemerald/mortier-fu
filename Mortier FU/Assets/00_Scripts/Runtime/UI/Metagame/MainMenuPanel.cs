using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MortierFu
{
    public class MainMenuPanel : UIPanel
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private GameObject quitConfirmationPanel;
    
        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private GameObject settingsButton;
        [SerializeField] private GameObject creditsButton;
        [SerializeField] private GameObject quitButton;
    
        private void Start()
        {
            Show();
            InitializeButtons();
        }

        private void InitializeButtons()
        {
            playButton.onClick.AddListener(OpenLobbyPanel);
        }
        private void OpenLobbyPanel()
        {
            Hide();
            lobbyPanel.SetActive(true);
        }
    }
}