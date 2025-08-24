namespace DotNetARX.Diagnostics
{
    /// <summary>
    /// 诊断分析器接口
    /// </summary>
    public interface IDiagnosticAnalyzer
    {
        string Name { get; }
        string Description { get; }

        DiagnosticResult Analyze();
    }

    /// <summary>
    /// 诊断结果
    /// </summary>
    public class DiagnosticResult
    {
        public string AnalyzerName { get; set; }
        public DiagnosticSeverity Severity { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public List<string> Recommendations { get; set; } = new();
        public Dictionary<string, object> Metrics { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 诊断报告
    /// </summary>
    public class DiagnosticReport
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public List<DiagnosticResult> AnalyzerResults { get; set; } = new();
        public int OverallScore { get; set; }
        public string Summary { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== DotNetARX 诊断报告 ===");
            sb.AppendLine($"开始时间: {StartTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"结束时间: {EndTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"执行时长: {Duration.TotalSeconds:F2} 秒");
            sb.AppendLine($"总体评分: {OverallScore}/100");
            sb.AppendLine($"摘要: {Summary}");
            sb.AppendLine();

            if (HasError)
            {
                sb.AppendLine($"❌ 诊断过程中发生错误: {ErrorMessage}");
                sb.AppendLine();
            }

            var errors = AnalyzerResults.Where(r => r.Severity == DiagnosticSeverity.Error).ToList();
            var warnings = AnalyzerResults.Where(r => r.Severity == DiagnosticSeverity.Warning).ToList();
            var infos = AnalyzerResults.Where(r => r.Severity == DiagnosticSeverity.Info).ToList();

            if (errors.Any())
            {
                sb.AppendLine("🔴 错误:");
                foreach (var error in errors)
                {
                    sb.AppendLine($"  - {error.AnalyzerName}: {error.Message}");
                    if (error.Recommendations.Any())
                    {
                        sb.AppendLine($"    建议: {string.Join("; ", error.Recommendations)}");
                    }
                }
                sb.AppendLine();
            }

            if (warnings.Any())
            {
                sb.AppendLine("🟡 警告:");
                foreach (var warning in warnings)
                {
                    sb.AppendLine($"  - {warning.AnalyzerName}: {warning.Message}");
                    if (warning.Recommendations.Any())
                    {
                        sb.AppendLine($"    建议: {string.Join("; ", warning.Recommendations)}");
                    }
                }
                sb.AppendLine();
            }

            if (infos.Any())
            {
                sb.AppendLine("ℹ️ 信息:");
                foreach (var info in infos)
                {
                    sb.AppendLine($"  - {info.AnalyzerName}: {info.Message}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// 健康检查结果
    /// </summary>
    public class HealthCheckResult
    {
        public DateTime CheckTime { get; set; }
        public bool IsHealthy { get; set; }
        public int Score { get; set; }
        public List<HealthIssue> Issues { get; set; } = new();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== 系统健康检查 ===");
            sb.AppendLine($"检查时间: {CheckTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"健康状态: {(IsHealthy ? "✅ 健康" : "❌ 有问题")}");
            sb.AppendLine($"健康评分: {Score}/100");
            sb.AppendLine();

            if (Issues.Any())
            {
                var errors = Issues.Where(i => i.Type == HealthIssueType.Error).ToList();
                var warnings = Issues.Where(i => i.Type == HealthIssueType.Warning).ToList();
                var infos = Issues.Where(i => i.Type == HealthIssueType.Info).ToList();

                if (errors.Any())
                {
                    sb.AppendLine("🔴 错误:");
                    foreach (var error in errors)
                    {
                        sb.AppendLine($"  - {error.Component}: {error.Message}");
                        sb.AppendLine($"    建议: {error.Suggestion}");
                    }
                    sb.AppendLine();
                }

                if (warnings.Any())
                {
                    sb.AppendLine("🟡 警告:");
                    foreach (var warning in warnings)
                    {
                        sb.AppendLine($"  - {warning.Component}: {warning.Message}");
                        sb.AppendLine($"    建议: {warning.Suggestion}");
                    }
                    sb.AppendLine();
                }

                if (infos.Any())
                {
                    sb.AppendLine("ℹ️ 信息:");
                    foreach (var info in infos)
                    {
                        sb.AppendLine($"  - {info.Component}: {info.Message}");
                    }
                }
            }
            else
            {
                sb.AppendLine("✅ 未发现问题");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// 健康问题
    /// </summary>
    public class HealthIssue
    {
        public HealthIssueType Type { get; set; }
        public string Component { get; set; }
        public string Message { get; set; }
        public string Suggestion { get; set; }
    }

    /// <summary>
    /// 代码建议
    /// </summary>
    public class CodeSuggestion
    {
        public SuggestionType Type { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Example { get; set; }
        public string MoreInfo { get; set; }
    }

    /// <summary>
    /// 自动修复建议
    /// </summary>
    public class AutoFixSuggestion
    {
        public string Issue { get; set; }
        public string FixAction { get; set; }
        public string AutoFixCode { get; set; }
        public bool CanAutoFix { get; set; }
        public AutoFixSeverity Severity { get; set; }
    }

    /// <summary>
    /// 诊断严重程度
    /// </summary>
    public enum DiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// 健康问题类型
    /// </summary>
    public enum HealthIssueType
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// 建议类型
    /// </summary>
    public enum SuggestionType
    {
        Performance,
        BestPractice,
        Reliability,
        Security,
        Maintenance
    }

    /// <summary>
    /// 自动修复严重程度
    /// </summary>
    public enum AutoFixSeverity
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }
}