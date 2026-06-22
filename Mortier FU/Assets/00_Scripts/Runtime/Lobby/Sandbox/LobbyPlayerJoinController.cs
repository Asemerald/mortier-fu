using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbyPlayerJoinController : MonoBehaviour
    {
        [Header("Join")]
        [SerializeField] private bool _enableJoiningOnStart = true;

        [Tooltip("Auto-spawn one PlayerInput per already connected gamepad when entering the lobby.")]
        [SerializeField] private bool _autoJoinConnectedGamepads = true;

        [Header("Debug")]
        [SerializeField] private bool _debugInputState = true;

        private PlayerInputBridge _bridge;

        private void Start()
        {
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            _bridge = await WaitForPlayerInputBridgeAsync();

            if (_bridge == null)
            {
                Logs.LogError("[LobbyPlayerJoinController] PlayerInputBridge could not be resolved.");
                return;
            }

            if (_enableJoiningOnStart)
            {
                _bridge.CanJoin(true);
            }

            if (_debugInputState)
            {
                _bridge.DebugLogInputState();
            }

            if (_autoJoinConnectedGamepads)
            {
                _bridge.JoinAllUnpairedGamepads();
            }
        }

        private async UniTask<PlayerInputBridge> WaitForPlayerInputBridgeAsync()
        {
            const int maxFramesToWait = 120;

            for (int i = 0; i < maxFramesToWait; i++)
            {
                var bridge = ResolvePlayerInputBridge();

                if (bridge != null)
                    return bridge;

                await UniTask.Yield();
            }

            return null;
        }

        private PlayerInputBridge ResolvePlayerInputBridge()
        {
            if (PlayerInputBridge.Instance != null)
                return PlayerInputBridge.Instance;

            return FindFirstObjectByType<PlayerInputBridge>(FindObjectsInactive.Include);
        }

        private void OnDestroy()
        {
            if (_bridge == null)
                return;

            _bridge.CanJoin(false);
        }
    }
}