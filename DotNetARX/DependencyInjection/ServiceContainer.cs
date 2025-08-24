namespace DotNetARX.DependencyInjection
{
    /// <summary>
    /// 服务生命周期
    /// </summary>
    public enum ServiceLifetime
    {
        /// <summary>
        /// 瞬态 - 每次请求都创建新实例
        /// </summary>
        Transient,

        /// <summary>
        /// 单例 - 全局共享一个实例
        /// </summary>
        Singleton,

        /// <summary>
        /// 作用域 - 在同一作用域内共享实例
        /// </summary>
        Scoped
    }

    /// <summary>
    /// 服务描述符
    /// </summary>
    public class ServiceDescriptor
    {
        public Type ServiceType { get; set; }
        public Type ImplementationType { get; set; }
        public object Instance { get; set; }
        public Func<IServiceProvider, object> Factory { get; set; }
        public ServiceLifetime Lifetime { get; set; }

        public ServiceDescriptor(Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            ServiceType = serviceType;
            ImplementationType = implementationType;
            Lifetime = lifetime;
        }

        public ServiceDescriptor(Type serviceType, object instance)
        {
            ServiceType = serviceType;
            Instance = instance;
            Lifetime = ServiceLifetime.Singleton;
        }

        public ServiceDescriptor(Type serviceType, Func<IServiceProvider, object> factory, ServiceLifetime lifetime)
        {
            ServiceType = serviceType;
            Factory = factory;
            Lifetime = lifetime;
        }
    }

    /// <summary>
    /// 简单的依赖注入容器
    /// </summary>
    public interface IServiceProvider
    {
        /// <summary>
        /// 获取服务实例
        /// </summary>
        T GetService<T>();

        /// <summary>
        /// 获取服务实例
        /// </summary>
        object GetService(Type serviceType);

        /// <summary>
        /// 获取必需的服务实例
        /// </summary>
        T GetRequiredService<T>();

        /// <summary>
        /// 获取必需的服务实例
        /// </summary>
        object GetRequiredService(Type serviceType);

        /// <summary>
        /// 获取所有指定类型的服务实例
        /// </summary>
        IEnumerable<T> GetServices<T>();

        /// <summary>
        /// 获取所有指定类型的服务实例
        /// </summary>
        IEnumerable<object> GetServices(Type serviceType);
    }

    /// <summary>
    /// 服务容器接口
    /// </summary>
    public interface IServiceContainer : IServiceProvider
    {
        /// <summary>
        /// 注册瞬态服务
        /// </summary>
        IServiceContainer RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface;

        /// <summary>
        /// 注册单例服务
        /// </summary>
        IServiceContainer RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface;

        /// <summary>
        /// 注册单例实例
        /// </summary>
        IServiceContainer RegisterSingleton<TInterface>(TInterface instance)
            where TInterface : class;

        /// <summary>
        /// 注册工厂方法
        /// </summary>
        IServiceContainer RegisterFactory<TInterface>(Func<IServiceProvider, TInterface> factory, ServiceLifetime lifetime = ServiceLifetime.Transient);

        /// <summary>
        /// 检查服务是否已注册
        /// </summary>
        bool IsRegistered<TInterface>();

        /// <summary>
        /// 检查服务是否已注册
        /// </summary>
        bool IsRegistered(Type serviceType);

        /// <summary>
        /// 创建子容器作用域
        /// </summary>
        IServiceScope CreateScope();
    }

    /// <summary>
    /// 服务作用域接口
    /// </summary>
    public interface IServiceScope : IDisposable
    {
        IServiceProvider ServiceProvider { get; }
    }

    /// <summary>
    /// 简单的依赖注入容器实现
    /// </summary>
    public class SimpleServiceContainer : IServiceContainer, IDisposable
    {
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<Type, ServiceDescriptor> _services;
        private readonly ConcurrentDictionary<Type, object> _singletonInstances;
        private readonly object _lockObject = new object();
        private bool _disposed = false;

        public SimpleServiceContainer()
        {
            _logger = LogManager.GetLogger(typeof(SimpleServiceContainer));
            _services = new ConcurrentDictionary<Type, ServiceDescriptor>();
            _singletonInstances = new ConcurrentDictionary<Type, object>();

            // 注册容器本身
            RegisterSingleton<IServiceContainer>(this);
            RegisterSingleton<IServiceProvider>(this);
        }

        public IServiceContainer RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            return RegisterService<TInterface, TImplementation>(ServiceLifetime.Transient);
        }

        public IServiceContainer RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            return RegisterService<TInterface, TImplementation>(ServiceLifetime.Singleton);
        }

        public IServiceContainer RegisterSingleton<TInterface>(TInterface instance)
            where TInterface : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var serviceType = typeof(TInterface);
            var descriptor = new ServiceDescriptor(serviceType, instance);

            _services.AddOrUpdate(serviceType, descriptor, (key, existing) => descriptor);
            _singletonInstances.AddOrUpdate(serviceType, instance, (key, existing) => instance);

            _logger.Debug($"注册单例实例: {serviceType.Name}");
            return this;
        }

        public IServiceContainer RegisterFactory<TInterface>(Func<IServiceProvider, TInterface> factory, ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var serviceType = typeof(TInterface);
            var descriptor = new ServiceDescriptor(serviceType, provider => factory(provider), lifetime);

            _services.AddOrUpdate(serviceType, descriptor, (key, existing) => descriptor);

            _logger.Debug($"注册工厂方法: {serviceType.Name}, 生命周期: {lifetime}");
            return this;
        }

        private IServiceContainer RegisterService<TInterface, TImplementation>(ServiceLifetime lifetime)
            where TImplementation : class, TInterface
        {
            var serviceType = typeof(TInterface);
            var implementationType = typeof(TImplementation);
            var descriptor = new ServiceDescriptor(serviceType, implementationType, lifetime);

            _services.AddOrUpdate(serviceType, descriptor, (key, existing) => descriptor);

            _logger.Debug($"注册服务: {serviceType.Name} -> {implementationType.Name}, 生命周期: {lifetime}");
            return this;
        }

        public T GetService<T>()
        {
            var result = GetService(typeof(T));
            return result != null ? (T)result : default;
        }

        public object GetService(Type serviceType)
        {
            try
            {
                return CreateInstance(serviceType);
            }
            catch (Exception ex)
            {
                _logger.Warning($"获取服务失败: {serviceType.Name}, 错误: {ex.Message}");
                return null;
            }
        }

        public T GetRequiredService<T>()
        {
            var service = GetService<T>();
            if (service == null)
            {
                CADExceptionHandler.ThrowCADException("获取必需服务", $"未找到服务: {typeof(T).Name}");
            }
            return service;
        }

        public object GetRequiredService(Type serviceType)
        {
            var service = GetService(serviceType);
            if (service == null)
            {
                CADExceptionHandler.ThrowCADException("获取必需服务", $"未找到服务: {serviceType.Name}");
            }
            return service;
        }

        public IEnumerable<T> GetServices<T>()
        {
            return GetServices(typeof(T)).Cast<T>();
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            var service = GetService(serviceType);
            return service != null ? new[] { service } : Enumerable.Empty<object>();
        }

        public bool IsRegistered<TInterface>()
        {
            return IsRegistered(typeof(TInterface));
        }

        public bool IsRegistered(Type serviceType)
        {
            return _services.ContainsKey(serviceType);
        }

        public IServiceScope CreateScope()
        {
            return new ServiceScope(this);
        }

        private object CreateInstance(Type serviceType)
        {
            if (!_services.TryGetValue(serviceType, out var descriptor))
            {
                // 尝试自动注册具体类型
                if (serviceType.IsClass && !serviceType.IsAbstract)
                {
                    return CreateInstanceByReflection(serviceType);
                }
                return null;
            }

            switch (descriptor.Lifetime)
            {
                case ServiceLifetime.Singleton:
                    return _singletonInstances.GetOrAdd(serviceType, _ => CreateServiceInstance(descriptor));

                case ServiceLifetime.Transient:
                    return CreateServiceInstance(descriptor);

                case ServiceLifetime.Scoped:
                    // 简化实现：作用域服务按瞬态处理
                    return CreateServiceInstance(descriptor);

                default:
                    throw new InvalidOperationException($"未知的服务生命周期: {descriptor.Lifetime}");
            }
        }

        private object CreateServiceInstance(ServiceDescriptor descriptor)
        {
            if (descriptor.Instance != null)
            {
                return descriptor.Instance;
            }

            if (descriptor.Factory != null)
            {
                return descriptor.Factory(this);
            }

            if (descriptor.ImplementationType != null)
            {
                return CreateInstanceByReflection(descriptor.ImplementationType);
            }

            throw new InvalidOperationException($"无法创建服务实例: {descriptor.ServiceType.Name}");
        }

        private object CreateInstanceByReflection(Type implementationType)
        {
            var constructors = implementationType.GetConstructors();
            var constructor = constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();

            if (constructor == null)
            {
                throw new InvalidOperationException($"类型 {implementationType.Name} 没有可用的构造函数");
            }

            var parameters = constructor.GetParameters();
            var parameterInstances = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var parameterType = parameters[i].ParameterType;
                parameterInstances[i] = GetService(parameterType);

                if (parameterInstances[i] == null && !parameters[i].HasDefaultValue)
                {
                    throw new InvalidOperationException($"无法解析参数 {parameters[i].Name} (类型: {parameterType.Name})");
                }
            }

            try
            {
                var instance = Activator.CreateInstance(implementationType, parameterInstances);
                _logger.Debug($"通过反射创建实例: {implementationType.Name}");
                return instance;
            }
            catch (Exception ex)
            {
                _logger.Error($"创建实例失败: {implementationType.Name}", ex);
                throw;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_lockObject)
                {
                    if (!_disposed)
                    {
                        // 释放单例实例
                        foreach (var instance in _singletonInstances.Values)
                        {
                            if (instance is IDisposable disposable)
                            {
                                try
                                {
                                    disposable.Dispose();
                                }
                                catch (Exception ex)
                                {
                                    _logger.Warning($"释放服务实例时发生异常: {ex.Message}");
                                }
                            }
                        }

                        _singletonInstances.Clear();
                        _services.Clear();
                        _disposed = true;

                        _logger.Debug("服务容器已释放");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 服务作用域实现
    /// </summary>
    internal class ServiceScope : IServiceScope, IServiceProvider
    {
        private readonly IServiceProvider _parentProvider;
        private readonly Dictionary<Type, object> _scopedServices;
        private bool _disposed = false;

        public IServiceProvider ServiceProvider => this;

        internal ServiceScope(IServiceProvider parentProvider)
        {
            _parentProvider = parentProvider;
            _scopedServices = new Dictionary<Type, object>();
        }

        public object GetService(Type serviceType)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ServiceScope));

            if (_scopedServices.TryGetValue(serviceType, out var service))
                return service;

            service = _parentProvider.GetService(serviceType);
            if (service != null)
            {
                _scopedServices[serviceType] = service;
            }

            return service;
        }

        public T GetService<T>()
        {
            return (T)GetService(typeof(T));
        }

        public T GetRequiredService<T>()
        {
            var service = GetService<T>();
            if (service == null)
                throw new InvalidOperationException($"Required service of type {typeof(T).Name} is not registered.");
            return service;
        }

        public object GetRequiredService(Type serviceType)
        {
            var service = GetService(serviceType);
            if (service == null)
                throw new InvalidOperationException($"Required service of type {serviceType.Name} is not registered.");
            return service;
        }

        public IEnumerable<T> GetServices<T>()
        {
            return GetServices(typeof(T)).Cast<T>();
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            var service = GetService(serviceType);
            return service != null ? new[] { service } : Enumerable.Empty<object>();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var service in _scopedServices.Values)
                {
                    if (service is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

                _scopedServices.Clear();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 全局服务容器
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Lazy<SimpleServiceContainer> _container =
            new Lazy<SimpleServiceContainer>(() => new SimpleServiceContainer());

        public static IServiceContainer Current => _container.Value;

        /// <summary>
        /// 配置服务
        /// </summary>
        public static void ConfigureServices(Action<IServiceContainer> configure)
        {
            configure?.Invoke(Current);
        }

        /// <summary>
        /// 获取服务
        /// </summary>
        public static T GetService<T>()
        {
            return Current.GetService<T>();
        }

        /// <summary>
        /// 获取必需的服务
        /// </summary>
        public static T GetRequiredService<T>()
        {
            return Current.GetRequiredService<T>();
        }
    }

    /// <summary>
    /// 服务容器静态访问类
    /// </summary>
    public static class ServiceContainer
    {
        /// <summary>
        /// 全局服务容器实例
        /// </summary>
        public static IServiceContainer Instance => ServiceLocator.Current;
    }
}