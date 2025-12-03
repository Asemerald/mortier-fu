using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MortierFu {
    public class MainMenuPanel : MonoBehaviour {
        [Header("Panels")] [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private GameObject quitConfirmationPanel;

        [Header("Buttons")] [SerializeField] private Button playButton;
        [SerializeField] private GameObject settingsButton;
        [SerializeField] private GameObject creditsButton;
        [SerializeField] private GameObject quitButton;

        private void Start() {
#if UNITY_EDITOR
            if (EditorPrefs.GetBool("SkipMenuEnabled", false)) {
                foreach (Gamepad gamepad in Gamepad.all) {
                    var playerInputManager = FindFirstObjectByType<PlayerInputManager>();
                    playerInputManager.JoinPlayer(pairWithDevice: gamepad);
                    Logs.Log("[PlayerInputBridge]: Auto-connecting device with ID: " + gamepad.deviceId);
                }
            }
#endif

            Show();
            InitializeButtons();
        }

        private void InitializeButtons() {
            playButton.onClick.AddListener(OpenLobbyPanel);
        }

        public void Show() {
            mainMenuPanel.SetActive(true);
        }

        public void Hide() {
            mainMenuPanel.SetActive(false);
        }

        private void OpenLobbyPanel() {
            Hide();
            lobbyPanel.SetActive(true);
        }
    }
}