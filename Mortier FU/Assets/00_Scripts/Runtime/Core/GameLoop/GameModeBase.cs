using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MortierFu.Shared;
using Random = UnityEngine.Random;
using MEC;
using UnityEngine;

namespace MortierFu
{
    public abstract class GameModeBase : IGameMode {
        // Config
        public SO_GameModeData GameModeData;

        protected List<PlayerTeam> teams;
        public ReadOnlyCollection<PlayerTeam> Teams { get; private set; }

        protected int currentRound;
        protected int currentRank;
        protected GameState currentState;

        // Dependencies
        protected LobbyService lobbyService;
        protected CountdownTimer _timer;

        public virtual int MinPlayerCount => GameModeData.MinPlayerCount;
        public virtual int MaxPlayerCount => GameModeData.MaxPlayerCount;

        public bool IsReady {
            get {
                var players = lobbyService.GetPlayers();
                return players.Count >= MinPlayerCount && players.Count <= MaxPlayerCount;
            }
        }

        public int CurrentRoundCount => currentRound;
        public GameState CurrentState => currentState;
        
        /// EVENTS
        public event Action<GameState> OnGameStateChanged;
        public event Action<PlayerManager, PlayerManager> OnPlayerKilled; // (killer, victim)
        public event Action OnGameStarted;
        public event Action<int> OnRoundStarted;
        public event Action<int> OnRoundEnded;

        private const string k_gameplayActionMap = "Gameplay";
        private const string k_uiActionMap = "UI";

        public virtual void StartGame()
        {
            if (!IsReady) {
                Logs.LogWarning("Not enough players or too many players for this gamemode ! Falling back to playground.");
                StartRound();
                return;
            }
            
            Logs.Log("Starting the game...");
            currentRound = 0;
            Timing.RunCoroutine(GameplayCoroutine());
        }
        
        // TODO: Maybe it is valuable to ask for player 1 input to proceed to each step for fluidity
        protected virtual IEnumerator<float> GameplayCoroutine()
        {
            UpdateGameState(GameState.StartGame);
            OnGameStarted?.Invoke();

            while (currentState != GameState.EndGame)
            {
                StartRound();

                while (!OneTeamStanding())
                    yield return 0f;
                
                UpdateGameState(GameState.EndRound);
                EndRound();

                UpdateGameState(GameState.DisplayScores);
                DisplayScores();
                while (_timer.IsRunning)
                    yield return 0f;
                HideScores();
            
                if (IsGameOver(out PlayerTeam victor))
                {
                    Logs.Log($"Game Over! Team {victor.Index} wins!");
                    UpdateGameState(GameState.EndGame);
                }
                else // Augment selection
                {
                    UpdateGameState(GameState.ShowcaseAugments);
                    ShowcaseAugments();
                    while (_timer.IsRunning)
                        yield return 0f;
                    
                    var system = SystemManager.Instance.Get<AugmentSelectionSystem>();
                    system.StartAugmentSelection(GameModeData.AugmentSelectionDuration);
                    StartAugmentSelection();
                    
                    while (!system.IsSelectionOver)
                        yield return 0f;
                    
                    system.EndAugmentSelection();
                    EndAugmentSelection();
                }
            }

            EndGame();
        }

        private void SpawnPlayers()
        {
            // TODO: Choose spawn position based on a level script
            
            foreach (var team in teams)
            {
                foreach (var member in team.Members)
                {
                    Vector3 spawnPosition = Random.insideUnitSphere.With(y: 1f).normalized * 10;
                    member.SpawnInGame(spawnPosition);
                    member.Character.transform.position = spawnPosition;
                }
            }
        }

        private void EnablePlayerInputs(bool enabled = true)
        {
            foreach (var team in teams)
            {
                foreach (var member in team.Members)
                {
                    member.PlayerInput.SwitchCurrentActionMap(enabled ? k_gameplayActionMap : k_uiActionMap);
                }
            }
        }
        
        protected virtual bool OneTeamStanding()
        {
            int aliveTeam = 0;
            PlayerTeam roundVictor = null;
            
            foreach (var team in teams)
            {
                foreach (var member in team.Members)
                {
                    if (!member.IsInGame || member.Character == null) continue;
                    
                    if (member.Character.Health.IsAlive)
                    {
                        aliveTeam++;
                        if (aliveTeam > 1) return false;
                        roundVictor = team;
                        break;
                    }
                }
            }

            if (aliveTeam != 1)
            {
                Logs.LogError("[GameModeBase] OneTeamStanding called but no team is standing!");
                return false;
            }

            if (roundVictor != null)
            {
                roundVictor.Rank = 1;
            } 
            else { 
                Logs.LogError("[GameModeBase] OneTeamStanding called but roundVictor is null!");
            }

            return true;
        }

        protected virtual bool AllPlayersReady()
        {
            // TODO: Implement player ready check
            return false;
        }

        protected virtual void ResetPlayers()
        {
            foreach (var team in teams)
            {
                foreach (var member in team.Members)
                {
                    member.Character.Reset();
                }
            }
        }
        
