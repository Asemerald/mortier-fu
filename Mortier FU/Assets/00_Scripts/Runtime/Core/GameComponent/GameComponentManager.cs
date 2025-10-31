using System.Collections.Generic;
using System.Linq;
using System;

namespace MortierFu
{
    public class GameComponentManager : IDisposable
    {
        protected readonly Dictionary<Type, IGameComponent> _components = new();
        
        public void Register<T>(T system) where T : class, IGameComponent
        {
            _components[typeof(T)] = system ?? throw new ArgumentNullException(nameof(system));
        }
        
        public void DisposeAllSystemsOfType<T>() where T : class, IGameComponent
        {
            var typesToDispose = _components.Keys.Where(t => typeof(T).IsAssignableFrom(t)).ToList();
            foreach (var type in typesToDispose)
            {
                _components[type].Dispose();
                _components.Remove(type);
            }
        }
        
        public T Get<T>() where T : class, IGameComponent
        {
            _components.TryGetValue(typeof(T), out var system);
            return system as T;
        }
        public void Tick()
        {
            foreach (var system in _components.Values) system.Tick();
        }

        public void Dispose()
        {
            foreach (var system in _components.Values) system.Dispose();
            _components.Clear();
        }
    }
}