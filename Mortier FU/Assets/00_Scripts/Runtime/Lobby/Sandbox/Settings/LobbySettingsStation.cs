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

        protected override bool CanInteract(PlayerManager player)
        {
            if (!base.CanInteract(player))
                return false;

            if (_activePlayer != null)
                return false;

            if (_stateController == null)
                return false;

            return _stateController.CanUseSettingsStation(player);
        }

        protected override void Interact(PlayerManager player)
        {
            if (player == null)
                return;

            if (_stateController == null)
            {
                Logs.LogError("[LobbySettingsStation] State controller reference is missing.");
                return;
            }

            if (_settingsPanel == null)
            {
                Logs.LogError("[LobbySettingsStation] Settings panel reference is missing.");
                return;
            }

            if (!_stateController.TryEnterSettings(player))
                return;

            _activePlayer = player;

            Logs.Log($"[LobbySettingsStation] Player {player.PlayerIndex + 1} entered settings.");

            _cameraFocusController?.FocusSettings();

            _settingsPanel.Open(
                player,
                OnSettingsClosed
            );
        }

        private void OnSettingsClosed(PlayerManager player)
        {
            if (player == null)
                return;

            if (player != _activePlayer)
                return;

            _settingsPanel.Close();

            _cameraFocusController?.FocusSandbox();

            _stateController?.TryExitSettings(player);

            Logs.Log($"[LobbySettingsStation] Player {player.PlayerIndex + 1} left settings.");

            _activePlayer = null;
        }
    }
}