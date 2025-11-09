using MortierFu.Shared;
using System;
using Cysharp.Threading.Tasks;

namespace MortierFu
{
    public class SystemManager : GameComponentManager
    {
        public GameInitializer GameInitializer{ get; private set; }
        
        public static SystemManager Instance { get; private set; }

        public SystemManager(GameInitializer gameInitializer)
        {
            GameInitializer = gameInitializer;
        }
        
        public static SO_GameConfig Config => Instance.GameInitializer.config;
        
        // Initialize all uninitialized registered systems
        public async UniTask Initialize() 
        {
            Instance = this;
            
            try
            {
                foreach (var system in _components.Values)
                {
                    if(system.IsInitialized) continue; 
                    await system.Initialize();
                }
                Logs.Log($"[GameSystems] Initialized {_components.Count} systems.");
            }
            catch (Exception e)
            {
                Logs.LogError($"[GameSystems] Initialization failed: {e.Message}");
                foreach (var system in _components)
                    if (system.Value == null)
                        Logs.LogError($"[GameSystems] System {system.Key.Name} is null.");
                    else
                        Logs.Log($"[GameSystems] System {system.Key.Name} initialized successfully.");

                throw;
            }
        }
        
        public void CreateAndRegister<TSystem>() where TSystem : class, IGameSystem, new()
        {
            var system = new TSystem();
            Register(system);
        }
        
        public void UnregisterAndDispose<TSystem>() where TSystem : class, IGameSystem
        {
            var system = Get<TSystem>();
            if (system != null)
            {
                system.Dispose();
                _components.Remove(typeof(TSystem));
            }
        }
    }
}