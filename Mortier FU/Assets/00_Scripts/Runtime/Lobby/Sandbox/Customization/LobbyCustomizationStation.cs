using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbyCustomizationStation : LobbyInteractionZone
    {
        [Header("References")]
        [SerializeField] private LobbySandboxController _sandboxController;
        [SerializeField] private LobbyCustomizationPanel _customizationPanel;

        private PlayerManager _activePlayer;
        
        private void OnEnable()
        {
            if (_sandboxController != null)
            {
                _sandboxController.OnGlobalLockStarted += HandleGlobalLockStarted;
            }
        }

        protected override void OnDisable()
        {
            if (_sandboxController != null)
            {
                _sandboxController.OnGlobalLockStarted -= HandleGlobalLockStarted;
            }

            ForceCloseCustomization(restoreSandboxControl: false);

            base.OnDisable();
        }
        
        private void HandleGlobalLockStarted()
        {
            ForceCloseCustomization(restoreSandboxControl: false);
        }

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

            if (_sandboxController != null && _sandboxController.IsGlobalLockActive)
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

            bool restoreSandboxControl =
                _sandboxController == null || !_sandboxController.IsGlobalLockActive;

            ForceCloseCustomization(restoreSandboxControl);
        }
        
        private void ForceCloseCustomization(bool restoreSandboxControl)
        {
            if (_activePlayer == null)
                return;

            var player = _activePlayer;

            _customizationPanel?.Close();

            if (restoreSandboxControl)
            {
                player.SetControlContext(PlayerControlContext.LobbySandbox);
            }
            else
            {
                player.SetControlContext(PlayerControlContext.LobbyLocked);
            }

            Logs.Log($"[LobbyCustomizationStation] Player {player.PlayerIndex + 1} left customization.");

            _activePlayer = null;
        }
    }
}