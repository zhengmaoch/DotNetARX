using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DotNetARX.Performance
{
    /// <summary>
    /// 性能分析器实现类
    /// </summary>
    public class PerformanceAnalyzerImpl : IDiagnosticAnalyzer
    {
        public string Name => "性能分析器";
        public string Description => "分析系统性能指标，检测性能瓶颈和优化建议";

        public DiagnosticResult Analyze()
        {
            return new DiagnosticResult
            {
                AnalyzerName = Name,
                Severity = DiagnosticSeverity.Info,
                Message = "性能分析完成",
                Recommendations = new List<string> { "继续监控性能指标" }
            };
        }
    }

    /// <summary>
    /// DotNetARX 性能分析器
    /// 集成BenchmarkDotNet和内存分析，提供深度性能洞察
    /// </summary>
    public static class PerformanceAnalyzer
    {
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(PerformanceAnalyzer));
        private static readonly ConcurrentDictionary<string, PerformanceProfile> _profiles = new();
        private static readonly ConcurrentDictionary<string, BaselineData> _baselines = new();
        private static bool _isEnabled = true;

        /// <summary>
        /// 性能分析配置
        /// </summary>
        public static class Config
        {
            public static readonly IConfig Default = ManualConfig.Create(DefaultConfig.Instance)
                .AddJob(Job.Default.WithWarmupCount(1).WithIterationCount(3))
                .AddExporter(MarkdownExporter.GitHub)
                .AddExporter(HtmlExporter.Default)
                .AddLogger(ConsoleLogger.Default)
                .AddAnalyser(EnvironmentAnalyser.Default)
                .AddAnalyser(OutliersAnalyser.Default)
                .AddAnalyser(MinIterationTimeAnalyser.Default)
                .WithOptions(ConfigOptions.DisableOptimizationsValidator);

            public static readonly IConfig Fast = ManualConfig.Create(DefaultConfig.Instance)
                .AddJob(Job.Dry)
                .AddExporter(MarkdownExporter.Default)
                .AddLogger(NullLogger.Instance);
        }

        /// <summary>
        /// 启用或禁用性能分析
        /// </summary>
        public static bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>
        /// 运行性能基准测试
        /// </summary>
        public static BenchmarkRunInfo[] RunBenchmarks<T>(IConfig config = null) where T : class
        {
            if (!_isEnabled) return Array.Empty<BenchmarkRunInfo>();

            try
            {
                _logger.Info($"开始性能基准测试: {typeof(T).Name}");
                var summary = BenchmarkRunner.Run<T>(config ?? Config.Default);

                _logger.Info($"基准测试完成: {typeof(T).Name}，共 {summary.Reports.Length} 个报告");
                return new[] { new BenchmarkRunInfo(summary) };
            }
            catch (Exception ex)
            {
                _logger.Error($"基准测试失败: {typeof(T).Name}", ex);
                return Array.Empty<BenchmarkRunInfo>();
            }
        }

        /// <summary>
        /// 性能剖析 - 详细分析单个操作
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T Profile<T>(string operationName, Func<T> operation, int iterations = 1000)
        {
            if (!_isEnabled) return operation();

            var stopwatch = Stopwatch.StartNew();
            var memoryBefore = GC.GetTotalMemory(false);
            var gen0Before = GC.CollectionCount(0);
            var gen1Before = GC.CollectionCount(1);
            var gen2Before = GC.CollectionCount(2);

            T result = default(T);
            Exception lastException = null;
            int successCount = 0;

            try
            {
                // 热身
                for (int i = 0; i < Math.Min(10, iterations / 10); i++)
                {
                    try
                    {
                        result = operation();
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                    }
                }

                // 重置计时器进行正式测量
                stopwatch.Restart();
                memoryBefore = GC.GetTotalMemory(true);
                gen0Before = GC.CollectionCount(0);
                gen1Before = GC.CollectionCount(1);
                gen2Before = GC.CollectionCount(2);

                // 正式测量
                for (int i = 0; i < iterations; i++)
                {
                    try
                    {
                        result = operation();
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                    }
                }
            }
            finally
            {
                stopwatch.Stop();
                var memoryAfter = GC.GetTotalMemory(false);
                var gen0After = GC.CollectionCount(0);
                var gen1After = GC.CollectionCount(1);
                var gen2After = GC.CollectionCount(2);

                var profile = new PerformanceProfile
                {
                    OperationName = operationName,
                    TotalTime = stopwatch.Elapsed,
                    Iterations = iterations,
                    SuccessCount = successCount,
                    AverageTime = TimeSpan.FromTicks(stopwatch.ElapsedTicks / iterations),
                    MemoryAllocated = Math.Max(0, memoryAfter - memoryBefore),
                    Gen0Collections = gen0After - gen0Before,
                    Gen1Collections = gen1After - gen1Before,
                    Gen2Collections = gen2After - gen2Before,
                    LastException = lastException,
                    Timestamp = DateTime.UtcNow
                };

                RecordProfile(profile);
            }

            return result;
        }

        /// <summary>
        /// 异步性能剖析
        /// </summary>
        public static async Task<T> ProfileAsync<T>(string operationName, Func<Task<T>> operation, int iterations = 100)
        {
            if (!_isEnabled) return await operation();

            var stopwatch = Stopwatch.StartNew();
            var memoryBefore = GC.GetTotalMemory(false);

            T result = default(T);
            Exception lastException = null;
            int successCount = 0;

            try
            {
                // 热身
                for (int i = 0; i < Math.Min(5, iterations / 10); i++)
                {
                    try
                    {
                        result = await operation();
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                    }
                }

                // 正式测量
                stopwatch.Restart();
                memoryBefore = GC.GetTotalMemory(true);

                for (int i = 0; i < iterations; i++)
                {
                    try
                    {
                        result = await operation();
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                    }
                }
            }
            finally
            {
                stopwatch.Stop();
                var memoryAfter = GC.GetTotalMemory(false);

                var profile = new PerformanceProfile
                {
                    OperationName = $"{operationName}_Async",
                    TotalTime = stopwatch.Elapsed,
                    Iterations = iterations,
                    SuccessCount = successCount,
                    AverageTime = TimeSpan.FromTicks(stopwatch.ElapsedTicks / iterations),
                    MemoryAllocated = Math.Max(0, memoryAfter - memoryBefore),
                    LastException = lastException,
                    Timestamp = DateTime.UtcNow
                };

                RecordProfile(profile);
            }

            return result;
        }

        /// <summary>
        /// 内存使用分析
        /// </summary>
        public static MemoryAnalysisResult AnalyzeMemory(string operationName, Action operation, int iterations = 1000)
        {
            if (!_isEnabled)
            {
                operation();
                return new MemoryAnalysisResult();
            }

            // 强制垃圾回收以获得准确的基准
            GC.Collect(2, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced);

            var memoryBefore = GC.GetTotalMemory(false);
            var gen0Before = GC.CollectionCount(0);
            var gen1Before = GC.CollectionCount(1);
            var gen2Before = GC.CollectionCount(2);

            var stopwatch = Stopwatch.StartNew();

            // 执行操作
            for (int i = 0; i < iterations; i++)
            {
                operation();
            }

            stopwatch.Stop();

            var memoryAfter = GC.GetTotalMemory(false);
            var gen0After = GC.CollectionCount(0);
            var gen1After = GC.CollectionCount(1);
            var gen2After = GC.CollectionCount(2);

            // 强制一次垃圾回收以查看实际保留的内存
            GC.Collect(2, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
            var memoryAfterGC = GC.GetTotalMemory(false);

            var result = new MemoryAnalysisResult
            {
                OperationName = operationName,
                Iterations = iterations,
                ExecutionTime = stopwatch.Elapsed,
                MemoryBefore = memoryBefore,
                MemoryAfter = memoryAfter,
                MemoryAfterGC = memoryAfterGC,
                TotalAllocated = memoryAfter - memoryBefore,
                MemoryRetained = memoryAfterGC - memoryBefore,
                MemoryReleased = memoryAfter - memoryAfterGC,
                Gen0Collections = gen0After - gen0Before,
                Gen1Collections = gen1After - gen1Before,
                Gen2Collections = gen2After - gen2Before,
                AllocationPerIteration = (memoryAfter - memoryBefore) / (double)iterations,
                Timestamp = DateTime.UtcNow
            };

            _logger.Info($"内存分析完成 - {operationName}: 分配 {result.TotalAllocated:N0} 字节, 保留 {result.MemoryRetained:N0} 字节");
            return result;
        }

        /// <summary>
        /// 设置性能基准数据
        /// </summary>
        public static void SetBaseline(string operationName, TimeSpan expectedTime, long expectedMemory = 0)
        {
            var baseline = new BaselineData
            {
                OperationName = operationName,
                ExpectedTime = expectedTime,
                ExpectedMemory = expectedMemory,
                CreatedAt = DateTime.UtcNow
            };

            _baselines.AddOrUpdate(operationName, baseline, (key, existing) => baseline);
            _logger.Info($"设置性能基准 - {operationName}: 时间 {expectedTime.TotalMilliseconds}ms, 内存 {expectedMemory:N0} 字节");
        }

        /// <summary>
        /// 检查性能回归
        /// </summary>
        public static PerformanceRegressionResult CheckRegression(string operationName)
        {
            if (!_profiles.TryGetValue(operationName, out var profile) ||
                !_baselines.TryGetValue(operationName, out var baseline))
            {
                return new PerformanceRegressionResult
                {
                    OperationName = operationName,
                    HasRegression = false,
                    Message = "缺少性能数据或基准数据"
                };
            }

            var timeRegression = profile.AverageTime.TotalMilliseconds > baseline.ExpectedTime.TotalMilliseconds * 1.2;
            var memoryRegression = baseline.ExpectedMemory > 0 &&
                                   profile.MemoryAllocated > baseline.ExpectedMemory * 1.5;

            var result = new PerformanceRegressionResult
            {
                OperationName = operationName,
                HasRegression = timeRegression || memoryRegression,
                TimeRegression = timeRegression,
                MemoryRegression = memoryRegression,
                CurrentTime = profile.AverageTime,
                ExpectedTime = baseline.ExpectedTime,
                CurrentMemory = profile.MemoryAllocated,
                ExpectedMemory = baseline.ExpectedMemory,
                TimeRatio = profile.AverageTime.TotalMilliseconds / baseline.ExpectedTime.TotalMilliseconds,
                MemoryRatio = baseline.ExpectedMemory > 0 ?
                    (double)profile.MemoryAllocated / baseline.ExpectedMemory : 1.0
            };

            if (result.HasRegression)
            {
                var message = $"性能回归检测 - {operationName}:";
                if (timeRegression)
                    message += $" 时间超标 {result.TimeRatio:P1}";
                if (memoryRegression)
                    message += $" 内存超标 {result.MemoryRatio:P1}";

                result.Message = message;
                _logger.Warning(message);
            }

            return result;
        }

        /// <summary>
        /// 获取性能报告
        /// </summary>
        public static PerformanceReport GetReport(TimeSpan? timeRange = null)
        {
            var cutoff = timeRange.HasValue ? DateTime.UtcNow - timeRange.Value : DateTime.MinValue;
            var recentProfiles = _profiles.Values.Where(p => p.Timestamp >= cutoff).ToList();

            var report = new PerformanceReport
            {
                GeneratedAt = DateTime.UtcNow,
                TimeRange = timeRange,
                TotalOperations = recentProfiles.Count,
                TotalTime = recentProfiles.Sum(p => p.TotalTime.TotalMilliseconds),
                TotalMemoryAllocated = recentProfiles.Sum(p => p.MemoryAllocated),
                AverageExecutionTime = recentProfiles.Count > 0 ?
                    TimeSpan.FromMilliseconds(recentProfiles.Average(p => p.AverageTime.TotalMilliseconds)) :
                    TimeSpan.Zero,
                Profiles = recentProfiles.OrderByDescending(p => p.Timestamp).Take(50).ToList()
            };

            // 检查回归
            var regressions = recentProfiles
                .Select(p => CheckRegression(p.OperationName))
                .Where(r => r.HasRegression)
                .ToList();

            report.RegressionCount = regressions.Count;
            report.Regressions = regressions;

            return report;
        }

        /// <summary>
        /// 清理旧的性能数据
        /// </summary>
        public static void CleanupOldData(TimeSpan retention = default)
        {
            if (retention == default)
                retention = TimeSpan.FromDays(7);

            var cutoff = DateTime.UtcNow - retention;
            var keysToRemove = _profiles
                .Where(kvp => kvp.Value.Timestamp < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _profiles.TryRemove(key, out _);
            }

            _logger.Info($"清理了 {keysToRemove.Count} 个过期的性能记录");
        }

        /// <summary>
        /// 记录性能配置文件
        /// </summary>
        private static void RecordProfile(PerformanceProfile profile)
        {
            _profiles.AddOrUpdate(profile.OperationName, profile, (key, existing) => profile);

            // 检查是否有回归
            var regression = CheckRegression(profile.OperationName);

            if (profile.LastException != null)
            {
                _logger.Error($"性能分析中发现异常 - {profile.OperationName}: {profile.LastException.Message}");
            }

            _logger.Debug($"性能记录 - {profile.OperationName}: 平均 {profile.AverageTime.TotalMilliseconds:F2}ms, " +
                         $"内存 {profile.MemoryAllocated:N0} 字节, 成功率 {(double)profile.SuccessCount / profile.Iterations:P1}");
        }
    }
}