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
        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private CreditsPanel creditsPanel;
    
        [Header("Buttons")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;
    
        [Header("Other")]
        [SerializeField] private GameObject _animatedCharacter;
        [SerializeField] private GameObject _animatedOutlineCharacter;
        
        private void Start()
        {
            InitializeButtons();
        }

        private void InitializeButtons()
        {
            settingsButton.onClick.AddListener(OpenSettingsPanel);
            creditsButton.onClick.AddListener(OpenCreditsPanel);
            quitButton.onClick.AddListener(QuitGame);
        }
        
        private void OpenSettingsPanel()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Select);
            Hide();
            settingsPanel.Show();
            _animatedCharacter.SetActive(false);
            _animatedOutlineCharacter.SetActive(false);
        }
        private void OpenCreditsPanel()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Select);
            Hide();
            creditsPanel.Show();
            _animatedCharacter.SetActive(false);
            _animatedOutlineCharacter.SetActive(false);
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
            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OpenSettingsPanel);

            if (creditsButton != null)
                creditsButton.onClick.RemoveListener(OpenCreditsPanel);

            if (quitButton != null)
                quitButton.onClick.RemoveListener(QuitGame);
        }
    }
}