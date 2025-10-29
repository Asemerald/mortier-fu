using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class PlayerManager : MonoBehaviour
    {
        [Header("Setup")]
        [Tooltip("Prefab du personnage à instancier en jeu.")]
        public GameObject playerInGamePrefab;

        public PlayerMetrics Metrics;
        private PlayerInput _playerInput;
        private GameObject _inGameCharacter;
        private bool _isInGame = false;

        public PlayerInput PlayerInput => _playerInput;
        public GameObject CharacterGO => _inGameCharacter;

        private Character _character;

        public Character Character
        {
            get
            {
                _character ??= _inGameCharacter != null ? _inGameCharacter.GetComponent<Character>() : null;
                return _character;
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
        }

        /// <summary>
        /// Instancie le personnage du joueur dans la scène de jeu.
        /// </summary>
        public void SpawnInGame(Vector3 spawnPosition)
        {
            if (_isInGame)
                return;

            if (_inGameCharacter == null && playerInGamePrefab != null)
            {
                _inGameCharacter = Instantiate(playerInGamePrefab, spawnPosition, Quaternion.identity);
                Character.Initialize(this);
            }

            if (_inGameCharacter != null)
            {
                _inGameCharacter.SetActive(true);

                _isInGame = true;
            }
            else
            {
                Logs.LogError($"[PlayerManager] No in-game prefab assigned for Player {PlayerIndex}");
            }
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
    }
}
