namespace DotNetARX.DependencyInjection
{
    /// <summary>
    /// 智能服务定位器
    /// 自动选择最佳的依赖注入容器实现
    /// </summary>
    public static class SmartServiceLocator
    {
        private static IServiceContainer _current;
        private static readonly object _lock = new object();
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(SmartServiceLocator));
        private static ContainerType _containerType = ContainerType.Auto;

        /// <summary>
        /// 容器类型枚举
        /// </summary>
        public enum ContainerType
        {
            Auto,      // 自动选择
            Autofac,   // 强制使用Autofac
            Simple     // 强制使用简单容器
        }

        /// <summary>
        /// 当前服务容器
        /// </summary>
        public static IServiceContainer Current
        {
            get
            {
                if (_current == null)
                {
                    lock (_lock)
                    {
                        if (_current == null)
                        {
                            _current = CreateOptimalContainer();
                        }
                    }
                }
                return _current;
            }
        }

        /// <summary>
        /// 设置容器类型偏好
        /// </summary>
        /// <param name="containerType">容器类型</param>
        /// <param name="force">是否强制重新创建容器</param>
        public static void SetContainerType(ContainerType containerType, bool force = false)
        {
            _containerType = containerType;

            if (force && _current != null)
            {
                lock (_lock)
                {
                    var oldContainer = _current;
                    _current = CreateOptimalContainer();

                    // 安全释放旧容器
                    try
                    {
                        (oldContainer as IDisposable)?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"释放旧容器时发生警告: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 重置服务定位器
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                try
                {
                    (_current as IDisposable)?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.Warning($"重置时释放容器发生警告: {ex.Message}");
                }
                finally
                {
                    _current = null;
                }
            }
        }

        /// <summary>
        /// 获取容器信息
        /// </summary>
        public static string GetContainerInfo()
        {
            var container = Current;
            var type = container.GetType().Name;

            var info = $"容器类型: {type}";

            if (container is AutofacServiceContainer autofacContainer)
            {
                try
                {
                    var stats = autofacContainer.GetStatistics();
                    info += $", 已注册服务: {stats.RegisteredServicesCount}, 已构建: {stats.IsBuilt}";
                }
                catch
                {
                    // 忽略统计获取失败
                }
            }

            return info;
        }

        /// <summary>
        /// 创建最优容器
        /// </summary>
        private static IServiceContainer CreateOptimalContainer()
        {
            try
            {
                switch (_containerType)
                {
                    case ContainerType.Autofac:
                        return CreateAutofacContainer();

                    case ContainerType.Simple:
                        return CreateSimpleContainer();

                    case ContainerType.Auto:
                    default:
                        // 自动选择：优先尝试Autofac，失败则回退到简单容器
                        try
                        {
                            var container = CreateAutofacContainer();
                            _logger.Info("使用Autofac高性能容器");
                            return container;
                        }
                        catch (Exception ex)
                        {
                            _logger.Warning($"Autofac容器创建失败，回退到简单容器: {ex.Message}");
                            return CreateSimpleContainer();
                        }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("创建容器失败，使用最基础的简单容器", ex);
                return new SimpleServiceContainer();
            }
        }

        /// <summary>
        /// 创建Autofac容器
        /// </summary>
        private static IServiceContainer CreateAutofacContainer()
        {
            // 检查Autofac是否可用
            var autofacType = Type.GetType("Autofac.ContainerBuilder, Autofac");
            if (autofacType == null)
            {
                throw new InvalidOperationException("Autofac未安装或不可用");
            }

            return new AutofacServiceContainer();
        }

        /// <summary>
        /// 创建简单容器
        /// </summary>
        private static IServiceContainer CreateSimpleContainer()
        {
            _logger.Info("使用简单内置容器");
            return new SimpleServiceContainer();
        }
    }

    /// <summary>
    /// 服务定位器扩展方法
    /// </summary>
    public static class ServiceLocatorExtensions
    {
        /// <summary>
        /// 快速服务解析
        /// </summary>
        public static T Resolve<T>(this IServiceContainer container)
        {
            return container.GetRequiredService<T>();
        }

        /// <summary>
        /// 安全服务解析
        /// </summary>
        public static T TryResolve<T>(this IServiceContainer container, T defaultValue = default(T))
        {
            try
            {
                var service = container.GetService<T>();
                return service != null ? service : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 条件注册
        /// </summary>
        public static IServiceContainer RegisterIf<TInterface, TImplementation>(
            this IServiceContainer container,
            bool condition,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TImplementation : class, TInterface
        {
            if (!condition) return container;

            return lifetime switch
            {
                ServiceLifetime.Singleton => container.RegisterSingleton<TInterface, TImplementation>(),
                ServiceLifetime.Transient => container.RegisterTransient<TInterface, TImplementation>(),
                _ => container.RegisterTransient<TInterface, TImplementation>()
            };
        }

        /// <summary>
        /// 批量注册服务
        /// </summary>
        public static IServiceContainer RegisterServices(
            this IServiceContainer container,
            params (Type serviceType, Type implementationType, ServiceLifetime lifetime)[] services)
        {
            foreach (var (serviceType, implementationType, lifetime) in services)
            {
                switch (lifetime)
                {
                    case ServiceLifetime.Singleton:
                        container.GetType()
                            .GetMethod(nameof(IServiceContainer.RegisterSingleton))
                            ?.MakeGenericMethod(serviceType, implementationType)
                            ?.Invoke(container, null);
                        break;

                    case ServiceLifetime.Transient:
                        container.GetType()
                            .GetMethod(nameof(IServiceContainer.RegisterTransient))
                            ?.MakeGenericMethod(serviceType, implementationType)
                            ?.Invoke(container, null);
                        break;
                }
            }

            return container;
        }
    }

    /// <summary>
    /// DotNetARX服务注册扩展
    /// </summary>
    public static class DotNetARXServiceExtensions
    {
        /// <summary>
        /// 注册DotNetARX核心服务
        /// </summary>
        public static IServiceContainer RegisterDotNetARXServices(this IServiceContainer container)
        {
            var logger = LogManager.GetLogger(typeof(DotNetARXServiceExtensions));
            logger.Info("开始注册DotNetARX核心服务");

            try
            {
                // 核心基础设施
                container.RegisterSingleton<ILogger>(LogManager.DefaultLogger);
                container.RegisterSingleton<IConfigurationManager>(GlobalConfiguration.Instance);

                // 性能监控 (如果可用)
                if (IsTypeAvailable("DotNetARX.Performance.GlobalPerformanceMonitor"))
                {
                    container.RegisterSingleton<IPerformanceMonitor>(GlobalPerformanceMonitor.Instance);
                }

                // 事件系统 (如果可用)
                if (IsTypeAvailable("DotNetARX.Events.CADEventManager"))
                {
                    container.RegisterSingleton<IEventPublisher>(CADEventManager.Publisher);
                }

                // 核心服务
                RegisterCoreServices(container);

                logger.Info("DotNetARX核心服务注册完成");
                return container;
            }
            catch (Exception ex)
            {
                logger.Error("注册DotNetARX核心服务失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 注册核心业务服务
        /// </summary>
        private static void RegisterCoreServices(IServiceContainer container)
        {
            // 实体操作服务
            if (IsTypeAvailable("DotNetARX.Services.EntityService"))
            {
                container.RegisterTransient<IEntityService, Services.EntityService>();
            }

            // 图层管理服务
            if (IsTypeAvailable("DotNetARX.Services.LayerManagerService"))
            {
                container.RegisterTransient<ILayerManager, Services.LayerManagerService>();
            }

            // 其他核心服务可以在这里继续添加...

        }

        /// <summary>
        /// 检查类型是否可用
        /// </summary>
        private static bool IsTypeAvailable(string typeName)
        {
            try
            {
                return Type.GetType(typeName) != null;
            }
            catch
            {
                return false;
            }
        }
    }
}