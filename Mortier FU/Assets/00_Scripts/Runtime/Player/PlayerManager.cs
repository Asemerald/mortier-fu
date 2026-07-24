using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerManager : MonoBehaviour
    {
        [Header("Setup")]
        [Tooltip("Prefab du personnage à instancier en jeu.")]
        public GameObject playerInGamePrefab;

        public PlayerTeam Team { get; private set; }
        public PlayerMetrics Metrics;

        private PlayerInput _playerInput;
        private PlayerRuntimeController _runtimeController;
        private GamePauseSystem _gamePauseSystem;
        private PlayerInputRouter _inputRouter;

        private IPlayerPawn _activePawn;

        private readonly PlayerCustomizationData _customization = new();

        public PlayerInput PlayerInput
        {
            get
            {
                ResolvePlayerInput();
                return _playerInput;
            }
        }

        public GameObject CharacterGO => RuntimeController.CharacterGO;
        public PlayerCharacter Character => RuntimeController.Character;
        public bool IsInGame => RuntimeController.IsInGame;
        public IPlayerPawn ActivePawn => _activePawn;
        public bool IsControllingPawn => _activePawn is { IsPawnActive: true };
        public bool IsControllingGhost => _activePawn is PlayerGhostPawn { IsPawnActive: true };

        public PlayerControlContext ControlContext => InputRouter.ControlContext;
        public PlayerActionPermissions CurrentPermissions => InputRouter.CurrentPermissions;

        public int PlayerIndex
        {
            get
            {
                ResolvePlayerInput();
                return _playerInput ? _playerInput.playerIndex : -1;
            }
        }

        public PlayerCustomizationData Customization => _customization;
        
        private bool _unityEventSystemUIActive;

        public int SkinIndex => _customization.SkinIndex;
        public int FaceColumn => _customization.FaceColumn;
        public int FaceRow => _customization.FaceRow;

        public event System.Action<PlayerManager> OnPlayerInitialized;
        public event System.Action<PlayerManager> OnPlayerDestroyed;

        private PlayerInputRouter InputRouter
        {
            get
            {
                ResolvePlayerInput();

                if (!_playerInput)
                {
                    Logs.LogError("[PlayerManager] Cannot create PlayerInputRouter because PlayerInput is missing.", this);
                    return null;
                }

                _inputRouter ??= new PlayerInputRouter(_playerInput, TogglePause, NavigateUI, SubmitUI, CancelUI);

                return _inputRouter;
            }
        }

        private PlayerRuntimeController RuntimeController
        {
            get
            {
                _runtimeController ??= new PlayerRuntimeController(
                    this,
                    playerInGamePrefab
                );

                return _runtimeController;
            }
        }

        private PlayerUIInputService UIInputService => ServiceManager.Instance?.Get<PlayerUIInputService>();

        private void Awake()
        {
            ResolvePlayerInput();

            if (!_playerInput)
            {
                Logs.LogError("[PlayerManager] PlayerInput component is missing.");
                enabled = false;
                return;
            }

            DontDestroyOnLoad(gameObject);

            bool inputRouterAlreadyCreated = _inputRouter is not null;

            PlayerInputRouter inputRouter = InputRouter;

            if (!inputRouterAlreadyCreated)
            {
                inputRouter.SetContext(PlayerControlContext.Lobby, Character);
            }

            inputRouter.BindInputCallbacks();
        }

        private void OnDestroy()
        {
            OnPlayerDestroyed?.Invoke(this);

            ClearActivePawn();

            _inputRouter?.Dispose();
            _inputRouter = null;

            _runtimeController?.DestroyRuntime();
            _runtimeController = null;

            OnPlayerInitialized = null;
            OnPlayerDestroyed = null;
        }
        
        public void SetUnityEventSystemUIActive(bool active)
        {
            _unityEventSystemUIActive = active;
        }

        private void ResolvePlayerInput()
        {
            if (_playerInput)
                return;

            _playerInput = GetComponent<PlayerInput>();
        }
        
        public bool IsKeyboardAndMouseControlScheme()
        {
            return _playerInput != null
                   && string.Equals(
                       _playerInput.currentControlScheme,
                       "Keyboard and Mouse",
                       System.StringComparison.Ordinal);
        }

        private bool TryResolveGamePauseSystem()
        {
            if (SystemManager.Instance == null)
            {
                _gamePauseSystem = null;
                Logs.LogWarning("[PlayerManager] Cannot resolve GamePauseSystem because SystemManager is not available.");
                return false;
            }

            GamePauseSystem currentPauseSystem = SystemManager.Instance.Get<GamePauseSystem>();

            if (currentPauseSystem == null)
            {
                _gamePauseSystem = null;
                Logs.LogWarning("[PlayerManager] GamePauseSystem is not available.");
                return false;
            }

            if (ReferenceEquals(_gamePauseSystem, currentPauseSystem))
                return true;

            _gamePauseSystem = currentPauseSystem;
            Logs.Log($"[PlayerManager] Refreshed GamePauseSystem reference for Player {PlayerIndex + 1}.");

            return true;
        }

        public void SetControlContext(PlayerControlContext context) => InputRouter?.SetContext(context, Character);

        public void SetActivePawn(IPlayerPawn pawn)
        {
            if (ReferenceEquals(_activePawn, pawn))
                return;

            _activePawn?.ExitPawn();

            _activePawn = pawn;

            _activePawn?.EnterPawn();
        }

        public void ClearActivePawn(IPlayerPawn expectedPawn = null)
        {
            if (expectedPawn != null && !ReferenceEquals(_activePawn, expectedPawn))
                return;

            _activePawn?.ExitPawn();
            _activePawn = null;
        }

        public void SpawnInGame(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            if (!RuntimeController.Spawn(spawnPosition, spawnRotation, out bool createdCharacter))
                return;

            InputRouter?.ApplyCurrentContextTo(RuntimeController.Character);

            if (createdCharacter)
                OnPlayerInitialized?.Invoke(this);
        }

        public void DespawnInGame()
        {
            ClearActivePawn();
            RuntimeController.Despawn();
        }

        public void JoinTeam(PlayerTeam team) => Team = team;

        public void SelfDestroy() => Destroy(gameObject);

        private void TogglePause(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed)
                return;

            // if (PlayerIndex != 0)
            //     return;

            if (!CurrentPermissions.CanPause)
                return;

            if (!TryResolveGamePauseSystem())
                return;

            _gamePauseSystem.TogglePause(this);
        }

        private void NavigateUI(InputAction.CallbackContext ctx)
        {
            if (_unityEventSystemUIActive)
                return;

            if (!CurrentPermissions.CanNavigateUI)
                return;

            Vector2 direction = ctx.ReadValue<Vector2>();

            UIInputService?.TryNavigate(this, direction);
        }

        private void SubmitUI(InputAction.CallbackContext ctx)
        {
            if (_unityEventSystemUIActive)
                return;

            if (!ctx.performed)
                return;

            if (!CurrentPermissions.CanConfirmUI)
                return;

            UIInputService?.TrySubmit(this);
        }

        private void CancelUI(InputAction.CallbackContext ctx)
        {

            if (!ctx.performed)
                return;

            if (!CurrentPermissions.CanCancelUI)
                return;

            if (UIInputService != null && UIInputService.TryCancel(this))
                return;

            if (PlayerIndex != 0)
                return;

            if (!TryResolveGamePauseSystem())
                return;

            if (!_gamePauseSystem.IsPaused)
                return;

            _gamePauseSystem.Cancel();
        }
    }
}