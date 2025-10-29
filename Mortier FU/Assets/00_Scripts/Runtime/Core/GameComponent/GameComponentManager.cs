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
        protected readonly Dictionary<Type, IGameComponent> _services = new();
        public void Register<T>(T system) where T : class, IGameComponent
        {
            _services[typeof(T)] = system ?? throw new ArgumentNullException(nameof(system));
        }
        
        public T Get<T>() where T : class, IGameComponent
        {
            _services.TryGetValue(typeof(T), out var system);
            return system as T;
        }
        public void Tick()
        {
            foreach (var system in _services.Values) system.Tick();
        }

        public void Dispose()
        {
            foreach (var system in _services.Values) system.Dispose();
            _services.Clear();
        }
    }
}