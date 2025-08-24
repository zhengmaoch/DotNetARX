using Autofac;

namespace DotNetARX.DependencyInjection
{
    /// <summary>
    /// 基于Autofac的高性能服务容器
    /// 提供更快的服务解析和更丰富的功能
    /// </summary>
    public class AutofacServiceContainer : IServiceContainer, IDisposable
    {
        private readonly ContainerBuilder _builder;
        private IContainer _container;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<Type, bool> _registrationCache;
        private bool _isBuilt = false;
        private bool _disposed = false;

        public AutofacServiceContainer()
        {
            _builder = new ContainerBuilder();
            _logger = LogManager.GetLogger(typeof(AutofacServiceContainer));
            _registrationCache = new ConcurrentDictionary<Type, bool>();

            // 注册容器本身
            _builder.RegisterInstance(this).As<IServiceContainer>().SingleInstance();
            _builder.Register(ctx => ctx.Resolve<IServiceContainer>()).As<IServiceProvider>().SingleInstance();

            _logger.Info("Autofac服务容器初始化完成");
        }

        #region 服务注册

        public IServiceContainer RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            EnsureNotBuilt();

            _builder.RegisterType<TImplementation>()
                   .As<TInterface>()
                   .InstancePerDependency(); // Autofac中的瞬态模式

            _registrationCache.TryAdd(typeof(TInterface), true);
            _logger.Debug($"注册瞬态服务: {typeof(TInterface).Name} -> {typeof(TImplementation).Name}");

            return this;
        }

        public IServiceContainer RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            EnsureNotBuilt();

            _builder.RegisterType<TImplementation>()
                   .As<TInterface>()
                   .SingleInstance(); // Autofac中的单例模式

            _registrationCache.TryAdd(typeof(TInterface), true);
            _logger.Debug($"注册单例服务: {typeof(TInterface).Name} -> {typeof(TImplementation).Name}");

