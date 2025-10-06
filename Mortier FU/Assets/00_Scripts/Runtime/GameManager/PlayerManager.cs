using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class PlayerManager : MonoBehaviour
    {
        [Header("Setup")]
        public GameObject playerInGamePrefab; // le perso à spawn pendant la partie

        private PlayerInput _playerInput;
        private GameObject _inGameCharacter;
        private bool _isInGame = false;

        void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            DontDestroyOnLoad(gameObject); // garde la référence entre les scènes
        }

        void Start()
        {
            Debug.Log($"Player {_playerInput.playerIndex} joined with {_playerInput.devices[0].displayName}");
        }

        // Appelée depuis le GameManager quand on entre dans la GameScene
        public void SpawnInGame(Vector3 spawnPosition)
        {
            if (_isInGame) return;

            // On spawn un nouveau PlayerInput lié à CE joueur
            var newPlayer = PlayerInput.Instantiate(
                playerInGamePrefab,
                controlScheme: _playerInput.currentControlScheme,
                pairWithDevice: _playerInput.devices[0]
            );

            _inGameCharacter = newPlayer.gameObject;
            _inGameCharacter.transform.position = spawnPosition;

            _isInGame = true;
        }

        public void DespawnInGame()
        {
            if (_inGameCharacter != null)
                Destroy(_inGameCharacter);
            _isInGame = false;
        }
    }
}