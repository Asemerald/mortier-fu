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
        private bool _isInGame = false;

        public PlayerInput PlayerInput => _playerInput;
        public GameObject CharacterGO => _inGameCharacter;

        private PlayerCharacter _playerCharacter;

        private GamePauseSystem _gamePauseSystem;

        private PlayerInputRouter _inputRouter;
        private readonly PlayerCustomizationData _customization = new();

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

        public PlayerControlContext ControlContext => InputRouter.ControlContext;
        public PlayerActionPermissions CurrentPermissions => InputRouter.CurrentPermissions;

        public PlayerCharacter Character
        {
            get
            {
                _playerCharacter ??= _inGameCharacter != null ? _inGameCharacter.GetComponent<PlayerCharacter>() : null;
                return _playerCharacter;
            }
        }
        
        public int PlayerIndex => _playerInput.playerIndex;
        public bool IsInGame => _isInGame;

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

        private void OnDestroy()
        {
            OnPlayerDestroyed?.Invoke(this);

            _inputRouter?.Dispose();
            _inputRouter = null;
            
            OnPlayerInitialized = null;
            OnPlayerDestroyed = null;
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

        public void EnableGameplayInputMap(bool enable = true)
        {
            SetControlContext(enable
                ? PlayerControlContext.RoundGameplay
                : PlayerControlContext.Scoreboard);
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
            if (_inGameCharacter == null)
            {
                if (playerInGamePrefab == null)
                {
                    Logs.LogError($"[PlayerManager] No in-game prefab assigned for Player {PlayerIndex}.");
                    return;
                }

                _inGameCharacter = Instantiate(playerInGamePrefab, spawnPosition, spawnRotation);
                _playerCharacter = _inGameCharacter.GetComponent<PlayerCharacter>();

                if (_playerCharacter == null)
                {
                    Logs.LogError(
                        $"[PlayerManager] In-game prefab for Player {PlayerIndex} does not contain a PlayerCharacter component.");
                    Destroy(_inGameCharacter);
                    _inGameCharacter = null;
                    return;
                }

                _playerCharacter.Initialize(this);
                InputRouter.ApplyCurrentContextTo(_playerCharacter);

                OnPlayerInitialized?.Invoke(this);
            }

            _inGameCharacter.transform.SetPositionAndRotation(
                spawnPosition,
                spawnRotation
            );

            _inGameCharacter.SetActive(true);
            InputRouter.ApplyCurrentContextTo(_playerCharacter);

            _isInGame = true;

            BindGameplayInputCallbacks();
        }

        /// <summary>
        /// Supprime le joueur de la scène de jeu.
        /// </summary>
        public void DespawnInGame()
        {
            if (_inGameCharacter != null)
            {
                _inGameCharacter.SetActive(false);
            }

            _isInGame = false;
        }

        public void JoinTeam(PlayerTeam team) => Team = team;

        public void LeaveTeam()
        {
            if (Team == null)
            {
                Logs.LogError("Trying to remove a player from his team although he is not part of any team !");
                return;
            }

            if (!Team.Members.Remove(this))
                Logs.LogWarning("Trying to remove a player from a team where he isn't part of !");

            Team = null;
        }

        public void SelfDestroy()
        {
            Destroy(gameObject);
        }
    }
}