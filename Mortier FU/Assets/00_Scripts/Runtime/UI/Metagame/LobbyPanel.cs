using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MortierFu
{
    public class LobbyPanel : UIPanel
    {
        [Header("Dependencies")]
        [SerializeField] private LobbyMenu3D lobbyMenu3D;
        [Header("Buttons References")]
        [SerializeField] private Button startGameButton;
        
        void Awake()
        {
            // Resolve dependencies
            if (lobbyMenu3D == null)
                Logs.LogError("[LobbyPanel]: LobbyMenu3D reference is missing.", this);
            if (startGameButton == null)
                Logs.LogError("[LobbyPanel]: StartGameButton reference is missing.", this);
        }
        
        private void Start()
        {
            Hide();
            
#if UNITY_EDITOR
            if (EditorPrefs.GetBool("SkipMenuEnabled", false)) {
                MenuManager.Instance.StartGame().Forget();
            }
#endif
        }

        private void OnEnable()
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
            PlayerInputBridge.Instance.CanJoin(true);
            MenuManager.Instance.SwitchCameraPosition();
        }
        
        private void OnDisable()
        {
            startGameButton.onClick.RemoveListener(OnStartGameClicked);
            PlayerInputBridge.Instance.CanJoin(false);
        }

        private void OnStartGameClicked() => MenuManager.Instance.StartGame().Forget();
        
       
    }
}