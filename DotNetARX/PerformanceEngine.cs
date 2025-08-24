using System.Diagnostics;
using PerformanceAnalyzer = DotNetARX.Performance.PerformanceAnalyzer;

namespace DotNetARX
{
    /// <summary>
    /// 智能性能优化引擎
    /// 自动检测批处理机会、性能监控、智能优化
    /// </summary>
    public static class PerformanceEngine
    {
        private static readonly ConcurrentDictionary<string, PerformanceMetrics> _metrics = new();
        private static readonly ConcurrentQueue<PendingOperation> _batchQueue = new();
        private static readonly Timer _batchTimer;
        private static bool _initialized = false;

        static PerformanceEngine()
        {
            _batchTimer = new Timer(FlushBatch, null, 10, 10); // 10ms检查间隔
        }

        /// <summary>
        /// 初始化性能引擎
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            // 初始化性能监控组件
            var monitor = AutoPerformanceMonitor.Instance; // 启动自动监控

            // 启用性能分析器
            PerformanceAnalyzer.IsEnabled = true;

            // 设置默认基准数据
            SetupDefaultBaselines();

            _initialized = true;
        }

        /// <summary>
        /// 智能执行 - 自动选择最优路径
        /// </summary>
        public static T Execute<T>(string operationName, Func<T> operation)
        {
            if (!_initialized) Initialize();

            // 使用自动性能监控器
            using var scope = AutoPerformanceMonitor.Instance.StartMonitoring(operationName);

            var stopwatch = Stopwatch.StartNew();
            try
            {
                // 检查是否应该批处理
                if (ShouldBatch(operationName))
                {
                    return ExecuteWithBatching(operationName, operation);
                }

                // 直接执行并监控性能
                var result = operation();
                stopwatch.Stop();

                RecordSuccess(operationName, stopwatch.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                RecordFailure(operationName, stopwatch.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// 智能批处理检测
        /// </summary>
        private static bool ShouldBatch(string operationName)
        {
            var metrics = _metrics.GetOrAdd(operationName, _ => new PerformanceMetrics());

            // 基于历史数据智能判断是否需要批处理
            lock (metrics)
            {
                var now = DateTime.Now;

                // 清理过期的执行记录（超过1秒）
                while (metrics.RecentExecutions.Count > 0 &&
                       now - metrics.RecentExecutions.Peek() > TimeSpan.FromSeconds(1))
                {
                    metrics.RecentExecutions.Dequeue();
                }

                // 记录当前执行
                metrics.RecentExecutions.Enqueue(now);

                // 判断批处理条件：
                // 1. 在短时间内(100ms)有多次相同操作
                // 2. 操作频率较高(>5次/秒)
                var recentCount = metrics.RecentExecutions.Count(t => now - t < TimeSpan.FromMilliseconds(100));
                var frequencyCount = metrics.RecentExecutions.Count;

                return recentCount >= 3 && frequencyCount >= 5;
            }
        }

        /// <summary>
        /// 带批处理的执行
        /// </summary>
        private static T ExecuteWithBatching<T>(string operationName, Func<T> operation)
        {
            var pendingOp = new PendingOperation<T>
            {
                OperationType = operationName,
                Operation = operation,
                CompletionSource = new TaskCompletionSource<T>()
            };

            _batchQueue.Enqueue(pendingOp);

            // 等待批处理完成
            return pendingOp.CompletionSource.Task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// 批处理队列处理
        /// </summary>
        private static void FlushBatch(object state)
        {
            if (_batchQueue.IsEmpty) return;

            var operations = new List<PendingOperation>();

            // 收集一批操作（最多100个或1ms超时）
            var deadline = DateTime.Now.AddMilliseconds(1);
            while (_batchQueue.TryDequeue(out var op) &&
                   operations.Count < 100 &&
                   DateTime.Now < deadline)
            {
                operations.Add(op);
            }

            if (operations.Count == 0) return;

            // 按操作类型分组并并行处理
            var groups = operations.GroupBy(op => op.OperationType);

            Parallel.ForEach(groups, group =>
            {
                ProcessOperationGroup(group);
            });
        }

        /// <summary>
        /// 处理同类型操作组
        /// </summary>
        private static void ProcessOperationGroup(IGrouping<string, PendingOperation> group)
        {
            try
            {
                // 在单个AutoCAD上下文中批量执行
                AutoCADContext.ExecuteBatch(context =>
                {
                    foreach (var op in group)
                    {
                        try
                        {
                            var result = op.ExecuteOperation();
                            op.SetResult(result);
                        }
                        catch (Exception ex)
                        {
                            op.SetException(ex);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                // 如果批处理失败，将异常传播给所有操作
                foreach (var op in group)
                {
                    op.SetException(ex);
                }
            }
        }

        /// <summary>
        /// 记录成功执行
        /// </summary>
        private static void RecordSuccess(string operationName, TimeSpan duration)
        {
            var metrics = _metrics.GetOrAdd(operationName, _ => new PerformanceMetrics());

            lock (metrics)
            {
                metrics.TotalExecutions++;
                metrics.TotalDuration += duration;
                metrics.LastExecutionTime = duration;

                // 更新平均执行时间
                metrics.AverageExecutionTime = metrics.TotalDuration.TotalMilliseconds / metrics.TotalExecutions;
            }
        }

        /// <summary>
        /// 记录失败执行
        /// </summary>
        private static void RecordFailure(string operationName, TimeSpan duration, Exception exception)
        {
            var metrics = _metrics.GetOrAdd(operationName, _ => new PerformanceMetrics());

            lock (metrics)
            {
                metrics.TotalExecutions++;
                metrics.FailureCount++;
                metrics.LastException = exception;

                // 记录到日志（如果可用）
                try
                {
                    LogManager.GetLogger(typeof(PerformanceEngine))?.Error($"操作失败: {operationName}", exception);
                }
                catch
                {
                    // 忽略日志记录失败
                }
            }
        }

        /// <summary>
        /// 生成性能报告
        /// </summary>
        public static string GenerateReport()
        {
            var report = new StringBuilder();
            report.AppendLine("=== DotNetARX 性能报告 ===");
            report.AppendLine($"报告生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();

            if (_metrics.IsEmpty)
            {
                report.AppendLine("暂无性能数据");
                return report.ToString();
            }

            report.AppendLine("操作性能统计:");
            report.AppendLine(new string('-', 80));
            report.AppendLine($"{"操作名称",-20} {"执行次数",8} {"平均耗时(ms)",12} {"失败次数",8} {"成功率(%)",10}");
            report.AppendLine(new string('-', 80));

            foreach (var kvp in _metrics.OrderByDescending(x => x.Value.TotalExecutions))
            {
                var name = kvp.Key;
                var metrics = kvp.Value;

                lock (metrics)
                {
                    var successRate = metrics.TotalExecutions > 0
                        ? (double)(metrics.TotalExecutions - metrics.FailureCount) / metrics.TotalExecutions * 100
                        : 0;

                    report.AppendLine($"{name,-20} {metrics.TotalExecutions,8} {metrics.AverageExecutionTime,12:F2} {metrics.FailureCount,8} {successRate,10:F1}");
                }
            }

            report.AppendLine(new string('-', 80));

            // 添加系统信息
            report.AppendLine();
            report.AppendLine("系统信息:");
            report.AppendLine($"初始化状态: {(_initialized ? "已初始化" : "未初始化")}");
            report.AppendLine($"批处理队列: {_batchQueue.Count} 个待处理操作");
            report.AppendLine($"监控的操作类型: {_metrics.Count} 种");

            return report.ToString();
        }

        /// <summary>
        /// 重置性能统计
        /// </summary>
        public static void Reset()
        {
            _metrics.Clear();

            // 清空批处理队列
            while (_batchQueue.TryDequeue(out var op))
            {
                op?.SetException(new OperationCanceledException("性能引擎重置"));
            }
        }

        /// <summary>
        /// 获取特定操作的性能指标
        /// </summary>
        public static PerformanceMetrics GetMetrics(string operationName)
        {
            return _metrics.TryGetValue(operationName, out var metrics) ? metrics : null;
        }

        /// <summary>
        /// 设置默认基准数据
        /// </summary>
        private static void SetupDefaultBaselines()
        {
            // CAD操作基准
            PerformanceAnalyzer.SetBaseline("Move", TimeSpan.FromMilliseconds(1), 512);
            PerformanceAnalyzer.SetBaseline("Copy", TimeSpan.FromMilliseconds(2), 1024);
            PerformanceAnalyzer.SetBaseline("Rotate", TimeSpan.FromMilliseconds(1), 256);
            PerformanceAnalyzer.SetBaseline("Scale", TimeSpan.FromMilliseconds(1), 256);
            PerformanceAnalyzer.SetBaseline("Line", TimeSpan.FromMilliseconds(1), 512);
            PerformanceAnalyzer.SetBaseline("Circle", TimeSpan.FromMilliseconds(1), 512);
            PerformanceAnalyzer.SetBaseline("Arc", TimeSpan.FromMilliseconds(1), 512);
            PerformanceAnalyzer.SetBaseline("Text", TimeSpan.FromMilliseconds(2), 1024);

            // 图层操作基准
            PerformanceAnalyzer.SetBaseline("CreateLayer", TimeSpan.FromMilliseconds(5), 2048);
            PerformanceAnalyzer.SetBaseline("LayerExists", TimeSpan.FromMilliseconds(100), 64);

            // 查询操作基准
            PerformanceAnalyzer.SetBaseline("SelectByType", TimeSpan.FromMilliseconds(10), 4096);
            PerformanceAnalyzer.SetBaseline("Select", TimeSpan.FromMilliseconds(20), 8192);
        }
    }

    /// <summary>
    /// 性能指标
    /// </summary>
    public class PerformanceMetrics
    {
        public int TotalExecutions { get; set; }
        public int FailureCount { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public double AverageExecutionTime { get; set; }
        public TimeSpan LastExecutionTime { get; set; }
        public Exception LastException { get; set; }
        public Queue<DateTime> RecentExecutions { get; } = new(capacity: 100);
    }

    /// <summary>
    /// 待处理操作基类
    /// </summary>
    public abstract class PendingOperation
    {
        public string OperationType { get; set; }

        public abstract object ExecuteOperation();

        public abstract void SetResult(object result);

        public abstract void SetException(Exception exception);
    }

    /// <summary>
    /// 泛型待处理操作
    /// </summary>
    public class PendingOperation<T> : PendingOperation
    {
        public Func<T> Operation { get; set; }
        public TaskCompletionSource<T> CompletionSource { get; set; }

        public override object ExecuteOperation()
        {
            return Operation();
        }

        public override void SetResult(object result)
        {
            CompletionSource.SetResult((T)result);
        }

        public override void SetException(Exception exception)
        {
            CompletionSource.SetException(exception);
        }
    }
}