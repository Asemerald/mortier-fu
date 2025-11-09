using System.Collections.Generic;
using System.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class ConfirmationService : IGameService
    {
        private DeviceService _deviceService;
        private List<PlayerManager> _players;
        private int _confirmationCount;
        
        private const string k_confirmAction = "Confirm";

        public ConfirmationService()
        {
            _players = new List<PlayerManager>();
        }
        
        public Task OnInitialize()
        {
            _deviceService = ServiceManager.Instance.Get<DeviceService>();
            return Task.CompletedTask;
        }

        public async Task WaitUntilHostConfirmed()
        {
            if (!_deviceService.TryGetPlayerInput(0, out var playerInput))
            {
                Logs.LogError("[ConfirmationService]: No device found for host player (index 0).");
                return;
            }
            
            var action = playerInput.actions.FindAction(k_confirmAction);
            if (action == null)
            {
                Logs.LogError("[ConfirmationService]: 'Confirm' action not found in host player's input actions.");
                return;
            }

            _confirmationCount = 1;
            RequestConfirmation(playerInput);
            
            while (_confirmationCount > 0) 
                await Task.Yield();
            
            Logs.Log("[ConfirmationService]: Host player confirmed.");
        }
        
        public async Task WaitUntilAllConfirmed()
        {
            var players = _deviceService.GetAllPlayerInputs();
            _confirmationCount = players.Count;

            foreach (var playerInput in players)
            {
                RequestConfirmation(playerInput);
            }
            
            while (_confirmationCount > 0) 
                await Task.Yield();
            
            Logs.Log("[ConfirmationService]: All players confirmed.");
        }
        
        private void RequestConfirmation(PlayerInput playerInput)
        {
            var action = playerInput.actions.FindAction(k_confirmAction);
            if (action == null)
            {
                Logs.LogError($"[ConfirmationService]: 'Confirm' action not found in player {playerInput.playerIndex}'s input actions.");
                _confirmationCount--;
                return;
            }
            action.performed += OnConfirmed;
        }

        private void OnConfirmed(InputAction.CallbackContext context)
        {
            _confirmationCount--;
            context.action.performed -= OnConfirmed;
        } 
        
        public void Dispose()
        {
            _players.Clear();
        }
        
        public bool IsInitialized { get; set; }
    }
}
