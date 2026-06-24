using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbyReturnToMainMenuController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LobbySandboxController _sandboxController;
        [SerializeField] private LobbyStartReadyController _readyController;

        [Header("Options")]
        [SerializeField] private bool _despawnLobbyCharacters = true;
        [SerializeField] private bool _disableJoining = true;

        private bool _isReturning;

        private void Awake()
        {
            if (!_sandboxController)
                _sandboxController = GetComponent<LobbySandboxController>();

            if (!_readyController)
                _readyController = GetComponent<LobbyStartReadyController>();
        }

        public void ReturnToMainMenu()
        {
            if (_isReturning)
                return;

            ReturnToMainMenuAsync().Forget();
        }

        private async UniTaskVoid ReturnToMainMenuAsync()
        {
            _isReturning = true;

            Logs.Log("[LobbyReturnToMainMenuController] Returning to main menu from lobby.");

            LockLobby();
            ResetReadyState();
            DisableJoining();
            ClearActiveBombshells();
            PreparePlayersForMainMenu();

            var gameService = ServiceManager.Instance.Get<GameService>();

            if (gameService is null)
            {
                Logs.LogError("[LobbyReturnToMainMenuController] GameService is missing. Cannot return to main menu.");
                _isReturning = false;
                return;
            }

            await gameService.ReturnLobbyToMainMenuAsync();
        }

        private void LockLobby()
        {
            if (_sandboxController)
            {
                _sandboxController.LockAllPlayers();
            }
        }

        private void ResetReadyState()
        {
            if (_readyController)
            {
                _readyController.ResetReady();
            }
        }

        private void DisableJoining()
        {
            if (!_disableJoining)
                return;

            if (PlayerInputBridge.Instance)
            {
                PlayerInputBridge.Instance.CanJoin(false);
            }
        }

        private void ClearActiveBombshells()
        {
            var bombshellSystem = SystemManager.Instance?.Get<BombshellSystem>();
            bombshellSystem?.ClearActiveBombshells();
        }

        private void PreparePlayersForMainMenu()
        {
            if (!_sandboxController)
                return;

            var players = _sandboxController.GetSpawnedPlayers();

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];

                if (!player)
                    continue;

                player.SetControlContext(PlayerControlContext.Menu);

                if (_despawnLobbyCharacters)
                {
                    player.DespawnInGame();
                }
            }
        }
    }
}