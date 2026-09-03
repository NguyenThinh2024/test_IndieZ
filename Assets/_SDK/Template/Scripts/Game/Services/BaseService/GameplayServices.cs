using System;
using System.Collections.Generic;

namespace Thinh.Base.Gameplay
{
    public sealed class GameplayServices
    {
        private readonly Dictionary<Type, IGameplayService> _map = new();

        public void Add(IGameplayService service)
        {
            var key = service.GetType();
            if (_map.ContainsKey(key)) throw new Exception($"Service already registered: {key.Name}");
            _map.Add(key, service);
        }

        public T Get<T>() where T : class, IGameplayService
        {
            if (_map.TryGetValue(typeof(T), out var svc)) return (T)svc;
            throw new Exception($"Missing service: {typeof(T).Name}");
        }

        public bool TryGet<T>(out T service) where T : class, IGameplayService
        {
            if (_map.TryGetValue(typeof(T), out var svc))
            {
                service = (T)svc;
                return true;
            }
            service = null;
            return false;
        }
    }
}