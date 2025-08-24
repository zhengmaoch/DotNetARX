using System.Diagnostics;

namespace DotNetARX.Performance
{
    /// <summary>
    /// 性能指标类型
    /// </summary>
    public enum MetricType
    {
        /// <summary>
        /// 执行时间
        /// </summary>
        ExecutionTime,

        /// <summary>
        /// 内存使用
        /// </summary>
        MemoryUsage,

        /// <summary>
        /// 操作计数
        /// </summary>
        OperationCount,

        /// <summary>
        /// 错误计数
        /// </summary>
        ErrorCount,

        /// <summary>
        /// 自定义指标
        /// </summary>
        Custom
    }

    /// <summary>
    /// 性能指标数据
    /// </summary>
    public class PerformanceMetric
    {
        public string Name { get; set; }
        public MetricType Type { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }
        public DateTime Timestamp { get; set; }
        public string Category { get; set; }
        public Dictionary<string, object> Properties { get; set; }

        public PerformanceMetric()
        {
            Properties = new Dictionary<string, object>();
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 性能统计信息
    /// </summary>
    public class PerformanceStatistics
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public int Count { get; set; }
        public double TotalValue { get; set; }
        public double AverageValue { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public double StandardDeviation { get; set; }
        public TimeSpan TimeSpan { get; set; }
        public DateTime FirstRecorded { get; set; }
        public DateTime LastRecorded { get; set; }
    }

    /// <summary>
    /// 性能监控器接口
    /// </summary>
    public interface IPerformanceMonitor
    {
        /// <summary>
        /// 记录性能指标
        /// </summary>
        void RecordMetric(string name, double value, MetricType type = MetricType.Custom, string unit = null, string category = null);

        /// <summary>
        /// 开始计时
        /// </summary>
        IDisposable StartTimer(string operationName, string category = null);

        /// <summary>
        /// 开始操作监控
        /// </summary>
        IDisposable StartOperation(string operationName, string category = null);

        /// <summary>
        /// 记录执行时间
        /// </summary>
        void RecordExecutionTime(string operationName, TimeSpan duration, string category = null);

        /// <summary>
        /// 记录内存使用
        /// </summary>
        void RecordMemoryUsage(string operationName, long memoryBytes, string category = null);

        /// <summary>
        /// 增加操作计数
        /// </summary>
        void IncrementCounter(string counterName, string category = null);

        /// <summary>
        /// 增加错误计数
        /// </summary>
        void IncrementErrorCounter(string operationName, string category = null);

        /// <summary>
        /// 获取性能统计
        /// </summary>
        PerformanceStatistics GetStatistics(string name, string category = null);

        /// <summary>
        /// 获取所有统计信息
        /// </summary>
        IEnumerable<PerformanceStatistics> GetAllStatistics();

        /// <summary>
        /// 清除统计数据
        /// </summary>
        void ClearStatistics();

        /// <summary>
        /// 导出性能报告
        /// </summary>
        string GenerateReport();
    }

    /// <summary>
    /// 计时器实现
    /// </summary>
    public class PerformanceTimer : IDisposable
    {
        private readonly IPerformanceMonitor _monitor;
        private readonly string _operationName;
        private readonly string _category;
        private readonly Stopwatch _stopwatch;
        private bool _disposed = false;

        public PerformanceTimer(IPerformanceMonitor monitor, string operationName, string category)
        {
            _monitor = monitor;
            _operationName = operationName;
            _category = category;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _stopwatch.Stop();
                _monitor.RecordExecutionTime(_operationName, _stopwatch.Elapsed, _category);
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 性能监控器实现
    /// </summary>
    public class PerformanceMonitor : IPerformanceMonitor, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IConfigurationManager _config;
        private readonly ConcurrentDictionary<string, List<PerformanceMetric>> _metrics;
        private readonly Timer _reportTimer;
        private readonly object _lockObject = new object();
        private bool _disposed = false;

        public PerformanceMonitor(ILogger logger = null, IConfigurationManager config = null)
        {
            _logger = logger ?? LogManager.GetLogger(typeof(PerformanceMonitor));
            _config = config ?? GlobalConfiguration.Instance;
            _metrics = new ConcurrentDictionary<string, List<PerformanceMetric>>();

            // 定期生成性能报告
            var reportInterval = _config.GetSetting("Performance.ReportInterval", 300000); // 5分钟
            _reportTimer = new Timer(GeneratePeriodicReport, null, TimeSpan.FromMilliseconds(reportInterval),
                                   TimeSpan.FromMilliseconds(reportInterval));
        }

        public void RecordMetric(string name, double value, MetricType type = MetricType.Custom, string unit = null, string category = null)
        {
            if (string.IsNullOrEmpty(name)) return;

            var metric = new PerformanceMetric
            {
                Name = name,
                Type = type,
                Value = value,
                Unit = unit,
                Category = category
            };

            var key = GetMetricKey(name, category);
            _metrics.AddOrUpdate(key, new List<PerformanceMetric> { metric },
                (k, existing) =>
                {
                    lock (existing)
                    {
                        existing.Add(metric);

                        // 限制内存中保留的指标数量
                        var maxMetrics = _config.GetSetting("Performance.MaxMetricsPerKey", 1000);
                        if (existing.Count > maxMetrics)
                        {
                            existing.RemoveRange(0, existing.Count - maxMetrics);
                        }
                    }
                    return existing;
                });

            _logger.Debug($"记录性能指标: {name} = {value} {unit ?? ""} (类别: {category ?? "默认"})");
        }

        public IDisposable StartTimer(string operationName, string category = null)
        {
            return new PerformanceTimer(this, operationName, category);
        }

        public IDisposable StartOperation(string operationName, string category = null)
        {
            return StartTimer(operationName, category);
        }

        public void RecordExecutionTime(string operationName, TimeSpan duration, string category = null)
        {
            RecordMetric(operationName, duration.TotalMilliseconds, MetricType.ExecutionTime, "ms", category);
        }

        public void RecordMemoryUsage(string operationName, long memoryBytes, string category = null)
        {
            RecordMetric(operationName, memoryBytes, MetricType.MemoryUsage, "bytes", category);
        }

        public void IncrementCounter(string counterName, string category = null)
        {
            RecordMetric(counterName, 1, MetricType.OperationCount, "count", category);
        }

        public void IncrementErrorCounter(string operationName, string category = null)
        {
            RecordMetric($"{operationName}_Error", 1, MetricType.ErrorCount, "count", category);
        }

        public PerformanceStatistics GetStatistics(string name, string category = null)
        {
            var key = GetMetricKey(name, category);
            if (!_metrics.TryGetValue(key, out var metricsList))
            {
                return null;
            }

            List<PerformanceMetric> metrics;
            lock (metricsList)
            {
                metrics = metricsList.ToList();
            }

            if (metrics.Count == 0) return null;

            var values = metrics.Select(m => m.Value).ToArray();
            var mean = values.Average();
            var variance = values.Select(v => Math.Pow(v - mean, 2)).Average();
            var standardDeviation = Math.Sqrt(variance);

            return new PerformanceStatistics
            {
                Name = name,
                Category = category,
                Count = metrics.Count,
                TotalValue = values.Sum(),
                AverageValue = mean,
                MinValue = values.Min(),
                MaxValue = values.Max(),
                StandardDeviation = standardDeviation,
                FirstRecorded = metrics.Min(m => m.Timestamp),
                LastRecorded = metrics.Max(m => m.Timestamp),
                TimeSpan = metrics.Max(m => m.Timestamp) - metrics.Min(m => m.Timestamp)
            };
        }

        public IEnumerable<PerformanceStatistics> GetAllStatistics()
        {
            var statistics = new List<PerformanceStatistics>();

            foreach (var kvp in _metrics)
            {
                var parts = kvp.Key.Split('|');
                var name = parts[0];
                var category = parts.Length > 1 ? parts[1] : null;

                var stat = GetStatistics(name, category);
                if (stat != null)
                {
                    statistics.Add(stat);
                }
            }

            return statistics;
        }

        public void ClearStatistics()
        {
            lock (_lockObject)
            {
                _metrics.Clear();
                _logger.Info("性能统计数据已清除");
            }
        }

        public string GenerateReport()
        {
            var statistics = GetAllStatistics().ToList();
            if (statistics.Count == 0)
            {
                return "没有可用的性能统计数据。";
            }

            var report = new System.Text.StringBuilder();
            report.AppendLine("=== DotNetARX 性能监控报告 ===");
            report.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"统计项目数: {statistics.Count}");
            report.AppendLine();

            // 按类别分组
            var groupedStats = statistics.GroupBy(s => s.Category ?? "默认").ToList();

            foreach (var group in groupedStats)
            {
                report.AppendLine($"类别: {group.Key}");
                report.AppendLine(new string('-', 50));

                foreach (var stat in group.OrderBy(s => s.Name))
                {
                    report.AppendLine($"指标: {stat.Name}");
                    report.AppendLine($"  计数: {stat.Count}");
                    report.AppendLine($"  平均值: {stat.AverageValue:F2}");
                    report.AppendLine($"  最小值: {stat.MinValue:F2}");
                    report.AppendLine($"  最大值: {stat.MaxValue:F2}");
                    report.AppendLine($"  标准差: {stat.StandardDeviation:F2}");
                    report.AppendLine($"  时间跨度: {stat.TimeSpan.TotalMinutes:F1} 分钟");
                    report.AppendLine();
                }
            }

            // 性能热点分析
            var topByAverage = statistics.Where(s => s.Count > 5)
                                       .OrderByDescending(s => s.AverageValue)
                                       .Take(5)
                                       .ToList();

            if (topByAverage.Count > 0)
            {
                report.AppendLine("=== 性能热点 (按平均值排序) ===");
                foreach (var stat in topByAverage)
                {
                    report.AppendLine($"{stat.Name}: {stat.AverageValue:F2} (执行{stat.Count}次)");
                }
                report.AppendLine();
            }

            return report.ToString();
        }

        private string GetMetricKey(string name, string category)
        {
            return string.IsNullOrEmpty(category) ? name : $"{name}|{category}";
        }

        private void GeneratePeriodicReport(object state)
        {
            try
            {
                var report = GenerateReport();
                _logger.Info($"定期性能报告:\n{report}");
            }
            catch (Exception ex)
            {
                _logger.Error("生成定期性能报告失败", ex);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _reportTimer?.Dispose();

                // 生成最终报告
                try
                {
                    var finalReport = GenerateReport();
                    _logger.Info($"最终性能报告:\n{finalReport}");
                }
                catch (Exception ex)
                {
                    _logger.Error("生成最终性能报告失败", ex);
                }

                _metrics.Clear();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 全局性能监控器
    /// </summary>
    public static class GlobalPerformanceMonitor
    {
        private static readonly Lazy<PerformanceMonitor> _instance =
            new Lazy<PerformanceMonitor>(() => new PerformanceMonitor());

        public static IPerformanceMonitor Instance => _instance.Value;

        /// <summary>
        /// 记录性能指标
        /// </summary>
        public static void RecordMetric(string name, double value, MetricType type = MetricType.Custom, string unit = null, string category = null)
        {
            Instance.RecordMetric(name, value, type, unit, category);
        }

        /// <summary>
        /// 开始计时
        /// </summary>
        public static IDisposable StartTimer(string operationName, string category = null)
        {
            return Instance.StartTimer(operationName, category);
        }

        /// <summary>
        /// 记录执行时间
        /// </summary>
        public static void RecordExecutionTime(string operationName, TimeSpan duration, string category = null)
        {
            Instance.RecordExecutionTime(operationName, duration, category);
        }

        /// <summary>
        /// 增加计数器
        /// </summary>
        public static void IncrementCounter(string counterName, string category = null)
        {
            Instance.IncrementCounter(counterName, category);
        }

        /// <summary>
        /// 增加错误计数
        /// </summary>
        public static void IncrementErrorCounter(string operationName, string category = null)
        {
            Instance.IncrementErrorCounter(operationName, category);
        }

        /// <summary>
        /// 生成性能报告
        /// </summary>
        public static string GenerateReport()
        {
            return Instance.GenerateReport();
        }
    }
}