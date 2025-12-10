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

        private GameService _gameService;
        
        void Awake()
        {
            // Resolve dependencies
            _gameService = ServiceManager.Instance.Get<GameService>();
            
            if (_gameService == null)
                Logs.LogError("[LobbyPanel]: GameService could not be found in ServiceManager.", this);
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
                StartGame().Forget();
            }
#endif
        }

        private void OnEnable()
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
            PlayerInputBridge.Instance.CanJoin(true);
        }
        
        private void OnDisable()
        {
            startGameButton.onClick.RemoveListener(OnStartGameClicked);
            PlayerInputBridge.Instance.CanJoin(false);
        }

        private void OnStartGameClicked() => StartGame().Forget();
        
        private async UniTask StartGame()
        {
            Logs.Log("[LobbyPanel]: Start Game button clicked.");
            // When game mode is selected
            await _gameService.InitializeGameMode<GM_FFA>();
            
            // Should handle game mode teams

            _gameService.ExecuteGameplayPipeline().Forget();
        }
    }
}