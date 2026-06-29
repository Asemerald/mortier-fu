using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbyCustomizationStation : LobbyInteractionZone
    {
        [Header("References")]
        [SerializeField] private LobbySandboxController _sandboxController;
        [SerializeField] private LobbySandboxStateController _stateController;
        [SerializeField] private LobbyCustomizationController[] _playerPanels = new LobbyCustomizationController[4];

        private readonly Dictionary<PlayerManager, LobbyCustomizationController> _activePanels = new();

        private void OnEnable()
        {
            if (_stateController)
                _stateController.OnCustomizationInterrupted += HandleCustomizationInterrupted;
        }

        protected override void OnDisable()
        {
            if (_stateController)
                _stateController.OnCustomizationInterrupted -= HandleCustomizationInterrupted;

            ForceCloseAllCustomizations(restoreSandboxControl: false, exitState: true);

            base.OnDisable();
        }

        protected override bool CanInteract(PlayerManager player)
        {
            if (!base.CanInteract(player))
                return false;

            if (_activePanels.ContainsKey(player))
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

            var panel = GetPanelForPlayer(player);

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

            ForceCloseCustomization(player, restoreSandboxControl: true, exitState: true);
        }

        private void HandleCustomizationInterrupted(PlayerManager player)
        {
            if (!player)
                return;

            ForceCloseCustomization(player, restoreSandboxControl: false, exitState: false);
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
            if (!player)
                return;

            if (!_activePanels.TryGetValue(player, out var panel))
                return;

            if (panel)
                panel.Close();

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