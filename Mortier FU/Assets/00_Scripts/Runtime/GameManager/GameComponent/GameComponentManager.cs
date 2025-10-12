using System;
using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class GameComponentManager : IDisposable
    {
        private readonly Dictionary<Type, IGameSystem> _systems = new();

        public void Register<T>(T system) where T : class, IGameSystem
        {
            _systems[typeof(T)] = system ?? throw new ArgumentNullException(nameof(system));
        }

        public T Get<T>() where T : class, IGameSystem
        {
            _systems.TryGetValue(typeof(T), out var system);
            return system as T;
        }

        public void Initialize()
        {
            try
            {
                foreach (var system in _systems.Values) system.Initialize();
                Logs.Log($"[GameSystems] Initialized {_systems.Count} systems.");
            }
            catch (Exception e)
            {
                // Debug wich system failed to initialize
                Logs.LogError($"[GameSystems] Initialization failed: {e.Message}");
                foreach (var system in _systems)
                    if (system.Value == null)
                        Logs.LogError($"[GameSystems] System {system.Key.Name} is null.");
                    else
                        Logs.Log($"[GameSystems] System {system.Key.Name} initialized successfully.");

                throw;
            }
        }

        public void Tick()
        {
            foreach (var system in _systems.Values) system.Tick();
        }

        public void Dispose()
        {
            foreach (var system in _systems.Values) system.Dispose();
            _systems.Clear();
        }
    }

    public class SystemManager : GameComponentManager
    {
    }
    
    public class ServiceManager : GameComponentManager
    {
    }
}