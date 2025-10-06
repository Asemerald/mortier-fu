using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class PlayerManager : MonoBehaviour
    {
        [Header("Setup")]
        public GameObject playerInGamePrefab; // le perso à spawn pendant la partie

        private PlayerInput playerInput;
        private GameObject inGameCharacter;
        private bool isInGame = false;

        void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            DontDestroyOnLoad(gameObject); // garde la référence entre les scènes
        }

        void Start()
        {
            Debug.Log($"Player {playerInput.playerIndex} joined with {playerInput.devices[0].displayName}");
            // Ici tu peux faire feedback UI dans le lobby
        }

        // Appelée depuis le GameManager quand on entre dans la GameScene
        public void SpawnInGame(Vector3 spawnPosition)
        {
            if (isInGame) return;

            // On spawn un nouveau PlayerInput lié à CE joueur
            var newPlayer = PlayerInput.Instantiate(
                playerInGamePrefab,
                controlScheme: playerInput.currentControlScheme,
                pairWithDevice: playerInput.devices[0]
            );

            inGameCharacter = newPlayer.gameObject;
            inGameCharacter.transform.position = spawnPosition;

            isInGame = true;
        }

        public void DespawnInGame()
        {
            if (inGameCharacter != null)
                Destroy(inGameCharacter);
            isInGame = false;
        }
    }
}