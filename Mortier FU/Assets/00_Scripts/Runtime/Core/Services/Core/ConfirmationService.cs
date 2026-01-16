using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class ConfirmationService : IGameService
    {
        private DeviceService _deviceService;

        private readonly Dictionary<PlayerInput, Action<InputAction.CallbackContext>> _callbacks =
            new();

        private int _confirmationCount;
        private const string k_confirmAction = "Confirm";

        public bool IsInitialized { get; set; }

        public event Action<int> OnPlayerConfirmed;
        public event Action OnAllPlayersConfirmed;
        public event Action<int> OnStartConfirmation;

        public UniTask OnInitialize()
        {
            _deviceService = ServiceManager.Instance.Get<DeviceService>();
            return UniTask.CompletedTask;
        }

        public async Task WaitUntilHostConfirmed()
        {
            if (!_deviceService.TryGetPlayerInput(0, out var playerInput))
            {
                Logs.LogError("[ConfirmationService]: No input found for host (player 0).");
                return;
            }

            _confirmationCount = 1;
            RequestConfirmation(playerInput);

            while (_confirmationCount > 0)
                await Task.Yield();
            
            OnAllPlayersConfirmed?.Invoke();
            Logs.Log("[ConfirmationService] Host confirmed.");
        }
        
        public void ShowConfirmation(int activePlayers)
        {
            OnStartConfirmation?.Invoke(activePlayers);
            Logs.Log("[ConfirmationService] Confirmation started.");
        }

        public async Task WaitUntilAllConfirmed()
        {
            var players = _deviceService.GetAllPlayerInputs();
            _confirmationCount = players.Count;

            foreach (var playerInput in players)
                RequestConfirmation(playerInput);

            while (_confirmationCount > 0)
                await Task.Yield();

            OnAllPlayersConfirmed?.Invoke();
            Logs.Log("[ConfirmationService] All players confirmed.");
        }

        private void RequestConfirmation(PlayerInput playerInput)
        {
            var action = playerInput.actions.FindAction(k_confirmAction);
            if (action == null)
            {
                Logs.LogError($"[ConfirmationService] Confirm action not found for player {playerInput.playerIndex}");
                _confirmationCount--;
                return;
            }

            Action<InputAction.CallbackContext> callback =
                ctx => OnConfirmed(playerInput.playerIndex, playerInput, action);

            _callbacks[playerInput] = callback;
            action.performed += callback;
        }

        private void OnConfirmed(int playerIndex, PlayerInput playerInput, InputAction action)
        {
            _confirmationCount--;

            OnPlayerConfirmed?.Invoke(playerIndex);
            _deviceService.TryGetDevice(playerIndex, out InputDevice device);
            ShakeService.ShakeController(device, ShakeService.ShakeType.MID);

            if (_callbacks.TryGetValue(playerInput, out var callback))
            {
                action.performed -= callback;
                _callbacks.Remove(playerInput);
            }
        }

        public void Dispose()
        {
            foreach (var pair in _callbacks)
            {
                var action = pair.Key.actions.FindAction(k_confirmAction);
                if (action != null)
                    action.performed -= pair.Value;
            }

            _callbacks.Clear();
        }
    }
}
