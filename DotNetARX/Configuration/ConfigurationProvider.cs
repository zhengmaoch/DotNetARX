namespace DotNetARX.Configuration
{
    /// <summary>
    /// 配置提供者工厂
    /// 统一管理配置系统，提供便捷的配置访问入口
    /// </summary>
    public static class ConfigurationProvider
    {
        private static readonly Lazy<EnhancedConfigurationManager> _instance = new(() =>
        {
            var manager = new EnhancedConfigurationManager();

            // 注册默认观察者
            manager.RegisterObserver(new PerformanceConfigurationObserver());
            manager.RegisterObserver(new LoggingConfigurationObserver());

            return manager;
        });

        private static readonly ILogger _logger = LogManager.GetLogger(typeof(ConfigurationProvider));

        /// <summary>
        /// 获取配置管理器实例
        /// </summary>
        public static EnhancedConfigurationManager Instance => _instance.Value;

        /// <summary>
        /// 快速获取配置值
        /// </summary>
        public static T Get<T>(string key, T defaultValue = default)
        {
            return Instance.GetSetting(key, defaultValue);
        }

        /// <summary>
        /// 快速设置配置值
        /// </summary>
        public static void Set<T>(string key, T value)
        {
            Instance.SetSetting(key, value);
        }

        /// <summary>
        /// 获取环境特定配置
        /// </summary>
        public static T GetForEnvironment<T>(string key, T defaultValue = default, string environment = null)
        {
            return Instance.GetEnvironmentSetting(key, defaultValue, environment);
        }

        /// <summary>
        /// 设置环境特定配置
        /// </summary>
        public static void SetForEnvironment<T>(string key, T value, string environment = null)
        {
            Instance.SetEnvironmentSetting(key, value, environment);
        }

        /// <summary>
        /// 检查配置是否存在
        /// </summary>
        public static bool Exists(string key)
        {
            return Instance.HasSetting(key);
        }

        /// <summary>
        /// 保存所有配置
        /// </summary>
        public static void Save()
        {
            Instance.Save();
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        public static void Reload()
        {
            Instance.Load();
        }

        /// <summary>
        /// 获取配置摘要
        /// </summary>
        public static ConfigurationSummary GetSummary()
        {
            return Instance.GetConfigurationSummary();
        }

        /// <summary>
        /// 注册配置验证器
        /// </summary>
        public static void RegisterValidator(string key, IConfigurationValidator validator)
        {
            Instance.RegisterValidator(key, validator);
        }

        /// <summary>
        /// 注册配置观察者
        /// </summary>
        public static void RegisterObserver(IConfigurationObserver observer)
        {
            Instance.RegisterObserver(observer);
        }

        /// <summary>
        /// 启用或禁用热重载
        /// </summary>
        public static void SetHotReload(bool enabled)
        {
            Instance.HotReloadEnabled = enabled;
            _logger.Info($"配置热重载已{(enabled ? "启用" : "禁用")}");
        }

        /// <summary>
        /// 导出配置
        /// </summary>
        public static string Export(bool includeEnvironmentSpecific = true, bool includeUserSettings = false)
        {
            return Instance.ExportConfiguration(includeEnvironmentSpecific, includeUserSettings);
        }

        /// <summary>
        /// 导入配置
        /// </summary>
        public static void Import(string jsonConfiguration, bool mergeWithExisting = true)
        {
            Instance.ImportConfiguration(jsonConfiguration, mergeWithExisting);
        }

        /// <summary>
        /// 重置为默认配置
        /// </summary>
        public static void ResetToDefaults()
        {
            Instance.Reset();
            _logger.Info("配置已重置为默认值");
        }
    }

    /// <summary>
    /// 配置快捷访问器
    /// 提供强类型的配置访问
    /// </summary>
    public static class ConfigurationKeys
    {
        /// <summary>
        /// 性能相关配置
        /// </summary>
        public static class Performance
        {
            public const string CacheSize = "Performance.CacheSize";
            public const string CacheExpirationMinutes = "Performance.CacheExpirationMinutes";
            public const string EnableBatching = "Performance.EnableBatching";
            public const string BatchTimeoutMs = "Performance.BatchTimeoutMs";
            public const string ThreadPoolSize = "Performance.ThreadPoolSize";
            public const string EnablePerformanceMonitoring = "Performance.EnablePerformanceMonitoring";

            public static int GetCacheSize() => ConfigurationProvider.Get(CacheSize, 1000);

            public static void SetCacheSize(int value) => ConfigurationProvider.Set(CacheSize, value);

            public static int GetCacheExpirationMinutes() => ConfigurationProvider.Get(CacheExpirationMinutes, 30);

            public static void SetCacheExpirationMinutes(int value) => ConfigurationProvider.Set(CacheExpirationMinutes, value);

            public static bool GetEnableBatching() => ConfigurationProvider.Get(EnableBatching, true);

            public static void SetEnableBatching(bool value) => ConfigurationProvider.Set(EnableBatching, value);

            public static int GetBatchTimeoutMs() => ConfigurationProvider.Get(BatchTimeoutMs, 10);

            public static void SetBatchTimeoutMs(int value) => ConfigurationProvider.Set(BatchTimeoutMs, value);
        }

        /// <summary>
        /// 日志相关配置
        /// </summary>
        public static class Logging
        {
            public const string Level = "Logging.Level";
            public const string EnableFileLogging = "Logging.EnableFileLogging";
            public const string MaxFileSizeMB = "Logging.MaxFileSizeMB";
            public const string LogDirectory = "Logging.LogDirectory";
            public const string RetentionDays = "Logging.RetentionDays";

            public static LogLevel GetLevel() => ConfigurationProvider.Get(Level, LogLevel.Information);

            public static void SetLevel(LogLevel value) => ConfigurationProvider.Set(Level, value);

            public static bool GetEnableFileLogging() => ConfigurationProvider.Get(EnableFileLogging, true);

            public static void SetEnableFileLogging(bool value) => ConfigurationProvider.Set(EnableFileLogging, value);

            public static int GetMaxFileSizeMB() => ConfigurationProvider.Get(MaxFileSizeMB, 10);

            public static void SetMaxFileSizeMB(int value) => ConfigurationProvider.Set(MaxFileSizeMB, value);

            public static string GetLogDirectory() => ConfigurationProvider.Get(LogDirectory, "Logs");

            public static void SetLogDirectory(string value) => ConfigurationProvider.Set(LogDirectory, value);
        }

        /// <summary>
        /// AutoCAD相关配置
        /// </summary>
        public static class AutoCAD
        {
            public const string DefaultLineWeight = "AutoCAD.DefaultLineWeight";
            public const string DefaultColor = "AutoCAD.DefaultColor";
            public const string EnableTransactionLogging = "AutoCAD.EnableTransactionLogging";
            public const string DefaultLayer = "AutoCAD.DefaultLayer";
            public const string EnableAutoSave = "AutoCAD.EnableAutoSave";
            public const string AutoSaveIntervalMinutes = "AutoCAD.AutoSaveIntervalMinutes";

            public static double GetDefaultLineWeight() => ConfigurationProvider.Get(DefaultLineWeight, 0.25);

            public static void SetDefaultLineWeight(double value) => ConfigurationProvider.Set(DefaultLineWeight, value);

            public static int GetDefaultColor() => ConfigurationProvider.Get(DefaultColor, 256);

            public static void SetDefaultColor(int value) => ConfigurationProvider.Set(DefaultColor, value);

            public static bool GetEnableTransactionLogging() => ConfigurationProvider.Get(EnableTransactionLogging, false);

            public static void SetEnableTransactionLogging(bool value) => ConfigurationProvider.Set(EnableTransactionLogging, value);
        }

        /// <summary>
        /// 诊断相关配置
        /// </summary>
        public static class Diagnostics
        {
            public const string EnableAutoCheck = "Diagnostics.EnableAutoCheck";
            public const string CheckIntervalMinutes = "Diagnostics.CheckIntervalMinutes";
            public const string MaxReportRetentionDays = "Diagnostics.MaxReportRetentionDays";
            public const string EnablePerformanceAnalysis = "Diagnostics.EnablePerformanceAnalysis";
            public const string EnableMemoryAnalysis = "Diagnostics.EnableMemoryAnalysis";

            public static bool GetEnableAutoCheck() => ConfigurationProvider.Get(EnableAutoCheck, true);

            public static void SetEnableAutoCheck(bool value) => ConfigurationProvider.Set(EnableAutoCheck, value);

            public static int GetCheckIntervalMinutes() => ConfigurationProvider.Get(CheckIntervalMinutes, 15);

            public static void SetCheckIntervalMinutes(int value) => ConfigurationProvider.Set(CheckIntervalMinutes, value);

            public static int GetMaxReportRetentionDays() => ConfigurationProvider.Get(MaxReportRetentionDays, 7);

            public static void SetMaxReportRetentionDays(int value) => ConfigurationProvider.Set(MaxReportRetentionDays, value);
        }

        /// <summary>
        /// 开发工具相关配置
        /// </summary>
        public static class Development
        {
            public const string EnableIntelliSense = "Development.EnableIntelliSense";
            public const string EnableCodeGeneration = "Development.EnableCodeGeneration";
            public const string EnableAutoFix = "Development.EnableAutoFix";
            public const string MaxSuggestions = "Development.MaxSuggestions";

            public static bool GetEnableIntelliSense() => ConfigurationProvider.Get(EnableIntelliSense, true);

            public static void SetEnableIntelliSense(bool value) => ConfigurationProvider.Set(EnableIntelliSense, value);

            public static bool GetEnableCodeGeneration() => ConfigurationProvider.Get(EnableCodeGeneration, true);

            public static void SetEnableCodeGeneration(bool value) => ConfigurationProvider.Set(EnableCodeGeneration, value);

            public static bool GetEnableAutoFix() => ConfigurationProvider.Get(EnableAutoFix, true);

            public static void SetEnableAutoFix(bool value) => ConfigurationProvider.Set(EnableAutoFix, value);

            public static int GetMaxSuggestions() => ConfigurationProvider.Get(MaxSuggestions, 10);

            public static void SetMaxSuggestions(int value) => ConfigurationProvider.Set(MaxSuggestions, value);
        }
    }

    /// <summary>
    /// 配置助手类
    /// 提供配置管理的便捷方法
    /// </summary>
    public static class ConfigurationHelper
    {
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(ConfigurationHelper));

        /// <summary>
        /// 批量设置配置
        /// </summary>
        public static void SetBatch(Dictionary<string, object> settings)
        {
            foreach (var kvp in settings)
            {
                ConfigurationProvider.Set(kvp.Key, kvp.Value);
            }

            ConfigurationProvider.Save();
            _logger.Info($"批量设置 {settings.Count} 个配置项");
        }

        /// <summary>
        /// 批量获取配置
        /// </summary>
        public static Dictionary<string, object> GetBatch(params string[] keys)
        {
            var result = new Dictionary<string, object>();

            foreach (var key in keys)
            {
                var value = ConfigurationProvider.Get<object>(key);
                if (value != null)
                {
                    result[key] = value;
                }
            }

            return result;
        }

        /// <summary>
        /// 应用性能优化配置
        /// </summary>
        public static void ApplyPerformanceOptimizations()
        {
            var config = new Dictionary<string, object>
            {
                [ConfigurationKeys.Performance.EnableBatching] = true,
                [ConfigurationKeys.Performance.BatchTimeoutMs] = 10,
                [ConfigurationKeys.Performance.CacheSize] = 2000,
                [ConfigurationKeys.Performance.CacheExpirationMinutes] = 60,
                [ConfigurationKeys.Performance.EnablePerformanceMonitoring] = true
            };

            SetBatch(config);
            _logger.Info("已应用性能优化配置");
        }

        /// <summary>
        /// 应用开发环境配置
        /// </summary>
        public static void ApplyDevelopmentConfiguration()
        {
            var config = new Dictionary<string, object>
            {
                [ConfigurationKeys.Logging.Level] = LogLevel.Debug,
                [ConfigurationKeys.Diagnostics.EnableAutoCheck] = true,
                [ConfigurationKeys.Diagnostics.CheckIntervalMinutes] = 5,
                [ConfigurationKeys.Development.EnableIntelliSense] = true,
                [ConfigurationKeys.Development.EnableCodeGeneration] = true,
                [ConfigurationKeys.Development.EnableAutoFix] = true,
                [ConfigurationKeys.AutoCAD.EnableTransactionLogging] = true
            };

            SetBatch(config);
            _logger.Info("已应用开发环境配置");
        }

        /// <summary>
        /// 应用生产环境配置
        /// </summary>
        public static void ApplyProductionConfiguration()
        {
            var config = new Dictionary<string, object>
            {
                [ConfigurationKeys.Logging.Level] = LogLevel.Warning,
                [ConfigurationKeys.Diagnostics.EnableAutoCheck] = false,
                [ConfigurationKeys.Performance.EnablePerformanceMonitoring] = false,
                [ConfigurationKeys.AutoCAD.EnableTransactionLogging] = false,
                [ConfigurationKeys.Development.EnableIntelliSense] = false,
                [ConfigurationKeys.Development.EnableCodeGeneration] = false
            };

            SetBatch(config);
            _logger.Info("已应用生产环境配置");
        }

        /// <summary>
        /// 验证当前配置
        /// </summary>
        public static List<string> ValidateCurrentConfiguration()
        {
            var issues = new List<string>();

            try
            {
                // 验证性能配置
                var cacheSize = ConfigurationKeys.Performance.GetCacheSize();
                if (cacheSize < 100 || cacheSize > 10000)
                {
                    issues.Add($"缓存大小 {cacheSize} 超出推荐范围 (100-10000)");
                }

                var batchTimeout = ConfigurationKeys.Performance.GetBatchTimeoutMs();
                if (batchTimeout < 1 || batchTimeout > 1000)
                {
                    issues.Add($"批处理超时 {batchTimeout}ms 超出推荐范围 (1-1000ms)");
                }

                // 验证日志配置
                var maxFileSize = ConfigurationKeys.Logging.GetMaxFileSizeMB();
                if (maxFileSize < 1 || maxFileSize > 100)
                {
                    issues.Add($"日志文件最大大小 {maxFileSize}MB 超出推荐范围 (1-100MB)");
                }

                // 验证诊断配置
                var checkInterval = ConfigurationKeys.Diagnostics.GetCheckIntervalMinutes();
                if (checkInterval < 1 || checkInterval > 60)
                {
                    issues.Add($"诊断检查间隔 {checkInterval}分钟 超出推荐范围 (1-60分钟)");
                }

                _logger.Info($"配置验证完成，发现 {issues.Count} 个问题");
            }
            catch (Exception ex)
            {
                _logger.Error("配置验证失败", ex);
                issues.Add($"配置验证过程中发生错误: {ex.Message}");
            }

            return issues;
        }

        /// <summary>
        /// 生成配置报告
        /// </summary>
        public static string GenerateConfigurationReport()
        {
            var sb = new StringBuilder();
            var summary = ConfigurationProvider.GetSummary();

            sb.AppendLine("=== DotNetARX 配置报告 ===");
            sb.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"环境: {summary.Environment}");
            sb.AppendLine($"配置路径: {summary.ConfigurationPath}");
            sb.AppendLine($"热重载: {(summary.HotReloadEnabled ? "启用" : "禁用")}");
            sb.AppendLine($"最后修改: {summary.LastModified:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            sb.AppendLine("配置统计:");
            sb.AppendLine($"  总配置项: {summary.TotalSettings}");
            sb.AppendLine($"  通用配置: {summary.GeneralSettings}");
            sb.AppendLine($"  环境配置: {summary.EnvironmentSettings}");
            sb.AppendLine($"  用户配置: {summary.UserSettings}");
            sb.AppendLine();

            // 性能配置
            sb.AppendLine("性能配置:");
            sb.AppendLine($"  缓存大小: {ConfigurationKeys.Performance.GetCacheSize()}");
            sb.AppendLine($"  缓存过期时间: {ConfigurationKeys.Performance.GetCacheExpirationMinutes()} 分钟");
            sb.AppendLine($"  启用批处理: {ConfigurationKeys.Performance.GetEnableBatching()}");
            sb.AppendLine($"  批处理超时: {ConfigurationKeys.Performance.GetBatchTimeoutMs()} ms");
            sb.AppendLine();

            // 日志配置
            sb.AppendLine("日志配置:");
            sb.AppendLine($"  日志级别: {ConfigurationKeys.Logging.GetLevel()}");
            sb.AppendLine($"  文件日志: {ConfigurationKeys.Logging.GetEnableFileLogging()}");
            sb.AppendLine($"  最大文件大小: {ConfigurationKeys.Logging.GetMaxFileSizeMB()} MB");
            sb.AppendLine($"  日志目录: {ConfigurationKeys.Logging.GetLogDirectory()}");
            sb.AppendLine();

            // 验证问题
            var issues = ValidateCurrentConfiguration();
            if (issues.Any())
            {
                sb.AppendLine("配置问题:");
                foreach (var issue in issues)
                {
                    sb.AppendLine($"  ⚠️  {issue}");
                }
            }
            else
            {
                sb.AppendLine("✅ 配置验证通过，无问题");
            }

            return sb.ToString();
        }
    }
}