using System.Diagnostics;

namespace DotNetARX.Diagnostics
{
    /// <summary>
    /// AutoCAD上下文分析器
    /// </summary>
    public class AutoCADContextAnalyzer : IDiagnosticAnalyzer
    {
        public string Name => "AutoCAD Context Analyzer";
        public string Description => "分析AutoCAD上下文状态和连接性";

        public DiagnosticResult Analyze()
        {
            var result = new DiagnosticResult
            {
                AnalyzerName = Name,
                Severity = DiagnosticSeverity.Info
            };

            try
            {
                // 检查AutoCAD应用程序状态
                var app = Application.DocumentManager;
                if (app == null)
                {
                    result.Severity = DiagnosticSeverity.Error;
                    result.Message = "无法访问AutoCAD应用程序";
                    result.Recommendations.Add("确保AutoCAD正在运行");
                    return result;
                }

                var activeDoc = app.MdiActiveDocument;
                if (activeDoc == null)
                {
                    result.Severity = DiagnosticSeverity.Warning;
                    result.Message = "没有活动的AutoCAD文档";
                    result.Recommendations.Add("打开一个AutoCAD文档以使用DotNetARX功能");
                }
                else
                {
                    result.Message = $"AutoCAD上下文正常，活动文档: {activeDoc.Name}";

                    // 检查文档状态
                    var database = activeDoc.Database;
                    result.Metrics["DocumentName"] = activeDoc.Name;
                    result.Metrics["EntityCount"] = CountEntities(database);
                    result.Metrics["LayerCount"] = CountLayers(database);
                }

                // 检查DotNetARX初始化状态
                result.Metrics["DotNetARXInitialized"] = CAD.IsInitialized;
                if (!CAD.IsInitialized)
                {
                    result.Recommendations.Add("调用任何CAD.*方法以初始化DotNetARX");
                }

                // 检查线程安全
                var isMainThread = Thread.CurrentThread.ManagedThreadId == 1;
                result.Metrics["IsMainThread"] = isMainThread;
                if (!isMainThread)
                {
                    result.Severity = DiagnosticSeverity.Warning;
                    result.Message += " (在非主线程中执行)";
                    result.Recommendations.Add("确保AutoCAD操作在主线程中执行");
                }
            }
            catch (Exception ex)
            {
                result.Severity = DiagnosticSeverity.Error;
                result.Message = $"AutoCAD上下文分析失败: {ex.Message}";
                result.Details = ex.ToString();
                result.Recommendations.Add("检查AutoCAD安装和DotNetARX配置");
            }

            return result;
        }

        private int CountEntities(Database database)
        {
            try
            {
                using var transaction = database.TransactionManager.StartTransaction();
                var modelSpace = transaction.GetObject(database.GetModelSpaceId(), OpenMode.ForRead) as BlockTableRecord;
                return modelSpace?.Count ?? 0;
            }
            catch
            {
                return -1;
            }
        }

        private int CountLayers(Database database)
        {
            try
            {
                using var transaction = database.TransactionManager.StartTransaction();
                var layerTable = transaction.GetObject(database.LayerTableId, OpenMode.ForRead) as LayerTable;
                return layerTable?.Count ?? 0;
            }
            catch
            {
                return -1;
            }
        }
    }

    /// <summary>
    /// 性能分析器
    /// </summary>
    public class PerformanceAnalyzer : IDiagnosticAnalyzer
    {
        public string Name => "Performance Analyzer";
        public string Description => "分析系统性能指标和瓶颈";

