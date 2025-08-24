using Autodesk.AutoCAD.Colors;
using DotNetARX.Caching;
using DotNetARX.DependencyInjection;
using System.Runtime.CompilerServices;


namespace DotNetARX
{
    /// <summary>
    /// DotNetARX 终极统一API
    /// 易用性与高性能的完美结合 - 默认即最优实现
    /// </summary>
    public static class CAD
    {
        private static readonly Lazy<bool> _initialized = new(() =>
        {
            InitializeCADSystem();
            return true;
        });

        /// <summary>
        /// 确保系统已初始化
        /// </summary>
        private static void EnsureInitialized() => _ = _initialized.Value;

        /// <summary>
        /// 初始化CAD系统
        /// </summary>
        private static void InitializeCADSystem()
        {
            // 初始化智能服务定位器
            var container = SmartServiceLocator.Current;

            // 注册DotNetARX核心服务
            container.RegisterDotNetARXServices();

            // 初始化性能监控
            PerformanceEngine.Initialize();

            // 初始化智能上下文管理器
            AutoCADContext.Initialize();

            // 初始化日志系统
            LogManager.Initialize();

            // 记录容器信息
            var logger = LogManager.GetLogger(typeof(CAD));
            logger.Info($"CAD系统初始化完成 - {SmartServiceLocator.GetContainerInfo()}");
        }

        #region 实体操作 - 智能优化

