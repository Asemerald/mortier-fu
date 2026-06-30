using System;
using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MortierFu
{
    public sealed class LobbyCustomizationStation : LobbyInteractionZone
    {
        [Header("References")]
        [SerializeField] private LobbySandboxController _sandboxController;
        [SerializeField] private LobbySandboxStateController _stateController;
        [SerializeField] private LobbyCustomizationController[] _playerPanels = new LobbyCustomizationController[4];

        private readonly Dictionary<PlayerManager, LobbyCustomizationController> _activePanels = new();
        private readonly HashSet<PlayerManager> _closingPlayers = new();
        private CancellationTokenSource _stationCancellation;

        private void Awake()
        {
            _stationCancellation = new CancellationTokenSource();
        }
        
        private void OnEnable()
        {
            if (_stateController)
                _stateController.OnCustomizationInterrupted += HandleCustomizationInterrupted;
        }

        protected override void OnDisable()
        {
            if (_stateController)
                _stateController.OnCustomizationInterrupted -= HandleCustomizationInterrupted;

            _stationCancellation?.Cancel();
            
            ForceCloseAllCustomizations(restoreSandboxControl: false, exitState: true);

            base.OnDisable();
        }
        
        private void OnDestroy()
        {
            _stationCancellation?.Cancel();
            _stationCancellation?.Dispose();
            _stationCancellation = null;
        }

        protected override bool CanInteract(PlayerManager player)
        {
            if (!base.CanInteract(player))
                return false;

            if (_activePanels.ContainsKey(player))
                return false;

            if (_closingPlayers.Contains(player))
                return false;
            
            if (!_stateController)
                return false;

            if (!GetPanelForPlayer(player))
                return false;

            return _stateController.CanUseCustomizationStation(player);
        }

        protected override void Interact(PlayerManager player)
        {
            if (!player)
                return;

            if (!_stateController)
            {
                Logs.LogError("[LobbyCustomizationStation] State controller reference is missing.");
                return;
            }

            LobbyCustomizationController panel = GetPanelForPlayer(player);

            if (!panel)
            {
                Logs.LogError($"[LobbyCustomizationStation] No customization panel assigned for Player {player.PlayerIndex + 1}.");
                return;
            }

            if (!_stateController.TryEnterCustomization(player))
                return;

            _activePanels[player] = panel;

            Logs.Log($"[LobbyCustomizationStation] Player {player.PlayerIndex + 1} entered customization.");

            player.SetControlContext(PlayerControlContext.LobbyCustomization);

            panel.Open(player, OnCustomizationConfirmed);
        }

        private void OnCustomizationConfirmed(PlayerManager player)
        {
            if (!player)
                return;

            ForceCloseCustomizationAsync(
                player,
                restoreSandboxControl: true,
                exitState: true,
                waitForExitAnimation: true
            ).Forget();
        }

        private void HandleCustomizationInterrupted(PlayerManager player)
        {
            if (!player)
                return;

            ForceCloseCustomizationAsync(
                player,
                restoreSandboxControl: false,
                exitState: false,
                waitForExitAnimation: false
            ).Forget();
        }

        private void ForceCloseAllCustomizations(bool restoreSandboxControl, bool exitState)
        {
            if (_activePanels.Count == 0)
                return;

            var players = new List<PlayerManager>(_activePanels.Keys);

            for (int i = 0; i < players.Count; i++)
            {
                ForceCloseCustomization(players[i], restoreSandboxControl, exitState);
            }
        }

        private void ForceCloseCustomization(PlayerManager player, bool restoreSandboxControl, bool exitState)
        {
            ForceCloseCustomizationAsync(
                player,
                restoreSandboxControl,
                exitState,
                waitForExitAnimation: false
            ).Forget();
        }
        
        private async UniTaskVoid ForceCloseCustomizationAsync(PlayerManager player, bool restoreSandboxControl, bool exitState, bool waitForExitAnimation)
        {
            if (!player)
                return;

            if (!_activePanels.TryGetValue(player, out var panel))
                return;

            if (!_closingPlayers.Add(player))
                return;

            try
            {
                player.SetControlContext(PlayerControlContext.LobbyCustomization);

                if (panel)
                {
                    if (waitForExitAnimation && _stationCancellation != null)
                    {
                        await panel.CloseAsync(_stationCancellation.Token);
                    }
                    else
                    {
                        panel.Close();
                    }
                }

                _activePanels.Remove(player);

                if (exitState && _stateController)
                    _stateController.TryExitCustomization(player);

                if (restoreSandboxControl)
                {
                    RestorePlayerLobbyContext(player);
                }
                else
                {
                    player.SetControlContext(PlayerControlContext.LobbyLocked);
                }

                Logs.Log($"[LobbyCustomizationStation] Player {player.PlayerIndex + 1} left customization.");
            }
            catch (OperationCanceledException)
            {
                if (panel)
                    panel.Close();

                _activePanels.Remove(player);

                if (exitState && _stateController)
                    _stateController.TryExitCustomization(player);

                player.SetControlContext(PlayerControlContext.LobbyLocked);
            }
            finally
            {
                _closingPlayers.Remove(player);
            }
        }

        private void RestorePlayerLobbyContext(PlayerManager player)
        {
            if (!player)
                return;

            if (_sandboxController)
            {
                _sandboxController.ApplyCurrentContextToPlayer(player);
                return;
            }

            player.SetControlContext(PlayerControlContext.LobbySandbox);
        }

        private LobbyCustomizationController GetPanelForPlayer(PlayerManager player)
        {
            if (!player)
                return null;

            if (_playerPanels is null)
                return null;

            int index = player.PlayerIndex;

            if (index < 0 || index >= _playerPanels.Length)
                return null;

            return _playerPanels[index];
        }
    }
}