﻿using System;
using System.Collections.ObjectModel;

namespace MortierFu 
{
    public interface IGameMode : IDisposable
    {
        /// EVENTS
        public event Action<GameState> OnGameStateChanged;
        public event Action<PlayerManager, PlayerManager> OnPlayerKilled; // (killer, victim)
        public event Action OnGameStarted;
        public event Action<int> OnRoundStarted;
        public event Action<int> OnRoundEnded;
        
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
        public void Initialize();

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
    }
}