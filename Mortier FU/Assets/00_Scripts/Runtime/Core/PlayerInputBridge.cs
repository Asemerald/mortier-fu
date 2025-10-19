using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    [RequireComponent(typeof(PlayerInputManager))]
    public class PlayerInputBridge : MonoBehaviour
    {
        public PlayerInputManager PlayerInputManager;
        
        public static PlayerInputBridge Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Logs.LogWarning("[PlayerInputBridge] Instance already exists, destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            PlayerInputManager = GetComponent<PlayerInputManager>();
            PlayerInputManager.onPlayerJoined += OnPlayerJoined;
            PlayerInputManager.onPlayerLeft += OnPlayerLeft;
        }

        private void OnDestroy()
        {
            PlayerInputManager.onPlayerJoined -= OnPlayerJoined;
            PlayerInputManager.onPlayerLeft -= OnPlayerLeft;
        }

        private void OnPlayerJoined(PlayerInput playerInput)
        {
            Debug.Log($"[PlayerInputBridge] Player joined: {playerInput.playerIndex}");

            // Relais vers ton DeviceService
            var deviceService = ServiceManager.Instance.Get<DevicesService>();
            deviceService?.RegisterPlayerInput(playerInput);
        }

        private void OnPlayerLeft(PlayerInput playerInput)
        {
            Debug.Log($"[PlayerInputBridge] Player left: {playerInput.playerIndex}");

            var deviceService = ServiceManager.Instance.Get<DevicesService>();
            deviceService?.UnregisterPlayerInput(playerInput);
        }
    }
}