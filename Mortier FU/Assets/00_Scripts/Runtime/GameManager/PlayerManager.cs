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

        public GameObject CharacterGO => _inGameCharacter;
        void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            DontDestroyOnLoad(gameObject); // garde la référence entre les scènes
        }

        // Appelée depuis le GameManager quand on entre dans la GameScene
        public void SpawnInGame(Vector3 spawnPosition)
        {
            if (_isInGame) return;

            if (_inGameCharacter != null)
            {
                _inGameCharacter.SetActive(true);
                _inGameCharacter.transform.position = spawnPosition;
                return;
            }
            
            var newPlayer = PlayerInput.Instantiate(
                _playerInGamePrefab,
                controlScheme: _playerInput.currentControlScheme,
                pairWithDevice: _playerInput.devices[0]
            );

            _inGameCharacter = newPlayer.gameObject;
            _inGameCharacter.transform.position = spawnPosition;
            
            _isInGame = true;
        }
        
        private void DespawnInGame()
        {
            if (_inGameCharacter != null)
            {
                _inGameCharacter.SetActive(false);
            }
            _isInGame = false;
        }
    }
}