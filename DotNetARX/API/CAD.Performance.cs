using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetARX.API
{
    public static partial class CAD
    {
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

        #endregion 系统管理和监控
    }
}