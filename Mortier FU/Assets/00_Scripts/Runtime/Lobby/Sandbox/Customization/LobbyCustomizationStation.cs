using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbyCustomizationStation : LobbyInteractionZone
    {
        [Header("References")]
        [SerializeField] private LobbyCustomizationPanel _customizationPanel;

        private PlayerManager _activePlayer;

        protected override bool CanInteract(PlayerManager player)
        {
            if (!base.CanInteract(player))
                return false;

            if (_activePlayer != null)
                return false;

            return true;
        }

        protected override void Interact(PlayerManager player)
        {
            if (player == null)
                return;

            if (_customizationPanel == null)
            {
                Logs.LogError("[LobbyCustomizationStation] Customization panel reference is missing.");
                return;
            }

            _activePlayer = player;

            Logs.Log($"[LobbyCustomizationStation] Player {player.PlayerIndex + 1} entered customization.");

            player.SetControlContext(PlayerControlContext.LobbyCustomization);

            _customizationPanel.Open(
                player,
                OnCustomizationConfirmed
            );
        }

        private void OnCustomizationConfirmed(PlayerManager player)
        {
            if (player == null)
                return;

            if (player != _activePlayer)
                return;

            _customizationPanel.Close();

            player.SetControlContext(PlayerControlContext.LobbySandbox);

            Logs.Log($"[LobbyCustomizationStation] Player {player.PlayerIndex + 1} left customization.");

            _activePlayer = null;
        }
    }
}