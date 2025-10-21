using System;
using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace MortierFu
{
    /// <summary>
    /// Service responsable du suivi et de la gestion des périphériques d'entrée des joueurs.
    /// </summary>
    public class LobbyService : IGameService
    {
        // Mapping principal : PlayerIndex <-> InputDevice
        private readonly Dictionary<int, InputDevice> _playerDevices = new();
        private readonly Dictionary<InputDevice, int> _deviceToPlayer = new();
        private readonly Dictionary<int, PlayerInput> _playerInputs = new();

        public event Action<int, InputDevice> OnDeviceRegistered;
        public event Action<int, InputDevice> OnDeviceUnregistered;
        public event Action<int, InputDevice> OnDeviceDisconnected;
        public event Action<int, InputDevice> OnDeviceReconnected;

        public void Initialize()
        {
            Logs.Log("[DevicesService] Initialized.");
            InputSystem.onDeviceChange += HandleDeviceChange;
        }

        public void Dispose()
        {
            InputSystem.onDeviceChange -= HandleDeviceChange;
            _playerDevices.Clear();
            _deviceToPlayer.Clear();
            _playerInputs.Clear();
            Logs.Log("[DevicesService] Disposed.");
        }

        #region Registration

        public void RegisterPlayerInput(PlayerInput input)
        {
            if (input == null)
                return;

            int playerIndex = input.playerIndex;
            InputDevice device = input.devices.Count > 0 ? input.devices[0] : null;

            if (device == null)
            {
                Logs.LogWarning($"[DevicesService] Player {playerIndex} joined without device.");
                return;
            }

            _playerInputs[playerIndex] = input;
            RegisterPlayerDevice(playerIndex, device);

            Logs.Log($"[DevicesService] Player {playerIndex} registered with device {device.displayName}.");
        }

        public void UnregisterPlayerInput(PlayerInput input)
        {
            if (input == null)
                return;

            int playerIndex = input.playerIndex;
            UnregisterPlayer(playerIndex);

            _playerInputs.Remove(playerIndex);
            Logs.Log($"[DevicesService] Player {playerIndex} unregistered.");
        }

        public void RegisterPlayerDevice(int playerIndex, InputDevice device)
        {
            if (device == null) return;

            _playerDevices[playerIndex] = device;
            _deviceToPlayer[device] = playerIndex;

            OnDeviceRegistered?.Invoke(playerIndex, device);
        }

        public void UnregisterPlayer(int playerIndex)
        {
            if (_playerDevices.TryGetValue(playerIndex, out var device))
            {
                _deviceToPlayer.Remove(device);
                _playerDevices.Remove(playerIndex);

                OnDeviceUnregistered?.Invoke(playerIndex, device);
            }
        }

        #endregion

        #region Device Events

        private void HandleDeviceChange(InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                case InputDeviceChange.Disconnected:
                    OnDeviceLost(device);
                    break;

                case InputDeviceChange.Reconnected:
                    OnDeviceRestored(device);
                    break;
            }
        }

        private void OnDeviceLost(InputDevice device)
        {
            if (!_deviceToPlayer.TryGetValue(device, out int playerIndex))
                return;

            Logs.LogWarning($"[DevicesService] Device '{device.displayName}' disconnected for Player {playerIndex}");
            OnDeviceDisconnected?.Invoke(playerIndex, device);

            // Désactiver temporairement les entrées du joueur
            if (_playerInputs.TryGetValue(playerIndex, out var playerInput))
                playerInput.DeactivateInput();
        }

        private void OnDeviceRestored(InputDevice device)
        {
            if (!_deviceToPlayer.TryGetValue(device, out int playerIndex))
            {
                Logs.LogWarning($"[DevicesService] Device '{device.displayName}' reconnected but not linked to any player.");
                return;
            }

            Logs.Log($"[DevicesService] Device '{device.displayName}' reconnected for Player {playerIndex}");
            OnDeviceReconnected?.Invoke(playerIndex, device);

            // Réapparier et réactiver l’entrée
            if (_playerInputs.TryGetValue(playerIndex, out var playerInput))
            {
                InputUser.PerformPairingWithDevice(device, playerInput.user);
                playerInput.ActivateInput();
                Logs.Log($"[DevicesService] Device '{device.displayName}' re-paired for Player {playerIndex}");
            }
        }

        #endregion

        #region Utility

        public bool TryGetDevice(int playerIndex, out InputDevice device)
            => _playerDevices.TryGetValue(playerIndex, out device);

        public bool TryGetPlayerIndex(InputDevice device, out int playerIndex)
            => _deviceToPlayer.TryGetValue(device, out playerIndex);

        #endregion
    }
}
