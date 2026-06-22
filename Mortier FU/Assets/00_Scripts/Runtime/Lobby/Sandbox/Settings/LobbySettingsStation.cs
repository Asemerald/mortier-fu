using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbySettingsStation : LobbyInteractionZone
    {
        [Header("References")]
        [SerializeField] private LobbySandboxController _sandboxController;
        [SerializeField] private LobbySettingsPanel _settingsPanel;
        [SerializeField] private LobbyCameraFocusController _cameraFocusController;

        [Header("Rules")]
        [SerializeField] private int _ownerPlayerIndex = 0;

        private PlayerManager _activePlayer;

        protected override bool CanInteract(PlayerManager player)
        {
            if (!base.CanInteract(player))
                return false;

            if (_activePlayer != null)
                return false;

            if (player.PlayerIndex != _ownerPlayerIndex)
                return false;

            return true;
        }

        protected override void Interact(PlayerManager player)
        {
            if (player == null)
                return;

            if (_sandboxController == null)
            {
                Logs.LogError("[LobbySettingsStation] Sandbox controller reference is missing.");
                return;
            }

            if (_settingsPanel == null)
            {
                Logs.LogError("[LobbySettingsStation] Settings panel reference is missing.");
                return;
            }

            _activePlayer = player;

            Logs.Log($"[LobbySettingsStation] Player {player.PlayerIndex + 1} entered settings.");

            _sandboxController.LockAllPlayers();

            player.SetControlContext(PlayerControlContext.LobbySettingsOwner);

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

            _sandboxController.UnlockAllPlayers();

            Logs.Log($"[LobbySettingsStation] Player {player.PlayerIndex + 1} left settings.");

            _activePlayer = null;
        }
    }
}