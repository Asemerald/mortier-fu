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

        public bool IsReady = false;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            DontDestroyOnLoad(gameObject);
            
            // lobby type shit
            _lobbyPlayer = LobbyMenu3D.Instance.playerPrefabs[PlayerIndex].GetComponent<LobbyPlayer>();
            
            _navigateAction = _playerInput.actions.FindAction("Navigate");
            _submitAction = _playerInput.actions.FindAction("Submit");
            
            if (_navigateAction != null)
                _navigateAction.performed += Navigate;
            if (_submitAction != null)
                _submitAction.performed += Submit;
            
            _playerInput.SwitchCurrentActionMap("UI");
            
            if (PlayerIndex == 0)
            {
                Logs.Log("[PlayerManager] Assigning Player 1 Input Action");
                MenuManager.Instance.Player1InputAction = _playerInput;
            }
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
                _pauseAction.performed -= TogglePause;
            
            if (_unPauseAction != null)
                _unPauseAction.performed -= TogglePause;


            if (_cancelUIAction != null)
                _cancelUIAction.performed -= CancelUI;
        }

        // TODO: Faire un input manager plus tard
        private void TogglePause(InputAction.CallbackContext ctx)
        {
            if (PlayerIndex != 0)
                return;
            _gamePauseSystem.TogglePause();
            RefreshActionMap();
        }
        
        public void RefreshActionMap()
        {
            if (_gamePauseSystem == null) return;

            string targetMap = (!PlayerCharacter.AllowGameplayActions || _gamePauseSystem.IsPaused)
                ? "UI"
                : "Gameplay";

            PlayerInput.SwitchCurrentActionMap(targetMap);
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

            if (_pauseAction != null) _pauseAction.performed += TogglePause;
            if (_unPauseAction != null) _unPauseAction.performed += TogglePause;
            if (_cancelUIAction != null) _cancelUIAction.performed += CancelUI;
            
            _playerInput.SwitchCurrentActionMap("Gameplay");
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
        
        #region Lobby Methods
        
        private InputAction _navigateAction;
        private InputAction _submitAction;
        
        private LobbyPlayer _lobbyPlayer;
        
       

        public int SkinIndex = 0;
        public int FaceColumn = 1;
        public int FaceRow = 1;
        
        private Vector2 _previousNavigateInput = Vector2.zero;
        private float _lastNavigateTime = 0f;
        private float _navigateCooldown = 0.3f;
        private const float _threshold = 0.7f;

        private void Navigate(InputAction.CallbackContext ctx)
        {
            Debug.Log($"[PlayerManager] Player {PlayerIndex} Navigate called! Value: {ctx.ReadValue<Vector2>()}");
            
            if (_lobbyPlayer == null) return;

            Vector2 currentInput = ctx.ReadValue<Vector2>();

            // Vérifier si on vient de passer le seuil sur l'axe X (horizontal)
            bool wasNotPushedX = Mathf.Abs(_previousNavigateInput.x) < _threshold;
            bool isPushedNowX = Mathf.Abs(currentInput.x) >= _threshold;
    
            // Vérifier si on vient de passer le seuil sur l'axe Y (vertical)
            bool wasNotPushedY = Mathf.Abs(_previousNavigateInput.y) < _threshold;
            bool isPushedNowY = Mathf.Abs(currentInput.y) >= _threshold;

            // Vérifier le cooldown
            bool cooldownExpired = Time.time - _lastNavigateTime >= _navigateCooldown;

            // Changer seulement si :
            // - On vient de pousser le stick sur X OU Y
            // - OU le stick est poussé ET le cooldown est écoulé
            bool shouldTrigger = ((wasNotPushedX && isPushedNowX) || (wasNotPushedY && isPushedNowY)) 
                                 || ((isPushedNowX || isPushedNowY) && cooldownExpired);
    
            if (shouldTrigger)
            {
                _lobbyPlayer.ChangeSkin(currentInput);
                _lastNavigateTime = Time.time;
            }

            _previousNavigateInput = currentInput;
        }
        
        
        public void Submit(InputAction.CallbackContext context)
        {
            if (_lobbyPlayer != null && context.performed)
            {
                _lobbyPlayer.ToggleReady();
                IsReady = _lobbyPlayer.IsReady;
            
                // Sauvegarder les valeurs de customisation
                SkinIndex = _lobbyPlayer.SkinIndex;
                FaceColumn = _lobbyPlayer.FaceColumn;
                FaceRow = _lobbyPlayer.FaceRow;
            }
        }
        
        #endregion
    }
}