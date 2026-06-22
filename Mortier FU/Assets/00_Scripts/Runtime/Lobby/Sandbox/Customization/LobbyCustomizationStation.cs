using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbyCustomizationStation : LobbyInteractionZone
    {
        [Header("References")]
        [SerializeField] private LobbySandboxStateController _stateController;
        [SerializeField] private LobbyCustomizationPanel[] _playerPanels = new LobbyCustomizationPanel[4];

        private readonly Dictionary<PlayerManager, LobbyCustomizationPanel> _activePanels = new();
        
        private void OnEnable()
        {
            if (_stateController != null)
            {
                _stateController.OnCustomizationInterrupted += HandleCustomizationInterrupted;
            }
        }

        protected override void OnDisable()
        {
            if (_stateController != null)
            {
                _stateController.OnCustomizationInterrupted -= HandleCustomizationInterrupted;
            }

            ForceCloseAllCustomizations(restoreSandboxControl: false);

            base.OnDisable();
        }
        
        private void ForceCloseAllCustomizations(bool restoreSandboxControl)
        {
            var players = new List<PlayerManager>(_activePanels.Keys);

            foreach (var player in players)
            {
                ForceCloseCustomization(player, restoreSandboxControl);
            }
        }
        
        private LobbyCustomizationPanel GetPanelForPlayer(PlayerManager player)
        {
            if (player == null)
                return null;

            int index = player.PlayerIndex;

            if (_playerPanels == null)
                return null;

            if (index < 0 || index >= _playerPanels.Length)
                return null;

            return _playerPanels[index];
        }
        
        protected override bool CanInteract(PlayerManager player)
        {
            if (!base.CanInteract(player))
                return false;

            if (_activePanels.ContainsKey(player))
                return false;

            if (_stateController == null)
                return false;

            return _stateController.CanUseCustomizationStation(player);
        }

        protected override void Interact(PlayerManager player)
        {
            if (player == null)
                return;

            if (_stateController == null)
            {
                Logs.LogError("[LobbyCustomizationStation] State controller reference is missing.");
                return;
            }

            var panel = GetPanelForPlayer(player);

            if (panel == null)
            {
                Logs.LogError($"[LobbyCustomizationStation] No customization panel assigned for Player {player.PlayerIndex + 1}.");
                return;
            }

            if (!_stateController.TryEnterCustomization(player))
                return;

            _activePanels[player] = panel;

            Logs.Log($"[LobbyCustomizationStation] Player {player.PlayerIndex + 1} entered customization.");

            player.SetControlContext(PlayerControlContext.LobbyCustomization);

            panel.Open(
                player,
                OnCustomizationConfirmed
            );
        }

        private void OnCustomizationConfirmed(PlayerManager player)
        {
            if (player == null)
                return;

            ForceCloseCustomization(player, restoreSandboxControl: true);

            _stateController?.TryExitCustomization(player);
        }
        
        private void HandleCustomizationInterrupted(PlayerManager player)
        {
            if (player == null)
                return;

            ForceCloseCustomization(player, restoreSandboxControl: false);
        }
        
        private void ForceCloseCustomization(PlayerManager player, bool restoreSandboxControl)
        {
            if (player == null)
                return;

            if (!_activePanels.TryGetValue(player, out var panel))
                return;

            panel?.Close();

            _activePanels.Remove(player);

            if (restoreSandboxControl)
            {
                player.SetControlContext(PlayerControlContext.LobbySandbox);
            }
            else
            {
                player.SetControlContext(PlayerControlContext.LobbyLocked);
            }

            Logs.Log($"[LobbyCustomizationStation] Player {player.PlayerIndex + 1} left customization.");
        }
    }
}