using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    [DefaultExecutionOrder(-100)] // Pour s'assurer qu'il s'initialise avant le reste
    public class GameInstance : MonoBehaviour
    {
        public static GameInstance Instance { get; private set; }

        [Header("Input")]
        public PlayerInputManager playerInputManager;

        public DevicesService DevicesService { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            DevicesService = new DevicesService(this);

            playerInputManager.onPlayerJoined += OnPlayerJoined;
            playerInputManager.onPlayerLeft += OnPlayerLeft;

            InputSystem.onDeviceChange += OnDeviceChange;
        }

        private void OnDestroy()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            playerInputManager.onPlayerJoined -= OnPlayerJoined;
            playerInputManager.onPlayerLeft -= OnPlayerLeft;
        }

        private void OnPlayerJoined(PlayerInput playerInput)
        {
            var device = playerInput.devices[0];
            var index = playerInput.playerIndex;

            Logs.Log($"Player {index} joined with {device.displayName}");
            DevicesService.RegisterPlayerDevice(index, device);
        }

        private void OnPlayerLeft(PlayerInput playerInput)
        {
            var index = playerInput.playerIndex;
            Logs.Warning($"Player {index} left");
            DevicesService.UnregisterPlayer(index);
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            DevicesService.HandleDeviceChange(device, change);
        }
    }
}