using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class LobbyManager : MonoBehaviour
    {
        private const int k_maxPlayers = 4;
        private const int k_minPlayers = 2;

        [SerializeField] private string _gameSceneName = "Enzo";

        [SerializeField] private List<Vector3> _spawnPositions;

        [SerializeField] private LobbyPanel _lobbyPanel;

        [SerializeField] private EventSystem _eventSystem;

        public bool _gameStarted = false;

        private List<PlayerInput> _joinedPlayers = new (k_maxPlayers);
        public static LobbyManager Instance { get; private set; }


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            if (GameInstance.Instance == null || GameInstance.Instance._playerInputManager == null) 
                return;
            
            GameInstance.Instance._playerInputManager.onPlayerJoined += OnPlayerJoined;
            GameInstance.Instance._playerInputManager.onPlayerLeft += OnPlayerLeft;
        }

        private void OnDisable()
        {
            if (GameInstance.Instance == null || GameInstance.Instance._playerInputManager == null) 
                return;
            
            GameInstance.Instance._playerInputManager.onPlayerJoined -= OnPlayerJoined;
            GameInstance.Instance._playerInputManager.onPlayerLeft -= OnPlayerLeft;
        }

        private void OnPlayerJoined(PlayerInput playerInput)
        {
            if (_gameStarted) return;
            if (_joinedPlayers.Count >= k_maxPlayers) return;

            if (_joinedPlayers.Contains(playerInput)) return;
            
            _joinedPlayers.Add(playerInput);
            _lobbyPanel?.UpdateSlots(_joinedPlayers);
        }

        private void OnPlayerLeft(PlayerInput playerInput)
        {
            if (_gameStarted) return;

            if (!_joinedPlayers.Contains(playerInput)) return;
            
            _joinedPlayers.Remove(playerInput);
            _lobbyPanel?.UpdateSlots(_joinedPlayers);
        }
        
        public void TryStartGame()
        {
            if (_joinedPlayers.Count < k_minPlayers || _joinedPlayers.Count > k_maxPlayers)
                return;
            SceneManager.sceneLoaded += OnGameSceneLoaded;
            SceneManager.LoadScene(_gameSceneName);
            _gameStarted = true;
        }

        private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            for (var i = 0; i < _joinedPlayers.Count; i++)
            {
                var playerManager = _joinedPlayers[i].GetComponent<PlayerManager>();
                if (playerManager != null)
                {
                    playerManager.SpawnInGame(_spawnPositions[i]);
                }
            }
            SceneManager.sceneLoaded -= OnGameSceneLoaded;
        }
    }
}
