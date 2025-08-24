using DotNetARX.Performance.Benchmarks;

namespace DotNetARX.Performance
{
    /// <summary>
    /// 性能测试运行器
    /// 提供便捷的基准测试执行和管理功能
    /// </summary>
    public static class PerformanceTestRunner
    {
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(PerformanceTestRunner));

        /// <summary>
        /// 运行所有基准测试
        /// </summary>
        public static async Task<PerformanceTestReport> RunAllBenchmarks(bool fastMode = false)
        {
            _logger.Info("开始运行所有性能基准测试...");

            var report = new PerformanceTestReport
            {
                StartTime = DateTime.UtcNow,
                FastMode = fastMode
            };

            var config = fastMode ? PerformanceAnalyzer.Config.Fast : PerformanceAnalyzer.Config.Default;

            try
            {
                // 运行CAD操作基准测试
                _logger.Info("运行 CAD 操作基准测试...");
                var cadResults = await Task.Run(() => PerformanceAnalyzer.RunBenchmarks<CADOperationBenchmarks>(config));
                report.BenchmarkResults.AddRange(cadResults);

                // 运行缓存性能基准测试
                _logger.Info("运行缓存性能基准测试...");
                var cacheResults = await Task.Run(() => PerformanceAnalyzer.RunBenchmarks<CachePerformanceBenchmarks>(config));
                report.BenchmarkResults.AddRange(cacheResults);

                // 运行服务定位器基准测试
                _logger.Info("运行服务定位器基准测试...");
                var serviceResults = await Task.Run(() => PerformanceAnalyzer.RunBenchmarks<ServiceLocatorBenchmarks>(config));
                report.BenchmarkResults.AddRange(serviceResults);

                // 运行工具类基准测试
                _logger.Info("运行工具类基准测试...");
                var utilityResults = await Task.Run(() => PerformanceAnalyzer.RunBenchmarks<UtilityBenchmarks>(config));
                report.BenchmarkResults.AddRange(utilityResults);

                report.EndTime = DateTime.UtcNow;
                report.IsSuccessful = true;
                report.Summary = GenerateBenchmarkSummary(report);

                _logger.Info($"所有基准测试完成，耗时 {report.Duration.TotalMinutes:F1} 分钟");
            }
            catch (Exception ex)
            {
                report.EndTime = DateTime.UtcNow;
                report.IsSuccessful = false;
                report.ErrorMessage = ex.Message;
                _logger.Error("基准测试执行失败", ex);
            }

            return report;
        }

        /// <summary>
        /// 运行指定的基准测试
        /// </summary>
        public static async Task<PerformanceTestReport> RunBenchmark<T>(bool fastMode = false) where T : class
        {
            _logger.Info($"开始运行基准测试: {typeof(T).Name}");

            var report = new PerformanceTestReport
            {
                StartTime = DateTime.UtcNow,
                FastMode = fastMode
            };

            var config = fastMode ? PerformanceAnalyzer.Config.Fast : PerformanceAnalyzer.Config.Default;

            try
            {
                var results = await Task.Run(() => PerformanceAnalyzer.RunBenchmarks<T>(config));
                report.BenchmarkResults.AddRange(results);

                report.EndTime = DateTime.UtcNow;
                report.IsSuccessful = true;
                report.Summary = GenerateBenchmarkSummary(report);

                _logger.Info($"基准测试 {typeof(T).Name} 完成");
            }
            catch (Exception ex)
            {
                report.EndTime = DateTime.UtcNow;
                report.IsSuccessful = false;
                report.ErrorMessage = ex.Message;
                _logger.Error($"基准测试 {typeof(T).Name} 执行失败", ex);
            }

            return report;
        }

