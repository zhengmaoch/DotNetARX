namespace DotNetARX.CodeGeneration
{
    /// <summary>
    /// IntelliSense 智能代码助手
    /// 提供代码提示、自动补全和智能建议
    /// </summary>
    public static class IntelliSenseHelper
    {
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(IntelliSenseHelper));
        private static readonly Dictionary<string, CodeTemplate> _templates = new();
        private static readonly Dictionary<string, List<string>> _contextualSuggestions = new();
        private static bool _initialized = false;

        /// <summary>
        /// 初始化IntelliSense助手
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            LoadCodeTemplates();
            LoadContextualSuggestions();

            _initialized = true;
            _logger.Info("IntelliSense助手已初始化");
        }

        /// <summary>
        /// 获取代码补全建议
        /// </summary>
        /// <param name="context">当前代码上下文</param>
        /// <param name="currentInput">当前输入</param>
        /// <returns>补全建议列表</returns>
        public static List<CompletionItem> GetCompletions(string context, string currentInput)
        {
            if (!_initialized) Initialize();

            var completions = new List<CompletionItem>();

            try
            {
                // 基于当前输入的基本补全
                if (currentInput.StartsWith("CAD."))
                {
                    completions.AddRange(GetCADMethodCompletions(currentInput));
                }
                else if (currentInput.StartsWith("SmartCacheManager."))
                {
                    completions.AddRange(GetCacheMethodCompletions(currentInput));
                }
                else if (currentInput.StartsWith("AutoCADContext."))
                {
                    completions.AddRange(GetContextMethodCompletions(currentInput));
                }

                // 基于上下文的智能建议
                completions.AddRange(GetContextualCompletions(context, currentInput));

                // 代码模板建议
                completions.AddRange(GetTemplateCompletions(currentInput));

                // 排序和去重
                completions = completions
                    .GroupBy(c => c.Text)
                    .Select(g => g.OrderByDescending(c => c.Priority).First())
                    .OrderByDescending(c => c.Priority)
                    .ThenBy(c => c.Text)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.Error("获取代码补全建议时发生错误", ex);
            }

            return completions;
        }

        /// <summary>
        /// 获取方法签名帮助
        /// </summary>
        /// <param name="methodName">方法名</param>
        /// <returns>签名帮助信息</returns>
        public static SignatureHelp GetSignatureHelp(string methodName)
        {
            if (!_initialized) Initialize();

            try
            {
                var help = new SignatureHelp { MethodName = methodName };

                // 查找CAD类的方法
                var cadType = typeof(CAD);
                var methods = cadType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var method in methods)
                {
                    var signature = new MethodSignature
                    {
                        ReturnType = method.ReturnType.Name,
                        Parameters = method.GetParameters().Select(p => new ParameterInfo
                        {
                            Name = p.Name,
                            Type = p.ParameterType.Name,
                            IsOptional = p.IsOptional,
                            DefaultValue = p.DefaultValue?.ToString(),
                            Description = GetParameterDescription(methodName, p.Name)
                        }).ToList(),
                        Description = GetMethodDescription(methodName),
                        Example = GetMethodExample(methodName)
                    };

                    help.Signatures.Add(signature);
                }

                return help;
            }
            catch (Exception ex)
            {
                _logger.Error($"获取方法签名帮助失败: {methodName}", ex);
                return new SignatureHelp { MethodName = methodName };
            }
        }

        /// <summary>
        /// 获取错误诊断和修复建议
        /// </summary>
        /// <param name="code">源代码</param>
        /// <returns>诊断结果</returns>
        public static List<CodeDiagnostic> DiagnoseCode(string code)
        {
            if (!_initialized) Initialize();

            var diagnostics = new List<CodeDiagnostic>();

            try
            {
                // 检查常见错误模式
                diagnostics.AddRange(CheckCommonPatterns(code));

                // 检查性能问题
                diagnostics.AddRange(CheckPerformanceIssues(code));

                // 检查最佳实践
                diagnostics.AddRange(CheckBestPractices(code));

                // 检查线程安全问题
                diagnostics.AddRange(CheckThreadSafetyIssues(code));
            }
            catch (Exception ex)
            {
                _logger.Error("代码诊断时发生错误", ex);
            }

            return diagnostics;
        }

        /// <summary>
        /// 生成代码片段
        /// </summary>
        /// <param name="templateName">模板名称</param>
        /// <param name="parameters">参数</param>
        /// <returns>生成的代码</returns>
        public static string GenerateCodeSnippet(string templateName, Dictionary<string, string> parameters = null)
        {
            if (!_initialized) Initialize();

            if (!_templates.TryGetValue(templateName, out var template))
            {
                return $"// 未找到模板: {templateName}";
            }

            try
            {
                var code = template.Template;

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        code = code.Replace($"{{{param.Key}}}", param.Value);
                    }
                }

                // 替换默认值
                foreach (var defaultParam in template.DefaultParameters)
                {
                    code = code.Replace($"{{{defaultParam.Key}}}", defaultParam.Value);
                }

                return code;
            }
            catch (Exception ex)
            {
                _logger.Error($"生成代码片段失败: {templateName}", ex);
                return $"// 生成失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 获取智能重构建议
        /// </summary>
        /// <param name="code">源代码</param>
        /// <returns>重构建议</returns>
        public static List<RefactoringAction> GetRefactoringSuggestions(string code)
        {
            if (!_initialized) Initialize();

            var suggestions = new List<RefactoringAction>();

            try
            {
                // 检查是否可以使用批处理优化
                if (Regex.IsMatch(code, @"CAD\.(Move|Copy|Rotate|Scale)\s*\(.*?\)\s*;.*CAD\.\1\s*\(.*?\)\s*;", RegexOptions.Multiline))
                {
                    suggestions.Add(new RefactoringAction
                    {
                        Title = "使用批处理优化",
                        Description = "检测到多个相同的操作，可以合并为批处理",
                        ActionType = RefactoringActionType.Performance,
                        Example = "CAD.Move(new[] { (id1, from1, to1), (id2, from2, to2) });"
                    });
                }

                // 检查是否可以使用缓存
                if (Regex.IsMatch(code, @"CAD\.SelectByType<.*?>\(\).*CAD\.SelectByType<.*?>\(\)", RegexOptions.Multiline))
                {
                    suggestions.Add(new RefactoringAction
                    {
                        Title = "缓存查询结果",
                        Description = "重复的查询操作可以缓存结果",
                        ActionType = RefactoringActionType.Performance,
                        Example = "var entities = CAD.SelectByType<Line>(); // 结果会自动缓存"
                    });
                }

                // 检查异常处理
                if (code.Contains("CAD.") && !code.Contains("try") && !code.Contains("catch"))
                {
                    suggestions.Add(new RefactoringAction
                    {
                        Title = "添加异常处理",
                        Description = "AutoCAD操作应该包含适当的异常处理",
                        ActionType = RefactoringActionType.Reliability,
                        Example = "try { CAD.Move(...); } catch (Exception ex) { /* 处理错误 */ }"
                    });
                }

                // 检查using语句
                if (Regex.IsMatch(code, @"var\s+context\s*=\s*AutoCADContext\."))
                {
                    suggestions.Add(new RefactoringAction
                    {
                        Title = "使用using语句",
                        Description = "AutoCAD上下文应该使用using语句确保正确释放",
                        ActionType = RefactoringActionType.BestPractice,
                        Example = "using var context = AutoCADContext.Create();"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.Error("获取重构建议时发生错误", ex);
            }

            return suggestions;
        }

        #region 私有辅助方法

        private static void LoadCodeTemplates()
        {
            // 基本绘图模板
            _templates["draw_line"] = new CodeTemplate
            {
                Name = "绘制直线",
                Template = "var lineId = CAD.Line(new Point3d({x1}, {y1}, 0), new Point3d({x2}, {y2}, 0));",
                DefaultParameters = { ["x1"] = "0", ["y1"] = "0", ["x2"] = "100", ["y2"] = "100" }
            };

            _templates["draw_circle"] = new CodeTemplate
            {
                Name = "绘制圆",
                Template = "var circleId = CAD.Circle(new Point3d({x}, {y}, 0), {radius});",
                DefaultParameters = { ["x"] = "0", ["y"] = "0", ["radius"] = "50" }
            };

            _templates["batch_move"] = new CodeTemplate
            {
                Name = "批量移动",
                Template = @"var operations = new[]
{
    (entityId1, fromPoint1, toPoint1),
    (entityId2, fromPoint2, toPoint2)
};
CAD.Move(operations);",
                DefaultParameters = { }
            };

            _templates["safe_context"] = new CodeTemplate
            {
                Name = "安全上下文操作",
                Template = @"using var context = AutoCADContext.Create();
try
{
    // 你的操作代码
    {code}
    context.Commit();
}
catch (Exception ex)
{
    context.Abort();
    throw;
}",
                DefaultParameters = { ["code"] = "// 在此添加你的代码" }
            };
        }

        private static void LoadContextualSuggestions()
        {
            _contextualSuggestions["drawing"] = new List<string>
            {
                "CAD.Line(Point3d.Origin, new Point3d(100, 100, 0))",
                "CAD.Circle(new Point3d(50, 50, 0), 25)",
                "CAD.Arc(Point3d.Origin, 30, 0, Math.PI)",
                "CAD.Text(\"Hello\", Point3d.Origin, 5)"
            };

            _contextualSuggestions["transform"] = new List<string>
            {
                "CAD.Move(entityId, fromPoint, toPoint)",
                "CAD.Copy(entityId, fromPoint, toPoint)",
                "CAD.Rotate(entityId, basePoint, angle)",
                "CAD.Scale(entityId, basePoint, scaleFactor)"
            };

            _contextualSuggestions["query"] = new List<string>
            {
                "CAD.SelectByType<Line>()",
                "CAD.SelectByType<Circle>()",
                "CAD.Select<Line>(line => line.Length > 50)"
            };
        }

        private static List<CompletionItem> GetCADMethodCompletions(string input)
        {
            var completions = new List<CompletionItem>();
            var cadMethods = typeof(CAD).GetMethods(BindingFlags.Public | BindingFlags.Static);

            foreach (var method in cadMethods)
            {
                if (method.Name.StartsWith(input.Substring(4), StringComparison.OrdinalIgnoreCase))
                {
                    completions.Add(new CompletionItem
                    {
                        Text = method.Name,
                        DisplayText = GetMethodDisplayText(method),
                        Description = GetMethodDescription(method.Name),
                        CompletionType = CompletionType.Method,
                        Priority = 100
                    });
                }
            }

            return completions;
        }

        private static List<CompletionItem> GetCacheMethodCompletions(string input)
        {
            var methods = new[]
            {
                "GetCache<TKey, TValue>",
                "GetStatistics",
                "ClearAll",
                "RemoveCache"
            };

            return methods
                .Where(m => m.StartsWith(input.Substring(19), StringComparison.OrdinalIgnoreCase))
                .Select(m => new CompletionItem
                {
                    Text = m,
                    DisplayText = m,
                    Description = $"智能缓存管理器方法: {m}",
                    CompletionType = CompletionType.Method,
                    Priority = 90
                })
                .ToList();
        }

        private static List<CompletionItem> GetContextMethodCompletions(string input)
        {
            var methods = new[]
            {
                "Create",
                "ExecuteSafely",
                "ExecuteBatch",
                "Current"
            };

            return methods
                .Where(m => m.StartsWith(input.Substring(15), StringComparison.OrdinalIgnoreCase))
                .Select(m => new CompletionItem
                {
                    Text = m,
                    DisplayText = m,
                    Description = $"AutoCAD上下文方法: {m}",
                    CompletionType = CompletionType.Method,
                    Priority = 85
                })
                .ToList();
        }

        private static List<CompletionItem> GetContextualCompletions(string context, string input)
        {
            var completions = new List<CompletionItem>();

            foreach (var contextKey in _contextualSuggestions.Keys)
            {
                if (context.ToLower().Contains(contextKey))
                {
                    foreach (var suggestion in _contextualSuggestions[contextKey])
                    {
                        if (suggestion.Contains(input, StringComparison.OrdinalIgnoreCase))
                        {
                            completions.Add(new CompletionItem
                            {
                                Text = suggestion,
                                DisplayText = suggestion,
                                Description = $"上下文建议: {contextKey}",
                                CompletionType = CompletionType.Snippet,
                                Priority = 70
                            });
                        }
                    }
                }
            }

            return completions;
        }

        private static List<CompletionItem> GetTemplateCompletions(string input)
        {
            return _templates.Values
                .Where(t => t.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                .Select(t => new CompletionItem
                {
                    Text = t.Template,
                    DisplayText = t.Name,
                    Description = $"代码模板: {t.Name}",
                    CompletionType = CompletionType.Template,
                    Priority = 60
                })
                .ToList();
        }

        private static List<CodeDiagnostic> CheckCommonPatterns(string code)
        {
            var diagnostics = new List<CodeDiagnostic>();

            // 检查是否忘记使用CAD前缀
            if (Regex.IsMatch(code, @"(?<!CAD\.)(Line|Circle|Arc|Move|Copy)\s*\("))
            {
                diagnostics.Add(new CodeDiagnostic
                {
                    Severity = DiagnosticSeverity.Warning,
                    Message = "建议使用 CAD. 前缀调用DotNetARX方法",
                    Suggestion = "使用 CAD.Line() 而不是 Line()",
                    Range = new CodeRange { Start = 0, End = code.Length }
                });
            }

            return diagnostics;
        }

        private static List<CodeDiagnostic> CheckPerformanceIssues(string code)
        {
            var diagnostics = new List<CodeDiagnostic>();

            // 检查循环中的重复操作
            if (Regex.IsMatch(code, @"for\s*\(.*?\)\s*\{[^}]*CAD\.(Move|Copy|Rotate|Scale)", RegexOptions.Multiline))
            {
                diagnostics.Add(new CodeDiagnostic
                {
                    Severity = DiagnosticSeverity.Warning,
                    Message = "循环中的CAD操作可能影响性能",
                    Suggestion = "考虑使用批处理版本的方法",
                    Range = new CodeRange { Start = 0, End = code.Length }
                });
            }

            return diagnostics;
        }

        private static List<CodeDiagnostic> CheckBestPractices(string code)
        {
            var diagnostics = new List<CodeDiagnostic>();

            // 检查是否缺少异常处理
            if (code.Contains("CAD.") && !code.Contains("try"))
            {
                diagnostics.Add(new CodeDiagnostic
                {
                    Severity = DiagnosticSeverity.Info,
                    Message = "建议添加异常处理",
                    Suggestion = "AutoCAD操作可能抛出异常，建议使用try-catch",
                    Range = new CodeRange { Start = 0, End = code.Length }
                });
            }

            return diagnostics;
        }

        private static List<CodeDiagnostic> CheckThreadSafetyIssues(string code)
        {
            var diagnostics = new List<CodeDiagnostic>();

            // 检查Task.Run中的AutoCAD调用
            if (Regex.IsMatch(code, @"Task\.Run\s*\([^)]*CAD\."))
            {
                diagnostics.Add(new CodeDiagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Message = "不要在Task.Run中调用AutoCAD API",
                    Suggestion = "AutoCAD API必须在主线程中调用",
                    Range = new CodeRange { Start = 0, End = code.Length }
                });
            }

            return diagnostics;
        }

        private static string GetMethodDisplayText(MethodInfo method)
        {
            var parameters = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
            return $"{method.Name}({parameters})";
        }

        private static string GetMethodDescription(string methodName)
        {
            return methodName.ToLower() switch
            {
                "line" => "绘制直线，高性能实现",
                "circle" => "绘制圆，自动优化",
                "move" => "移动实体，支持批处理",
                "copy" => "复制实体，智能缓存",
                "selectbytype" => "按类型选择实体，结果缓存",
                _ => $"DotNetARX {methodName} 方法"
            };
        }

        private static string GetParameterDescription(string methodName, string paramName)
        {
            return (methodName.ToLower(), paramName.ToLower()) switch
            {
                ("line", "startpoint") => "直线起点",
                ("line", "endpoint") => "直线终点",
                ("circle", "center") => "圆心坐标",
                ("circle", "radius") => "半径",
                ("move", "entityid") => "要移动的实体ID",
                ("move", "frompoint") => "源点坐标",
                ("move", "topoint") => "目标点坐标",
                _ => paramName
            };
        }

        private static string GetMethodExample(string methodName)
        {
            return methodName.ToLower() switch
            {
                "line" => "CAD.Line(Point3d.Origin, new Point3d(100, 100, 0))",
                "circle" => "CAD.Circle(new Point3d(50, 50, 0), 25)",
                "move" => "CAD.Move(entityId, Point3d.Origin, new Point3d(10, 10, 0))",
                _ => ""
            };
        }

        #endregion 私有辅助方法
    }
}