using System;
using System.Collections.Generic;

namespace Viridian.Utils
{
    public static class AppContext
    {
        private static readonly Dictionary<Type, object> services = new();

        public static void Register<T>(T service) where T : class
        {
            services[typeof(T)] = service;
        }

        public static void Unregister<T>(T service) where T : class
        {
            if (services.TryGetValue(typeof(T), out var registered) && ReferenceEquals(registered, service))
            {
                if (registered is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                services.Remove(typeof(T));
            }
        }

        public static T Get<T>() where T : class
        {
            if (services.TryGetValue(typeof(T), out var service))
                return (T)service;

            UnityEngine.Debug.LogError($"[AppContext] {typeof(T).Name} not registered.");
            return null;
        }

        public static void Clear()
        {
            foreach (var service in services.Values)
            {
                if (service is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError($"[AppContext] Error disposing {service.GetType().Name}: {e.Message}");
                    }
                }
            }
            
            services.Clear();
        }
    }
}
