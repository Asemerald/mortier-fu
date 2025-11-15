using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
            
            #if UNITY_EDITOR
            if (EditorPrefs.GetBool("SkipMenuEnabled", false)) {
                foreach (Gamepad gamepad in Gamepad.all) {
                    PlayerInputManager.JoinPlayer(pairWithDevice: gamepad);
                    Logs.Log("[PlayerInputBridge]: Auto-connecting device with ID: " + gamepad.deviceId);
                }
            }
            #endif
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
    }
}