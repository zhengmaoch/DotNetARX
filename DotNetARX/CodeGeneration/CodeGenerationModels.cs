namespace DotNetARX.CodeGeneration
{
    /// <summary>
    /// 代码补全项
    /// </summary>
    public class CompletionItem
    {
        public string Text { get; set; }
        public string DisplayText { get; set; }
        public string Description { get; set; }
        public CompletionType CompletionType { get; set; }
        public int Priority { get; set; }
        public string Documentation { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// 方法签名帮助
    /// </summary>
    public class SignatureHelp
    {
        public string MethodName { get; set; }
        public List<MethodSignature> Signatures { get; set; } = new();
        public int ActiveSignature { get; set; }
        public int ActiveParameter { get; set; }
    }

    /// <summary>
    /// 方法签名
    /// </summary>
    public class MethodSignature
    {
        public string ReturnType { get; set; }
        public List<ParameterInfo> Parameters { get; set; } = new();
        public string Description { get; set; }
        public string Example { get; set; }
        public List<string> SeeAlso { get; set; } = new();
    }

    /// <summary>
    /// 参数信息
    /// </summary>
    public class ParameterInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsOptional { get; set; }
        public string DefaultValue { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// 代码诊断
    /// </summary>
    public class CodeDiagnostic
    {
        public DiagnosticSeverity Severity { get; set; }
        public string Message { get; set; }
        public string Suggestion { get; set; }
        public CodeRange Range { get; set; }
        public List<CodeFix> Fixes { get; set; } = new();
    }

    /// <summary>
    /// 代码范围
    /// </summary>
    public class CodeRange
    {
        public int Start { get; set; }
        public int End { get; set; }
        public int StartLine { get; set; }
        public int StartColumn { get; set; }
        public int EndLine { get; set; }
        public int EndColumn { get; set; }
    }

    /// <summary>
    /// 代码修复
    /// </summary>
    public class CodeFix
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string NewCode { get; set; }
        public CodeRange Range { get; set; }
    }

    /// <summary>
    /// 代码模板
    /// </summary>
    public class CodeTemplate
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Template { get; set; }
        public Dictionary<string, string> DefaultParameters { get; set; } = new();
        public List<string> RequiredParameters { get; set; } = new();
        public string Category { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// 重构操作
    /// </summary>
    public class RefactoringAction
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public RefactoringActionType ActionType { get; set; }
        public string Example { get; set; }
        public CodeRange TargetRange { get; set; }
        public string NewCode { get; set; }
        public bool RequiresUserInput { get; set; }
        public Dictionary<string, string> Parameters { get; set; } = new();
    }

    /// <summary>
    /// 补全类型
    /// </summary>
    public enum CompletionType
    {
        Method,
        Property,
        Field,
        Class,
        Interface,
        Enum,
        Namespace,
        Keyword,
        Snippet,
        Template,
        Variable,
        Parameter
    }

    /// <summary>
    /// 重构操作类型
    /// </summary>
    public enum RefactoringActionType
    {
        Performance,
        BestPractice,
        Reliability,
        Security,
        Maintainability,
        Readability
    }

    /// <summary>
    /// 智能代码生成器
    /// </summary>
    public static class SmartCodeGenerator
    {
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(SmartCodeGenerator));

        /// <summary>
        /// 生成AutoCAD实体创建代码
        /// </summary>
        public static string GenerateEntityCreationCode(string entityType, Dictionary<string, object> parameters)
        {
            try
            {
                return entityType.ToLower() switch
                {
                    "line" => GenerateLineCode(parameters),
                    "circle" => GenerateCircleCode(parameters),
                    "arc" => GenerateArcCode(parameters),
                    "text" => GenerateTextCode(parameters),
                    "polyline" => GeneratePolylineCode(parameters),
                    _ => $"// 不支持的实体类型: {entityType}"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"生成实体创建代码失败: {entityType}", ex);
                return $"// 代码生成失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 生成批处理操作代码
        /// </summary>
        public static string GenerateBatchOperationCode(string operationType, int count)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"// 批量{operationType}操作");
            sb.AppendLine("var operations = new[]");
            sb.AppendLine("{");

            for (int i = 0; i < count; i++)
            {
                sb.AppendLine($"    (entityId{i + 1}, fromPoint{i + 1}, toPoint{i + 1}),");
            }

            sb.AppendLine("};");
            sb.AppendLine($"CAD.{operationType}(operations);");

            return sb.ToString();
        }

        /// <summary>
        /// 生成错误处理代码
        /// </summary>
        public static string GenerateErrorHandlingCode(string operationCode)
        {
            return $@"try
{{
    {operationCode}
}}
catch (System.Exception ex)
{{
    // 记录错误
    LogManager.GetLogger().Error(""操作失败"", ex);

    // 根据需要处理错误
    throw; // 重新抛出异常，或者返回错误值
}}";
        }

        /// <summary>
        /// 生成上下文安全操作代码
        /// </summary>
        public static string GenerateSafeOperationCode(string operationCode)
        {
            return $@"return AutoCADContext.ExecuteSafely(() =>
{{
    {operationCode.Replace("\n", "\n    ")}
}});";
        }

        #region 私有辅助方法

        private static string GenerateLineCode(Dictionary<string, object> parameters)
        {
            var startPoint = parameters.GetValueOrDefault("startPoint", "Point3d.Origin");
            var endPoint = parameters.GetValueOrDefault("endPoint", "new Point3d(100, 100, 0)");

            return $"var lineId = CAD.Line({startPoint}, {endPoint});";
        }

        private static string GenerateCircleCode(Dictionary<string, object> parameters)
        {
            var center = parameters.GetValueOrDefault("center", "new Point3d(50, 50, 0)");
            var radius = parameters.GetValueOrDefault("radius", "25");

            return $"var circleId = CAD.Circle({center}, {radius});";
        }

        private static string GenerateArcCode(Dictionary<string, object> parameters)
        {
            var center = parameters.GetValueOrDefault("center", "Point3d.Origin");
            var radius = parameters.GetValueOrDefault("radius", "30");
            var startAngle = parameters.GetValueOrDefault("startAngle", "0");
            var endAngle = parameters.GetValueOrDefault("endAngle", "Math.PI");

            return $"var arcId = CAD.Arc({center}, {radius}, {startAngle}, {endAngle});";
        }

        private static string GenerateTextCode(Dictionary<string, object> parameters)
        {
            var text = parameters.GetValueOrDefault("text", "\"Hello World\"");
            var position = parameters.GetValueOrDefault("position", "Point3d.Origin");
            var height = parameters.GetValueOrDefault("height", "5");

            return $"var textId = CAD.Text({text}, {position}, {height});";
        }

        private static string GeneratePolylineCode(Dictionary<string, object> parameters)
        {
            var points = parameters.GetValueOrDefault("points", "new[] { Point3d.Origin, new Point3d(100, 0, 0), new Point3d(100, 100, 0) }");

            return $"var polylineId = CAD.Polyline({points});";
        }

        #endregion 私有辅助方法
    }

    /// <summary>
    /// 代码质量分析器
    /// </summary>
    public static class CodeQualityAnalyzer
    {
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(CodeQualityAnalyzer));

        /// <summary>
        /// 分析代码质量
        /// </summary>
        public static CodeQualityReport AnalyzeCode(string code)
        {
            var report = new CodeQualityReport
            {
                AnalysisTime = DateTime.UtcNow,
                SourceCode = code
            };

            try
            {
                // 复杂度分析
                report.CyclomaticComplexity = CalculateCyclomaticComplexity(code);

                // 代码行数统计
                var lines = code.Split('\n');
                report.TotalLines = lines.Length;
                report.CodeLines = lines.Count(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("//"));
                report.CommentLines = lines.Count(line => line.Trim().StartsWith("//"));

                // 方法数量
                report.MethodCount = Regex.Matches(code, @"(public|private|protected|internal)\s+.*?\s+\w+\s*\(").Count;

                // 重复代码检测
                report.DuplicateCodeBlocks = DetectDuplicateCode(code);

                // 命名规范检查
                report.NamingViolations = CheckNamingConventions(code);

                // 性能问题检测
                report.PerformanceIssues = DetectPerformanceIssues(code);

                // 计算总体评分
                report.QualityScore = CalculateQualityScore(report);

                _logger.Debug($"代码质量分析完成，评分: {report.QualityScore}/100");
            }
            catch (Exception ex)
            {
                _logger.Error("代码质量分析失败", ex);
                report.AnalysisError = ex.Message;
            }

            return report;
        }

        #region 私有分析方法

        private static int CalculateCyclomaticComplexity(string code)
        {
            var complexity = 1; // 基础复杂度

            // 条件语句
            complexity += Regex.Matches(code, @"\bif\b").Count;
            complexity += Regex.Matches(code, @"\belse\b").Count;
            complexity += Regex.Matches(code, @"\bwhile\b").Count;
            complexity += Regex.Matches(code, @"\bfor\b").Count;
            complexity += Regex.Matches(code, @"\bforeach\b").Count;
            complexity += Regex.Matches(code, @"\bswitch\b").Count;
            complexity += Regex.Matches(code, @"\bcase\b").Count;
            complexity += Regex.Matches(code, @"\bcatch\b").Count;
            complexity += Regex.Matches(code, @"\b&&\b").Count;
            complexity += Regex.Matches(code, @"\b\|\|\b").Count;

            return complexity;
        }

        private static List<DuplicateCodeBlock> DetectDuplicateCode(string code)
        {
            var duplicates = new List<DuplicateCodeBlock>();
            var lines = code.Split('\n');

            // 简单的重复行检测
            for (int i = 0; i < lines.Length - 1; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                    continue;

                for (int j = i + 1; j < lines.Length; j++)
                {
                    if (lines[j].Trim() == line)
                    {
                        duplicates.Add(new DuplicateCodeBlock
                        {
                            Content = line,
                            FirstOccurrence = i + 1,
                            SecondOccurrence = j + 1
                        });
                    }
                }
            }

            return duplicates.GroupBy(d => d.Content).Select(g => g.First()).ToList();
        }

        private static List<NamingViolation> CheckNamingConventions(string code)
        {
            var violations = new List<NamingViolation>();

            // 检查方法命名（应该是PascalCase）
            var methods = Regex.Matches(code, @"(public|private|protected|internal)\s+.*?\s+(\w+)\s*\(");
            foreach (Match match in methods)
            {
                var methodName = match.Groups[2].Value;
                if (!char.IsUpper(methodName[0]))
                {
                    violations.Add(new NamingViolation
                    {
                        Type = "Method",
                        Name = methodName,
                        Issue = "方法名应使用PascalCase命名"
                    });
                }
            }

            // 检查变量命名（应该是camelCase）
            var variables = Regex.Matches(code, @"var\s+(\w+)\s*=");
            foreach (Match match in variables)
            {
                var varName = match.Groups[1].Value;
                if (char.IsUpper(varName[0]))
                {
                    violations.Add(new NamingViolation
                    {
                        Type = "Variable",
                        Name = varName,
                        Issue = "变量名应使用camelCase命名"
                    });
                }
            }

            return violations;
        }

        private static List<PerformanceIssue> DetectPerformanceIssues(string code)
        {
            var issues = new List<PerformanceIssue>();

            // 检查字符串连接
            if (Regex.IsMatch(code, @"\w+\s*\+=\s*.*?;.*\w+\s*\+=\s*.*?;", RegexOptions.Multiline))
            {
                issues.Add(new PerformanceIssue
                {
                    Type = "String Concatenation",
                    Description = "多次字符串连接，建议使用StringBuilder",
                    Severity = "Medium"
                });
            }

            // 检查循环中的对象创建
            if (Regex.IsMatch(code, @"for\s*\(.*?\)\s*\{[^}]*new\s+\w+", RegexOptions.Multiline))
            {
                issues.Add(new PerformanceIssue
                {
                    Type = "Object Creation in Loop",
                    Description = "循环中创建对象可能影响性能",
                    Severity = "Low"
                });
            }

            return issues;
        }

        private static int CalculateQualityScore(CodeQualityReport report)
        {
            var score = 100;

            // 复杂度扣分
            if (report.CyclomaticComplexity > 20)
                score -= 20;
            else if (report.CyclomaticComplexity > 10)
                score -= 10;

            // 重复代码扣分
            score -= Math.Min(20, report.DuplicateCodeBlocks.Count * 5);

            // 命名规范扣分
            score -= Math.Min(15, report.NamingViolations.Count * 3);

            // 性能问题扣分
            score -= Math.Min(25, report.PerformanceIssues.Count * 5);

            return Math.Max(0, score);
        }

        #endregion 私有分析方法
    }

    /// <summary>
    /// 代码质量报告
    /// </summary>
    public class CodeQualityReport
    {
        public DateTime AnalysisTime { get; set; }
        public string SourceCode { get; set; }
        public int QualityScore { get; set; }
        public int CyclomaticComplexity { get; set; }
        public int TotalLines { get; set; }
        public int CodeLines { get; set; }
        public int CommentLines { get; set; }
        public int MethodCount { get; set; }
        public List<DuplicateCodeBlock> DuplicateCodeBlocks { get; set; } = new();
        public List<NamingViolation> NamingViolations { get; set; } = new();
        public List<PerformanceIssue> PerformanceIssues { get; set; } = new();
        public string AnalysisError { get; set; }
    }

    /// <summary>
    /// 重复代码块
    /// </summary>
    public class DuplicateCodeBlock
    {
        public string Content { get; set; }
        public int FirstOccurrence { get; set; }
        public int SecondOccurrence { get; set; }
    }

    /// <summary>
    /// 命名规范违反
    /// </summary>
    public class NamingViolation
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Issue { get; set; }
    }

    /// <summary>
    /// 性能问题
    /// </summary>
    public class PerformanceIssue
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }
    }
}