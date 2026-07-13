using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbySettingsStation : LobbyInteractionZone
    {
        [Header("References")] [SerializeField]
        private LobbySandboxStateController _stateController;
        [SerializeField] private LobbyCameraFocusController _cameraFocusController;

        [SerializeField] private LobbySettingsPanel _settingsPanel;

        [Header("Camera")] [SerializeField] private bool _focusCameraWhileSettingsOpen = true;

        private PlayerManager _activePlayer;

        private void OnEnable()
        {
            if (_stateController)
                _stateController.OnSettingsInterrupted += HandleSettingsInterrupted;
        }

        protected override void OnDisable()
        {
            if (_stateController)
                _stateController.OnSettingsInterrupted -= HandleSettingsInterrupted;

            ForceCloseActiveSettings(closePanel: true, exitState: true, lockPlayer: false);

            base.OnDisable();
        }

        protected override bool CanInteract(PlayerManager player)
        {
            if (!base.CanInteract(player))
                return false;

            if (_activePlayer || !_stateController)
                return false;

            return _settingsPanel && _stateController.CanUseSettingsStation(player);
        }

        protected override void Interact(PlayerManager player)
        {
            if (!player)
                return;

            if (!_stateController)
            {
                Logs.LogError("[LobbySettingsStation] State controller reference is missing.");
                return;
            }

            if (!_settingsPanel)
            {
                Logs.LogError("[LobbySettingsStation] Settings panel reference is missing.");
                return;
            }

            if (!_stateController.TryEnterSettings(player))
                return;

            _activePlayer = player;

            Logs.Log($"[LobbySettingsStation] Player {player.PlayerIndex + 1} entered settings.");

            if (_focusCameraWhileSettingsOpen && _cameraFocusController)
                _cameraFocusController.FocusSettings();

            _settingsPanel.Open(player, OnSettingsClosed);
        }

        private void OnSettingsClosed(PlayerManager player)
        {
            if (!player)
                return;

            if (!_activePlayer)
                return;

            if (!ReferenceEquals(_activePlayer, player))
                return;

            ForceCloseActiveSettings(closePanel: false, exitState: true, lockPlayer: false);
        }

        private void HandleSettingsInterrupted(PlayerManager player)
        {
            if (!player)
                return;

            if (!_activePlayer)
                return;

            if (!ReferenceEquals(_activePlayer, player))
                return;

            ForceCloseActiveSettings(closePanel: true, exitState: false, lockPlayer: true);
        }

        private void ForceCloseActiveSettings(bool closePanel, bool exitState, bool lockPlayer)
        {
            if (!_activePlayer)
                return;

            PlayerManager player = _activePlayer;
            _activePlayer = null;

            if (closePanel && _settingsPanel)
                _settingsPanel.Close();

            if (_focusCameraWhileSettingsOpen && _cameraFocusController)
                _cameraFocusController.FocusSandbox();

            if (exitState && _stateController)
                _stateController.TryExitSettings(player);
            else if (lockPlayer)
                player.SetControlContext(PlayerControlContext.LobbyLocked);

            Logs.Log($"[LobbySettingsStation] Player {player.PlayerIndex + 1} left settings.");
        }
    }
}