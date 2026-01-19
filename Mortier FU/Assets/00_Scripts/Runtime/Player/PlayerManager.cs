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
        private InputAction _pauseAction;
        private InputAction _unPauseAction;
        private InputAction _cancelUIAction;

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

        public event System.Action<PlayerManager> OnPlayerInitialized;
        public event System.Action<PlayerManager> OnPlayerDestroyed;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            ServiceManager.Instance.Get<LobbyService>().RegisterPlayer(this);
            OnPlayerInitialized?.Invoke(this);
        }

        private void OnDestroy()
        {
            OnPlayerDestroyed?.Invoke(this);

            if (_pauseAction != null)
                _pauseAction.performed -= Pause;

            if (_unPauseAction != null)
                _unPauseAction.performed -= UnPause;

            if (_cancelUIAction != null)
                _cancelUIAction.performed -= CancelUI;
        }

        // TODO: Faire un input manager plus tard
        private void Pause(InputAction.CallbackContext ctx)
        {
            if (PlayerIndex != 0)
                return;

            PlayerInput.SwitchCurrentActionMap("UI");
            _gamePauseSystem.Pause();
        }

        private void UnPause(InputAction.CallbackContext ctx)
        {
            if (PlayerIndex != 0)
                return;

            PlayerInput.SwitchCurrentActionMap("Gameplay");
            _gamePauseSystem.UnPause();
        }

        private void CancelUI(InputAction.CallbackContext ctx)
        {
            if (PlayerIndex != 0)
                return;

            if (!_gamePauseSystem.IsPaused) return;

            _gamePauseSystem.Cancel();
        }

        /// <summary>
        /// Instancie le personnage du joueur dans la scène de jeu.
        /// </summary>
        public void SpawnInGame(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            // if (_isInGame)
            //     return;
            if (_inGameCharacter == null && playerInGamePrefab != null)
            {
                _inGameCharacter = Instantiate(playerInGamePrefab, spawnPosition, spawnRotation);
                Character.Initialize(this);

                _isInGame = true;
            }

            if (_inGameCharacter != null)
            {
                _inGameCharacter.transform.SetPositionAndRotation(
                    spawnPosition,
                    spawnRotation
                );
                _inGameCharacter.SetActive(true);

                _isInGame = true;
            }
            else
            {
                Logs.LogError($"[PlayerManager] No in-game prefab assigned for Player {PlayerIndex}");
            }

            _gamePauseSystem = SystemManager.Instance.Get<GamePauseSystem>();

            _pauseAction = PlayerInput.actions.FindAction("Pause");
            _unPauseAction = PlayerInput.actions.FindAction("UnPause");
            _cancelUIAction = PlayerInput.actions.FindAction("Cancel");

            if (_pauseAction != null) _pauseAction.performed += Pause;
            if (_unPauseAction != null) _unPauseAction.performed += UnPause;
            if (_cancelUIAction != null) _cancelUIAction.performed += CancelUI;
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
    }
}