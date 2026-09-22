using System;
using System.Collections.Generic;

namespace SoloHero.Core.Common
{
    public static class Services
    {
        private static readonly Dictionary<Type, object> _map = new Dictionary<Type, object>();

        public static void Register<T>(T instance) where T : class => _map[typeof(T)] = instance;

        public static T Get<T>() where T : class =>
            _map.TryGetValue(typeof(T), out var found)
                ? (T)found
                : throw new InvalidOperationException("service not registered: " + typeof(T).Name);

        public static void Clear() => _map.Clear();
    }
}