        protected virtual void StartRound()
        {
            UpdateGameState(GameState.Round);

            currentRound++;
            currentRank = teams.Count;
            
            SpawnPlayers();
            EnablePlayerInputs();
            PlayerCharacter.AllowGameplayActions = true;

            foreach (var team in teams)
            {
                foreach (var member in team.Members)
                {
                    member.Metrics.ResetRoundMetrics();
                }
            }
            
            OnRoundStarted?.Invoke(currentRound);
            Logs.Log($"Round #{currentRound} is starting...");
        }

        protected virtual void EndRound()
        {
            _timer.Stop();
            //EnablePlayerInputs(false);
            ResetPlayers();
            PlayerCharacter.AllowGameplayActions = false;

            EvaluateScores();
            
            OnRoundEnded?.Invoke(currentRound);
            Logs.Log("Round ended.");
        }
        
        protected virtual void EvaluateScores()
        {
            foreach (var team in teams)
            {
                int rankBonusScore = GetScorePerRank(team.Rank);
                int killBonusScore = team.Members.Sum(m => m.Metrics.RoundKills * GameModeData.KillBonusScore);
                team.Score += rankBonusScore + killBonusScore;
            }
        }
        
        protected virtual  int GetScorePerRank(int teamRank)
        {
            if (teamRank >= teams.Count) return 0;

            return teamRank switch
            {
                1 => GameModeData.FirstRankBonusScore,
                2 => GameModeData.SecondRankBonusScore,
                3 => GameModeData.ThirdRankBonusScore,
                _ => 0
            };
        }

        protected virtual void DisplayScores()
        {
            UpdateGameState(GameState.DisplayScores);
            
            _timer.Reset(GameModeData.DisplayScoresDuration);
            _timer.Start();
            
            // Update UI Score Panel
            // Link countdown timer
            
            Logs.Log("Displaying scores...");
            
            foreach (var team in teams)
            {
                Logs.Log($"Team Score: {team.Score}");
            }
        }

        protected virtual void HideScores()
        {
            _timer.Stop();
            
            // Hide UI
        }
        
        protected virtual void ShowcaseAugments()
        {
            UpdateGameState(GameState.ShowcaseAugments);
            
            _timer.Reset(GameModeData.AugmentShowcaseDuration);
            _timer.Start();
            
            // Link to UI and countdown
            
            Logs.Log("Showcasing the augments which are going to be available...");
        }
        
        protected virtual void StartAugmentSelection()
        {
            UpdateGameState(GameState.AugmentSelection);
            
            // Hide previous showcase UI            
            EnablePlayerInputs();
            
            Logs.Log("Starting augment selection...");
        }

        protected virtual void EndAugmentSelection()
        {
            UpdateGameState(GameState.EndAugmentSelection);

            EnablePlayerInputs(false);

            // stop selection UI
        }

        protected virtual void EndGame()
        {
            // The game state is already set to EndGame at that point
            // Show the victory screen
        }
        
        protected virtual void UpdateGameState(GameState newState)
        {
            currentState = newState;
            OnGameStateChanged?.Invoke(newState);
        }
        
        public virtual void Initialize()
        {
            // Resolve Dependencies
            lobbyService = ServiceManager.Instance.Get<LobbyService>();
            _timer = new CountdownTimer(0f);
            
            if (!IsReady) {
                Logs.LogWarning("Invalid number of players for this game mode.");
            }
            
            teams = new List<PlayerTeam>();
            Teams = teams.AsReadOnly();
            
            var players = lobbyService.GetPlayers();
            for (int i = 0; i < players.Count; i++)
            {
                var team = new PlayerTeam(i, players[i]);
                teams.Add(team);
            }
            
            Logs.Log("Game mode initialized successfully.");
        }

        public virtual void Update()
        { }
        
        public virtual void Dispose()
        {
            teams.Clear();
            _timer.Dispose();
        }

        /// <summary>
        /// Default implementation is to select the victor based on a score to win variable.
        /// </summary>
        /// <param name="victor">The team which won the game.</param>
        /// <returns>If the game is over.</returns>
        public virtual bool IsGameOver(out PlayerTeam victor)
        {
            foreach (var team in teams)
            {
                if (team.Score > GameModeData.ScoreToWin)
                {
                    victor = team;
                    return true;
                }
            }

            victor = null;
            return false;
        }
        
        // TODO: Can be improved ?
        public virtual void NotifyKillEvent(PlayerCharacter killerPlayerCharacter, PlayerCharacter victimPlayerCharacter)
        {
            var killer = killerPlayerCharacter.Owner;
            var victim = victimPlayerCharacter.Owner;
            
            killer.Metrics.RoundKills += 1;
            
            // TODO: Can be improved
            var victimTeam = teams.FirstOrDefault(t => t.Members.Contains(victim));
            if(victimTeam == null) 
            {
                Logs.LogError("[GameModeBase] Victim's team not found!");
                return;
            }

            if (victimTeam.Members.All(m => m.Character.Health.IsAlive == false))
            {
                victimTeam.Rank = currentRank;
                currentRank--;
            }
            
            OnPlayerKilled?.Invoke(killer, victim);
        }
    }
}