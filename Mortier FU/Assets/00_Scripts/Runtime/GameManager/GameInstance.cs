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
        public PlayerInputManager _playerInputManager;

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

            _playerInputManager.onPlayerJoined += OnPlayerJoined;
            _playerInputManager.onPlayerLeft += OnPlayerLeft;

            InputSystem.onDeviceChange += OnDeviceChange;
        }

        private void OnDestroy()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            _playerInputManager.onPlayerJoined -= OnPlayerJoined;
            _playerInputManager.onPlayerLeft -= OnPlayerLeft;
        }

        private void OnPlayerJoined(PlayerInput playerInput)
        {
            if (LobbyManager.Instance._gameStarted) return;
            
            var deviceMain = playerInput.devices.Count > 0 ? playerInput.devices[0] : null;
            var index = playerInput.playerIndex;

            Logs.Log($"Player {index} joined with {(deviceMain != null ? deviceMain.displayName : "Unknown Device")}");
            DevicesService.RegisterPlayerDevice(index, deviceMain);
        }

        private void OnPlayerLeft(PlayerInput playerInput)
        {
            if (LobbyManager.Instance._gameStarted) return;
            
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