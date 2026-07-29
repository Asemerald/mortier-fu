using System;
using System.Collections.ObjectModel;
using Cysharp.Threading.Tasks;

namespace MortierFu 
{
    public enum GameState
    {
        Lobby = 0,

        StartGame = 10,

        AugmentIntro = 20,
        AugmentRace = 21,
        EndAugmentRace = 22,

        RoundCountdown = 30,
        Round = 31,
        EndRound = 32,

        DisplayScores = 40,

        EndGame = 100,
    }
    
    public interface IGameMode : IDisposable
    {
        /// EVENTS
        public event Action<GameState> OnGameStateChanged;
        public event Action<PlayerManager, PlayerManager> OnPlayerKilled; // (killer, victim)
        public event Action OnGameStarted;
        public event Action<RoundInfo> OnRoundStarted;
        public event Action<RoundInfo> OnRoundGameplayStarted;
        public event Action<RoundInfo> OnRoundEnded;
        
        public event Action<int> OnGameEnded; 
        
        public int MinPlayerCount { get; }
        public int MaxPlayerCount { get; }
        public bool IsReady { get; }
        
        public ReadOnlyCollection<PlayerTeam> Teams { get; }
        
        public int CurrentRoundCount { get; }
        
        public UniTask Initialize();

        public UniTask StartGame();
        
        public void Update();

        public bool IsGameOver(out PlayerTeam victor);
        
        public int GetWinnerPlayerIndex();
        
        public void SetMatchConfig(MatchConfig config);

        public void SetScoreToWin(int scoreToWin);
        
        public GameState CurrentGameState { get; }
    }
}