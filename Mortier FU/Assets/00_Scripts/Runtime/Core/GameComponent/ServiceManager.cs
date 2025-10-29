using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MortierFu.Shared;

namespace MortierFu
{
    public class ServiceManager : GameComponentManager
    {
        public static ServiceManager Instance { get; private set; } = new ServiceManager();
        
        public Task Initialize()
        {
            Instance = this;
            
            try
            {
                foreach (var system in _services.Values) system.Initialize();
                Logs.Log($"[GameSystems] Initialized {_services.Count} services.");
            }
            catch (Exception e)
            {
                // Debug wich system failed to initialize
                Logs.LogError($"[GameSystems] Initialization failed: {e.Message}");
                foreach (var system in _services)
                    if (system.Value == null)
                        Logs.LogError($"[GameSystems] System {system.Key.Name} is null.");
                    else
                        Logs.Log($"[GameSystems] System {system.Key.Name} initialized successfully.");

                throw;
            }
            return Task.CompletedTask;
        }
        
#if UNITY_EDITOR
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
                if (!_services.ContainsKey(type))
                {
                    Logs.LogWarning($"[ServiceManager] Missing service of type {type.FullName}");
                }
            }
            return Task.CompletedTask;
        }
#endif
    }
}