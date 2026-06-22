using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbyMatchLauncher : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LobbySandboxController _sandboxController;
        [SerializeField] private LobbySandboxStateController _stateController;
        [SerializeField] private LobbyMatchSettingsData _settingsData;
        
        [Header("Rules")]
        [SerializeField] private int _minimumPlayersToLaunch = 2;

        private bool _isLaunching;

        public bool CanLaunch(IReadOnlyList<PlayerManager> players)
        {
            if (_isLaunching)
                return false;

            if (players == null)
                return false;

            return players.Count >= _minimumPlayersToLaunch;
        }

        public void LaunchMatch()
        {
            if (_isLaunching)
                return;

            LaunchMatchAsync().Forget();
        }

        private async UniTaskVoid LaunchMatchAsync()
        {
            _isLaunching = true;

            Logs.Log("[LobbyMatchLauncher] Launching match from sandbox lobby.");

            if (_stateController != null)
            {
                if (!_stateController.TryBeginLaunching())
                {
                    _isLaunching = false;
                    return;
                }
            }
            else if (_sandboxController != null)
            {
                _sandboxController.LockAllPlayers();
            }

            if (PlayerInputBridge.Instance != null)
            {
                PlayerInputBridge.Instance.CanJoin(false);
            }

            var bombshellSystem = SystemManager.Instance?.Get<BombshellSystem>();
            bombshellSystem?.ClearActiveBombshells();

            var gameService = ServiceManager.Instance.Get<GameService>();

            if (gameService == null)
            {
                Logs.LogError("[LobbyMatchLauncher] GameService is missing. Cannot launch match.");
                _isLaunching = false;
                return;
            }
            
            if (_settingsData != null)
            {
                gameService.SetPendingMatchConfig(_settingsData.ToMatchConfig());
            }

            await gameService.InitializeGameMode<GM_FFA>();

            gameService.ExecuteGameplayPipeline().Forget();
        }
    }
}