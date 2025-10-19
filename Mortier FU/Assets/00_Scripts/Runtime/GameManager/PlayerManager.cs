using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class PlayerManager : MonoBehaviour
    {
        [Header("Setup")]
        public GameObject _playerInGamePrefab; // le perso à spawn pendant la partie

        private PlayerInput _playerInput;
        private GameObject _inGameCharacter;
        private bool _isInGame = false;

        public PlayerInput PlayerInput => _playerInput;
        public GameObject CharacterGO => _inGameCharacter;
        
        void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            DontDestroyOnLoad(gameObject); // garde la référence entre les scène
        }

        private void Start()
        {
            GM_Base.Instance.RegisterPlayer(_playerInput);
        }

        private void OnDestroy()
        {
            GM_Base.Instance.UnregisterPlayer(_playerInput);
        }

        // Appelée depuis le GameManager quand on entre dans la GameScene
        public void SpawnInGame(Vector3 spawnPosition)
        {
            if (_isInGame) return;
            
            _inGameCharacter.SetActive(true);
            _inGameCharacter.transform.position = spawnPosition;
            if (_inGameCharacter.TryGetComponent(out Character character) && character.Health != null)
            {
                character.Health.ResetHealth();
            }
            _isInGame = true;
        }

        public void InitializePlayer()
        {
            if (_isInGame) return;

            var newPlayer = PlayerInput.Instantiate(
                _playerInGamePrefab,
                controlScheme: _playerInput.currentControlScheme,
                pairWithDevice: _playerInput.devices[0]
            );
            
            _inGameCharacter = newPlayer.gameObject;
        }
        
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