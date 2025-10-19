using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class GameComponentManager : IDisposable
    {
        
        protected readonly Dictionary<Type, IGameComponent> _systems = new();
        
        public static GameComponentManager Instance { get; private set; }
        public void Register<T>(T system) where T : class, IGameComponent
        {
            _systems[typeof(T)] = system ?? throw new ArgumentNullException(nameof(system));
        }


        public T Get<T>() where T : class, IGameComponent
        {
            _systems.TryGetValue(typeof(T), out var system);
            return system as T;
        }

        public Task Initialize()
        {
            Instance = this;
            
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
            return Task.CompletedTask;
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
        /// <summary>
        /// Check for missing services of type T in _services dictionary.
        /// Optionally filtered by namespace or assembly.
        /// <param name="assembly">
        /// If provided, only types from this assembly are checked.
        /// </param>
        /// <param name="namespaceFilter">
        ///  If provided, only types within this namespace (or its sub-namespaces) are checked.
        /// </param>
        /// </summary>
        public Task CheckForMissingServices<T> (Assembly assembly = null, string namespaceFilter = null) where T : class, IGameComponent
        {
            assembly ??= Assembly.GetExecutingAssembly();

            var types = assembly.GetTypes()
                .Where(t => typeof(T).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

            if (!string.IsNullOrEmpty(namespaceFilter))
                types = types.Where(t => t.Namespace != null && t.Namespace.StartsWith(namespaceFilter));

            foreach (var type in types)
            {
                // if type not in _services, log warning
                if (!_systems.ContainsKey(type))
                {
                    Logs.LogWarning($"[ServiceManager] Missing service of type {type.FullName}");
                }
            }
            return Task.CompletedTask;
        }
    }
}