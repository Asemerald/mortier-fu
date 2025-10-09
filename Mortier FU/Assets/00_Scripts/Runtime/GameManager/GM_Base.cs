using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;

namespace MortierFu
{
    public enum GameState
    {
        Lobby,
        StartGame,
        StartRound,
        EndRound,
        StartBonusSelection,
        EndBonusSelection,
        EndGame
    }

    public class GM_Base : MonoBehaviour
    {
        public static GM_Base Instance { get; private set; }
        
        [SerializeField] private ScorePanel _scorePanel;
        [SerializeField] private BonusSelectionPanel _bonusSelectionPanel;
        
        [SerializeField] private int _maxRound = 4;

        [SerializeField] private float _roundDuration = 180f; // en secondes
        [SerializeField] private float _bonusSelectionDuration = 60f; // en secondes
        [SerializeField] private float _showScoreDuration = 10f; // en secondes

        [SerializeField] private List<Vector3> _spawnPositions;

        private int _currentRound;

        private GameState _currentState = GameState.Lobby;

        private List<PlayerInput> _joinedPlayers = new List<PlayerInput>();
        private Dictionary<PlayerInput, int> _scores;

        private CountdownTimer _timer;

        private List<string> bonusList = new List<string> { "bonus1", "bonus2", "bonus3", "bonus4", "bonus5" };
        public GameState CurrentState => _currentState;

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

        private void StartGame()
        {
            // Initialisation des variables de la partie
            _scores = new();
            _currentRound = 0;

            _scorePanel.Init(_joinedPlayers);

            SetGameState(GameState.StartRound);

            //TODO: lancement de la musique de début de partie
        }

        private void StartRound()
        {
            // Initialisation du timer du round
            _timer = new CountdownTimer(_roundDuration);
            _timer.Start();
            _timer.OnTimerStop += EndRound;

            // Spawn des joueurs
            InitializePlayers();
            
            // Activation des inputs des joueurs pour se battre
            EnablePlayerInputs();

            // TODO : lancement de la musique de round
            // TODO : lancer 
        }

        private void EndRound()
        {
            // Fin du timer de round
            _timer.OnTimerStop -= EndRound;
            _timer.Stop();
            
            _timer = new CountdownTimer(_showScoreDuration);
            _timer.Start();
            _timer.OnTimerStop += StartBonusSelection;

            // Mise à jour du round actuel et vérification de la fin de partie
            _currentRound++;
            
            if(_currentRound == _maxRound)
            {
                SetGameState(GameState.EndGame);
                return;
            }
            
            _currentState = GameState.EndRound; // Je modifierai plus tard
            
            DisablePlayerInputs();
            
            // Mise à jour des visuels de score et affichage
            _scorePanel.UpdateAllScores();
            _scorePanel.Show();
            
            // TODO : doit être appeler à chaque fois qu'un joueur est mort
            // TODO : lancement de la musique de fin de round
        }

        private void StartBonusSelection()
        {
            _currentState = GameState.StartBonusSelection; // Je modifierai plus tard
            
            _scorePanel.Hide();
            
            _timer.OnTimerStop -= StartBonusSelection;
            _timer.Stop();   
            
            _timer = new CountdownTimer(_bonusSelectionDuration);
            _timer.Start();
            _timer.OnTimerStop += EndBonusSelection;
            
            _bonusSelectionPanel.Init(_joinedPlayers, bonusList);
            _bonusSelectionPanel.OnAllPlayersSelected += HandleAllPlayersSelected;
            _bonusSelectionPanel.Show();
            
            // TODO: Peut être load un nouveau controller ou du moins qui bloque certains inputs pour ne faire que du melee
        }
        private void EndBonusSelection()
        {
            _currentState = GameState.EndBonusSelection; // Je modifierai plus tard
            
            _bonusSelectionPanel.Hide();
            
            _timer.OnTimerStop -= EndBonusSelection;
            _timer.Stop();
            
            SetGameState(GameState.StartRound);
        }

        private void EndGame()
        {
            _timer = null;

            // TODO : afficher l'écran de fin de partie avec les scores finaux
            // TODO : permettre de retourner au lobby ou de quitter le jeu ou de relancer une partie
            // TODO : lancement de la musique de fin de partie
            // TODO : reset des joueurs, de leurs états, de leurs scores, etc...
        }

        public void RegisterPlayer(PlayerInput playerInput)
        {
            if (_joinedPlayers.Contains(playerInput)) return;

            _joinedPlayers.Add(playerInput);
        }

        public void UnregisterPlayer(PlayerInput playerInput)
        {
            if (!_joinedPlayers.Contains(playerInput)) return;

            _joinedPlayers.Remove(playerInput);
        }

        public void SetGameState(GameState newState)
        {
            _currentState = newState;

            switch (newState)
            {
                case GameState.StartGame: StartGame(); break;
                case GameState.StartRound: StartRound(); break;
                case GameState.StartBonusSelection: StartBonusSelection(); break;
                case GameState.EndRound: EndRound(); break;
                case GameState.EndBonusSelection: EndBonusSelection(); break;
                case GameState.EndGame: EndGame(); break;
            }
        }

        private void InitializePlayers()
        {
            for (var i = 0; i < _joinedPlayers.Count; i++)
            {
                var playerManager = _joinedPlayers[i].GetComponent<PlayerManager>();
                if (playerManager != null)
                {
                    playerManager.SpawnInGame(_spawnPositions[i]);
                    _joinedPlayers[i].DeactivateInput();
                }
            }
        }

        private void EnablePlayerInputs()
        {
            foreach (var player in _joinedPlayers)
            {
                player.ActivateInput();
            }
        }

        private void DisablePlayerInputs()
        {
            foreach (var player in _joinedPlayers)
            {
                player.DeactivateInput();
            }
        }

        public void AddScore(PlayerInput playerInput, int score)
        {
            if (!_scores.ContainsKey(playerInput))
            {
                _scores[playerInput] = 0;
            }

            _scores[playerInput] += score;
        }

        public int GetPlayerScore(PlayerInput playerInput)
        {
            return _scores.ContainsKey(playerInput) ? _scores[playerInput] : 0;
        }
        
        private void HandleAllPlayersSelected()
        {
            _bonusSelectionPanel.OnAllPlayersSelected -= HandleAllPlayersSelected;
            SetGameState(GameState.EndBonusSelection);
        }

    }
}
