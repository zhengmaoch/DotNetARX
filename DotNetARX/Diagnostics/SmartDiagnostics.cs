using System.Diagnostics;

namespace DotNetARX.Diagnostics
{
    /// <summary>
    /// 智能诊断系统
    /// 提供自动错误检测、性能分析和代码质量建议
    /// </summary>
    public static class SmartDiagnostics
    {
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(SmartDiagnostics));
        private static readonly ConcurrentDictionary<string, DiagnosticResult> _diagnosticCache = new();
        private static readonly List<IDiagnosticAnalyzer> _analyzers = new();
        private static bool _initialized = false;

        /// <summary>
        /// 初始化诊断系统
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            // 注册内置分析器
            RegisterAnalyzer(new AutoCADContextAnalyzer());
            RegisterAnalyzer(new PerformanceAnalyzerImpl());
            RegisterAnalyzer(new MemoryLeakAnalyzer());
            RegisterAnalyzer(new ThreadSafetyAnalyzer());
            RegisterAnalyzer(new APIUsageAnalyzer());

            _initialized = true;
            _logger.Info("智能诊断系统已初始化");
        }

        /// <summary>
        /// 注册诊断分析器
        /// </summary>
        public static void RegisterAnalyzer(IDiagnosticAnalyzer analyzer)
        {
            _analyzers.Add(analyzer);
            _logger.Debug($"已注册诊断分析器: {analyzer.GetType().Name}");
        }

        /// <summary>
        /// 运行完整诊断
        /// </summary>
        public static DiagnosticReport RunFullDiagnostic()
        {
            if (!_initialized) Initialize();

            _logger.Info("开始运行完整诊断...");
            var stopwatch = Stopwatch.StartNew();

            var report = new DiagnosticReport
            {
                StartTime = DateTime.UtcNow,
                AnalyzerResults = new List<DiagnosticResult>()
            };

            try
            {
                foreach (var analyzer in _analyzers)
                {
                    try
                    {
                        _logger.Debug($"运行分析器: {analyzer.Name}");
                        var result = analyzer.Analyze();
                        report.AnalyzerResults.Add(result);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"分析器 {analyzer.Name} 执行失败", ex);
                        report.AnalyzerResults.Add(new DiagnosticResult
                        {
                            AnalyzerName = analyzer.Name,
                            Severity = DiagnosticSeverity.Error,
                            Message = $"分析器执行失败: {ex.Message}",
                            Recommendations = new List<string> { "检查分析器配置和依赖" }
                        });
                    }
                }

                stopwatch.Stop();
                report.EndTime = DateTime.UtcNow;
                report.Duration = stopwatch.Elapsed;

                // 生成总体评分
                report.OverallScore = CalculateOverallScore(report.AnalyzerResults);
                report.Summary = GenerateSummary(report);

                _logger.Info($"完整诊断完成，耗时 {report.Duration.TotalSeconds:F2} 秒，评分: {report.OverallScore}/100");
            }
            catch (Exception ex)
            {
                _logger.Error("诊断过程中发生错误", ex);
                report.HasError = true;
                report.ErrorMessage = ex.Message;
            }

            return report;
        }

        /// <summary>
        /// 运行特定类型的诊断
        /// </summary>
        public static DiagnosticResult RunDiagnostic<T>() where T : IDiagnosticAnalyzer
        {
            if (!_initialized) Initialize();

            var analyzer = _analyzers.OfType<T>().FirstOrDefault();
            if (analyzer == null)
            {
                return new DiagnosticResult
                {
                    AnalyzerName = typeof(T).Name,
                    Severity = DiagnosticSeverity.Warning,
                    Message = $"未找到分析器: {typeof(T).Name}",
                    Recommendations = new List<string> { "检查分析器是否已注册" }
                };
            }

            var cacheKey = analyzer.GetType().FullName;
            var cachedResult = _diagnosticCache.GetOrAdd(cacheKey, _ =>
            {
                _logger.Debug($"运行诊断: {analyzer.Name}");
                return analyzer.Analyze();
            });

            // 如果缓存结果超过5分钟，重新分析
            if (DateTime.UtcNow - cachedResult.Timestamp > TimeSpan.FromMinutes(5))
            {
                _diagnosticCache.TryRemove(cacheKey, out _);
                return RunDiagnostic<T>();
            }

            return cachedResult;
        }

        /// <summary>
        /// 实时健康检查
        /// </summary>
        public static HealthCheckResult CheckSystemHealth()
        {
            if (!_initialized) Initialize();

            var result = new HealthCheckResult
            {
                CheckTime = DateTime.UtcNow,
                Issues = new List<HealthIssue>()
            };

            try
            {
                // AutoCAD 连接检查
                try
                {
                    var doc = Application.DocumentManager.MdiActiveDocument;
                    if (doc == null)
                    {
                        result.Issues.Add(new HealthIssue
                        {
                            Type = HealthIssueType.Warning,
                            Component = "AutoCAD",
                            Message = "没有活动的AutoCAD文档",
                            Suggestion = "打开一个AutoCAD文档以使用DotNetARX功能"
                        });
                    }
                }
                catch (Exception ex)
                {
                    result.Issues.Add(new HealthIssue
                    {
                        Type = HealthIssueType.Error,
                        Component = "AutoCAD",
                        Message = $"无法连接到AutoCAD: {ex.Message}",
                        Suggestion = "确保AutoCAD正在运行且DotNetARX已正确加载"
                    });
                }

                // 内存使用检查
                var memoryUsage = GC.GetTotalMemory(false);
                if (memoryUsage > 500 * 1024 * 1024) // 500MB
                {
                    result.Issues.Add(new HealthIssue
                    {
                        Type = HealthIssueType.Warning,
                        Component = "Memory",
                        Message = $"内存使用较高: {memoryUsage / 1024 / 1024:F1}MB",
                        Suggestion = "考虑调用 CAD.ResetPerformanceMetrics() 清理缓存"
                    });
                }

                // 性能监控检查
                var perfSummary = AutoPerformanceMonitor.Instance.GetSystemSummary();
                if (perfSummary.ErrorRate > 0.1) // 10%
                {
                    result.Issues.Add(new HealthIssue
                    {
                        Type = HealthIssueType.Error,
                        Component = "Performance",
                        Message = $"操作错误率过高: {perfSummary.ErrorRate:P1}",
                        Suggestion = "检查最近的错误日志，修复相关问题"
                    });
                }

                // 缓存系统检查
                var cacheStats = DotNetARX.Caching.SmartCacheManager.GetStatistics();
                if (cacheStats.HitRatio < 0.7) // 命中率低于70%
                {
                    result.Issues.Add(new HealthIssue
                    {
                        Type = HealthIssueType.Warning,
                        Component = "Cache",
                        Message = $"缓存命中率较低: {cacheStats.HitRatio:P1}",
                        Suggestion = "检查缓存配置，考虑增加缓存大小或调整过期策略"
                    });
                }

                // 线程安全检查
                if (Environment.CurrentManagedThreadId != 1) // 主线程ID通常是1
                {
                    result.Issues.Add(new HealthIssue
                    {
                        Type = HealthIssueType.Error,
                        Component = "Threading",
                        Message = "在非主线程中调用AutoCAD API",
                        Suggestion = "确保所有AutoCAD操作都在主线程中执行"
                    });
                }

                result.IsHealthy = !result.Issues.Any(i => i.Type == HealthIssueType.Error);
                result.Score = CalculateHealthScore(result.Issues);
            }
            catch (Exception ex)
            {
                _logger.Error("健康检查失败", ex);
                result.Issues.Add(new HealthIssue
                {
                    Type = HealthIssueType.Error,
                    Component = "System",
                    Message = $"健康检查失败: {ex.Message}",
                    Suggestion = "检查系统状态和日志"
                });
                result.IsHealthy = false;
            }

            return result;
        }

        /// <summary>
        /// 获取智能代码建议
        /// </summary>
        public static List<CodeSuggestion> GetCodeSuggestions(string operationName, object[] parameters = null)
        {
            var suggestions = new List<CodeSuggestion>();

            // 基于操作名称的建议
            switch (operationName.ToLower())
            {
                case "move":
                    suggestions.Add(new CodeSuggestion
                    {
                        Type = SuggestionType.Performance,
                        Title = "批量移动优化",
                        Description = "对于多个移动操作，使用 CAD.Move(operations) 进行批处理",
                        Example = "CAD.Move(new[] { (id1, from1, to1), (id2, from2, to2) });"
                    });
                    break;

                case "line":
                case "circle":
                case "arc":
                    suggestions.Add(new CodeSuggestion
                    {
                        Type = SuggestionType.BestPractice,
                        Title = "使用统一API",
                        Description = "使用 CAD.Line(), CAD.Circle() 等方法获得最佳性能",
                        Example = "var lineId = CAD.Line(Point3d.Origin, new Point3d(100, 100, 0));"
                    });
                    break;

                case "selectbytype":
                    suggestions.Add(new CodeSuggestion
                    {
                        Type = SuggestionType.Performance,
                        Title = "缓存查询结果",
                        Description = "对于频繁的查询操作，考虑缓存结果",
                        Example = "var lines = CAD.SelectByType<Line>(); // 自动缓存"
                    });
                    break;
            }

            // 基于性能数据的建议
            var perfMetrics = AutoPerformanceMonitor.Instance.GetMetrics(operationName);
            if (perfMetrics != null)
            {
                if (perfMetrics.AverageExecutionTime.TotalMilliseconds > 1000)
                {
                    suggestions.Add(new CodeSuggestion
                    {
                        Type = SuggestionType.Performance,
                        Title = "性能优化",
                        Description = $"操作 '{operationName}' 执行时间较长 ({perfMetrics.AverageExecutionTime.TotalMilliseconds:F0}ms)",
                        Example = "考虑使用批处理或异步操作来提高性能"
                    });
                }

                if (perfMetrics.ErrorRate > 0.05)
                {
                    suggestions.Add(new CodeSuggestion
                    {
                        Type = SuggestionType.Reliability,
                        Title = "错误处理",
                        Description = $"操作 '{operationName}' 错误率较高 ({perfMetrics.ErrorRate:P1})",
                        Example = "添加适当的错误处理和输入验证"
                    });
                }
            }

            return suggestions;
        }

        /// <summary>
        /// 自动修复建议
        /// </summary>
        public static List<AutoFixSuggestion> GetAutoFixSuggestions()
        {
            var suggestions = new List<AutoFixSuggestion>();

            // 检查缓存状态
            var cacheStats = DotNetARX.Caching.SmartCacheManager.GetStatistics();
            if (cacheStats.MemoryPressureLevel >= DotNetARX.Caching.MemoryPressureLevel.High)
            {
                suggestions.Add(new AutoFixSuggestion
                {
                    Issue = "内存压力过高",
                    FixAction = "清理缓存",
                    AutoFixCode = "SmartCacheManager.ClearAll();",
                    CanAutoFix = true,
                    Severity = AutoFixSeverity.Medium
                });
            }

            // 检查性能回归
            var recommendations = AutoPerformanceMonitor.Instance.GetRecommendations();
            foreach (var rec in recommendations.Where(r => r.Severity >= PerformanceSeverity.High))
            {
                suggestions.Add(new AutoFixSuggestion
                {
                    Issue = rec.Message,
                    FixAction = rec.Suggestion,
                    AutoFixCode = GenerateAutoFixCode(rec),
                    CanAutoFix = rec.Type == RecommendationType.HighMemoryUsage,
                    Severity = (AutoFixSeverity)(int)rec.Severity
                });
            }

            return suggestions;
        }

        /// <summary>
        /// 清理诊断缓存
        /// </summary>
        public static void ClearDiagnosticCache()
        {
            _diagnosticCache.Clear();
            _logger.Info("诊断缓存已清理");
        }

        #region 私有辅助方法

        private static int CalculateOverallScore(List<DiagnosticResult> results)
        {
            if (!results.Any()) return 0;

            var baseScore = 100;
            foreach (var result in results)
            {
                switch (result.Severity)
                {
                    case DiagnosticSeverity.Error:
                        baseScore -= 20;
                        break;

                    case DiagnosticSeverity.Warning:
                        baseScore -= 10;
                        break;

                    case DiagnosticSeverity.Info:
                        baseScore -= 2;
                        break;
                }
            }

            return Math.Max(0, Math.Min(100, baseScore));
        }

        private static string GenerateSummary(DiagnosticReport report)
        {
            var errors = report.AnalyzerResults.Count(r => r.Severity == DiagnosticSeverity.Error);
            var warnings = report.AnalyzerResults.Count(r => r.Severity == DiagnosticSeverity.Warning);
            var infos = report.AnalyzerResults.Count(r => r.Severity == DiagnosticSeverity.Info);

            return $"诊断完成: {errors} 个错误, {warnings} 个警告, {infos} 个信息提示";
        }

        private static int CalculateHealthScore(List<HealthIssue> issues)
        {
            var baseScore = 100;
            foreach (var issue in issues)
            {
                switch (issue.Type)
                {
                    case HealthIssueType.Error:
                        baseScore -= 30;
                        break;

                    case HealthIssueType.Warning:
                        baseScore -= 15;
                        break;

                    case HealthIssueType.Info:
                        baseScore -= 5;
                        break;
                }
            }

            return Math.Max(0, Math.Min(100, baseScore));
        }

        private static string GenerateAutoFixCode(PerformanceRecommendation recommendation)
        {
            return recommendation.Type switch
            {
                RecommendationType.HighMemoryUsage => "GC.Collect(); GC.WaitForPendingFinalizers();",
                RecommendationType.SlowExecution => "// 考虑使用批处理或缓存优化",
                RecommendationType.HighErrorRate => "// 添加错误处理和输入验证",
                _ => "// 请手动优化"
            };
        }

        #endregion 私有辅助方法
    }
}