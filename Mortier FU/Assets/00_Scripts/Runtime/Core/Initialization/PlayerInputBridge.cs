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
            if (EditorPrefs.GetBool("SkipMenuEnabled", false))
            {
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

            Debug.Log($"[PlayerInputBridge] Found PlayerInput component: {playerInput != null}");

            var playerManager = playerInput.GetComponent<PlayerManager>();
            Debug.Log($"[PlayerInputBridge] Found PlayerManager component: {playerManager != null}");

            ServiceManager.Instance.Get<LobbyService>()?.RegisterPlayer(playerManager);
        }

        private void OnPlayerLeft(PlayerInput playerInput)
        {
            Logs.Log($"[PlayerInputBridge] Player left: {playerInput.playerIndex}");

            var deviceService = ServiceManager.Instance.Get<DeviceService>();
            deviceService?.UnregisterPlayerInput(playerInput);

            var playerManager = playerInput.GetComponent<PlayerManager>();
            ServiceManager.Instance.Get<LobbyService>()?.UnregisterPlayer(playerManager);
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

        public void JoinAllUnpairedGamepads()
        {
            if (PlayerInputManager == null)
            {
                Logs.LogError("[PlayerInputBridge] Cannot auto-join because PlayerInputManager is null.");
                return;
            }

            DebugLogInputState();

            Logs.Log("[PlayerInputBridge] Trying to auto-join all unpaired gamepads...");

            foreach (var gamepad in Gamepad.all)
            {
                if (IsDeviceAlreadyPaired(gamepad))
                {
                    Logs.Log($"[PlayerInputBridge] Skipping already paired gamepad: {gamepad.displayName}");
                    continue;
                }

                Logs.Log($"[PlayerInputBridge] Auto-joining gamepad: {gamepad.displayName}");

                try
                {
                    var playerInput = PlayerInputManager.JoinPlayer(
                        playerIndex: -1,
                        splitScreenIndex: -1,
                        controlScheme: null,
                        pairWithDevice: gamepad
                    );

                    if (playerInput == null)
                    {
                        Logs.LogWarning($"[PlayerInputBridge] JoinPlayer returned null for {gamepad.displayName}.");
                    }
                    else
                    {
                        Logs.Log($"[PlayerInputBridge] JoinPlayer created PlayerInput index {playerInput.playerIndex}.");
                    }
                }
                catch (System.Exception e)
                {
                    Logs.LogError($"[PlayerInputBridge] Failed to join gamepad {gamepad.displayName}: {e}");
                }
            }

            DebugLogInputState();
        }

        private bool IsDeviceAlreadyPaired(InputDevice device)
        {
            if (device == null)
                return true;

            var lobbyService = ServiceManager.Instance.Get<LobbyService>();

            if (lobbyService == null)
                return false;

            var players = lobbyService.GetPlayers();

            foreach (var player in players)
            {
                if (player == null || player.PlayerInput == null)
                    continue;

                foreach (var pairedDevice in player.PlayerInput.devices)
                {
                    if (pairedDevice == device)
                        return true;
                }
            }

            return false;
        }
        
        public void DebugLogInputState()
        {
            Logs.Log($"[PlayerInputBridge] Gamepads detected: {Gamepad.all.Count}");
            Logs.Log($"[PlayerInputBridge] PlayerInputs detected: {PlayerInput.all.Count}");

            for (int i = 0; i < Gamepad.all.Count; i++)
            {
                var gamepad = Gamepad.all[i];
                Logs.Log($"[PlayerInputBridge] Gamepad {i}: {gamepad.displayName} / {gamepad.deviceId}");
            }

            foreach (var playerInput in PlayerInput.all)
            {
                if (playerInput == null)
                    continue;

                string devices = "";

                foreach (var device in playerInput.devices)
                {
                    devices += $"{device.displayName}({device.deviceId}) ";
                }

                Logs.Log(
                    $"[PlayerInputBridge] PlayerInput index={playerInput.playerIndex}, " +
                    $"scheme={playerInput.currentControlScheme}, devices=[{devices}]"
                );
            }
        }
    }
}