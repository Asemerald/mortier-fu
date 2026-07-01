using System;
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
        [Header("Debug"), SerializeField] private bool _enableDebug = true;
        
        public static PlayerInputBridge Instance { get; private set; }

        private PlayerInputManager _playerInputManager;

        // Cette variable est utile surtout pour le PortableBootstrapTool, 
        public event Action<PlayerManager> OnPlayerManagerJoined;
        
        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Logs.LogError("[PlayerInputBridge] Multiple instances detected! Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _playerInputManager = GetComponent<PlayerInputManager>();

            _playerInputManager.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

            _playerInputManager.onPlayerJoined -= HandlePlayerJoined;
            _playerInputManager.onPlayerLeft -= HandlePlayerLeft;

            _playerInputManager.onPlayerJoined += HandlePlayerJoined;
            _playerInputManager.onPlayerLeft += HandlePlayerLeft;
        }

#if UNITY_EDITOR
        private void Start()
        {
            if (!EditorPrefs.GetBool("SkipMenuEnabled", false))
                return;

            var gamepads = Gamepad.all;

            for (int i = 0; i < gamepads.Count; i++)
            {
                _playerInputManager.JoinPlayer(-1, -1, null, gamepads[i]);
            }
        }
#endif

        private void OnDestroy()
        {
            if (_playerInputManager)
            {
                _playerInputManager.onPlayerJoined -= HandlePlayerJoined;
                _playerInputManager.onPlayerLeft -= HandlePlayerLeft;
            }

            if (Instance == this)
                Instance = null;
        }

        private void HandlePlayerJoined(PlayerInput playerInput)
        {
            if (!playerInput && _enableDebug)
            {
                Logs.LogError("[PlayerInputBridge] Player joined event received with null PlayerInput.", this);
                return;
            }

            if(_enableDebug) 
                Logs.Log($"[PlayerInputBridge] Player joined: {playerInput.playerIndex}");

            var playerManager = playerInput.GetComponent<PlayerManager>();

            if (!playerManager && _enableDebug)
            {
                Logs.LogError($"[PlayerInputBridge] PlayerInput {playerInput.playerIndex} has no PlayerManager on the same GameObject.", playerInput);
                return;
            }

            if (ServiceManager.Instance is null && _enableDebug)
            {
                Logs.LogError("[PlayerInputBridge] ServiceManager is missing. Cannot register joined player.", this);
                return;
            }

            var deviceService = ServiceManager.Instance?.Get<DeviceService>();
            deviceService?.RegisterPlayerInput(playerInput);

            var lobbyService = ServiceManager.Instance?.Get<LobbyService>();

            if (lobbyService is null && _enableDebug)
            {
                Logs.LogError("[PlayerInputBridge] LobbyService is missing. Cannot register joined player.", this);
                return;
            }

            lobbyService?.RegisterPlayer(playerManager);
            OnPlayerManagerJoined?.Invoke(playerManager);
        }

        private void HandlePlayerLeft(PlayerInput playerInput)
        {
            if (!playerInput)
                return;

            if(_enableDebug) 
                Logs.Log($"[PlayerInputBridge] Player left: {playerInput.playerIndex}");

            if (ServiceManager.Instance is null)
                return;

            DeviceService deviceService = ServiceManager.Instance.Get<DeviceService>();
            deviceService?.UnregisterPlayerInput(playerInput);

            PlayerManager playerManager = playerInput.GetComponent<PlayerManager>();

            if (!playerManager)
                return;

            ServiceManager.Instance.Get<LobbyService>()?.UnregisterPlayer(playerManager);
        }

        public void CanJoin(bool canJoin)
        {
            if (!_playerInputManager)
            {
                Logs.LogError("[PlayerInputBridge] Cannot change join state because PlayerInputManager is missing.", this);
                return;
            }

            if (canJoin)
            {
                _playerInputManager.EnableJoining();

                if (_enableDebug)
                    Logs.Log($"[PlayerInputBridge] Players can now join. Current PlayerInputs={PlayerInput.all.Count}, Max={_playerInputManager.maxPlayerCount}", this);

                return;
            }

            _playerInputManager.DisableJoining();

            if (_enableDebug)
                Logs.Log($"[PlayerInputBridge] Players can no longer join. Current PlayerInputs={PlayerInput.all.Count}, Max={_playerInputManager.maxPlayerCount}", this);
        }

        public void JoinAllUnpairedGamepads()
        {
            if (!_playerInputManager && _enableDebug)
            {
                Logs.LogError("[PlayerInputBridge] Cannot auto-join because PlayerInputManager is null.", this);
                return;
            }

            DebugLogInputState();

            if(_enableDebug) 
                Logs.Log("[PlayerInputBridge] Trying to auto-join all unpaired gamepads...");

            foreach (var gamepad in Gamepad.all)
            {
                if (IsDeviceAlreadyPaired(gamepad) && _enableDebug)
                {
                    Logs.Log($"[PlayerInputBridge] Skipping already paired gamepad: {gamepad.displayName}");
                    continue;
                }

                if(_enableDebug) 
                    Logs.Log($"[PlayerInputBridge] Auto-joining gamepad: {gamepad.displayName}");

                try
                {
                    PlayerInput playerInput = _playerInputManager.JoinPlayer(
                        playerIndex: -1,
                        splitScreenIndex: -1,
                        controlScheme: null,
                        pairWithDevice: gamepad
                    );

                    if (!playerInput && _enableDebug)
                    {
                        Logs.LogWarning($"[PlayerInputBridge] JoinPlayer returned null for {gamepad.displayName}.");
                    }
                    else if(_enableDebug)
                    {
                        Logs.Log($"[PlayerInputBridge] JoinPlayer created PlayerInput index {playerInput.playerIndex}.");
                    }
                }
                catch (Exception e)
                {
                    Logs.LogError($"[PlayerInputBridge] Failed to join gamepad {gamepad.displayName}: {e}");
                }
            }

            DebugLogInputState();
        }

        private bool IsDeviceAlreadyPaired(InputDevice device)
        {
            if (device is null)
                return true;

            foreach (PlayerInput playerInput in PlayerInput.all)
            {
                if (!playerInput)
                    continue;

                var devices = playerInput.devices;

                for (int i = 0; i < devices.Count; i++)
                {
                    if (devices[i] == device)
                        return true;
                }
            }

            return false;
        }
        
        public void ValidateMaxPlayers(int expectedMaxPlayers)
        {
            if (!_playerInputManager)
            {
                Logs.LogError("[PlayerInputBridge] Cannot validate max players because PlayerInputManager is missing.", this);
                return;
            }

            expectedMaxPlayers = Mathf.Max(1, expectedMaxPlayers);

            if (_playerInputManager.maxPlayerCount == expectedMaxPlayers)
                return;

            Logs.LogWarning($"[PlayerInputBridge] PlayerInputManager Max Player Count is {_playerInputManager.maxPlayerCount}, " + $"but lobby expects {expectedMaxPlayers}. Please fix it in the Inspector.", this);
        }

        public void DebugLogInputState()
        {
            if (_enableDebug)
            {
                Logs.Log($"[PlayerInputBridge] Gamepads detected: {Gamepad.all.Count}");
                Logs.Log($"[PlayerInputBridge] PlayerInputs detected: {PlayerInput.all.Count}");   
            }

            for (int i = 0; i < Gamepad.all.Count; i++)
            {
                Gamepad gamepad = Gamepad.all[i];
                
                if(_enableDebug) 
                    Logs.Log($"[PlayerInputBridge] Gamepad {i}: {gamepad.displayName} / {gamepad.deviceId} / added={gamepad.added}");
            }

            foreach (PlayerInput playerInput in PlayerInput.all)
            {
                if (!playerInput)
                    continue;

                string devices = "";

                foreach (InputDevice device in playerInput.devices)
                {
                    devices += $"{device.displayName}({device.deviceId}) ";
                }

                if (_enableDebug) 
                    Logs.Log($"[PlayerInputBridge] PlayerInput index={playerInput.playerIndex}, " + $"scheme={playerInput.currentControlScheme}, devices=[{devices}]");
            }
        }
    }
}