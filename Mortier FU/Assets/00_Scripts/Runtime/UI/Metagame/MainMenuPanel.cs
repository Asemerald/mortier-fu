using System;
using Cysharp.Threading.Tasks;
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
            LobbyTransition().Forget();
        }
        
        private UniTask LobbyTransition()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Select);
            Hide();
            MenuManager.Instance.cameraManager.TeleportToPosition(2);
            lobbyPanel.Show();
            
            return UniTask.CompletedTask;
        }
        private void OpenSettingsPanel()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Select);
            Hide();
            settingsPanel.Show();
        }
        private void OpenCreditsPanel()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Select);
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
#pragma warning disable CS0162 // Unreachable code detected
            Application.Quit();
#pragma warning restore CS0162 // Unreachable code detected
        }
        private void OnDestroy()
        {
            playButton.onClick.RemoveListener(OpenLobbyPanel);
        }
    }
}