using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Reports;

namespace DotNetARX.Performance
{
    /// <summary>
    /// 性能配置文件
    /// </summary>
    public class PerformanceProfile
    {
        public string OperationName { get; set; }
        public TimeSpan TotalTime { get; set; }
        public TimeSpan AverageTime { get; set; }
        public int Iterations { get; set; }
        public int SuccessCount { get; set; }
        public long MemoryAllocated { get; set; }
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }
        public Exception LastException { get; set; }
        public DateTime Timestamp { get; set; }

        public double SuccessRate => Iterations > 0 ? (double)SuccessCount / Iterations : 0;
        public double OperationsPerSecond => AverageTime.TotalSeconds > 0 ? 1.0 / AverageTime.TotalSeconds : 0;
    }

    /// <summary>
    /// 内存分析结果
    /// </summary>
    public class MemoryAnalysisResult
    {
        public string OperationName { get; set; }
        public int Iterations { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public long MemoryBefore { get; set; }
        public long MemoryAfter { get; set; }
        public long MemoryAfterGC { get; set; }
        public long TotalAllocated { get; set; }
        public long MemoryRetained { get; set; }
        public long MemoryReleased { get; set; }
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }
        public double AllocationPerIteration { get; set; }
        public DateTime Timestamp { get; set; }

        public double AllocationRate => ExecutionTime.TotalSeconds > 0 ? TotalAllocated / ExecutionTime.TotalSeconds : 0;
        public double RetentionRate => TotalAllocated > 0 ? (double)MemoryRetained / TotalAllocated : 0;
    }

    /// <summary>
    /// 基准数据
    /// </summary>
    public class BaselineData
    {
        public string OperationName { get; set; }
        public TimeSpan ExpectedTime { get; set; }
        public long ExpectedMemory { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 性能回归结果
    /// </summary>
    public class PerformanceRegressionResult
    {
        public string OperationName { get; set; }
        public bool HasRegression { get; set; }
        public bool TimeRegression { get; set; }
        public bool MemoryRegression { get; set; }
        public TimeSpan CurrentTime { get; set; }
        public TimeSpan ExpectedTime { get; set; }
        public long CurrentMemory { get; set; }
        public long ExpectedMemory { get; set; }
        public double TimeRatio { get; set; }
        public double MemoryRatio { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// 性能报告
    /// </summary>
    public class PerformanceReport
    {
        public DateTime GeneratedAt { get; set; }
        public TimeSpan? TimeRange { get; set; }
        public int TotalOperations { get; set; }
        public double TotalTime { get; set; }
        public long TotalMemoryAllocated { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public int RegressionCount { get; set; }
        public List<PerformanceProfile> Profiles { get; set; } = new();
        public List<PerformanceRegressionResult> Regressions { get; set; } = new();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== DotNetARX 性能报告 ===");
            sb.AppendLine($"生成时间: {GeneratedAt:yyyy-MM-dd HH:mm:ss}");

            if (TimeRange.HasValue)
                sb.AppendLine($"时间范围: 最近 {TimeRange.Value.TotalHours:F1} 小时");

            sb.AppendLine($"总操作数: {TotalOperations:N0}");
            sb.AppendLine($"总执行时间: {TotalTime:F2} ms");
            sb.AppendLine($"总内存分配: {TotalMemoryAllocated:N0} 字节");
            sb.AppendLine($"平均执行时间: {AverageExecutionTime.TotalMilliseconds:F2} ms");

            if (RegressionCount > 0)
            {
                sb.AppendLine($"⚠️  性能回归: {RegressionCount} 个");
                foreach (var regression in Regressions.Take(5))
                {
                    sb.AppendLine($"  - {regression.OperationName}: {regression.Message}");
                }
            }
            else
            {
                sb.AppendLine("✅ 无性能回归");
            }

            if (Profiles.Any())
            {
                sb.AppendLine("\n🏆 TOP 性能操作:");
                var topProfiles = Profiles
                    .OrderBy(p => p.AverageTime)
                    .Take(5);

                foreach (var profile in topProfiles)
                {
                    sb.AppendLine($"  {profile.OperationName}: {profile.AverageTime.TotalMilliseconds:F2}ms " +
                                 $"({profile.OperationsPerSecond:F0} ops/sec)");
                }

                sb.AppendLine("\n⏱️  最慢操作:");
                var slowestProfiles = Profiles
                    .OrderByDescending(p => p.AverageTime)
                    .Take(5);

                foreach (var profile in slowestProfiles)
                {
                    sb.AppendLine($"  {profile.OperationName}: {profile.AverageTime.TotalMilliseconds:F2}ms " +
                                 $"(内存: {profile.MemoryAllocated:N0} 字节)");
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// 基准测试运行信息
    /// </summary>
    public class BenchmarkRunInfo
    {
        public Summary Summary { get; set; }
        public DateTime RunTime { get; set; }
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }

        public BenchmarkRunInfo(Summary summary)
        {
            Summary = summary;
            RunTime = DateTime.UtcNow;
            IsSuccessful = summary.HasCriticalValidationErrors == false;

            if (!IsSuccessful)
            {
                ErrorMessage = string.Join("; ", summary.ValidationErrors);
            }
        }
    }

    /// <summary>
    /// 性能基准测试基类
    /// </summary>
    public abstract class DotNetARXBenchmarkBase
    {
        protected readonly ILogger _logger = LogManager.GetLogger(typeof(DotNetARXBenchmarkBase));

        [GlobalSetup]
        public virtual void Setup()
        {
            _logger.Info($"基准测试初始化: {GetType().Name}");
        }

        [GlobalCleanup]
        public virtual void Cleanup()
        {
            _logger.Info($"基准测试清理: {GetType().Name}");
        }
    }
}