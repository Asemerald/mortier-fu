using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class PlayerManager : MonoBehaviour
    {
        [Header("Setup")] [Tooltip("Prefab du personnage à instancier en jeu.")]
        public GameObject playerInGamePrefab;

        public PlayerTeam Team { get; private set; }
        public PlayerMetrics Metrics;
        private PlayerInput _playerInput;
        private GameObject _inGameCharacter;

        public PlayerInput PlayerInput => _playerInput;
        
        private PlayerRuntimeController _runtimeController;

        private GamePauseSystem _gamePauseSystem;

        private PlayerInputRouter _inputRouter;
        private readonly PlayerCustomizationData _customization = new();
        
        public GameObject CharacterGO => RuntimeController.CharacterGO;
        public PlayerCharacter Character => RuntimeController.Character;
        public bool IsInGame => RuntimeController.IsInGame;
        
        private PlayerInputRouter InputRouter
        {
            get
            {
                _inputRouter ??= new PlayerInputRouter(
                    PlayerInput,
                    TogglePause,
                    CancelUI
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

        public PlayerControlContext ControlContext => InputRouter.ControlContext;
        public PlayerActionPermissions CurrentPermissions => InputRouter.CurrentPermissions;
        
        public int PlayerIndex => _playerInput.playerIndex;

        public bool IsReady { get; private set; }

        public PlayerCustomizationData Customization => _customization;

        public int SkinIndex => _customization.SkinIndex;
        public int FaceColumn => _customization.FaceColumn;
        public int FaceRow => _customization.FaceRow;
        
        public event System.Action<PlayerManager> OnPlayerInitialized;
        public event System.Action<PlayerManager> OnPlayerDestroyed;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            DontDestroyOnLoad(gameObject);

            _playerInput.SwitchCurrentActionMap("UI");

            if (PlayerIndex != 0 || MenuManager.Instance == null) return;

            Logs.Log("[PlayerManager] Assigning Player 1 Input Action");
            MenuManager.Instance.SetPlayer1InputAction(_playerInput);
        }

        private bool TryResolveGamePauseSystem()
        {
            if (_gamePauseSystem != null)
                return true;

            if (SystemManager.Instance == null)
            {
                Logs.LogWarning("[PlayerManager] Cannot resolve GamePauseSystem because SystemManager is not available.");
                return false;
            }

            _gamePauseSystem = SystemManager.Instance.Get<GamePauseSystem>();

            if (_gamePauseSystem != null)
                return true;

            Logs.LogWarning("[PlayerManager] GamePauseSystem is not available.");
            return false;
        }

        // TODO: Faire un input manager plus tard
        private void TogglePause(InputAction.CallbackContext ctx)
        {
            if (PlayerIndex != 0)
                return;

            if (!CurrentPermissions.CanPause)
                return;

            if (!TryResolveGamePauseSystem())
                return;

            _gamePauseSystem.TogglePause();
        }

        public void SetControlContext(PlayerControlContext context)
        {
            InputRouter.SetContext(context, Character);
        }

        private void BindGameplayInputCallbacks()
        {
            InputRouter.BindGameplayInputCallbacks();
        }
        
        private void UnbindGameplayInputCallbacks()
        {
            _inputRouter?.UnbindGameplayInputCallbacks();
        }

        private void CancelUI(InputAction.CallbackContext ctx)
        {
            if (PlayerIndex != 0)
                return;

            if (!CurrentPermissions.CanCancelUI)
                return;

            if (!TryResolveGamePauseSystem())
                return;

            if (!_gamePauseSystem.IsPaused)
                return;

            _gamePauseSystem.Cancel();
        }

        /// <summary>
        /// Instancie ou repositionne le personnage du joueur dans la scène de jeu.
        /// </summary>
        public void SpawnInGame(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            if (!RuntimeController.Spawn(spawnPosition, spawnRotation, out bool createdCharacter))
                return;

            InputRouter.ApplyCurrentContextTo(RuntimeController.Character);

            if (createdCharacter)
            {
                OnPlayerInitialized?.Invoke(this);
            }

            BindGameplayInputCallbacks();
        }

        /// <summary>
        /// Supprime le joueur de la scène de jeu.
        /// </summary>
        public void DespawnInGame()
        {
            UnbindGameplayInputCallbacks();
            RuntimeController.Despawn();
        }

        public void JoinTeam(PlayerTeam team) => Team = team;
        
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

        public void SelfDestroy()
        {
            Destroy(gameObject);
        }
    }
}