        /// <summary>
        /// 运行性能回归检测
        /// </summary>
        public static async Task<RegressionTestReport> RunRegressionTests()
        {
            _logger.Info("开始运行性能回归检测...");

            var report = new RegressionTestReport
            {
                StartTime = DateTime.UtcNow
            };

            try
            {
                // 设置基准数据（如果还没有的话）
                SetupBaselines();

                // 运行快速基准测试
                var benchmarkReport = await RunAllBenchmarks(fastMode: true);

                // 检查回归
                var regressions = new List<PerformanceRegressionResult>();

                foreach (var benchmarkResult in benchmarkReport.BenchmarkResults)
                {
                    if (benchmarkResult.IsSuccessful && benchmarkResult.Summary != null)
                    {
                        foreach (var benchmarkReport2 in benchmarkResult.Summary.Reports)
                        {
                            var operationName = $"{benchmarkResult2.BenchmarkCase.Descriptor.Type.Name}.{benchmarkResult2.BenchmarkCase.Descriptor.WorkloadMethod.Name}";
                            var regression = PerformanceAnalyzer.CheckRegression(operationName);

                            if (regression.HasRegression)
                            {
                                regressions.Add(regression);
                            }
                        }
                    }
                }

                report.EndTime = DateTime.UtcNow;
                report.IsSuccessful = true;
                report.Regressions = regressions;
                report.RegressionCount = regressions.Count;

                if (regressions.Any())
                {
                    _logger.Warning($"检测到 {regressions.Count} 个性能回归");
                    foreach (var regression in regressions.Take(5))
                    {
                        _logger.Warning($"  - {regression.OperationName}: {regression.Message}");
                    }
                }
                else
                {
                    _logger.Info("未检测到性能回归");
                }
            }
            catch (Exception ex)
            {
                report.EndTime = DateTime.UtcNow;
                report.IsSuccessful = false;
                report.ErrorMessage = ex.Message;
                _logger.Error("性能回归检测失败", ex);
            }

            return report;
        }

