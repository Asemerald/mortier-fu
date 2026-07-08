using System;
using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbySandboxStateController : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private LobbySandboxController _sandboxController;

        [Header("Rules")] private LobbySandboxState CurrentState { get; set; } = LobbySandboxState.Sandbox;

        private readonly HashSet<PlayerManager> _customizationPlayers = new();

        private PlayerManager ActiveSettingsPlayer { get; set; }

        private bool IsLaunching => CurrentState == LobbySandboxState.LaunchingGame;

        public event Action<PlayerManager> OnCustomizationInterrupted;
        public event Action<PlayerManager> OnSettingsInterrupted;

        public bool TryEnterCustomization(PlayerManager player)
        {
            if (!CanUseCustomizationStation(player))
                return false;

            _customizationPlayers.Add(player);

            Logs.Log($"[LobbySandboxStateController] Player {player.PlayerIndex + 1} entered customization.");

            return true;
        }
        
        
        public bool TryExitCustomization(PlayerManager player)
        {
            if (!player)
                return false;

            if (!_customizationPlayers.Remove(player))
                return false;

            Logs.Log($"[LobbySandboxStateController] Player {player.PlayerIndex + 1} exited customization.");
            

            return true;
        }

        public bool TryEnterSettings(PlayerManager player)
        {
            if (!CanUseSettingsStation(player))
                return false;

            ActiveSettingsPlayer = player;

            player.SetControlContext(PlayerControlContext.LobbySettingsOwner);

            Logs.Log($"[LobbySandboxStateController] Player {player.PlayerIndex + 1} entered settings.");

            return true;
        }

        public bool TryExitSettings(PlayerManager player)
        {
            if (!player)
                return false;

            if (!ReferenceEquals(ActiveSettingsPlayer, player))
                return false;

            ActiveSettingsPlayer = null;

            RestorePlayerLobbyContext(player);

            Logs.Log($"[LobbySandboxStateController] Player {player.PlayerIndex + 1} exited settings.");

            return true;
        }

        public bool TryBeginLaunching()
        {
            if (IsLaunching)
                return false;

            InterruptActiveSettings();
            InterruptAllCustomizations();

            ChangeState(LobbySandboxState.LaunchingGame);

            _sandboxController?.LockAllPlayers();

            Logs.Log("[LobbySandboxStateController] Lobby entered launching state.");

            return true;
        }

        public bool CanUseStartTarget() => !IsLaunching;

        public bool CanUseCustomizationStation(PlayerManager player)
        {
            if (!player)
                return false;

            if (IsLaunching)
                return false;

            if (_customizationPlayers.Contains(player))
                return false;

            return !ReferenceEquals(ActiveSettingsPlayer, player);
        }

        public bool CanUseSettingsStation(PlayerManager player)
        {
            if (!player)
                return false;

            if (IsLaunching)
                return false;

            if (ActiveSettingsPlayer)
                return false;

            return !_customizationPlayers.Contains(player);
        }

        private void InterruptAllCustomizations()
        {
            if (_customizationPlayers.Count == 0)
                return;

            var interruptedPlayers = new List<PlayerManager>(_customizationPlayers);

            _customizationPlayers.Clear();

            for (var i = 0; i < interruptedPlayers.Count; i++)
            {
                var player = interruptedPlayers[i];

                if (!player)
                    continue;

                Logs.Log($"[LobbySandboxStateController] Interrupting customization for Player {player.PlayerIndex + 1}.");
                OnCustomizationInterrupted?.Invoke(player);
            }
        }

        private void InterruptActiveSettings()
        {
            if (!ActiveSettingsPlayer)
                return;

            var interruptedPlayer = ActiveSettingsPlayer;
            ActiveSettingsPlayer = null;

            Logs.Log($"[LobbySandboxStateController] Interrupting settings for Player {interruptedPlayer.PlayerIndex + 1}.");
            OnSettingsInterrupted?.Invoke(interruptedPlayer);
        }

        private void RestorePlayerLobbyContext(PlayerManager player)
        {
            if (!player)
                return;

            if (IsLaunching)
            {
                player.SetControlContext(PlayerControlContext.LobbyLocked);
                return;
            }

            if (_sandboxController)
            {
                _sandboxController.ApplyCurrentContextToPlayer(player);
                return;
            }

            player.SetControlContext(PlayerControlContext.LobbySandbox);
        }

        private void ChangeState(LobbySandboxState newState)
        {
            if (CurrentState == newState)
                return;

            var previousState = CurrentState;
            CurrentState = newState;

            Logs.Log($"[LobbySandboxStateController] State changed: {previousState} -> {newState}");
        }
    }
}