        public DiagnosticResult Analyze()
        {
            var result = new DiagnosticResult
            {
                AnalyzerName = Name,
                Severity = DiagnosticSeverity.Info
            };

            try
            {
                // 获取性能摘要
                var perfSummary = AutoPerformanceMonitor.Instance.GetSystemSummary();

                result.Metrics["TotalOperations"] = perfSummary.TotalOperations;
                result.Metrics["AverageExecutionTime"] = perfSummary.AverageExecutionTime.TotalMilliseconds;
                result.Metrics["ErrorRate"] = perfSummary.ErrorRate;
                result.Metrics["TotalMemoryAllocated"] = perfSummary.TotalMemoryAllocated;

                // 性能评估
                var issues = new List<string>();

                if (perfSummary.ErrorRate > 0.1) // 10%
                {
                    result.Severity = DiagnosticSeverity.Error;
                    issues.Add($"错误率过高: {perfSummary.ErrorRate:P1}");
                    result.Recommendations.Add("检查错误日志并修复相关问题");
                }

                if (perfSummary.AverageExecutionTime.TotalMilliseconds > 1000) // 1秒
                {
                    if (result.Severity < DiagnosticSeverity.Warning)
                        result.Severity = DiagnosticSeverity.Warning;
                    issues.Add($"平均执行时间较长: {perfSummary.AverageExecutionTime.TotalMilliseconds:F0}ms");
                    result.Recommendations.Add("考虑使用批处理或缓存优化");
                }

                if (perfSummary.TotalMemoryAllocated > 100 * 1024 * 1024) // 100MB
                {
                    if (result.Severity < DiagnosticSeverity.Warning)
                        result.Severity = DiagnosticSeverity.Warning;
                    issues.Add($"内存分配较多: {perfSummary.TotalMemoryAllocated / 1024.0 / 1024.0:F1}MB");
                    result.Recommendations.Add("检查内存泄漏，考虑清理缓存");
                }

                // 分析最慢的操作
                if (perfSummary.TopSlowOperations.Any())
                {
                    var slowest = perfSummary.TopSlowOperations.First();
                    result.Metrics["SlowestOperation"] = slowest.OperationName;
                    result.Metrics["SlowestOperationTime"] = slowest.AverageExecutionTime.TotalMilliseconds;

                    if (slowest.AverageExecutionTime.TotalMilliseconds > 5000) // 5秒
                    {
                        issues.Add($"存在极慢操作: {slowest.OperationName} ({slowest.AverageExecutionTime.TotalMilliseconds:F0}ms)");
                        result.Recommendations.Add($"优化操作 {slowest.OperationName}");
                    }
                }

                result.Message = issues.Any()
                    ? string.Join("; ", issues)
                    : $"性能状态良好，已执行 {perfSummary.TotalOperations} 个操作";

                // 获取性能建议
                var recommendations = AutoPerformanceMonitor.Instance.GetRecommendations();
                foreach (var rec in recommendations.Take(3)) // 只取前3个建议
                {
                    result.Recommendations.Add($"{rec.OperationName}: {rec.Suggestion}");
                }
            }
            catch (Exception ex)
            {
                result.Severity = DiagnosticSeverity.Error;
                result.Message = $"性能分析失败: {ex.Message}";
                result.Details = ex.ToString();
            }

            return result;
        }
    }

    /// <summary>
    /// 内存泄漏分析器
    /// </summary>
    public class MemoryLeakAnalyzer : IDiagnosticAnalyzer
    {
        public string Name => "Memory Leak Analyzer";
        public string Description => "检测潜在的内存泄漏问题";

        private static readonly Dictionary<DateTime, long> _memoryHistory = new();