        /// <summary>
        /// 生成性能分析报告
        /// </summary>
        public static async Task<string> GeneratePerformanceReport(TimeSpan? timeRange = null)
        {
            _logger.Info("生成性能分析报告...");

            var sb = new StringBuilder();
            sb.AppendLine("# DotNetARX 性能分析报告");
            sb.AppendLine($"生成时间: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine();

            try
            {
                // 获取性能分析器报告
                var performanceReport = PerformanceAnalyzer.GetReport(timeRange);
                sb.AppendLine("## 性能监控摘要");
                sb.AppendLine(performanceReport.ToString());
                sb.AppendLine();

                // 获取自动监控报告
                var systemSummary = AutoPerformanceMonitor.Instance.GetSystemSummary();
                sb.AppendLine("## 系统性能摘要");
                sb.AppendLine($"总操作数: {systemSummary.TotalOperations:N0}");
                sb.AppendLine($"平均执行时间: {systemSummary.AverageExecutionTime.TotalMilliseconds:F2} ms");
                sb.AppendLine($"总内存分配: {systemSummary.TotalMemoryAllocated / 1024.0 / 1024.0:F2} MB");
                sb.AppendLine($"错误率: {systemSummary.ErrorRate:P2}");
                sb.AppendLine();

                // 最慢操作
                if (systemSummary.TopSlowOperations.Any())
                {
                    sb.AppendLine("### 最慢操作 (Top 5)");
                    foreach (var op in systemSummary.TopSlowOperations.Take(5))
                    {
                        sb.AppendLine($"- {op.OperationName}: {op.AverageExecutionTime.TotalMilliseconds:F2} ms");
                    }
                    sb.AppendLine();
                }

                // 内存使用最多的操作
                if (systemSummary.TopMemoryOperations.Any())
                {
                    sb.AppendLine("### 内存使用最多的操作 (Top 5)");
                    foreach (var op in systemSummary.TopMemoryOperations.Take(5))
                    {
                        sb.AppendLine($"- {op.OperationName}: {op.AverageMemoryUsage / 1024.0 / 1024.0:F2} MB");
                    }
                    sb.AppendLine();
                }

                // 性能建议
                var recommendations = AutoPerformanceMonitor.Instance.GetRecommendations();
                if (recommendations.Any())
                {
                    sb.AppendLine("## 性能优化建议");
                    var groupedRecommendations = recommendations.GroupBy(r => r.Severity);

                    foreach (var group in groupedRecommendations.OrderByDescending(g => g.Key))
                    {
                        sb.AppendLine($"### {group.Key} 级别");
                        foreach (var rec in group.Take(5))
                        {
                            sb.AppendLine($"- **{rec.OperationName}**: {rec.Message}");
                            sb.AppendLine($"  *建议*: {rec.Suggestion}");
                        }
                        sb.AppendLine();
                    }
                }

                // 缓存统计
                var cacheStats = SmartCacheManager.GetStatistics();
                sb.AppendLine("## 缓存性能统计");
                sb.AppendLine($"缓存总数: {cacheStats.TotalCaches}");
                sb.AppendLine($"缓存项总数: {cacheStats.TotalItems:N0}");
                sb.AppendLine($"命中率: {cacheStats.HitRatio:P2}");
                sb.AppendLine($"内存使用: {cacheStats.TotalMemoryUsage / 1024.0 / 1024.0:F2} MB");
                sb.AppendLine($"内存压力: {cacheStats.MemoryPressureLevel}");
                sb.AppendLine();

                _logger.Info("性能分析报告生成完成");
            }
            catch (Exception ex)
            {
                _logger.Error("生成性能报告时发生错误", ex);
                sb.AppendLine($"## 错误");
                sb.AppendLine($"生成报告时发生错误: {ex.Message}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 设置基准数据
        /// </summary>
        private static void SetupBaselines()
        {
            // 为主要操作设置性能基准
            PerformanceAnalyzer.SetBaseline("CADOperationBenchmarks.CreateLine", TimeSpan.FromMilliseconds(1), 1024);
            PerformanceAnalyzer.SetBaseline("CADOperationBenchmarks.CreateCircle", TimeSpan.FromMilliseconds(1), 1024);
            PerformanceAnalyzer.SetBaseline("CADOperationBenchmarks.MoveEntity", TimeSpan.FromMilliseconds(0.5), 512);
            PerformanceAnalyzer.SetBaseline("CADOperationBenchmarks.CopyEntity", TimeSpan.FromMilliseconds(2), 2048);

            PerformanceAnalyzer.SetBaseline("CachePerformanceBenchmarks.SmartCache_Set", TimeSpan.FromMilliseconds(10), 10240);
            PerformanceAnalyzer.SetBaseline("CachePerformanceBenchmarks.SmartCache_Get", TimeSpan.FromMilliseconds(5), 1024);

            PerformanceAnalyzer.SetBaseline("ServiceLocatorBenchmarks.GetService_Logger", TimeSpan.FromMicroseconds(100), 256);
            PerformanceAnalyzer.SetBaseline("ServiceLocatorBenchmarks.GetService_ConfigManager", TimeSpan.FromMicroseconds(100), 256);
        }

        /// <summary>
        /// 生成基准测试摘要
        /// </summary>
        private static string GenerateBenchmarkSummary(PerformanceTestReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("基准测试摘要:");
            sb.AppendLine($"- 执行时间: {report.Duration.TotalMinutes:F1} 分钟");
            sb.AppendLine($"- 测试模式: {(report.FastMode ? "快速模式" : "完整模式")}");
            sb.AppendLine($"- 成功: {report.IsSuccessful}");
            sb.AppendLine($"- 基准测试组数: {report.BenchmarkResults.Count}");

            var successfulTests = report.BenchmarkResults.Count(r => r.IsSuccessful);
            sb.AppendLine($"- 成功的测试: {successfulTests}/{report.BenchmarkResults.Count}");

            return sb.ToString();
        }
    }

    /// <summary>
    /// 性能测试报告
    /// </summary>
    public class PerformanceTestReport
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public bool FastMode { get; set; }
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }
        public string Summary { get; set; }
        public List<BenchmarkRunInfo> BenchmarkResults { get; set; } = new();
    }

    /// <summary>
    /// 回归测试报告
    /// </summary>
    public class RegressionTestReport
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }
        public int RegressionCount { get; set; }
        public List<PerformanceRegressionResult> Regressions { get; set; } = new();
    }
}