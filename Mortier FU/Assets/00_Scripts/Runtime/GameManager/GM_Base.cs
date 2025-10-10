using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;
using MortierFu.Shared;
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

    public class Cnc
    {
        public PlayerManager PlayerManager;
        public PlayerInput LobbyInput;
        public PlayerInput GameInput;
        public Character Character;
        public int PlayerNumber;
        public int Score;
    }

    public class GM_Base : MonoBehaviour
    {
        public static GM_Base Instance { get; private set; }
        
        private ScorePanel _scorePanel;
        private BonusSelectionPanel _bonusSelectionPanel;
        
        [SerializeField] private int _maxRound = 4;

        [SerializeField] private float _roundDuration = 180f; // en secondes
        [SerializeField] private float _bonusSelectionDuration = 60f; // en secondes
        [SerializeField] private float _showScoreDuration = 10f; // en secondes

        [SerializeField] private List<Vector3> _spawnPositions;
        
        [Header("Debugging")]
        [SerializeField] private bool _enableDebug = true;

        private int _currentRound;

        private GameState _currentState = GameState.Lobby;

        private List<Cnc> _joinedPlayers = new();
        
        private CountdownTimer _timer;

        private List<string> _bonusList = new List<string>() { "bonus1", "bonus2", "bonus3", "bonus4", "bonus5" };
        public GameState CurrentState => _currentState;

        /// (killer, victim)
        public Action<Cnc, Cnc> OnPlayerKilled; 
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _joinedPlayers = new List<Cnc>();
        }

        private void StartGame()
        {
            if(_enableDebug)
                Logs.Log("Game is started");
            // Initialisation des variables de la partie
            _currentRound = 0;
            
            InitializePlayers();
            
            // Recherche dynamique du ScorePanel si non assigné
            if (_scorePanel == null)
                _scorePanel = FindFirstObjectByType<ScorePanel>(); 
            if (_bonusSelectionPanel == null) 
                _bonusSelectionPanel = FindFirstObjectByType<BonusSelectionPanel>();

            _scorePanel.Init(_joinedPlayers);
            
            _bonusSelectionPanel.Hide();
            
            SetGameState(GameState.StartRound);

            // TODO: lancement de la musique de début de partie
        }

        private void StartRound()
        {
            if(_enableDebug)
                Logs.Log("Round is started");

            _timer ??= new CountdownTimer(0);
            StopAllTimers();
            
            _timer.Reset(_roundDuration);
            _timer.OnTimerStop += EndRound;
            _timer.Start();

            RespawnPlayers();
            EnablePlayerInputs();

            // TODO : lancement de la musique de round
            // TODO : lancer 
        }

        private void EndRound()
        {
            if(_enableDebug)
                Logs.Log("Round is ended");

            // Fin du timer de round
            StopAllTimers();

            _timer.Reset(_showScoreDuration);
            _timer.OnTimerStop += StartBonusSelection;
            _timer.Start();

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

            // TODO : lancement de la musique de fin de round
        }

        private void StartBonusSelection()
        {
            if(_enableDebug)
                Logs.Log("Bonus selection is started");

            _currentState = GameState.StartBonusSelection; // Je modifierai plus tard

            _scorePanel.Hide();
            
            StopAllTimers();  
            _timer.Reset(_bonusSelectionDuration);
            _timer.OnTimerStop += EndBonusSelection;
            _timer.Start();

            _bonusSelectionPanel.Init(_joinedPlayers, _bonusList);
            _bonusSelectionPanel.OnAllPlayersSelected += HandleAllPlayersSelected;
            _bonusSelectionPanel.Show();

            // TODO: Peut être load un nouveau controller ou du moins qui bloque certains inputs pour ne faire que du melee
        }
        private void EndBonusSelection()
        {
            if(_enableDebug)
                Logs.Log("Bonus selection is ended");

            _currentState = GameState.EndBonusSelection; // Je modifierai plus tard

            _bonusSelectionPanel.Hide();

            StopAllTimers();

            SetGameState(GameState.StartRound);
        }

        private void EndGame()
        {
            if(_enableDebug)
                Logs.Log("Game is finished");
            
            StopAllTimers();
            
            ResetAllScores();
            RespawnPlayers();
            
            SetGameState(GameState.StartGame);
            
            // TODO : afficher l'écran de fin de partie avec les scores finaux
            // TODO : permettre de retourner au lobby ou de quitter le jeu ou de relancer une partie
            // TODO : lancement de la musique de fin de partie
        }

        private void ResetAllScores()
        {
            foreach (var player in _joinedPlayers)
            {
                player.Score = 0;
            }
            if (_scorePanel != null)
                _scorePanel.UpdateAllScores();
        }

        public void RegisterPlayer(PlayerInput playerInput)
        {
            if (_joinedPlayers.Any(p => p.LobbyInput == playerInput)) 
                return;

            if (!playerInput.TryGetComponent(out PlayerManager playerManager))
            {
                Logs.Error("PlayerInput does not have a PlayerManager component.");
                return;
            }
            
            _joinedPlayers.Add(new Cnc
            {
                PlayerManager = playerManager,
                LobbyInput = playerInput,
                Score = 0,
                PlayerNumber = playerInput.playerIndex
            });
        }

        public void UnregisterPlayer(PlayerInput playerInput)
        {
            var cnc = _joinedPlayers.FirstOrDefault(p => p.LobbyInput == playerInput);
            if (cnc == null) return;
            _joinedPlayers.Remove(cnc);
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
                var cnc = _joinedPlayers[i];
                
                cnc.PlayerManager.InitializePlayer();
                _joinedPlayers[i].LobbyInput.DeactivateInput();
                
                cnc.GameInput = cnc.PlayerManager.PlayerInput;
                
                if (!cnc.PlayerManager.CharacterGO.TryGetComponent(out cnc.Character))
                {
                    Logs.Error("Player's CharacterGO does not have a Character component.");
                }
            } 
        }
        
        private void RespawnPlayers()
        {
            for (var i = 0; i < _joinedPlayers.Count; i++)
            { 
                var cnc = _joinedPlayers[i];
                cnc.Character.ResetCharacter();
                cnc.PlayerManager.SpawnInGame(_spawnPositions[i]);
            }
        }

        private void EnablePlayerInputs()
        {
            foreach (var player in _joinedPlayers)
            {
                player.LobbyInput.enabled = true;
                player.GameInput.enabled = true;
            }
        }

        private void DisablePlayerInputs()
        {
            foreach (var player in _joinedPlayers)
            {
                player.LobbyInput.enabled = false;
                player.GameInput.enabled = false;
            }
        }
        
        private void HandleAllPlayersSelected()
        {
            _bonusSelectionPanel.OnAllPlayersSelected -= HandleAllPlayersSelected;
            SetGameState(GameState.EndBonusSelection);
        }

        public void NotifyKillEvent(Character killer, Character victim)
        {
            if (!killer || !victim)
            {
                Logs.Error("Killer or victim is null in NotifyKillEvent.");
                return;
            }
            
            var killerCnc = _joinedPlayers.FirstOrDefault(p => p.Character == killer);
            var victimCnc = _joinedPlayers.FirstOrDefault(p => p.Character == victim);

            if (killerCnc == null || victimCnc == null)
            {
                Logs.Error($"An unregistered character was involved in a kill event. Killer: {killer.name}, Victim: {victim.name}");
                return;
            }
            
            killerCnc.Score += 1;
            OnPlayerKilled?.Invoke(killerCnc, victimCnc);
            
            victimCnc.PlayerManager.DespawnInGame();
            
            if (_enableDebug)
            {
                Logs.Log($"Player {killerCnc.LobbyInput.playerIndex + 1} killed Player {victimCnc.LobbyInput.playerIndex + 1}. New score: {killerCnc.Score}");
            }
            
            // Check if all but one player are dead
            int aliveCount = _joinedPlayers.Count(p => p.Character.Health.IsAlive);
            if (aliveCount <= 1)
            {
                SetGameState(GameState.EndRound);
            }
        }
        
        private void StopAllTimers()
        {
            if (_timer == null) return;
            
            _timer.OnTimerStop -= EndRound;
            _timer.OnTimerStop -= StartBonusSelection;
            _timer.OnTimerStop -= EndBonusSelection;
            _timer.Stop();
        }
    }
}