        /// <summary>
        /// 移动实体 - 智能路径选择，零配置高性能
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="fromPoint">源点</param>
        /// <param name="toPoint">目标点</param>
        /// <returns>操作是否成功</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Move(ObjectId entityId, Point3d fromPoint, Point3d toPoint)
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("Move", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    // 快速退出检查
                    if (fromPoint.IsEqualTo(toPoint, 1e-10))
                        return true;

                    var context = AutoCADContext.Current;
                    var entity = context.GetObject<Entity>(entityId, OpenMode.ForWrite);
                    if (entity == null) return false;

                    entity.TransformBy(Matrix3d.Displacement(toPoint - fromPoint));
                    return true;
                })
            );
        }

        /// <summary>
        /// 复制实体 - 自动优化
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="fromPoint">源点</param>
        /// <param name="toPoint">目标点</param>
        /// <returns>复制的实体ID</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectId Copy(ObjectId entityId, Point3d fromPoint, Point3d toPoint)
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("Copy", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var original = context.GetObject<Entity>(entityId, OpenMode.ForRead);
                    if (original == null) return ObjectId.Null;

                    var displacement = toPoint - fromPoint;
                    var copy = original.GetTransformedCopy(Matrix3d.Displacement(displacement));

                    var modelSpace = context.GetObject<BlockTableRecord>(
                        context.Database.GetModelSpaceId(), OpenMode.ForWrite);

                    var copyId = modelSpace.AppendEntity(copy);
                    context.Transaction.AddNewlyCreatedDBObject(copy, true);

                    return copyId;
                })
            );
        }

        /// <summary>
        /// 旋转实体
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Rotate(ObjectId entityId, Point3d basePoint, double angle)
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("Rotate", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    if (Math.Abs(angle) < 1e-10) return true;

                    var context = AutoCADContext.Current;
                    var entity = context.GetObject<Entity>(entityId, OpenMode.ForWrite);
                    if (entity == null) return false;

                    entity.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, basePoint));
                    return true;
                })
            );
        }

        /// <summary>
        /// 缩放实体
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Scale(ObjectId entityId, Point3d basePoint, double scaleFactor)
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("Scale", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    if (scaleFactor <= 0) return false;
                    if (Math.Abs(scaleFactor - 1.0) < 1e-10) return true;

                    var context = AutoCADContext.Current;
                    var entity = context.GetObject<Entity>(entityId, OpenMode.ForWrite);
                    if (entity == null) return false;

                    entity.TransformBy(Matrix3d.Scaling(scaleFactor, basePoint));
                    return true;
                })
            );
        }

        /// <summary>
        /// 批量移动 - 智能批处理优化
        /// </summary>
        /// <param name="operations">移动操作列表</param>
        public static void Move(IEnumerable<(ObjectId Id, Point3d From, Point3d To)> operations)
        {
            EnsureInitialized();

            var ops = operations?.ToList();
            if (ops == null || ops.Count == 0) return;

            if (ops.Count == 1)
            {
                // 单个操作，直接调用
                var (id, from, to) = ops[0];
                Move(id, from, to);
                return;
            }

            // 智能批量操作优化
            PerformanceEngine.Execute("BatchMove", () =>
                AutoCADContext.ExecuteBatch(context =>
                {
                    // 按位移向量分组优化，减少矩阵计算
                    var groups = ops.GroupBy(op => op.To - op.From);

                    foreach (var group in groups)
                    {
                        var moveMatrix = Matrix3d.Displacement(group.Key);

                        foreach (var (id, _, _) in group)
                        {
                            var entity = context.GetObject<Entity>(id, OpenMode.ForWrite);
                            entity?.TransformBy(moveMatrix);
                        }
                    }
                })
            );
        }

        #endregion 实体操作 - 智能优化

        #region 绘图操作 - 高性能实现

        /// <summary>
        /// 绘制直线 - 零开销
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectId Line(Point3d startPoint, Point3d endPoint)
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("DrawLine", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var line = new Line(startPoint, endPoint);
                    return AddToCurrentSpace(line);
                })
            );
        }

        /// <summary>
        /// 绘制圆 - 零开销
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectId Circle(Point3d center, double radius)
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("DrawCircle", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var circle = new Circle(center, Vector3d.ZAxis, radius);
                    return AddToCurrentSpace(circle);
                })
            );
        }

        /// <summary>
        /// 绘制圆弧
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectId Arc(Point3d center, double radius, double startAngle, double endAngle)
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("DrawArc", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var arc = new Arc(center, radius, startAngle, endAngle);
                    return AddToCurrentSpace(arc);
                })
            );
        }

        /// <summary>
        /// 绘制文本
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectId Text(string text, Point3d position, double height, double rotation = 0)
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("DrawText", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var dbText = new DBText
                    {
                        TextString = text ?? "",
                        Position = position,
                        Height = height,
                        Rotation = rotation
                    };
                    return AddToCurrentSpace(dbText);
                })
            );
        }

        /// <summary>
        /// 批量绘制 - 智能批处理
        /// </summary>
        public static ObjectIdCollection Draw(IEnumerable<Entity> entities)
        {
            EnsureInitialized();

            var entitiesList = entities?.ToList();
            if (entitiesList == null || entitiesList.Count == 0)
                return new ObjectIdCollection();

            return PerformanceEngine.Execute("BatchDraw", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var modelSpace = context.GetObject<BlockTableRecord>(
                        context.Database.GetModelSpaceId(), OpenMode.ForWrite);

                    var results = new ObjectIdCollection();

                    foreach (var entity in entitiesList)
                    {
                        if (entity != null)
                        {
                            var id = modelSpace.AppendEntity(entity);
                            context.Transaction.AddNewlyCreatedDBObject(entity, true);
                            results.Add(id);
                        }
                    }

                    return results;
                })
            );
        }

        #endregion 绘图操作 - 高性能实现

        #region 图层操作 - 智能缓存

        private static readonly Lazy<ISmartCache<string, ObjectId>> _layerCache =
            new(() => SmartCacheManager.GetCache<string, ObjectId>("LayerCache", 500, TimeSpan.FromHours(1)));

        /// <summary>
        /// 创建图层 - 智能缓存和去重
        /// </summary>
        /// <param name="layerName">图层名称</param>
        /// <param name="colorIndex">颜色索引</param>
        /// <returns>图层ID</returns>
        public static ObjectId CreateLayer(string layerName, short colorIndex = 7)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(layerName))
                return ObjectId.Null;

            return _layerCache.Value.GetOrAdd(layerName, name =>
                PerformanceEngine.Execute("CreateLayer", () =>
                    AutoCADContext.ExecuteSafely(() =>
                    {
                        var context = AutoCADContext.Current;
                        var layerTable = context.GetObject<LayerTable>(
                            context.Database.LayerTableId, OpenMode.ForRead);

                        if (layerTable.Has(name))
                        {
                            return layerTable[name];
                        }

                        layerTable.UpgradeOpen();
                        var layerRecord = new LayerTableRecord
                        {
                            Name = name,
                            Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
                        };

                        var layerId = layerTable.Add(layerRecord);
                        context.Transaction.AddNewlyCreatedDBObject(layerRecord, true);

                        return layerId;
                    })
                )
            );
        }

        /// <summary>
        /// 设置当前图层
        /// </summary>
        public static bool SetCurrentLayer(string layerName)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(layerName))
                return false;

            return PerformanceEngine.Execute("SetCurrentLayer", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var layerTable = context.GetObject<LayerTable>(
                        context.Database.LayerTableId, OpenMode.ForRead);

                    if (!layerTable.Has(layerName)) return false;

                    context.Database.Clayer = layerTable[layerName];
                    return true;
                })
            );
        }

        /// <summary>
        /// 检查图层是否存在
        /// </summary>
        public static bool LayerExists(string layerName)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(layerName))
                return false;

            return _layerCache.Value.ContainsKey(layerName) ||
                   PerformanceEngine.Execute("CheckLayer", () =>
                       AutoCADContext.ExecuteSafely(() =>
                       {
                           var context = AutoCADContext.Current;
                           var layerTable = context.GetObject<LayerTable>(
                               context.Database.LayerTableId, OpenMode.ForRead);
                           var exists = layerTable.Has(layerName);

                           // 如果存在但不在缓存中，加入缓存
                           if (exists && !_layerCache.Value.ContainsKey(layerName))
                           {
                               _layerCache.Value.Set(layerName, layerTable[layerName]);
                           }

                           return exists;
                       })
                   );
        }

        #endregion 图层操作 - 智能缓存

        #region 选择操作 - 高性能查询

        /// <summary>
        /// 按类型选择实体
        /// </summary>
        public static List<T> SelectByType<T>() where T : Entity
        {
            EnsureInitialized();

            return PerformanceEngine.Execute($"SelectByType<{typeof(T).Name}>", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var results = new List<T>();

                    var modelSpace = context.GetObject<BlockTableRecord>(
                        context.Database.GetModelSpaceId(), OpenMode.ForRead);

                    foreach (ObjectId id in modelSpace)
                    {
                        var entity = context.GetObject<Entity>(id, OpenMode.ForRead);
                        if (entity is T typedEntity)
                        {
                            results.Add(typedEntity);
                        }
                    }

                    return results;
                })
            );
        }

        /// <summary>
        /// 智能过滤选择
        /// </summary>
        public static List<T> Select<T>(Func<T, bool> predicate = null) where T : Entity
        {
            EnsureInitialized();

            return PerformanceEngine.Execute($"SmartSelect<{typeof(T).Name}>", () =>
            {
                var allEntities = SelectByType<T>();
                return predicate == null ? allEntities : allEntities.Where(predicate).ToList();
            });
        }

        #endregion 选择操作 - 高性能查询

        #region 内部辅助方法

        /// <summary>
        /// 添加实体到当前空间 - 内联优化
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ObjectId AddToCurrentSpace(Entity entity)
        {
            var context = AutoCADContext.Current;
            var currentSpace = context.GetObject<BlockTableRecord>(
                context.Database.GetCurrentSpaceId(), OpenMode.ForWrite);

            var id = currentSpace.AppendEntity(entity);
            context.Transaction.AddNewlyCreatedDBObject(entity, true);
            return id;
        }

        #endregion 内部辅助方法

        #region 系统管理和监控

        /// <summary>
        /// 获取性能报告
        /// </summary>
        public static string GetPerformanceReport()
        {
            EnsureInitialized();
            return PerformanceEngine.GenerateReport();
        }

        /// <summary>
        /// 获取详细性能分析报告
        /// </summary>
        public static async Task<string> GetDetailedPerformanceReport(TimeSpan? timeRange = null)
        {
            EnsureInitialized();
            return await PerformanceTestRunner.GeneratePerformanceReport(timeRange);
        }

        /// <summary>
        /// 运行性能基准测试
        /// </summary>
        public static async Task<PerformanceTestReport> RunBenchmarks(bool fastMode = true)
        {
            EnsureInitialized();
            return await PerformanceTestRunner.RunAllBenchmarks(fastMode);
        }

        /// <summary>
        /// 检查性能回归
        /// </summary>
        public static async Task<RegressionTestReport> CheckPerformanceRegression()
        {
            EnsureInitialized();
            return await PerformanceTestRunner.RunRegressionTests();
        }

        /// <summary>
        /// 获取性能建议
        /// </summary>
        public static List<PerformanceRecommendation> GetPerformanceRecommendations()
        {
            EnsureInitialized();
            return AutoPerformanceMonitor.Instance.GetRecommendations();
        }

        /// <summary>
        /// 获取系统性能摘要
        /// </summary>
        public static SystemPerformanceSummary GetSystemPerformanceSummary()
        {
            EnsureInitialized();
            return AutoPerformanceMonitor.Instance.GetSystemSummary();
        }

        /// <summary>
        /// 运行智能诊断
        /// </summary>
        public static DiagnosticReport RunSystemDiagnostic()
        {
            EnsureInitialized();
            return SmartDiagnostics.RunFullDiagnostic();
        }

        /// <summary>
        /// 检查系统健康状态
        /// </summary>
        public static HealthCheckResult CheckSystemHealth()
        {
            EnsureInitialized();
            return SmartDiagnostics.CheckSystemHealth();
        }

        /// <summary>
        /// 获取智能代码建议
        /// </summary>
        public static List<CodeSuggestion> GetCodeSuggestions(string operationName, object[] parameters = null)
        {
            EnsureInitialized();
            return SmartDiagnostics.GetCodeSuggestions(operationName, parameters);
        }

        /// <summary>
        /// 获取自动修复建议
        /// </summary>
        public static List<AutoFixSuggestion> GetAutoFixSuggestions()
        {
            EnsureInitialized();
            return SmartDiagnostics.GetAutoFixSuggestions();
        }

        /// <summary>
        /// 获取代码补全建议
        /// </summary>
        public static List<CompletionItem> GetCodeCompletions(string context, string input)
        {
            EnsureInitialized();
            return IntelliSenseHelper.GetCompletions(context, input);
        }

        /// <summary>
        /// 获取方法签名帮助
        /// </summary>
        public static SignatureHelp GetSignatureHelp(string methodName)
        {
            EnsureInitialized();
            return IntelliSenseHelper.GetSignatureHelp(methodName);
        }

        /// <summary>
        /// 生成代码片段
        /// </summary>
        public static string GenerateCodeSnippet(string templateName, Dictionary<string, string> parameters = null)
        {
            EnsureInitialized();
            return IntelliSenseHelper.GenerateCodeSnippet(templateName, parameters);
        }

        /// <summary>
        /// 分析代码质量
        /// </summary>
        public static CodeQualityReport AnalyzeCodeQuality(string code)
        {
            EnsureInitialized();
            return CodeQualityAnalyzer.AnalyzeCode(code);
        }

        /// <summary>
        /// 获取配置设置
        /// </summary>
        public static T GetConfiguration<T>(string key, T defaultValue = default)
        {
            return ConfigurationProvider.Get(key, defaultValue);
        }

        /// <summary>
        /// 设置配置值
        /// </summary>
        public static void SetConfiguration<T>(string key, T value)
        {
            ConfigurationProvider.Set(key, value);
        }

        /// <summary>
        /// 获取配置摘要
        /// </summary>
        public static ConfigurationSummary GetConfigurationSummary()
        {
            return ConfigurationProvider.GetSummary();
        }

        /// <summary>
        /// 生成配置报告
        /// </summary>
        public static string GetConfigurationReport()
        {
            return ConfigurationHelper.GenerateConfigurationReport();
        }

        /// <summary>
        /// 应用性能优化配置
        /// </summary>
        public static void ApplyPerformanceOptimizations()
        {
            ConfigurationHelper.ApplyPerformanceOptimizations();
        }

        /// <summary>
        /// 应用开发环境配置
        /// </summary>
        public static void ApplyDevelopmentConfiguration()
        {
            ConfigurationHelper.ApplyDevelopmentConfiguration();
        }

        /// <summary>
        /// 应用生产环境配置
        /// </summary>
        public static void ApplyProductionConfiguration()
        {
            ConfigurationHelper.ApplyProductionConfiguration();
        }

        /// <summary>
        /// 重置性能统计
        /// </summary>
        public static void ResetPerformanceMetrics()
        {
            EnsureInitialized();
            PerformanceEngine.Reset();
            AutoPerformanceMonitor.Instance.ResetMetrics();
            PerformanceAnalyzer.CleanupOldData();
        }

        /// <summary>
        /// 获取系统状态
        /// </summary>
        public static bool IsInitialized => _initialized.IsValueCreated && _initialized.Value;

        #endregion 系统管理和监控
    }
}