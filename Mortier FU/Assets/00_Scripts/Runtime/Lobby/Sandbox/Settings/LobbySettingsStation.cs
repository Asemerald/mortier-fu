using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbySettingsStation : LobbyInteractionZone
    {
        [Header("References")]
        [SerializeField] private LobbySandboxStateController _stateController;
        [SerializeField] private LobbySettingsPanel _settingsPanel;
        [SerializeField] private LobbyCameraFocusController _cameraFocusController;

        private PlayerManager _activePlayer;

        protected override void OnDisable()
        {
            ForceCloseActiveSettings(closePanel: true, exitState: true);
            base.OnDisable();
        }

        protected override bool CanInteract(PlayerManager player)
        {
            if (!base.CanInteract(player))
                return false;

            if (_activePlayer)
                return false;

            if (!_stateController)
                return false;

            if (!_settingsPanel)
                return false;

            return _stateController.CanUseSettingsStation(player);
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

            if (_cameraFocusController)
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

            ForceCloseActiveSettings(closePanel: false, exitState: true);
        }

        private void ForceCloseActiveSettings(bool closePanel, bool exitState)
        {
            if (!_activePlayer)
                return;

            var player = _activePlayer;
            _activePlayer = null;

            if (closePanel && _settingsPanel)
                _settingsPanel.Close();

            if (_cameraFocusController)
                _cameraFocusController.FocusSandbox();

            if (exitState && _stateController)
                _stateController.TryExitSettings(player);

            Logs.Log($"[LobbySettingsStation] Player {player.PlayerIndex + 1} left settings.");
        }
    }
}