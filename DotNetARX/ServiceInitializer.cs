using DotNetARX.DependencyInjection;
using DotNetARX.Events;
using DotNetARX.Services;

namespace DotNetARX
{
    /// <summary>
    /// DotNetARX核心服务初始化器
    /// </summary>
    public static class ServiceInitializer
    {
        private static bool _initialized = false;
        private static readonly object _lockObject = new object();

        /// <summary>
        /// 初始化DotNetARX核心服务
        /// </summary>
        public static void Initialize(Action<IServiceContainer> additionalConfiguration = null)
        {
            lock (_lockObject)
            {
                if (_initialized)
                {
                    LogManager.DefaultLogger.Warning("DotNetARX服务已经初始化，跳过重复初始化");
                    return;
                }

                try
                {
                    var container = ServiceLocator.Current;

                    // 注册核心服务
                    RegisterCoreServices(container);

                    // 注册业务服务
                    RegisterBusinessServices(container);

                    // 执行额外的配置
                    additionalConfiguration?.Invoke(container);

                    // 初始化性能监控
                    InitializePerformanceMonitoring();

                    _initialized = true;

                    LogManager.DefaultLogger.Info("DotNetARX核心服务初始化完成");

                    // 记录初始化性能指标
                    GlobalPerformanceMonitor.IncrementCounter("ServiceInitialization", "System");
                }
                catch (Exception ex)
                {
                    LogManager.DefaultLogger.Error("DotNetARX核心服务初始化失败", ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// 注册核心服务
        /// </summary>
        private static void RegisterCoreServices(IServiceContainer container)
        {
            // 日志服务
            container.RegisterSingleton(LogManager.DefaultLogger);

            // 配置管理服务
            container.RegisterSingleton(GlobalConfiguration.Instance);

            // 性能监控服务
            container.RegisterSingleton(GlobalPerformanceMonitor.Instance);

            // 事件系统服务
            container.RegisterSingleton<IEventBus>(CADEventManager.DefaultBus);
            container.RegisterSingleton<IEventPublisher>(CADEventManager.Publisher);
        }

        /// <summary>
        /// 注册业务服务
        /// </summary>
        private static void RegisterBusinessServices(IServiceContainer container)
        {
            // 实体操作服务
            container.RegisterTransient<IEntityOperations, EntityOperationService>();

            // 图层管理服务
            container.RegisterTransient<ILayerManager, LayerManagerService>();

            // 选择操作服务
            container.RegisterTransient<ISelectionService, SelectionService>();

            // 数据库操作服务
            container.RegisterTransient<IDatabaseOperations, DatabaseOperationsService>();

            // 绘图操作服务
            container.RegisterTransient<IDrawingOperations, DrawingOperationsService>();

            // 块操作服务
            container.RegisterTransient<IBlockOperations, BlockOperationsService>();

            // 进度管理器
            container.RegisterTransient<IProgressManager, ProgressManagerService>();

            // 命令操作服务
            container.RegisterTransient<ICommandService, CommandService>();

            // 文档操作服务
            container.RegisterTransient<IDocumentService, DocumentService>();

            // 几何工具服务
            container.RegisterTransient<IGeometryService, GeometryService>();

            // 样式管理服务
            container.RegisterTransient<IStyleService, StyleService>();

            // 表格操作服务
            container.RegisterTransient<ITableService, TableService>();

            // 布局操作服务
            container.RegisterTransient<ILayoutService, LayoutService>();

            // 用户界面服务
            container.RegisterTransient<IUIService, UIService>();

            // 工具服务
            container.RegisterTransient<IUtilityService, UtilityService>();

            LogManager.DefaultLogger.Info("所有业务服务注册完成");
        }

        /// <summary>
        /// 初始化性能监控
        /// </summary>
        private static void InitializePerformanceMonitoring()
        {
            var config = GlobalConfiguration.Instance;
            var enableMonitoring = config.GetSetting(ConfigurationKeys.EnablePerformanceMonitoring, true);

            if (enableMonitoring)
            {
                LogManager.DefaultLogger.Info("性能监控已启用");

                // 设置默认的性能监控配置
                config.SetSetting("Performance.ReportInterval", 300000); // 5分钟报告间隔
                config.SetSetting("Performance.MaxMetricsPerKey", 1000);  // 每个指标最多保留1000条记录
            }
            else
            {
                LogManager.DefaultLogger.Info("性能监控已禁用");
            }
        }

        /// <summary>
        /// 检查服务是否已初始化
        /// </summary>
        public static bool IsInitialized => _initialized;

        /// <summary>
        /// 获取服务实例
        /// </summary>
        public static T GetService<T>()
        {
            EnsureInitialized();
            return ServiceLocator.GetService<T>();
        }

        /// <summary>
        /// 获取必需的服务实例
        /// </summary>
        public static T GetRequiredService<T>()
        {
            EnsureInitialized();
            return ServiceLocator.GetRequiredService<T>();
        }

        /// <summary>
        /// 确保服务已初始化
        /// </summary>
        private static void EnsureInitialized()
        {
            if (!_initialized)
            {
                Initialize();
            }
        }

        /// <summary>
        /// 重置服务（主要用于测试）
        /// </summary>
        public static void Reset()
        {
            lock (_lockObject)
            {
                _initialized = false;
                LogManager.DefaultLogger.Info("DotNetARX核心服务已重置");
            }
        }

        /// <summary>
        /// 生成系统信息报告
        /// </summary>
        public static string GenerateSystemReport()
        {
            EnsureInitialized();

            var report = new System.Text.StringBuilder();

            report.AppendLine("=== DotNetARX 系统状态报告 ===");
            report.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"服务初始化状态: {(_initialized ? "已初始化" : "未初始化")}");
            report.AppendLine();

            // 配置信息
            try
            {
                var config = ServiceLocator.GetService<IConfigurationManager>();
                if (config != null)
                {
                    report.AppendLine("=== 配置信息 ===");
                    report.AppendLine($"默认批处理大小: {config.GetSetting(ConfigurationKeys.DefaultBatchSize, "未设置")}");
                    report.AppendLine($"日志级别: {config.GetSetting(ConfigurationKeys.LogLevel, "未设置")}");
                    report.AppendLine($"性能监控: {config.GetSetting(ConfigurationKeys.EnablePerformanceMonitoring, "未设置")}");
                    report.AppendLine();
                }
            }
            catch (Exception ex)
            {
                report.AppendLine($"配置信息获取失败: {ex.Message}");
            }

            // 性能信息
            try
            {
                var perfReport = GlobalPerformanceMonitor.GenerateReport();
                report.AppendLine(perfReport);
            }
            catch (Exception ex)
            {
                report.AppendLine($"性能报告生成失败: {ex.Message}");
            }

            // 系统信息
            report.AppendLine("=== 系统信息 ===");
            report.AppendLine($"CLR版本: {Environment.Version}");
            report.AppendLine($"操作系统: {Environment.OSVersion}");
            report.AppendLine($"工作集内存: {Environment.WorkingSet / (1024 * 1024)} MB");
            report.AppendLine($"GC内存: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
            report.AppendLine($"处理器数量: {Environment.ProcessorCount}");

            return report.ToString();
        }
    }

    /// <summary>
    /// DotNetARX核心扩展方法
    /// </summary>
    public static class DotNetARXExtensions
    {
        /// <summary>
        /// 使用性能监控执行操作
        /// </summary>
        public static T WithPerformanceMonitoring<T>(this Func<T> operation, string operationName, string category = null)
        {
            using (GlobalPerformanceMonitor.StartTimer(operationName, category))
            {
                try
                {
                    var result = operation();
                    GlobalPerformanceMonitor.IncrementCounter($"{operationName}_Success", category);
                    return result;
                }
                catch
                {
                    GlobalPerformanceMonitor.IncrementErrorCounter(operationName, category);
                    throw;
                }
            }
        }

        /// <summary>
        /// 使用性能监控执行操作（无返回值版本）
        /// </summary>
        public static void WithPerformanceMonitoring(this Action operation, string operationName, string category = null)
        {
            using (GlobalPerformanceMonitor.StartTimer(operationName, category))
            {
                try
                {
                    operation();
                    GlobalPerformanceMonitor.IncrementCounter($"{operationName}_Success", category);
                }
                catch
                {
                    GlobalPerformanceMonitor.IncrementErrorCounter(operationName, category);
                    throw;
                }
            }
        }
    }
}