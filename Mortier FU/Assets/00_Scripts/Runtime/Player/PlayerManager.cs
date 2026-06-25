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

        public bool IsReady { get; private set; }

        public PlayerCustomizationData Customization => _customization;

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

                _inputRouter ??= new PlayerInputRouter(
                    _playerInput,
                    TogglePause,
                    NavigateUI,
                    SubmitUI,
                    CancelUI,
                    Interact
                );

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

        private PlayerUIInputService UIInputService =>
            ServiceManager.Instance?.Get<PlayerUIInputService>();

        private PlayerInteractionService InteractionService =>
            ServiceManager.Instance?.Get<PlayerInteractionService>();

        private void Awake()
        {
            ResolvePlayerInput();

            if (!_playerInput)
            {
                Logs.LogError("[PlayerManager] PlayerInput component is missing.", this);
                enabled = false;
                return;
            }

            DontDestroyOnLoad(gameObject);

            SetControlContext(PlayerControlContext.Menu);
            InputRouter?.BindInputCallbacks();

            if (PlayerIndex != 0 || !MenuManager.Instance)
                return;

            Logs.Log("[PlayerManager] Assigning Player 1 to MenuManager.");
            MenuManager.Instance.SetPlayer1(this);
        }

        private void OnDestroy()
        {
            OnPlayerDestroyed?.Invoke(this);

            _inputRouter?.Dispose();
            _inputRouter = null;

            _runtimeController?.DestroyRuntime();
            _runtimeController = null;

            OnPlayerInitialized = null;
            OnPlayerDestroyed = null;
        }

        private void ResolvePlayerInput()
        {
            if (_playerInput)
                return;

            _playerInput = GetComponent<PlayerInput>();
        }

        private bool TryResolveGamePauseSystem()
        {
            if (SystemManager.Instance == null)
            {
                _gamePauseSystem = null;
                Logs.LogWarning("[PlayerManager] Cannot resolve GamePauseSystem because SystemManager is not available.");
                return false;
            }

            var currentPauseSystem = SystemManager.Instance.Get<GamePauseSystem>();

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

        public void SetControlContext(PlayerControlContext context)
        {
            InputRouter?.SetContext(context, Character);
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
            RuntimeController.Despawn();
        }

        public void JoinTeam(PlayerTeam team)
        {
            Team = team;
        }

        public void SelfDestroy()
        {
            Destroy(gameObject);
        }

        private void TogglePause(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed)
                return;

            if (PlayerIndex != 0)
                return;

            if (!CurrentPermissions.CanPause)
                return;

            if (!TryResolveGamePauseSystem())
                return;

            _gamePauseSystem.TogglePause();
        }

        private void Interact(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed)
                return;

            if (!CurrentPermissions.CanInteract)
                return;

            InteractionService?.TryInteract(this);
        }

        private void NavigateUI(InputAction.CallbackContext ctx)
        {
            if (!CurrentPermissions.CanNavigateUI)
                return;

            Vector2 direction = ctx.ReadValue<Vector2>();

            UIInputService?.TryNavigate(this, direction);
        }

        private void SubmitUI(InputAction.CallbackContext ctx)
        {
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