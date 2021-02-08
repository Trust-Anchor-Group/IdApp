using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IdApp
{
    public enum IocScope
    {
        Singleton,
        PerRequest
    }

    public sealed class IoC : IDisposable
    {
        private readonly Dictionary<Type, Tuple<Type, IocScope>> dependencyMap;
        private readonly ConcurrentDictionary<Type, object> instanceCache;

        public IoC()
        {
            dependencyMap = new Dictionary<Type, Tuple<Type, IocScope>>();
            instanceCache = new ConcurrentDictionary<Type, object>();
        }

        ~IoC()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // managed types
                var disposableTypes = instanceCache.Where(x => x.Value is IDisposable).Select(x => x.Value).Cast<IDisposable>().ToList();
                foreach (var disposable in disposableTypes)
                {
                    disposable.Dispose();
                }
            }
            // unmanaged types
            instanceCache.Clear();
            dependencyMap.Clear();
        }

        public T Resolve<T>(Type t)
        {
            return (T)Resolve(t);
        }

        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        public void Register<TFrom, TTo>(IocScope iocScope = IocScope.PerRequest)
        {
            dependencyMap.Add(typeof(TFrom), Tuple.Create(typeof(TTo), iocScope));
        }

        public bool IsRegistered(Type type)
        {
            return dependencyMap.ContainsKey(type);
        }

        public bool IsRegistered<TFrom>()
        {
            return dependencyMap.ContainsKey(typeof(TFrom));
        }

        public void RegisterInstance<TFrom>(TFrom instance)
        {
            instanceCache[typeof(TFrom)] = instance;
            dependencyMap.Add(typeof(TFrom), Tuple.Create(typeof(TFrom), IocScope.Singleton));
        }

        public void UnRegisterInstance<TFrom>(TFrom instance)
        {
            var key = typeof(TFrom);
            if (instanceCache.TryGetValue(key, out var existing) && ReferenceEquals(existing, instance))
            {
                object dummy;
                instanceCache.TryRemove(key, out dummy);
                dependencyMap.Remove(key);
            }
        }

        public object Resolve(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Tuple<Type, IocScope> resolvedType = LookUpDependency(type);

            if (resolvedType == null)
            {
                throw new ArgumentOutOfRangeException(nameof(type), type, type.FullName + " could not be resolved by " + GetType().FullName);
            }
            if (resolvedType.Item2 == IocScope.PerRequest || !instanceCache.TryGetValue(type, out var instance))
            {
                // Check instance cache            
                object createInstance;
                ConstructorInfo constructor = resolvedType.Item1.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(x => !x.IsStatic);
                if (constructor == null)
                {
                    createInstance = Activator.CreateInstance(resolvedType.Item1);
                }
                else
                {
                    ParameterInfo[] parameters = constructor.GetParameters();

                    if (!parameters.Any())
                    {
                        createInstance = Activator.CreateInstance(resolvedType.Item1);
                    }
                    else
                    {
                        createInstance = constructor.Invoke(ResolveParameters(parameters).ToArray());
                    }
                }

                if (resolvedType.Item2 == IocScope.Singleton)
                {
                    instance = instanceCache.GetOrAdd(type, createInstance);
                }
                else
                {
                    instance = createInstance;
                }
            }

            return instance;
        }

        private Tuple<Type, IocScope> LookUpDependency(Type type)
        {
            dependencyMap.TryGetValue(type, out var result);
            return result;
        }

        private IEnumerable<object> ResolveParameters(IEnumerable<ParameterInfo> parameters)
        {
            return parameters.Select(p => Resolve(p.ParameterType));
        }
    }
}