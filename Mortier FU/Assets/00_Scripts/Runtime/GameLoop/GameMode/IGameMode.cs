using System;
using System.Collections.ObjectModel;
using Cysharp.Threading.Tasks;

namespace MortierFu 
{
    public enum GameState
    {
        Lobby,
        StartGame,
        Round,
        EndRound,
        DisplayScores,
        RaceInProgress,
        EndingRace,
        EndGame,
        DisplayAugment
    }
    
    public interface IGameMode : IDisposable
    {
        /// EVENTS
        ///  TODO: ça sert meme? genre ça s'implemente des actions ? 
        public event Action<GameState> OnGameStateChanged;
        public event Action<PlayerManager, PlayerManager> OnPlayerKilled; // (killer, victim)
        public event Action OnGameStarted;
        public event Action<RoundInfo> OnRoundStarted;
        public event Action<RoundInfo> OnRoundEnded;
        
        /// <summary>
        /// Event called when the game ends.
        /// <remarks>The int parameter represents the index of the winning player or team.</remarks>
        /// </summary>
        public event Action<int> OnGameEnded; 
        
        /// <summary>
        /// The minimum number of players required to launch the game.
        /// Internally capped at 1.
        /// </summary>
        public int MinPlayerCount { get; }
        /// <summary>
        /// The maximum number of players required to launch the game.
        /// Internally capped at 99.
        /// </summary>
        public int MaxPlayerCount { get; }
        public bool IsReady { get; }
        
        /// <summary>
        /// The teams containing their members (PlayerManagers)
        /// </summary>
        public ReadOnlyCollection<PlayerTeam> Teams { get; }
        
        /// <summary>
        /// The index of the ongoing round.
        /// </summary>
        public int CurrentRoundCount { get; }
        
        /// <summary>
        /// Initialization method
        /// </summary>
        public UniTask Initialize();

        /// <summary>
        /// Start the game and the gameplay loop
        /// </summary>
        public UniTask StartGame();
        
        /// <summary>
        /// Called every frame
        /// </summary>
        public void Update();

        /// <summary>
        /// Returns if the game should end, has a out parameter, if the game is over, returns the victor.
        /// </summary>
        /// <param name="victor"></param>
        /// <returns></returns>
        public bool IsGameOver(out PlayerTeam victor);
        
        /// <summary>
        /// Determines and returns the index of the winning player or team.
        /// </summary>
        public int GetWinnerPlayerIndex();

        public void SetScoreToWin(int scoreToWin);
    }
}