            return this;
        }

        public IServiceContainer RegisterSingleton<TInterface>(TInterface instance)
        {
            EnsureNotBuilt();

            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            _builder.RegisterInstance(instance).As<TInterface>();
            _registrationCache.TryAdd(typeof(TInterface), true);
            _logger.Debug($"注册单例实例: {typeof(TInterface).Name}");

            return this;
        }

        public IServiceContainer RegisterFactory<TInterface>(Func<IServiceProvider, TInterface> factory, ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            EnsureNotBuilt();

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var registration = _builder.Register(ctx => factory(ctx.Resolve<IServiceProvider>()))
                                     .As<TInterface>();

            // 根据生命周期设置Autofac的实例策略
            switch (lifetime)
            {
                case ServiceLifetime.Singleton:
                    registration.SingleInstance();
                    break;

                case ServiceLifetime.Scoped:
                    registration.InstancePerLifetimeScope();
                    break;

                case ServiceLifetime.Transient:
                default:
                    registration.InstancePerDependency();
                    break;
            }

            _registrationCache.TryAdd(typeof(TInterface), true);
            _logger.Debug($"注册工厂方法: {typeof(TInterface).Name} (生命周期: {lifetime})");

            return this;
        }

        /// <summary>
        /// 注册作用域服务 - Autofac扩展
        /// </summary>
        public IServiceContainer RegisterScoped<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            EnsureNotBuilt();

            _builder.RegisterType<TImplementation>()
                   .As<TInterface>()
                   .InstancePerLifetimeScope(); // Autofac中的作用域模式

            _registrationCache.TryAdd(typeof(TInterface), true);
            _logger.Debug($"注册作用域服务: {typeof(TInterface).Name} -> {typeof(TImplementation).Name}");

            return this;
        }

        /// <summary>
        /// 批量注册程序集中的服务 - 高性能反射
        /// </summary>
        public IServiceContainer RegisterAssemblyTypes(Assembly assembly, Func<Type, bool> predicate = null)
        {
            EnsureNotBuilt();

            var types = assembly.GetTypes().Where(t => predicate?.Invoke(t) ?? true);

            foreach (var type in types)
            {
                var interfaces = type.GetInterfaces();
                if (interfaces.Length > 0)
                {
                    var registration = _builder.RegisterType(type);

                    foreach (var @interface in interfaces)
                    {
                        registration.As(@interface);
                        _registrationCache.TryAdd(@interface, true);
                    }

                    registration.InstancePerDependency();
                }
            }

            _logger.Info($"批量注册程序集服务: {assembly.GetName().Name}");
            return this;
        }

        #endregion 服务注册

        #region 服务解析

        public T GetService<T>()
        {
            EnsureBuilt();

            try
            {
                return _container.Resolve<T>();
            }
            catch (ComponentNotRegisteredException)
            {
                return default(T);
            }
            catch (Exception ex)
            {
                _logger.Error($"解析服务失败: {typeof(T).Name}", ex);
                return default(T);
            }
        }

        public object GetService(Type serviceType)
        {
            EnsureBuilt();

            try
            {
                return _container.Resolve(serviceType);
            }
            catch (ComponentNotRegisteredException)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error($"解析服务失败: {serviceType.Name}", ex);
                return null;
            }
        }

        public T GetRequiredService<T>()
        {
            EnsureBuilt();

            try
            {
                return _container.Resolve<T>();
            }
            catch (Exception ex)
            {
                _logger.Error($"解析必需服务失败: {typeof(T).Name}", ex);
                throw new InvalidOperationException($"服务 {typeof(T).Name} 未注册", ex);
            }
        }

        public object GetRequiredService(Type serviceType)
        {
            EnsureBuilt();

            try
            {
                return _container.Resolve(serviceType);
            }
            catch (Exception ex)
            {
                _logger.Error($"解析必需服务失败: {serviceType.Name}", ex);
                throw new InvalidOperationException($"服务 {serviceType.Name} 未注册", ex);
            }
        }

        public IEnumerable<T> GetServices<T>()
        {
            EnsureBuilt();

            try
            {
                return _container.Resolve<IEnumerable<T>>();
            }
            catch (Exception ex)
            {
                _logger.Error($"解析服务集合失败: {typeof(T).Name}", ex);
                return Enumerable.Empty<T>();
            }
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            EnsureBuilt();

            try
            {
                var enumerableType = typeof(IEnumerable<>).MakeGenericType(serviceType);
                return (IEnumerable<object>)_container.Resolve(enumerableType);
            }
            catch (Exception ex)
            {
                _logger.Error($"解析服务集合失败: {serviceType.Name}", ex);
                return Enumerable.Empty<object>();
            }
        }

        #endregion 服务解析

        #region 高级功能

        public bool IsRegistered<TInterface>()
        {
            return IsRegistered(typeof(TInterface));
        }

        public bool IsRegistered(Type serviceType)
        {
            // 使用缓存快速检查
            if (_registrationCache.TryGetValue(serviceType, out var cached))
                return cached;

            if (_isBuilt)
            {
                var isRegistered = _container.IsRegistered(serviceType);
                _registrationCache.TryAdd(serviceType, isRegistered);
                return isRegistered;
            }

            return false;
        }

        public IServiceScope CreateScope()
        {
            EnsureBuilt();
            return new AutofacServiceScope(_container.BeginLifetimeScope());
        }

        /// <summary>
        /// 构建容器 - 优化性能
        /// </summary>
        public void Build()
        {
            if (_isBuilt) return;

            try
            {
                _container = _builder.Build();
                _isBuilt = true;
                _logger.Info("Autofac容器构建完成");
            }
            catch (Exception ex)
            {
                _logger.Error("Autofac容器构建失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 获取容器统计信息
        /// </summary>
        public ContainerStatistics GetStatistics()
        {
            EnsureBuilt();

            return new ContainerStatistics
            {
                RegisteredServicesCount = _registrationCache.Count,
                IsBuilt = _isBuilt,
                ContainerType = "Autofac"
            };
        }

        #endregion 高级功能

        #region 内部辅助方法

        private void EnsureNotBuilt()
        {
            if (_isBuilt)
                throw new InvalidOperationException("容器已构建，无法继续注册服务");
        }

        private void EnsureBuilt()
        {
            if (!_isBuilt)
            {
                Build();
            }
        }

        #endregion 内部辅助方法

        #region IDisposable实现

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    _container?.Dispose();
                    _registrationCache.Clear();
                    _logger.Info("Autofac服务容器已释放");
                }
                catch (Exception ex)
                {
                    _logger.Error("释放Autofac容器时发生错误", ex);
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        #endregion IDisposable实现
    }

    /// <summary>
    /// Autofac服务作用域
    /// </summary>
    public class AutofacServiceScope : IServiceScope
    {
        private readonly ILifetimeScope _scope;
        private readonly AutofacScopeServiceProvider _serviceProvider;
        private bool _disposed = false;

        public AutofacServiceScope(ILifetimeScope scope)
        {
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
            _serviceProvider = new AutofacScopeServiceProvider(_scope);
        }

        public IServiceProvider ServiceProvider => _serviceProvider;

        public void Dispose()
        {
            if (!_disposed)
            {
                _scope?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Autofac作用域服务提供者
    /// </summary>
    public class AutofacScopeServiceProvider : IServiceProvider
    {
        private readonly ILifetimeScope _scope;

        public AutofacScopeServiceProvider(ILifetimeScope scope)
        {
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        }

        public T GetService<T>()
        {
            try
            {
                return _scope.Resolve<T>();
            }
            catch (ComponentNotRegisteredException)
            {
                return default(T);
            }
        }

        public object GetService(Type serviceType)
        {
            try
            {
                return _scope.Resolve(serviceType);
            }
            catch (ComponentNotRegisteredException)
            {
                return null;
            }
        }

        public T GetRequiredService<T>()
        {
            return _scope.Resolve<T>();
        }

        public object GetRequiredService(Type serviceType)
        {
            return _scope.Resolve(serviceType);
        }

        public IEnumerable<T> GetServices<T>()
        {
            return _scope.Resolve<IEnumerable<T>>();
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(serviceType);
            return (IEnumerable<object>)_scope.Resolve(enumerableType);
        }
    }

    /// <summary>
    /// 容器统计信息
    /// </summary>
    public class ContainerStatistics
    {
        public int RegisteredServicesCount { get; set; }
        public bool IsBuilt { get; set; }
        public string ContainerType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}