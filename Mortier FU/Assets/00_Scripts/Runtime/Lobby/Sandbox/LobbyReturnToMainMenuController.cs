using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbyReturnToMainMenuController : LobbyInteractionZone
    {
        [Header("References")]
        [SerializeField] private LobbySandboxController _sandboxController;
        [SerializeField] private LobbySandboxStateController _stateController;
        [SerializeField] private LobbyStartReadyController _readyController;
        [SerializeField] private UIConfirmationModalController _confirmationModal;

        [Header("Text")]
        [SerializeField] private string _description = "Are You sure You want to return to main menu?";
        [SerializeField] private string _confirmLabel = "Confirm";
        [SerializeField] private string _cancelLabel = "Cancel";

        [Header("Options")]
        [SerializeField] private bool _despawnLobbyCharacters = true;
        [SerializeField] private bool _disableJoining = true;

        private PlayerManager _activePlayer;
        private bool _isReturning;

        protected override void OnDisable()
        {
            _activePlayer = null;
            base.OnDisable();
        }

        protected override bool CanEnter(PlayerManager player)
        {
            if (!base.CanEnter(player))
                return false;

            if (_isReturning)
                return false;

            if (!player)
                return false;

            if (!_confirmationModal || !_sandboxController)
                return false;

            if (_stateController && !_stateController.CanUseStartTarget())
                return false;

            if (!IsPlayerInSandbox(player))
                return false;

            return player.ControlContext == PlayerControlContext.LobbySandbox;
        }

        protected override void OnPlayerEntered(PlayerManager player)
        {
            if (!player)
                return;

            if (_activePlayer)
                return;

            _activePlayer = player;

            UIConfirmationRequest request = new(
                owner: player,
                description: _description,
                confirmLabel: _confirmLabel,
                cancelLabel: _cancelLabel,
                onConfirmAsync: () => ConfirmReturnToMainMenuAsync(player),
                onCancelAfterCloseAsync: () => CancelReturnToMainMenuAsync(player),
                pauseGameWhileOpen: true,
                lockPlayersWhileOpen: true,
                restoreContextOnConfirm: false,
                resumeTimeScaleOnConfirm: true
            );

            if (!_confirmationModal.TryOpen(request))
            {
                _activePlayer = null;
                return;
            }

            Logs.Log($"[LobbyReturnToMainMenuController] Confirmation opened by Player {player.PlayerIndex + 1}.");
        }

        private UniTask CancelReturnToMainMenuAsync(PlayerManager player)
        {
            if (player)
                IgnorePlayerUntilExit(player);

            _activePlayer = null;

            Logs.Log("[LobbyReturnToMainMenuController] Return to main menu canceled.");

            return UniTask.CompletedTask;
        }

        private async UniTask ConfirmReturnToMainMenuAsync(PlayerManager player)
        {
            if (_isReturning)
                return;

            _isReturning = true;

            Logs.Log("[LobbyReturnToMainMenuController] Returning to main menu from lobby.");

            LockLobbyForTransition();
            ResetReadyState();
            DisableJoining();
            ClearActiveBombshells();
            PreparePlayersForMainMenu();

            GameService gameService = ServiceManager.Instance.Get<GameService>();

            if (gameService is null)
            {
                Logs.LogError("[LobbyReturnToMainMenuController] GameService is missing. Cannot return to main menu.", this);
                _isReturning = false;
                _activePlayer = null;
                return;
            }

            await gameService.ReturnLobbyToMainMenuAsync();
        }

        private bool IsPlayerInSandbox(PlayerManager player)
        {
            if (!player || !_sandboxController)
                return false;

            var players = _sandboxController.GetSpawnedPlayers();

            for (int i = 0; i < players.Count; i++)
            {
                if (ReferenceEquals(players[i], player))
                    return true;
            }

            return false;
        }

        private void LockLobbyForTransition()
        {
            _stateController?.InterruptPlayerActivitiesForGlobalTransition();
            _sandboxController?.LockAllPlayers();
        }

        private void ResetReadyState()
        {
            if (_readyController)
                _readyController.ResetReady();
        }

        private void DisableJoining()
        {
            if (!_disableJoining)
                return;

            PlayerInputBridge.Instance?.CanJoin(false);
        }

        private void ClearActiveBombshells()
        {
            BombshellSystem bombshellSystem = SystemManager.Instance?.Get<BombshellSystem>();
            bombshellSystem?.ClearActiveBombshells();
        }

        private void PreparePlayersForMainMenu()
        {
            if (!_sandboxController)
                return;

            var players = _sandboxController.GetSpawnedPlayers();

            for (int i = 0; i < players.Count; i++)
            {
                PlayerManager player = players[i];

                if (!player)
                    continue;

                player.SetControlContext(PlayerControlContext.Menu);

                if (_despawnLobbyCharacters)
                    player.DespawnInGame();
            }
        }
    }
}