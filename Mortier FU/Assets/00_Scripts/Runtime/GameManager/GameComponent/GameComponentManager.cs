using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class GameComponentManager : IDisposable
    {
        
        private readonly Dictionary<Type, IGameComponent> _systems = new();
        public void Register<T>(T system) where T : class, IGameComponent
        {
            _systems[typeof(T)] = system ?? throw new ArgumentNullException(nameof(system));
        }
        
        /// <summary>
        /// Automatically finds and registers all services implementing IGameService.
        /// Optionally filtered by namespace or assembly.
        /// </summary>
        public void RegisterAll<T> (Assembly assembly = null, string namespaceFilter = null) where T : class, IGameComponent
        {
            assembly ??= Assembly.GetExecutingAssembly();

            var types = assembly.GetTypes()
                .Where(t => typeof(T).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

            if (!string.IsNullOrEmpty(namespaceFilter))
                types = types.Where(t => t.Namespace != null && t.Namespace.StartsWith(namespaceFilter));

            foreach (var type in types)
            {
                try
                {
                    var instance = (IGameComponent)Activator.CreateInstance(type);
                    Register(instance);
                }
                catch (Exception e)
                {
                    Logs.LogError($"[GameComponentManager] Failed to create {type.Name}: {e.Message}");
                    return;
                }
                Logs.Log($"[GameComponentManager] Registered GameComponent {type.Name}");
            }
        }


        public T Get<T>() where T : class, IGameComponent
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