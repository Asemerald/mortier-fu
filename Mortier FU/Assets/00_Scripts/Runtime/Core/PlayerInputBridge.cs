using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    [RequireComponent(typeof(PlayerInputManager))]
    public class PlayerInputBridge : MonoBehaviour
    {
        public PlayerInputManager PlayerInputManager { get; private set; }
        private void Awake()
        {
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
            var deviceService = ServiceManager.Instance.Get<LobbyService>();
            deviceService?.RegisterPlayerInput(playerInput);
        }

        private void OnPlayerLeft(PlayerInput playerInput)
        {
            Debug.Log($"[PlayerInputBridge] Player left: {playerInput.playerIndex}");

            var deviceService = ServiceManager.Instance.Get<LobbyService>();
            deviceService?.UnregisterPlayerInput(playerInput);
        }
    }
}