        public DiagnosticResult Analyze()
        {
            var result = new DiagnosticResult
            {
                AnalyzerName = Name,
                Severity = DiagnosticSeverity.Info
            };

            try
            {
                // 记录当前内存使用
                var currentMemory = GC.GetTotalMemory(false);
                var workingSet = Environment.WorkingSet;
                var now = DateTime.UtcNow;

                _memoryHistory[now] = currentMemory;

                // 清理5分钟前的历史记录
                var cutoff = now.AddMinutes(-5);
                var keysToRemove = _memoryHistory.Keys.Where(k => k < cutoff).ToList();
                foreach (var key in keysToRemove)
                {
                    _memoryHistory.Remove(key);
                }

                result.Metrics["CurrentMemory"] = currentMemory;
                result.Metrics["WorkingSet"] = workingSet;
                result.Metrics["Gen0Collections"] = GC.CollectionCount(0);
                result.Metrics["Gen1Collections"] = GC.CollectionCount(1);
                result.Metrics["Gen2Collections"] = GC.CollectionCount(2);

                // 分析内存趋势
                if (_memoryHistory.Count >= 2)
                {
                    var oldest = _memoryHistory.Values.First();
                    var growth = currentMemory - oldest;
                    var growthRate = (double)growth / oldest;

                    result.Metrics["MemoryGrowth"] = growth;
                    result.Metrics["MemoryGrowthRate"] = growthRate;

                    if (growthRate > 0.5) // 增长50%
                    {
                        result.Severity = DiagnosticSeverity.Warning;
                        result.Message = $"内存使用增长较快: {growthRate:P1} (增长 {growth / 1024.0 / 1024.0:F1}MB)";
                        result.Recommendations.Add("检查是否存在内存泄漏");
                        result.Recommendations.Add("调用 GC.Collect() 强制垃圾回收");
                    }
                    else if (growth > 50 * 1024 * 1024) // 增长50MB
                    {
                        result.Severity = DiagnosticSeverity.Warning;
                        result.Message = $"内存绝对增长较多: {growth / 1024.0 / 1024.0:F1}MB";
                        result.Recommendations.Add("监控内存使用模式");
                    }
                }

                // 检查缓存内存使用
                var cacheStats = SmartCacheManager.GetStatistics();
                result.Metrics["CacheMemoryUsage"] = cacheStats.TotalMemoryUsage;
                result.Metrics["CacheMemoryPressure"] = cacheStats.MemoryPressureLevel.ToString();

                if (cacheStats.MemoryPressureLevel >= MemoryPressureLevel.High)
                {
                    if (result.Severity < DiagnosticSeverity.Warning)
                        result.Severity = DiagnosticSeverity.Warning;
                    result.Message += $"; 缓存内存压力: {cacheStats.MemoryPressureLevel}";
                    result.Recommendations.Add("清理缓存: SmartCacheManager.ClearAll()");
                }

                if (result.Severity == DiagnosticSeverity.Info)
                {
                    result.Message = $"内存使用正常: {currentMemory / 1024.0 / 1024.0:F1}MB";
                }
            }
            catch (Exception ex)
            {
                result.Severity = DiagnosticSeverity.Error;
                result.Message = $"内存分析失败: {ex.Message}";
                result.Details = ex.ToString();
            }

            return result;
        }
    }

    /// <summary>
    /// 线程安全分析器
    /// </summary>
    public class ThreadSafetyAnalyzer : IDiagnosticAnalyzer
    {
        public string Name => "Thread Safety Analyzer";
        public string Description => "检查线程安全相关问题";

