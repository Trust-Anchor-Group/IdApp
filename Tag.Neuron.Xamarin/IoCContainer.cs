using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Tag.Neuron.Xamarin
{
    /// <summary>
    /// Represents IoC scope, specifying whether instances are shared or not.
    /// </summary>
    public enum IocScope
    {
        /// <summary>
        /// Singleton, meaning everyone shares the same instance.
        /// </summary>
        Singleton,
        /// <summary>
        /// New instance per request to the <see cref="IoCContainer"/>s <see cref="IoCContainer.Resolve{T}(System.Type)"/> method.
        /// </summary>
        PerRequest
    }

    /// <summary>
    /// Represents an Inversion of Control container, acting as a factory for creating instance of objects.
    /// </summary>
    public sealed class IoCContainer : IDisposable
    {
        private readonly Dictionary<Type, Tuple<Type, IocScope>> dependencyMap;
        private readonly ConcurrentDictionary<Type, object> instanceCache;

        /// <summary>
        /// Creates a new instance of an <see cref="IoCContainer"/> class.
        /// </summary>
        public IoCContainer()
        {
            dependencyMap = new Dictionary<Type, Tuple<Type, IocScope>>();
            instanceCache = new ConcurrentDictionary<Type, object>();
        }

        /// <summary>
        /// Finalizer for the <see cref="IoCContainer"/> class.
        /// </summary>
        ~IoCContainer()
        {
            Dispose(false);
        }

        /// <inheritdoc/>
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

        /// <summary>
        /// Resolves the implementation of type <c>T</c>.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <param name="t">The type.</param>
        /// <returns>The resolved type, or null.</returns>
        public T Resolve<T>(Type t)
        {
            return (T)Resolve(t);
        }

        /// <summary>
        /// Resolves the implementation of type <c>T</c>.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <returns>The resolved type, or null.</returns>
        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        /// <summary>
        /// Registers a certain type and the interface it implements.
        /// </summary>
        /// <typeparam name="TFrom">The interface type.</typeparam>
        /// <typeparam name="TTo">The implementation type.</typeparam>
        /// <param name="iocScope">The scope this type should be resolved to.</param>
        public void Register<TFrom, TTo>(IocScope iocScope = IocScope.PerRequest)
        {
            dependencyMap.Add(typeof(TFrom), Tuple.Create(typeof(TTo), iocScope));
        }

        /// <summary>
        /// Returns true if the type has been registered.
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns></returns>
        public bool IsRegistered(Type type)
        {
            return dependencyMap.ContainsKey(type);
        }

        /// <summary>
        /// Returns true if the type has been registered.
        /// </summary>
        /// <typeparam name="TFrom">The interface type to check.</typeparam>
        /// <returns></returns>
        public bool IsRegistered<TFrom>()
        {
            return dependencyMap.ContainsKey(typeof(TFrom));
        }

        /// <summary>
        /// Registers an instance, which by default is a singleton.
        /// </summary>
        /// <typeparam name="TFrom">The interface it implements</typeparam>
        /// <param name="instance">The actual implementation instance.</param>
        public void RegisterInstance<TFrom>(TFrom instance)
        {
            instanceCache[typeof(TFrom)] = instance;
            dependencyMap.Add(typeof(TFrom), Tuple.Create(typeof(TFrom), IocScope.Singleton));
        }

        /// <summary>
        /// Unregisters an instance.
        /// </summary>
        /// <typeparam name="TFrom">The interface it implements</typeparam>
        /// <param name="instance">The actual implementation instance.</param>
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

        /// <summary>
        /// Resolves a type from the current container.
        /// </summary>
        /// <param name="type">The type to resolve.</param>
        /// <returns></returns>
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