using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace MortierFu
{
    public class DevicesService : IGameService
    {

        private readonly Dictionary<int, InputDevice> _playerDevices = new();
        private readonly Dictionary<InputDevice, int> _deviceToPlayer = new();
        

        public void RegisterPlayerDevice(int playerIndex, InputDevice device)
        {
            if (_playerDevices.ContainsKey(playerIndex))
                _playerDevices[playerIndex] = device;
            else
                _playerDevices.Add(playerIndex, device);

            if (!_deviceToPlayer.ContainsKey(device))
                _deviceToPlayer.Add(device, playerIndex);

            Logs.Log($"Registered device {device.displayName} for Player {playerIndex}");
        }

        public void UnregisterPlayer(int playerIndex)
        {
            if (_playerDevices.TryGetValue(playerIndex, out var device))
                _deviceToPlayer.Remove(device);

            _playerDevices.Remove(playerIndex);
        }

        public void HandleDeviceChange(InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                case InputDeviceChange.Disconnected:
                    OnDeviceDisconnected(device);
                    break;
                case InputDeviceChange.Reconnected:
                    OnDeviceReconnected(device);
                    break;
            }
        }

        private void OnDeviceDisconnected(InputDevice device)
        {
            if (_deviceToPlayer.TryGetValue(device, out var index))
            {
                Logs.LogWarning($"Player {index}'s device '{device.displayName}' disconnected");
            }
        }

        private void OnDeviceReconnected(InputDevice device)
        {
            if (_deviceToPlayer.TryGetValue(device, out var index))
            {
                Logs.Log($"Device '{device.displayName}' reconnected for Player {index}");

                var playerInputs = Object.FindObjectsByType<PlayerInput>(sortMode: FindObjectsSortMode.None);
                foreach (var input in playerInputs)
                {
                    if (input.playerIndex == index)
                    {
                        InputUser.PerformPairingWithDevice(device, input.user);
                        input.ActivateInput();
                        Logs.Log($"Repaired device '{device.displayName}' for Player {index}");
                        return;
                    }
                }
            }
            else
            {
                Logs.LogWarning($"Reconnected device '{device.displayName}' not linked to any player");
            }
        }

        public void Dispose()
        {
        }
    }
}
