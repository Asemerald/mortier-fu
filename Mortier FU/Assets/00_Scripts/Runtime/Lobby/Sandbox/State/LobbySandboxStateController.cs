using System;
using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbySandboxStateController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LobbySandboxController _sandboxController;

        [Header("Rules")]
        [SerializeField] private int _settingsOwnerPlayerIndex = 0;

        public LobbySandboxState CurrentState { get; private set; } = LobbySandboxState.Sandbox;

        private readonly HashSet<PlayerManager> _customizationPlayers = new();

        public IReadOnlyCollection<PlayerManager> CustomizationPlayers => _customizationPlayers;
        public PlayerManager ActiveSettingsPlayer { get; private set; }

        public bool IsGlobalLockActive =>
            CurrentState is LobbySandboxState.GlobalSettings or LobbySandboxState.LaunchingGame;

        public bool IsLaunching => CurrentState == LobbySandboxState.LaunchingGame;

        public event Action<LobbySandboxState, LobbySandboxState> OnStateChanged;
        public event Action<PlayerManager> OnCustomizationInterrupted;

        public bool TryEnterCustomization(PlayerManager player)
        {
            if (player == null)
                return false;

            if (CurrentState is LobbySandboxState.GlobalSettings or LobbySandboxState.LaunchingGame)
                return false;

            if (_customizationPlayers.Contains(player))
                return false;

            bool wasEmpty = _customizationPlayers.Count == 0;

            _customizationPlayers.Add(player);

            if (wasEmpty)
            {
                ChangeState(LobbySandboxState.PlayerCustomization);
            }

            Logs.Log($"[LobbySandboxStateController] Player {player.PlayerIndex + 1} entered customization state.");

            return true;
        }

        public bool TryExitCustomization(PlayerManager player)
        {
            if (player == null)
                return false;

            if (!_customizationPlayers.Contains(player))
                return false;

            _customizationPlayers.Remove(player);

            Logs.Log($"[LobbySandboxStateController] Player {player.PlayerIndex + 1} exited customization state.");

            if (_customizationPlayers.Count == 0 &&
                CurrentState == LobbySandboxState.PlayerCustomization)
            {
                ChangeState(LobbySandboxState.Sandbox);
            }

            return true;
        }

        public bool TryEnterSettings(PlayerManager player)
        {
            if (player == null)
                return false;

            if (player.PlayerIndex != _settingsOwnerPlayerIndex)
                return false;

            if (CurrentState == LobbySandboxState.GlobalSettings)
                return false;

            if (CurrentState == LobbySandboxState.LaunchingGame)
                return false;

            if (_customizationPlayers.Count > 0)
            {
                InterruptAllCustomizations();
            }

            ActiveSettingsPlayer = player;

            ChangeState(LobbySandboxState.GlobalSettings);

            _sandboxController?.LockAllPlayers();

            player.SetControlContext(PlayerControlContext.LobbySettingsOwner);

            Logs.Log($"[LobbySandboxStateController] Player {player.PlayerIndex + 1} entered settings state.");

            return true;
        }

        public bool TryExitSettings(PlayerManager player)
        {
            if (player == null)
                return false;

            if (CurrentState != LobbySandboxState.GlobalSettings)
                return false;

            if (ActiveSettingsPlayer != player)
                return false;

            ActiveSettingsPlayer = null;

            _sandboxController?.UnlockAllPlayers();

            ChangeState(LobbySandboxState.Sandbox);

            Logs.Log($"[LobbySandboxStateController] Player {player.PlayerIndex + 1} exited settings state.");

            return true;
        }

        public bool TryBeginLaunching()
        {
            if (CurrentState == LobbySandboxState.LaunchingGame)
                return false;

            if (_customizationPlayers.Count > 0)
            {
                InterruptAllCustomizations();
            }

            ChangeState(LobbySandboxState.LaunchingGame);

            _sandboxController?.LockAllPlayers();

            Logs.Log("[LobbySandboxStateController] Lobby entered launching state.");

            return true;
        }

        public bool CanUseStartTarget()
        {
            return CurrentState == LobbySandboxState.Sandbox;
        }

        public bool CanUseCustomizationStation(PlayerManager player)
        {
            if (player == null)
                return false;

            if (_customizationPlayers.Contains(player))
                return false;

            return CurrentState is LobbySandboxState.Sandbox or LobbySandboxState.PlayerCustomization;
        }

        public bool CanUseSettingsStation(PlayerManager player)
        {
            if (player == null)
                return false;

            if (player.PlayerIndex != _settingsOwnerPlayerIndex)
                return false;

            return CurrentState is LobbySandboxState.Sandbox or LobbySandboxState.PlayerCustomization;
        }
        
        public PlayerControlContext GetContextForNewPlayer(PlayerManager player)
        {
            if (player == null)
                return PlayerControlContext.LobbyLocked;

            return CurrentState switch
            {
                LobbySandboxState.Sandbox => PlayerControlContext.LobbySandbox,

                LobbySandboxState.PlayerCustomization => PlayerControlContext.LobbySandbox,

                LobbySandboxState.GlobalSettings => PlayerControlContext.LobbyLocked,

                LobbySandboxState.LaunchingGame => PlayerControlContext.LobbyLocked,

                _ => PlayerControlContext.LobbyLocked
            };
        }

        private void InterruptAllCustomizations()
        {
            if (_customizationPlayers.Count == 0)
                return;

            var interruptedPlayers = new List<PlayerManager>(_customizationPlayers);

            _customizationPlayers.Clear();

            foreach (var player in interruptedPlayers)
            {
                if (player == null)
                    continue;

                Logs.Log($"[LobbySandboxStateController] Interrupting customization for Player {player.PlayerIndex + 1}.");
                OnCustomizationInterrupted?.Invoke(player);
            }
        }

        private void ChangeState(LobbySandboxState newState)
        {
            if (CurrentState == newState)
                return;

            var previousState = CurrentState;
            CurrentState = newState;

            Logs.Log($"[LobbySandboxStateController] State changed: {previousState} -> {newState}");

            OnStateChanged?.Invoke(previousState, newState);
        }
    }
}