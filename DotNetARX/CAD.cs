namespace DotNetARX
{
    /// <summary>
    /// DotNetARX 终极统一API
    /// 易用性与高性能的完美结合 - 默认即最优实现
    /// </summary>
    public static partial class CAD
    {
        private static readonly Lazy<bool> _initialized = new(() =>
        {
            InitializeCADSystem();
            return true;
        });

        /// <summary>
        /// 确保系统已初始化
        /// </summary>
        private static void EnsureInitialized() => _ = _initialized.Value;

        /// <summary>
        /// 初始化CAD系统
        /// </summary>
        private static void InitializeCADSystem()
        {
            // 初始化智能服务定位器
            var container = SmartServiceLocator.Current;

            // 注册DotNetARX核心服务
            container.RegisterDotNetARXServices();

            // 初始化性能监控
            PerformanceEngine.Initialize();

            // 初始化智能上下文管理器
            AutoCADContext.Initialize();

            // 初始化日志系统
            LogManager.Initialize();

            // 记录容器信息
            var logger = LogManager.GetLogger(typeof(CAD));
            logger.Info($"CAD系统初始化完成 - {SmartServiceLocator.GetContainerInfo()}");
        }

        // 实体操作已移至 CAD.Entity.cs 部分类

        // 绘图操作已移至 CAD.Entity.cs 部分类

        // 图层操作已移至 CAD.Layer.cs 部分类

        // 选择操作已移至 CAD.Selection.cs 部分类

        #region 内部辅助方法

        /// <summary>
        /// 添加实体到当前空间 - 内联优化
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ObjectId AddToCurrentSpace(Entity entity)
        {
            var context = AutoCADContext.Current;
            var currentSpace = context.GetObject<BlockTableRecord>(
                context.Database.GetCurrentSpaceId(), OpenMode.ForWrite);

            var id = currentSpace.AppendEntity(entity);
            context.Transaction.AddNewlyCreatedDBObject(entity, true);
            return id;
        }

        #endregion 内部辅助方法

        #region 系统管理和监控

        /// <summary>
        /// 获取性能报告
        /// </summary>
        public static string GetPerformanceReport()
        {
            EnsureInitialized();
            return PerformanceEngine.GenerateReport();
        }

        /// <summary>
        /// 获取详细性能分析报告
        /// </summary>
        public static async Task<string> GetDetailedPerformanceReport(TimeSpan? timeRange = null)
        {
            EnsureInitialized();
            return await PerformanceTestRunner.GeneratePerformanceReport(timeRange);
        }

        /// <summary>
        /// 运行性能基准测试
        /// </summary>
        public static async Task<PerformanceTestReport> RunBenchmarks(bool fastMode = true)
        {
            EnsureInitialized();
            return await PerformanceTestRunner.RunAllBenchmarks(fastMode);
        }

        /// <summary>
        /// 检查性能回归
        /// </summary>
        public static async Task<RegressionTestReport> CheckPerformanceRegression()
        {
            EnsureInitialized();
            return await PerformanceTestRunner.RunRegressionTests();
        }

        /// <summary>
        /// 获取性能建议
        /// </summary>
        public static List<PerformanceRecommendation> GetPerformanceRecommendations()
        {
            EnsureInitialized();
            return AutoPerformanceMonitor.Instance.GetRecommendations();
        }

        /// <summary>
        /// 获取系统性能摘要
        /// </summary>
        public static SystemPerformanceSummary GetSystemPerformanceSummary()
        {
            EnsureInitialized();
            return AutoPerformanceMonitor.Instance.GetSystemSummary();
        }

        /// <summary>
        /// 运行智能诊断
        /// </summary>
        public static DiagnosticReport RunSystemDiagnostic()
        {
            EnsureInitialized();
            return SmartDiagnostics.RunFullDiagnostic();
        }

        /// <summary>
        /// 检查系统健康状态
        /// </summary>
        public static HealthCheckResult CheckSystemHealth()
        {
            EnsureInitialized();
            return SmartDiagnostics.CheckSystemHealth();
        }

        /// <summary>
        /// 获取智能代码建议
        /// </summary>
        public static List<CodeSuggestion> GetCodeSuggestions(string operationName, object[] parameters = null)
        {
            EnsureInitialized();
            return SmartDiagnostics.GetCodeSuggestions(operationName, parameters);
        }

        /// <summary>
        /// 获取自动修复建议
        /// </summary>
        public static List<AutoFixSuggestion> GetAutoFixSuggestions()
        {
            EnsureInitialized();
            return SmartDiagnostics.GetAutoFixSuggestions();
        }

        /// <summary>
        /// 获取代码补全建议
        /// </summary>
        public static List<CompletionItem> GetCodeCompletions(string context, string input)
        {
            EnsureInitialized();
            return IntelliSenseHelper.GetCompletions(context, input);
        }

        /// <summary>
        /// 获取方法签名帮助
        /// </summary>
        public static SignatureHelp GetSignatureHelp(string methodName)
        {
            EnsureInitialized();
            return IntelliSenseHelper.GetSignatureHelp(methodName);
        }

        /// <summary>
        /// 生成代码片段
        /// </summary>
        public static string GenerateCodeSnippet(string templateName, Dictionary<string, string> parameters = null)
        {
            EnsureInitialized();
            return IntelliSenseHelper.GenerateCodeSnippet(templateName, parameters);
        }

        /// <summary>
        /// 分析代码质量
        /// </summary>
        public static CodeQualityReport AnalyzeCodeQuality(string code)
        {
            EnsureInitialized();
            return CodeQualityAnalyzer.AnalyzeCode(code);
        }

        /// <summary>
        /// 获取配置设置
        /// </summary>
        public static T GetConfiguration<T>(string key, T defaultValue = default)
        {
            return ConfigurationProvider.Get(key, defaultValue);
        }

        /// <summary>
        /// 设置配置值
        /// </summary>
        public static void SetConfiguration<T>(string key, T value)
        {
            ConfigurationProvider.Set(key, value);
        }

        /// <summary>
        /// 获取配置摘要
        /// </summary>
        public static ConfigurationSummary GetConfigurationSummary()
        {
            return ConfigurationProvider.GetSummary();
        }

        /// <summary>
        /// 生成配置报告
        /// </summary>
        public static string GetConfigurationReport()
        {
            return ConfigurationHelper.GenerateConfigurationReport();
        }

        /// <summary>
        /// 应用性能优化配置
        /// </summary>
        public static void ApplyPerformanceOptimizations()
        {
            ConfigurationHelper.ApplyPerformanceOptimizations();
        }

        /// <summary>
        /// 应用开发环境配置
        /// </summary>
        public static void ApplyDevelopmentConfiguration()
        {
            ConfigurationHelper.ApplyDevelopmentConfiguration();
        }

        /// <summary>
        /// 应用生产环境配置
        /// </summary>
        public static void ApplyProductionConfiguration()
        {
            ConfigurationHelper.ApplyProductionConfiguration();
        }

        /// <summary>
        /// 重置性能统计
        /// </summary>
        public static void ResetPerformanceMetrics()
        {
            EnsureInitialized();
            PerformanceEngine.Reset();
            AutoPerformanceMonitor.Instance.ResetMetrics();
            PerformanceAnalyzer.CleanupOldData();
        }

        /// <summary>
        /// 获取系统状态
        /// </summary>
        public static bool IsInitialized => _initialized.IsValueCreated && _initialized.Value;

        #endregion 系统管理和监控
    }
}