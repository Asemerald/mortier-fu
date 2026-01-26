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
        public static PlayerInputBridge Instance { get; private set; }
        public PlayerInputManager PlayerInputManager { get; private set; }
        

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Logs.LogError("[PlayerInputBridge] Multiple instances detected! Destroying duplicate.", this);
                Destroy(this.gameObject);
                return;
            }

            Instance = this;

            PlayerInputManager = GetComponent<PlayerInputManager>();
            PlayerInputManager.onPlayerJoined += OnPlayerJoined;
            PlayerInputManager.onPlayerLeft += OnPlayerLeft;
        }

#if UNITY_EDITOR 
        private void Start()
        {
            if (EditorPrefs.GetBool("SkipMenuEnabled", false)) {
                // Join a Player for each gamepad connected (for testing in editor)
                var gamepads = Gamepad.all;
                foreach (var gamepad in gamepads)
                {
                    PlayerInputManager.JoinPlayer(-1, -1, null, gamepad);
                }
            }
        }
#endif

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

        public void CanJoin(bool canJoin)
        {
            switch (canJoin)
            {
                case true:
                    PlayerInputManager.EnableJoining();
                    Logs.Log("[PlayerInputBridge] Players can now join.");
                    break;
                case false:
                    PlayerInputManager.DisableJoining();
                    Logs.Log("[PlayerInputBridge] Players can no longer join.");
                    break;
            }
        }
    }
}

