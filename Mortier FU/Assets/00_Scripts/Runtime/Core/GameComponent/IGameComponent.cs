﻿using System;
using System.Threading.Tasks;
using MortierFu.Shared;

namespace MortierFu
{
    /// <summary>
    /// This interface defines the basic structure for game components that require initialization, regular updates (ticks), and proper disposal.
    /// </summary>
    public interface IGameComponent : IDisposable
    {
        /// <summary>
        /// Method to call to initialize the service
        /// </summary>
        async Task Initialize()
        {
            IsInitialized = true;
            await OnInitialize();
        }

        Task OnInitialize();

        /// <summary>
        ///  Method to call every frame to update the service
        /// </summary>
        void Tick() 
        { }
        
        bool IsInitialized { get; set; }
    }
    /// <summary>
    /// For services that should be unique and persist across scenes (e.g., AudioService, InputService).
    /// </summary>
    public interface IGameService : IGameComponent
    {
    }
    
    /// <summary>
    /// For systems that are scene-specific and can be re-initialized when a new scene is loaded (e.g., EnemyManager, LevelManager).
    /// </summary>
    public interface IGameSystem : IGameComponent
    {
    }
}