        public DiagnosticResult Analyze()
        {
            var result = new DiagnosticResult
            {
                AnalyzerName = Name,
                Severity = DiagnosticSeverity.Info
            };

            try
            {
                var currentThread = Thread.CurrentThread;
                result.Metrics["CurrentThreadId"] = currentThread.ManagedThreadId;
                result.Metrics["IsMainThread"] = currentThread.ManagedThreadId == 1;
                result.Metrics["ThreadName"] = currentThread.Name ?? "未命名";
                result.Metrics["IsBackground"] = currentThread.IsBackground;

                // 检查是否在正确的线程中
                if (currentThread.ManagedThreadId != 1)
                {
                    result.Severity = DiagnosticSeverity.Warning;
                    result.Message = $"在非主线程中执行 (线程ID: {currentThread.ManagedThreadId})";
                    result.Recommendations.Add("AutoCAD API调用应在主线程中执行");
                    result.Recommendations.Add("考虑使用 Application.DocumentManager.MdiActiveDocument.LockDocument()");
                }

                // 检查AutoCAD上下文管理器状态
                try
                {
                    var context = AutoCADContext.Current;
                    if (context != null)
                    {
                        result.Metrics["HasActiveContext"] = true;
                        result.Message = "AutoCAD上下文管理正常";
                    }
                    else
                    {
                        result.Metrics["HasActiveContext"] = false;
                        if (result.Severity < DiagnosticSeverity.Warning)
                        {
                            result.Severity = DiagnosticSeverity.Warning;
                            result.Message = "没有活动的AutoCAD上下文";
                            result.Recommendations.Add("使用 AutoCADContext.ExecuteSafely() 确保线程安全");
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Metrics["ContextError"] = ex.Message;
                    result.Severity = DiagnosticSeverity.Error;
                    result.Message = $"AutoCAD上下文访问失败: {ex.Message}";
                }

                // 检查并发相关的潜在问题
                var activeThreads = Process.GetCurrentProcess().Threads.Count;
                result.Metrics["ActiveThreadCount"] = activeThreads;

                if (activeThreads > 50) // 线程数过多
                {
                    if (result.Severity < DiagnosticSeverity.Warning)
                        result.Severity = DiagnosticSeverity.Warning;
                    result.Message += $"; 线程数较多: {activeThreads}";
                    result.Recommendations.Add("检查是否有线程泄漏");
                }

                if (result.Severity == DiagnosticSeverity.Info && string.IsNullOrEmpty(result.Message))
                {
                    result.Message = "线程安全状态正常";
                }
            }
            catch (Exception ex)
            {
                result.Severity = DiagnosticSeverity.Error;
                result.Message = $"线程安全分析失败: {ex.Message}";
                result.Details = ex.ToString();
            }

            return result;
        }
    }

    /// <summary>
    /// API使用分析器
    /// </summary>
    public class APIUsageAnalyzer : IDiagnosticAnalyzer
    {
        public string Name => "API Usage Analyzer";
        public string Description => "分析API使用模式和最佳实践";

        public DiagnosticResult Analyze()
        {
            var result = new DiagnosticResult
            {
                AnalyzerName = Name,
                Severity = DiagnosticSeverity.Info
            };

            try
            {
                // 检查DotNetARX初始化状态
                result.Metrics["DotNetARXInitialized"] = CAD.IsInitialized;

                if (!CAD.IsInitialized)
                {
                    result.Severity = DiagnosticSeverity.Warning;
                    result.Message = "DotNetARX尚未初始化";
                    result.Recommendations.Add("调用任何 CAD.* 方法以触发初始化");
                    return result;
                }

                // 分析性能监控数据
                var allMetrics = AutoPerformanceMonitor.Instance.GetAllMetrics().ToList();
                result.Metrics["MonitoredOperations"] = allMetrics.Count;

                if (allMetrics.Any())
                {
                    var totalExecCount = allMetrics.Sum(m => m.ExecutionCount);
                    var avgExecTime = allMetrics.Average(m => m.AverageExecutionTime.TotalMilliseconds);
                    var totalErrors = allMetrics.Sum(m => m.ErrorCount);

                    result.Metrics["TotalExecutions"] = totalExecCount;
                    result.Metrics["AverageExecutionTime"] = avgExecTime;
                    result.Metrics["TotalErrors"] = totalErrors;

                    // 分析API使用模式
                    var mostUsedOp = allMetrics.OrderByDescending(m => m.ExecutionCount).First();
                    result.Metrics["MostUsedOperation"] = mostUsedOp.OperationName;
                    result.Metrics["MostUsedOperationCount"] = mostUsedOp.ExecutionCount;

                    // 检查是否有效使用批处理
                    var batchOperations = allMetrics.Where(m => m.OperationName.Contains("Batch")).ToList();
                    var singleOperations = allMetrics.Where(m => !m.OperationName.Contains("Batch")).ToList();

                    if (singleOperations.Any() && !batchOperations.Any())
                    {
                        var frequentOps = singleOperations.Where(m => m.ExecutionCount > 10).ToList();
                        if (frequentOps.Any())
                        {
                            result.Severity = DiagnosticSeverity.Warning;
                            result.Message = "检测到频繁的单个操作，建议使用批处理";
                            result.Recommendations.Add("对于重复操作，使用批处理版本的API");
                            result.Recommendations.Add("例如：CAD.Move(operations) 而不是多次调用 CAD.Move()");
                        }
                    }

                    // 检查缓存使用情况
                    var cacheStats = SmartCacheManager.GetStatistics();
                    result.Metrics["CacheHitRatio"] = cacheStats.HitRatio;

                    if (cacheStats.HitRatio < 0.7 && cacheStats.TotalItems > 10)
                    {
                        if (result.Severity < DiagnosticSeverity.Warning)
                            result.Severity = DiagnosticSeverity.Warning;
                        result.Message += $"; 缓存命中率较低: {cacheStats.HitRatio:P1}";
                        result.Recommendations.Add("检查缓存策略，考虑调整缓存大小或过期时间");
                    }
                }

                if (result.Severity == DiagnosticSeverity.Info)
                {
                    result.Message = allMetrics.Any()
                        ? $"API使用正常，已监控 {allMetrics.Count} 种操作"
                        : "DotNetARX已初始化，等待API调用";
                }
            }
            catch (Exception ex)
            {
                result.Severity = DiagnosticSeverity.Error;
                result.Message = $"API使用分析失败: {ex.Message}";
                result.Details = ex.ToString();
            }

            return result;
        }
    }
}