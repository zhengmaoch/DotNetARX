using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DotNetARX.Performance
{
    /// <summary>
    /// 自动性能监控器
    /// 实时监控系统性能，提供智能优化建议
    /// </summary>
    public sealed class AutoPerformanceMonitor : IDisposable
    {
        private static readonly Lazy<AutoPerformanceMonitor> _instance = new(() => new AutoPerformanceMonitor());
        private readonly Timer _monitoringTimer;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, PerformanceMetrics> _metrics;
        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _memoryCounter;
        private readonly PerformanceCounter _diskCounter;
        private bool _disposed = false;

        public static AutoPerformanceMonitor Instance => _instance.Value;

        private AutoPerformanceMonitor()
        {
            _logger = LogManager.GetLogger(typeof(AutoPerformanceMonitor));
            _metrics = new ConcurrentDictionary<string, PerformanceMetrics>();

            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
                _diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
            }
            catch (Exception ex)
            {
                _logger.Warning($"性能计数器初始化失败: {ex.Message}");
            }

            // 每30秒监控一次系统性能
            _monitoringTimer = new Timer(MonitorSystemPerformance, null, 5000, 30000);
            _logger.Info("自动性能监控器已启动");
        }

        /// <summary>
        /// 开始监控操作
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IPerformanceScope StartMonitoring(string operationName)
        {
            return new PerformanceScope(operationName, this);
        }

        /// <summary>
        /// 记录操作性能
        /// </summary>
        internal void RecordMetrics(string operationName, TimeSpan duration, long memoryAllocated, Exception exception = null)
        {
            var metrics = _metrics.AddOrUpdate(operationName,
                new PerformanceMetrics(operationName),
                (key, existing) => existing);

            metrics.RecordExecution(duration, memoryAllocated, exception);

            // 检查是否需要警告
            CheckPerformanceThresholds(metrics);
        }

        /// <summary>
        /// 获取操作性能指标
        /// </summary>
        public PerformanceMetrics GetMetrics(string operationName)
        {
            return _metrics.TryGetValue(operationName, out var metrics) ? metrics : null;
        }

        /// <summary>
        /// 获取所有性能指标
        /// </summary>
        public IEnumerable<PerformanceMetrics> GetAllMetrics()
        {
            return _metrics.Values.ToList();
        }

        /// <summary>
        /// 获取系统性能摘要
        /// </summary>
        public SystemPerformanceSummary GetSystemSummary()
        {
            var summary = new SystemPerformanceSummary
            {
                TotalOperations = _metrics.Values.Sum(m => m.ExecutionCount),
                AverageExecutionTime = TimeSpan.FromMilliseconds(_metrics.Values.Average(m => m.AverageExecutionTime.TotalMilliseconds)),
                TotalMemoryAllocated = _metrics.Values.Sum(m => m.TotalMemoryAllocated),
                ErrorRate = _metrics.Values.Sum(m => m.ErrorCount) / (double)Math.Max(1, _metrics.Values.Sum(m => m.ExecutionCount)),
                TopSlowOperations = _metrics.Values
                    .OrderByDescending(m => m.AverageExecutionTime)
                    .Take(10)
                    .ToList(),
                TopMemoryOperations = _metrics.Values
                    .OrderByDescending(m => m.AverageMemoryUsage)
                    .Take(10)
                    .ToList(),
                GeneratedAt = DateTime.UtcNow
            };

            return summary;
        }

        /// <summary>
        /// 获取性能建议
        /// </summary>
        public List<PerformanceRecommendation> GetRecommendations()
        {
            var recommendations = new List<PerformanceRecommendation>();

            foreach (var metrics in _metrics.Values)
            {
                // 检查执行时间过长的操作
                if (metrics.AverageExecutionTime.TotalMilliseconds > 1000)
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        OperationName = metrics.OperationName,
                        Type = RecommendationType.SlowExecution,
                        Severity = PerformanceSeverity.High,
                        Message = $"操作 '{metrics.OperationName}' 平均执行时间过长 ({metrics.AverageExecutionTime.TotalMilliseconds:F0}ms)",
                        Suggestion = "考虑优化算法、使用缓存或异步处理"
                    });
                }

                // 检查内存使用过多的操作
                if (metrics.AverageMemoryUsage > 50 * 1024 * 1024) // 50MB
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        OperationName = metrics.OperationName,
                        Type = RecommendationType.HighMemoryUsage,
                        Severity = PerformanceSeverity.Medium,
                        Message = $"操作 '{metrics.OperationName}' 内存使用过多 ({metrics.AverageMemoryUsage / 1024 / 1024:F1}MB)",
                        Suggestion = "检查是否有内存泄漏，考虑使用对象池或及时释放资源"
                    });
                }

                // 检查错误率高的操作
                if (metrics.ErrorRate > 0.1) // 10%
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        OperationName = metrics.OperationName,
                        Type = RecommendationType.HighErrorRate,
                        Severity = PerformanceSeverity.Critical,
                        Message = $"操作 '{metrics.OperationName}' 错误率过高 ({metrics.ErrorRate:P1})",
                        Suggestion = "检查输入验证、异常处理和业务逻辑"
                    });
                }

                // 检查性能退化
                if (metrics.PerformanceTrend < -0.2) // 性能下降20%
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        OperationName = metrics.OperationName,
                        Type = RecommendationType.PerformanceDegradation,
                        Severity = PerformanceSeverity.High,
                        Message = $"操作 '{metrics.OperationName}' 性能呈下降趋势 ({metrics.PerformanceTrend:P1})",
                        Suggestion = "分析最近的代码变更，检查是否引入了性能问题"
                    });
                }
            }

            return recommendations.OrderByDescending(r => r.Severity).ToList();
        }

        /// <summary>
        /// 重置所有性能指标
        /// </summary>
        public void ResetMetrics()
        {
            _metrics.Clear();
            _logger.Info("所有性能指标已重置");
        }

        /// <summary>
        /// 监控系统性能
        /// </summary>
        private void MonitorSystemPerformance(object state)
        {
            try
            {
                var cpuUsage = 0f;
                var availableMemory = 0f;
                var diskUsage = 0f;

                if (_cpuCounter != null)
                {
                    cpuUsage = _cpuCounter.NextValue();
                }

                if (_memoryCounter != null)
                {
                    availableMemory = _memoryCounter.NextValue();
                }

                if (_diskCounter != null)
                {
                    diskUsage = _diskCounter.NextValue();
                }

                var workingSet = Environment.WorkingSet;
                var gcMemory = GC.GetTotalMemory(false);

                // 检查系统资源压力
                if (cpuUsage > 80)
                {
                    _logger.Warning($"CPU使用率过高: {cpuUsage:F1}%");
                }

                if (availableMemory > 0 && availableMemory < 512) // 小于512MB
                {
                    _logger.Warning($"可用内存不足: {availableMemory:F0}MB");
                }

                if (diskUsage > 90)
                {
                    _logger.Warning($"磁盘使用率过高: {diskUsage:F1}%");
                }

                // 记录系统性能到日志（Debug级别）
                _logger.Debug($"系统性能 - CPU: {cpuUsage:F1}%, 可用内存: {availableMemory:F0}MB, " +
                             $"工作集: {workingSet / 1024 / 1024:F0}MB, GC内存: {gcMemory / 1024 / 1024:F0}MB");
            }
            catch (Exception ex)
            {
                _logger.Error("监控系统性能时发生错误", ex);
            }
        }

        /// <summary>
        /// 检查性能阈值
        /// </summary>
        private void CheckPerformanceThresholds(PerformanceMetrics metrics)
        {
            // 执行时间阈值检查
            if (metrics.AverageExecutionTime.TotalMilliseconds > 5000) // 5秒
            {
                _logger.Warning($"操作 '{metrics.OperationName}' 执行时间过长: {metrics.AverageExecutionTime.TotalMilliseconds:F0}ms");
            }

            // 内存使用阈值检查
            if (metrics.AverageMemoryUsage > 100 * 1024 * 1024) // 100MB
            {
                _logger.Warning($"操作 '{metrics.OperationName}' 内存使用过多: {metrics.AverageMemoryUsage / 1024 / 1024:F1}MB");
            }

            // 错误率阈值检查
            if (metrics.ErrorRate > 0.05) // 5%
            {
                _logger.Warning($"操作 '{metrics.OperationName}' 错误率过高: {metrics.ErrorRate:P1}");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _monitoringTimer?.Dispose();
                _cpuCounter?.Dispose();
                _memoryCounter?.Dispose();
                _diskCounter?.Dispose();
                _disposed = true;
                _logger.Info("自动性能监控器已停止");
            }
        }
    }

    /// <summary>
    /// 性能监控作用域
    /// </summary>
    public interface IPerformanceScope : IDisposable
    {
        string OperationName { get; }
        TimeSpan ElapsedTime { get; }
    }

    /// <summary>
    /// 性能作用域实现
    /// </summary>
    internal sealed class PerformanceScope : IPerformanceScope
    {
        private readonly string _operationName;
        private readonly AutoPerformanceMonitor _monitor;
        private readonly Stopwatch _stopwatch;
        private readonly long _startMemory;
        private bool _disposed = false;

        public string OperationName => _operationName;
        public TimeSpan ElapsedTime => _stopwatch.Elapsed;

        public PerformanceScope(string operationName, AutoPerformanceMonitor monitor)
        {
            _operationName = operationName;
            _monitor = monitor;
            _stopwatch = Stopwatch.StartNew();
            _startMemory = GC.GetTotalMemory(false);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _stopwatch.Stop();
                var endMemory = GC.GetTotalMemory(false);
                var memoryAllocated = Math.Max(0, endMemory - _startMemory);

                _monitor.RecordMetrics(_operationName, _stopwatch.Elapsed, memoryAllocated);
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 性能指标
    /// </summary>
    public class PerformanceMetrics
    {
        private readonly object _lock = new object();
        private readonly Queue<double> _recentExecutionTimes = new Queue<double>();
        private const int MaxRecentSamples = 100;

        public string OperationName { get; }
        public long ExecutionCount { get; private set; }
        public long ErrorCount { get; private set; }
        public TimeSpan TotalExecutionTime { get; private set; }
        public TimeSpan AverageExecutionTime { get; private set; }
        public TimeSpan MinExecutionTime { get; private set; } = TimeSpan.MaxValue;
        public TimeSpan MaxExecutionTime { get; private set; }
        public long TotalMemoryAllocated { get; private set; }
        public long AverageMemoryUsage { get; private set; }
        public double ErrorRate => ExecutionCount > 0 ? (double)ErrorCount / ExecutionCount : 0;
        public DateTime FirstExecution { get; private set; }
        public DateTime LastExecution { get; private set; }
        public double PerformanceTrend { get; private set; }

        public PerformanceMetrics(string operationName)
        {
            OperationName = operationName;
            FirstExecution = DateTime.UtcNow;
        }

        public void RecordExecution(TimeSpan duration, long memoryAllocated, Exception exception = null)
        {
            lock (_lock)
            {
                ExecutionCount++;
                LastExecution = DateTime.UtcNow;

                if (exception != null)
                {
                    ErrorCount++;
                    return; // 不计入性能统计
                }

                TotalExecutionTime = TotalExecutionTime.Add(duration);
                AverageExecutionTime = TimeSpan.FromTicks(TotalExecutionTime.Ticks / ExecutionCount);

                if (duration < MinExecutionTime)
                    MinExecutionTime = duration;
                if (duration > MaxExecutionTime)
                    MaxExecutionTime = duration;

                TotalMemoryAllocated += memoryAllocated;
                AverageMemoryUsage = TotalMemoryAllocated / ExecutionCount;

                // 计算性能趋势
                _recentExecutionTimes.Enqueue(duration.TotalMilliseconds);
                if (_recentExecutionTimes.Count > MaxRecentSamples)
                {
                    _recentExecutionTimes.Dequeue();
                }

                CalculatePerformanceTrend();
            }
        }

        private void CalculatePerformanceTrend()
        {
            if (_recentExecutionTimes.Count < 10) return;

            var times = _recentExecutionTimes.ToArray();
            var halfCount = times.Length / 2;

            var firstHalfAvg = times.Take(halfCount).Average();
            var secondHalfAvg = times.Skip(halfCount).Average();

            if (firstHalfAvg > 0)
            {
                PerformanceTrend = (secondHalfAvg - firstHalfAvg) / firstHalfAvg;
            }
        }
    }

    /// <summary>
    /// 系统性能摘要
    /// </summary>
    public class SystemPerformanceSummary
    {
        public long TotalOperations { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public long TotalMemoryAllocated { get; set; }
        public double ErrorRate { get; set; }
        public List<PerformanceMetrics> TopSlowOperations { get; set; } = new();
        public List<PerformanceMetrics> TopMemoryOperations { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// 性能建议
    /// </summary>
    public class PerformanceRecommendation
    {
        public string OperationName { get; set; }
        public RecommendationType Type { get; set; }
        public PerformanceSeverity Severity { get; set; }
        public string Message { get; set; }
        public string Suggestion { get; set; }
    }

    /// <summary>
    /// 建议类型
    /// </summary>
    public enum RecommendationType
    {
        SlowExecution,
        HighMemoryUsage,
        HighErrorRate,
        PerformanceDegradation
    }

    /// <summary>
    /// 性能严重程度
    /// </summary>
    public enum PerformanceSeverity
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }
}