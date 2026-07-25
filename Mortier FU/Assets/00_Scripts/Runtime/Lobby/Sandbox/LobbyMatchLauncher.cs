using System;
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
        [SerializeField] private UIConfirmationModalController _confirmationModal;

        [Header("Confirmation Text")]
        [SerializeField] private string _description = "Are You sure You want to start the Game?";
        [SerializeField] private string _confirmLabel = "Confirm";
        [SerializeField] private string _cancelLabel = "Cancel";

        [Header("Rules")]
        [SerializeField, Min(1)] private int _minimumPlayersToLaunch = 2;

        private bool _isLaunchConfirmationOpen;
        private bool _isLaunching;

        public bool CanLaunch(IReadOnlyList<PlayerManager> players)
        {
            if (players is null)
                return false;

            int validPlayerCount = 0;

            for (int i = 0; i < players.Count; i++)
            {
                if (players[i])
                    validPlayerCount++;
            }

            return validPlayerCount >= _minimumPlayersToLaunch;
        }

        public bool TryOpenLaunchConfirmation(PlayerManager owner, Func<UniTask> onCanceledAsync)
        {
            if (_isLaunching || _isLaunchConfirmationOpen)
                return false;

            if (!owner)
                return false;

            if (_stateController && !_stateController.CanUseStartTarget())
                return false;

            if (!IsPlayerInSandbox(owner))
                return false;

            if (!_confirmationModal)
            {
                Logs.LogError("[LobbyMatchLauncher] ConfirmationModal reference is missing.");
                return false;
            }

            UIConfirmationRequest request = new(
                owner: owner,
                description: _description,
                confirmLabel: _confirmLabel,
                cancelLabel: _cancelLabel,
                onConfirmAsync: ConfirmLaunchMatchAsync,
                onCancelAfterCloseAsync: () => CancelLaunchConfirmationAsync(onCanceledAsync),
                pauseGameWhileOpen: true,
                lockPlayersWhileOpen: true,
                restoreContextOnConfirm: true,
                resumeTimeScaleOnConfirm: true
            );

            if (!_confirmationModal.TryOpen(request))
                return false;

            _isLaunchConfirmationOpen = true;

            Logs.Log($"[LobbyMatchLauncher] Launch confirmation opened by Player {owner.PlayerIndex + 1}.");

            return true;
        }
        
        private void PreparePlayersForGameplayTransition()
        {
            if (!_sandboxController)
                return;

            IReadOnlyList<PlayerManager> players = _sandboxController.GetSpawnedPlayers();

            for (int i = 0; i < players.Count; i++)
            {
                PlayerManager player = players[i];

                if (!player)
                    continue;

                player.SetUnityEventSystemUIActive(false);
                player.SetControlContext(PlayerControlContext.Loading);
            }
        }

        private async UniTask ConfirmLaunchMatchAsync()
        {
            _isLaunchConfirmationOpen = false;

            await LaunchMatchAsync();
        }

        private async UniTask CancelLaunchConfirmationAsync(Func<UniTask> onCanceledAsync)
        {
            _isLaunchConfirmationOpen = false;

            Logs.Log("[LobbyMatchLauncher] Launch match canceled.");

            if (onCanceledAsync != null)
                await onCanceledAsync();
        }

        private async UniTask LaunchMatchAsync()
        {
            if (_isLaunching)
                return;

            bool launchStarted = false;
            _isLaunching = true;

            try
            {
                Logs.Log("[LobbyMatchLauncher] Launching match from sandbox lobby.");
                if (_stateController)
                {
                    if (!_stateController.TryBeginLaunching())
                        return;
                }
                else if (_sandboxController)
                {
                    _sandboxController.LockAllPlayers();
                }

                PreparePlayersForGameplayTransition();

                PlayerInputBridge.Instance?.CanJoin(false);

                BombshellSystem bombshellSystem = SystemManager.Instance?.Get<BombshellSystem>();
                bombshellSystem?.ClearActiveBombshells();

                GameService gameService = ServiceManager.Instance.Get<GameService>();

                if (gameService is null)
                {
                    Logs.LogError("[LobbyMatchLauncher] GameService is missing. Cannot launch match.");
                    return;
                }

                if (_settingsData)
                    gameService.SetPendingMatchConfig(_settingsData.ToMatchConfig());

                await gameService.InitializeGameMode<GM_FFA>();

                gameService.ExecuteGameplayPipeline().Forget();

                launchStarted = true;
            }
            catch (Exception exception)
            {
                Logs.LogError($"[LobbyMatchLauncher] Failed to launch match: {exception}");
            }
            finally
            {
                if (!launchStarted)
                    _isLaunching = false;
            }
        }

        private bool IsPlayerInSandbox(PlayerManager player)
        {
            if (!player || !_sandboxController)
                return false;

            IReadOnlyList<PlayerManager> players = _sandboxController.GetSpawnedPlayers();

            for (int i = 0; i < players.Count; i++)
            {
                if (ReferenceEquals(players[i], player))
                    return true;
            }

            return false;
        }
    }
}