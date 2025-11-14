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
            Logs.Log($"[PlayerInputBridge] Player joined: {playerInput.playerIndex}");
            
            var deviceService = ServiceManager.Instance.Get<DeviceService>();
            deviceService?.RegisterPlayerInput(playerInput);
        }

        private void OnPlayerLeft(PlayerInput playerInput)
        {
            Logs.Log($"[PlayerInputBridge] Player left: {playerInput.playerIndex}");
            var deviceService = ServiceManager.Instance.Get<DeviceService>();
            deviceService?.UnregisterPlayerInput(playerInput);
        }

        // TODO Make join manual or smth 
        /*public void OnSubmit(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;
            
            if (PlayerInputManager.playerCount < PlayerInputManager.maxPlayerCount)
            {
                PlayerInputManager.JoinPlayerFromActionIfNotAlreadyJoined(ctx);
                Logs.Log("[PlayerInputBridge] JoinPlayer called.");
            }
        }*/
    }
}