namespace DotNetARX
{
    /// <summary>
    /// DotNetARX 统一API门面
    /// 提供对所有DotNetARX功能的统一访问入口
    /// </summary>
    public static class ARX
    {
        private static bool _initialized = false;
        private static readonly object _initLock = new object();

        /// <summary>
        /// 初始化DotNetARX
        /// </summary>
        /// <param name="configure">额外配置</param>
        public static void Initialize(Action<IServiceContainer> configure = null)
        {
            lock (_initLock)
            {
                if (_initialized) return;

                ServiceInitializer.Initialize(configure);
                _initialized = true;
            }
        }

        /// <summary>
        /// 确保DotNetARX已初始化
        /// </summary>
        private static void EnsureInitialized()
        {
            if (!_initialized)
            {
                Initialize();
            }
        }

        #region 核心服务访问

        /// <summary>
        /// 日志记录器
        /// </summary>
        public static ILogger Logger => ServiceInitializer.GetService<ILogger>() ?? LogManager.DefaultLogger;

        /// <summary>
        /// 配置管理器
        /// </summary>
        public static IConfigurationManager Config => ServiceInitializer.GetService<IConfigurationManager>() ?? GlobalConfiguration.Instance;

        /// <summary>
        /// 性能监控器
        /// </summary>
        public static IPerformanceMonitor Performance => ServiceInitializer.GetService<IPerformanceMonitor>() ?? GlobalPerformanceMonitor.Instance;

        /// <summary>
        /// 事件发布器
        /// </summary>
        public static IEventPublisher Events => CADEventManager.Publisher;

        /// <summary>
        /// 服务容器
        /// </summary>
        public static IServiceContainer Services => ServiceLocator.Current;

        #endregion 核心服务访问

        #region 实体操作

        /// <summary>
        /// 实体操作（同步版本）
        /// </summary>
        public static class ARXEntity
        {
            private static IEntityService _operations;

            private static IEntityService Operations
            {
                get
                {
                    if (_operations == null)
                    {
                        EnsureInitialized();
                        _operations = ServiceInitializer.GetRequiredService<IEntityService>();
                    }
                    return _operations;
                }
            }

            /// <summary>
            /// 移动实体
            /// </summary>
            public static bool Move(ObjectId entityId, Point3d fromPoint, Point3d toPoint)
            {
                return Operations.MoveEntity(entityId, fromPoint, toPoint);
            }

            /// <summary>
            /// 复制实体
            /// </summary>
            public static ObjectId Copy(ObjectId entityId, Point3d fromPoint, Point3d toPoint)
            {
                return Operations.CopyEntity(entityId, fromPoint, toPoint);
            }

            /// <summary>
            /// 旋转实体
            /// </summary>
            public static bool Rotate(ObjectId entityId, Point3d basePoint, double angle)
            {
                return Operations.RotateEntity(entityId, basePoint, angle);
            }

            /// <summary>
            /// 缩放实体
            /// </summary>
            public static bool Scale(ObjectId entityId, Point3d basePoint, double scaleFactor)
            {
                return Operations.ScaleEntity(entityId, basePoint, scaleFactor);
            }

            /// <summary>
            /// 偏移实体
            /// </summary>
            public static ObjectIdCollection Offset(ObjectId entityId, double distance)
            {
                return Operations.OffsetEntity(entityId, distance);
            }

            /// <summary>
            /// 镜像实体
            /// </summary>
            public static ObjectId Mirror(ObjectId entityId, Point3d mirrorPt1, Point3d mirrorPt2, bool eraseSource = false)
            {
                return Operations.MirrorEntity(entityId, mirrorPt1, mirrorPt2, eraseSource);
            }

            /// <summary>
            /// 验证实体
            /// </summary>
            public static bool Validate(ObjectId entityId)
            {
                return Operations.ValidateEntity(entityId);
            }
        }

        /// <summary>
        /// 实体操作（异步版本）
        /// </summary>
        public static class EntityAsync
        {
            private static AsyncEntityOperationService _asyncOperations;

            private static AsyncEntityOperationService AsyncOperations
            {
                get
                {
                    if (_asyncOperations == null)
                    {
                        EnsureInitialized();
                        _asyncOperations = ServiceInitializer.GetService<AsyncEntityOperationService>()
                                         ?? new AsyncEntityOperationService();
                    }
                    return _asyncOperations;
                }
            }

            /// <summary>
            /// 异步移动实体
            /// </summary>
            public static async Task<AsyncOperationResult<bool>> MoveAsync(
                ObjectId entityId,
                Point3d fromPoint,
                Point3d toPoint,
                CancellationToken cancellationToken = default)
            {
                return await AsyncOperations.MoveEntityAsync(entityId, fromPoint, toPoint, cancellationToken);
            }

            /// <summary>
            /// 批量异步操作
            /// </summary>
            public static async Task<List<AsyncOperationResult<ObjectId>>> BatchOperationAsync<T>(
                IEnumerable<T> items,
                Func<T, ObjectId> operation,
                IProgress<AsyncProgress> progress = null,
                CancellationToken cancellationToken = default,
                int maxConcurrency = 4)
            {
                return await AsyncOperations.BatchOperationAsync(items, operation, progress, cancellationToken, maxConcurrency);
            }

            /// <summary>
            /// 异步获取实体
            /// </summary>
            public static async Task<AsyncOperationResult<List<T>>> GetEntitiesAsync<T>(
                Autodesk.AutoCAD.DatabaseServices.Database database,
                Func<T, bool> predicate = null,
                CancellationToken cancellationToken = default) where T : Autodesk.AutoCAD.DatabaseServices.Entity
            {
                return await AsyncOperations.GetEntitiesAsync(database, predicate, cancellationToken);
            }
        }

        #endregion 实体操作

        #region 图层操作

        /// <summary>
        /// 图层操作
        /// </summary>
        public static class ARXLayer
        {
            private static ILayerManager _layerManager;

            private static ILayerManager LayerManager
            {
                get
                {
                    if (_layerManager == null)
                    {
                        EnsureInitialized();
                        _layerManager = ServiceInitializer.GetService<ILayerManager>()
                                      ?? new Services.LayerManagerService();
                    }
                    return _layerManager;
                }
            }

            /// <summary>
            /// 创建图层
            /// </summary>
            public static ObjectId Create(string layerName, short colorIndex = 7)
            {
                return LayerManager.CreateLayer(layerName, colorIndex);
            }

            /// <summary>
            /// 设置当前图层
            /// </summary>
            public static bool SetCurrent(string layerName)
            {
                return LayerManager.SetCurrentLayer(layerName);
            }

            /// <summary>
            /// 删除图层
            /// </summary>
            public static bool Delete(string layerName)
            {
                return LayerManager.DeleteLayer(layerName);
            }

            /// <summary>
            /// 检查图层是否存在
            /// </summary>
            public static bool Exists(string layerName)
            {
                return LayerManager.LayerExists(layerName);
            }

            /// <summary>
            /// 获取所有图层名称
            /// </summary>
            public static IEnumerable<string> GetNames()
            {
                return LayerManager.GetLayerNames();
            }

            /// <summary>
            /// 获取所有图层
            /// </summary>
            public static IEnumerable<LayerTableRecord> GetAll()
            {
                return LayerManager.GetAllLayers();
            }

            /// <summary>
            /// 设置图层属性
            /// </summary>
            public static bool SetProperties(string layerName, short? colorIndex = null, bool? isLocked = null, bool? isFrozen = null)
            {
                return LayerManager.SetLayerProperties(layerName, colorIndex, isLocked, isFrozen);
            }
        }

        #endregion 图层操作

        #region 选择操作

        /// <summary>
        /// 选择操作
        /// </summary>
        public static class ARXSelection
        {
            private static ISelectionService _selectionService;

            private static ISelectionService SelectionService
            {
                get
                {
                    if (_selectionService == null)
                    {
                        EnsureInitialized();
                        _selectionService = ServiceInitializer.GetRequiredService<ISelectionService>();
                    }
                    return _selectionService;
                }
            }

            /// <summary>
            /// 按类型选择实体
            /// </summary>
            public static List<T> ByType<T>() where T : Autodesk.AutoCAD.DatabaseServices.Entity
            {
                return SelectionService.SelectByType<T>();
            }

            /// <summary>
            /// 在窗口内选择实体
            /// </summary>
            public static List<T> InWindow<T>(Point3d pt1, Point3d pt2) where T : Autodesk.AutoCAD.DatabaseServices.Entity
            {
                return SelectionService.SelectInWindow<T>(pt1, pt2);
            }

            /// <summary>
            /// 交叉窗口选择实体
            /// </summary>
            public static List<T> CrossingWindow<T>(Point3d pt1, Point3d pt2) where T : Autodesk.AutoCAD.DatabaseServices.Entity
            {
                return SelectionService.SelectCrossingWindow<T>(pt1, pt2);
            }

            /// <summary>
            /// 通过过滤器选择实体
            /// </summary>
            public static List<T> ByFilter<T>(SelectionFilter filter) where T : Autodesk.AutoCAD.DatabaseServices.Entity
            {
                return SelectionService.SelectByFilter<T>(filter);
            }

            /// <summary>
            /// 选择指定点处的实体
            /// </summary>
            public static List<T> AtPoint<T>(Point3d point) where T : Autodesk.AutoCAD.DatabaseServices.Entity
            {
                return SelectionService.SelectAtPoint<T>(point);
            }

            /// <summary>
            /// 获取当前选择集
            /// </summary>
            public static ObjectIdCollection GetCurrent()
            {
                return SelectionService.GetCurrentSelection();
            }
        }

        #endregion 选择操作

        #region 数据库操作

        /// <summary>
        /// 数据库操作
        /// </summary>
        public static class ARXDatabase
        {
            private static IDatabaseService _databaseOperations;

            private static IDatabaseService DatabaseOperations
            {
                get
                {
                    if (_databaseOperations == null)
                    {
                        EnsureInitialized();
                        _databaseOperations = ServiceInitializer.GetRequiredService<IDatabaseService>();
                    }
                    return _databaseOperations;
                }
            }

            /// <summary>
            /// 添加实体到模型空间
            /// </summary>
            public static ObjectId AddToModelSpace(Autodesk.AutoCAD.DatabaseServices.Entity entity)
            {
                return DatabaseOperations.AddToModelSpace(entity);
            }

            /// <summary>
            /// 批量添加实体到模型空间
            /// </summary>
            public static ObjectIdCollection AddToModelSpace(IEnumerable<Autodesk.AutoCAD.DatabaseServices.Entity> entities)
            {
                return DatabaseOperations.AddToModelSpace(entities);
            }

            /// <summary>
            /// 添加实体到图纸空间
            /// </summary>
            public static ObjectId AddToPaperSpace(Autodesk.AutoCAD.DatabaseServices.Entity entity)
            {
                return DatabaseOperations.AddToPaperSpace(entity);
            }

            /// <summary>
            /// 添加实体到当前空间
            /// </summary>
            public static ObjectId AddToCurrentSpace(Autodesk.AutoCAD.DatabaseServices.Entity entity)
            {
                return DatabaseOperations.AddToCurrentSpace(entity);
            }

            /// <summary>
            /// 删除实体
            /// </summary>
            public static bool DeleteEntity(ObjectId entityId)
            {
                return DatabaseOperations.DeleteEntity(entityId);
            }

            /// <summary>
            /// 批量删除实体
            /// </summary>
            public static int DeleteEntities(IEnumerable<ObjectId> entityIds)
            {
                return DatabaseOperations.DeleteEntities(entityIds);
            }

            /// <summary>
            /// 获取数据库信息
            /// </summary>
            public static DatabaseInfo GetInfo()
            {
                return DatabaseOperations.GetDatabaseInfo();
            }

            /// <summary>
            /// 返向兼容性方法 - 添加实体到模型空间（旧版本）
            /// </summary>
            [Obsolete("请使用 AddToModelSpace(Entity) 方法")]
            public static ObjectId AddToModelSpace(Autodesk.AutoCAD.DatabaseServices.Database db, Autodesk.AutoCAD.DatabaseServices.Entity entity)
            {
                return db.AddToModelSpaceImproved(entity);
            }

            /// <summary>
            /// 返向兼容性方法 - 批量添加实体到模型空间（旧版本）
            /// </summary>
            [Obsolete("请使用 AddToModelSpace(IEnumerable<Entity>) 方法")]
            public static ObjectIdCollection AddToModelSpace(Autodesk.AutoCAD.DatabaseServices.Database db, params Autodesk.AutoCAD.DatabaseServices.Entity[] entities)
            {
                return db.AddToModelSpaceImproved(entities);
            }

            /// <summary>
            /// 获取模型空间ID
            /// </summary>
            public static ObjectId GetModelSpaceId(Autodesk.AutoCAD.DatabaseServices.Database db)
            {
                return db.GetModelSpaceId();
            }

            /// <summary>
            /// 获取图纸空间ID
            /// </summary>
            public static ObjectId GetPaperSpaceId(Autodesk.AutoCAD.DatabaseServices.Database db)
            {
                return db.GetPaperSpaceId();
            }
        }

        #endregion 数据库操作

        #region 绘图操作

        /// <summary>
        /// 绘图操作
        /// </summary>
        public static class ARXDrawing
        {
            private static IDrawingService _drawingOperations;

            private static IDrawingService DrawingOperations
            {
                get
                {
                    if (_drawingOperations == null)
                    {
                        EnsureInitialized();
                        _drawingOperations = ServiceInitializer.GetRequiredService<IDrawingService>();
                    }
                    return _drawingOperations;
                }
            }

            /// <summary>
            /// 绘制直线
            /// </summary>
            public static ObjectId Line(Point3d startPoint, Point3d endPoint)
            {
                return DrawingOperations.DrawLine(startPoint, endPoint);
            }

            /// <summary>
            /// 绘制圆
            /// </summary>
            public static ObjectId Circle(Point3d center, double radius)
            {
                return DrawingOperations.DrawCircle(center, radius);
            }

            /// <summary>
            /// 绘制圆弧
            /// </summary>
            public static ObjectId Arc(Point3d center, double radius, double startAngle, double endAngle)
            {
                return DrawingOperations.DrawArc(center, radius, startAngle, endAngle);
            }

            /// <summary>
            /// 绘制多段线
            /// </summary>
            public static ObjectId Polyline(IEnumerable<Point2d> points, bool isClosed = false)
            {
                return DrawingOperations.DrawPolyline(points, isClosed);
            }

            /// <summary>
            /// 绘制文本
            /// </summary>
            public static ObjectId Text(string text, Point3d position, double height, double rotation = 0)
            {
                return DrawingOperations.DrawText(text, position, height, rotation);
            }

            /// <summary>
            /// 绘制多行文本
            /// </summary>
            public static ObjectId MText(string text, Point3d position, double width, double height)
            {
                return DrawingOperations.DrawMText(text, position, width, height);
            }
        }

        #endregion 绘图操作

        #region 命令操作

        /// <summary>
        /// 命令操作
        /// </summary>
        public static class Command
        {
            private static ICommandService _commandService;

            private static ICommandService CommandService
            {
                get
                {
                    if (_commandService == null)
                    {
                        EnsureInitialized();
                        _commandService = ServiceInitializer.GetRequiredService<ICommandService>();
                    }
                    return _commandService;
                }
            }

            /// <summary>
            /// COM方式执行命令
            /// </summary>
            public static bool ExecuteCOM(string command)
            {
                return CommandService.ExecuteCommandCOM(command);
            }

            /// <summary>
            /// 异步执行命令
            /// </summary>
            public static bool ExecuteAsync(string command)
            {
                return CommandService.ExecuteCommandAsync(command);
            }

            /// <summary>
            /// 队列表达式执行命令
            /// </summary>
            public static bool ExecuteQueue(string command)
            {
                return CommandService.ExecuteCommandQueue(command);
            }

            /// <summary>
            /// ARX方式执行命令
            /// </summary>
            public static bool ExecuteARX(string command, params string[] args)
            {
                return CommandService.ExecuteARXCommand(command, args);
            }
        }

        #endregion 命令操作

        #region 文档操作

        /// <summary>
        /// 文档操作
        /// </summary>
        public static class ARXDocument
        {
            private static IDocumentService _documentService;

            private static IDocumentService DocumentService
            {
                get
                {
                    if (_documentService == null)
                    {
                        EnsureInitialized();
                        _documentService = ServiceInitializer.GetRequiredService<IDocumentService>();
                    }
                    return _documentService;
                }
            }

            /// <summary>
            /// 检查文档是否需要保存
            /// </summary>
            public static bool NeedsSave()
            {
                return DocumentService.CheckDocumentNeedsSave();
            }

            /// <summary>
            /// 保存文档
            /// </summary>
            public static bool Save()
            {
                return DocumentService.SaveDocument();
            }

            /// <summary>
            /// 另存为
            /// </summary>
            public static bool SaveAs(string filePath)
            {
                return DocumentService.SaveDocumentAs(filePath);
            }

            /// <summary>
            /// 获取文档信息
            /// </summary>
            public static DocumentInfo GetInfo()
            {
                return DocumentService.GetDocumentInfo();
            }
        }

        #endregion 文档操作

        #region 几何工具

        /// <summary>
        /// 几何工具
        /// </summary>
        public static class ARXGeometry
        {
            private static IGeometryService _geometryService;

            private static IGeometryService GeometryService
            {
                get
                {
                    if (_geometryService == null)
                    {
                        EnsureInitialized();
                        _geometryService = ServiceInitializer.GetRequiredService<IGeometryService>();
                    }
                    return _geometryService;
                }
            }

            /// <summary>
            /// 计算两点距离
            /// </summary>
            public static double Distance(Point3d pt1, Point3d pt2)
            {
                return GeometryService.CalculateDistance(pt1, pt2);
            }

            /// <summary>
            /// 计算角度
            /// </summary>
            public static double Angle(Point3d pt1, Point3d pt2, Point3d pt3)
            {
                return GeometryService.CalculateAngle(pt1, pt2, pt3);
            }

            /// <summary>
            /// 检查点是否在多边形内
            /// </summary>
            public static bool PointInPolygon(Point3d point, IEnumerable<Point3d> polygon)
            {
                return GeometryService.IsPointInPolygon(point, polygon);
            }

            /// <summary>
            /// 获取实体边界框
            /// </summary>
            public static Extents3d GetBounds(ObjectId entityId)
            {
                return GeometryService.GetEntityBounds(entityId);
            }
        }

        #endregion 几何工具

        #region 样式管理

        /// <summary>
        /// 样式管理
        /// </summary>
        public static class ARXStyle
        {
            private static IStyleService _styleService;

            private static IStyleService StyleService
            {
                get
                {
                    if (_styleService == null)
                    {
                        EnsureInitialized();
                        _styleService = ServiceInitializer.GetRequiredService<IStyleService>();
                    }
                    return _styleService;
                }
            }

            /// <summary>
            /// 创建文字样式
            /// </summary>
            public static ObjectId CreateTextStyle(string styleName, string fontName, double textSize)
            {
                return StyleService.CreateTextStyle(styleName, fontName, textSize);
            }

            /// <summary>
            /// 创建标注样式
            /// </summary>
            public static ObjectId CreateDimStyle(string styleName, double textHeight, double arrowSize)
            {
                return StyleService.CreateDimStyle(styleName, textHeight, arrowSize);
            }

            /// <summary>
            /// 创建线型
            /// </summary>
            public static ObjectId CreateLineType(string linetypeName, string pattern, string description)
            {
                return StyleService.CreateLineType(linetypeName, pattern, description);
            }
        }

        #endregion 样式管理

        #region 表格操作

        /// <summary>
        /// 表格操作
        /// </summary>
        public static class ARXTable
        {
            private static ITableService _tableService;

            private static ITableService TableService
            {
                get
                {
                    if (_tableService == null)
                    {
                        EnsureInitialized();
                        _tableService = ServiceInitializer.GetRequiredService<ITableService>();
                    }
                    return _tableService;
                }
            }

            /// <summary>
            /// 创建表格
            /// </summary>
            public static ObjectId Create(Point3d position, int rows, int columns, double rowHeight, double columnWidth)
            {
                return TableService.CreateTable(position, rows, columns, rowHeight, columnWidth);
            }

            /// <summary>
            /// 设置单元格文本
            /// </summary>
            public static bool SetCellText(ObjectId tableId, int row, int column, string text)
            {
                return TableService.SetCellText(tableId, row, column, text);
            }

            /// <summary>
            /// 获取单元格文本
            /// </summary>
            public static string GetCellText(ObjectId tableId, int row, int column)
            {
                return TableService.GetCellText(tableId, row, column);
            }

            /// <summary>
            /// 合并单元格
            /// </summary>
            public static bool MergeCells(ObjectId tableId, int startRow, int startColumn, int endRow, int endColumn)
            {
                return TableService.MergeCells(tableId, startRow, startColumn, endRow, endColumn);
            }
        }

        #endregion 表格操作

        #region 布局操作

        /// <summary>
        /// 布局操作
        /// </summary>
        public static class ARXLayout
        {
            private static ILayoutService _layoutService;

            private static ILayoutService LayoutService
            {
                get
                {
                    if (_layoutService == null)
                    {
                        EnsureInitialized();
                        _layoutService = ServiceInitializer.GetRequiredService<ILayoutService>();
                    }
                    return _layoutService;
                }
            }

            /// <summary>
            /// 创建布局
            /// </summary>
            public static ObjectId Create(string layoutName)
            {
                return LayoutService.CreateLayout(layoutName);
            }

            /// <summary>
            /// 删除布局
            /// </summary>
            public static bool Delete(string layoutName)
            {
                return LayoutService.DeleteLayout(layoutName);
            }

            /// <summary>
            /// 创建视口
            /// </summary>
            public static ObjectId CreateViewport(Point3d center, double width, double height)
            {
                // 获取当前布局ID
                var layoutId = LayoutManager.Current.GetLayoutId(LayoutManager.Current.CurrentLayout);
                return LayoutService.CreateViewport(layoutId, center, width, height);
            }

            /// <summary>
            /// 设置视口比例
            /// </summary>
            public static bool SetViewportScale(ObjectId viewportId, double scale)
            {
                return LayoutService.SetViewportScale(viewportId, scale);
            }
        }

        #endregion 布局操作

        #region 用户界面

        /// <summary>
        /// 用户界面
        /// </summary>
        public static class ARXUI
        {
            private static IUIService _uiService;

            private static IUIService UIService
            {
                get
                {
                    if (_uiService == null)
                    {
                        EnsureInitialized();
                        _uiService = ServiceInitializer.GetRequiredService<IUIService>();
                    }
                    return _uiService;
                }
            }

            /// <summary>
            /// 显示消息
            /// </summary>
            public static void ShowMessage(string message, string title = "信息")
            {
                UIService.ShowMessage(message, title);
            }

            /// <summary>
            /// 显示确认对话框
            /// </summary>
            public static bool ShowConfirmation(string message, string title = "确认")
            {
                return UIService.ShowConfirmationDialog(message, title);
            }

            /// <summary>
            /// 获取用户输入
            /// </summary>
            public static string GetUserInput(string prompt, string defaultValue = "")
            {
                return UIService.GetUserInput(prompt, defaultValue);
            }

            /// <summary>
            /// 选择文件
            /// </summary>
            public static string SelectFile(string title, string filter, bool forSave = false)
            {
                return UIService.SelectFile(title, filter, forSave);
            }
        }

        #endregion 用户界面

        #region 工具服务

        /// <summary>
        /// 工具服务
        /// </summary>
        public static class Utility
        {
            private static IUtilityService _utilityService;

            private static IUtilityService UtilityService
            {
                get
                {
                    if (_utilityService == null)
                    {
                        EnsureInitialized();
                        _utilityService = ServiceInitializer.GetRequiredService<IUtilityService>();
                    }
                    return _utilityService;
                }
            }

            /// <summary>
            /// 验证字符串
            /// </summary>
            public static bool ValidateString(string value, string pattern)
            {
                return UtilityService.ValidateString(value, pattern);
            }

            /// <summary>
            /// 安全转换类型
            /// </summary>
            public static T SafeConvert<T>(object value, T defaultValue = default(T))
            {
                return UtilityService.SafeConvert(value, defaultValue);
            }

            /// <summary>
            /// 获取AutoCAD路径
            /// </summary>
            public static string GetAutoCADPath()
            {
                return UtilityService.GetAutoCADPath();
            }

            /// <summary>
            /// 亮显实体
            /// </summary>
            public static bool HighlightEntity(ObjectId entityId, bool highlight = true)
            {
                return UtilityService.HighlightEntity(entityId, highlight);
            }

            /// <summary>
            /// 安全执行操作
            /// </summary>
            public static T SafeExecute<T>(Func<T> operation, T defaultValue = default(T))
            {
                return UtilityService.SafeExecute(operation, defaultValue);
            }
        }

        #endregion 工具服务

        #region 块操作

        /// <summary>
        /// 块操作
        /// </summary>
        public static class ARXBlock
        {
            private static IBlockService _blockOperations;

            private static IBlockService BlockOperations
            {
                get
                {
                    if (_blockOperations == null)
                    {
                        EnsureInitialized();
                        _blockOperations = ServiceInitializer.GetRequiredService<IBlockService>();
                    }
                    return _blockOperations;
                }
            }

            /// <summary>
            /// 创建块定义
            /// </summary>
            public static ObjectId CreateDefinition(string blockName, IEnumerable<Autodesk.AutoCAD.DatabaseServices.Entity> entities, Point3d basePoint)
            {
                return BlockOperations.CreateBlockDefinition(blockName, entities, basePoint);
            }

            /// <summary>
            /// 插入块引用
            /// </summary>
            public static ObjectId Insert(string blockName, Point3d position, double scale = 1.0, double rotation = 0)
            {
                return BlockOperations.InsertBlock(blockName, position, scale, rotation);
            }

            /// <summary>
            /// 插入块引用（带属性）
            /// </summary>
            public static ObjectId InsertWithAttributes(string blockName, Point3d position, Dictionary<string, string> attributes, double scale = 1.0, double rotation = 0)
            {
                return BlockOperations.InsertBlockWithAttributes(blockName, position, attributes, scale, rotation);
            }

            /// <summary>
            /// 删除块定义
            /// </summary>
            public static bool DeleteDefinition(string blockName)
            {
                return BlockOperations.DeleteBlockDefinition(blockName);
            }

            /// <summary>
            /// 获取所有块名称
            /// </summary>
            public static IEnumerable<string> GetNames()
            {
                return BlockOperations.GetBlockNames();
            }

            /// <summary>
            /// 分解块引用
            /// </summary>
            public static ObjectIdCollection Explode(ObjectId blockReferenceId)
            {
                return BlockOperations.ExplodeBlock(blockReferenceId);
            }
        }

        #endregion 块操作

        #region 进度管理

        /// <summary>
        /// 进度管理
        /// </summary>
        public static class Progress
        {
            /// <summary>
            /// 创建进度管理器
            /// </summary>
            public static IProgressManager Create()
            {
                EnsureInitialized();
                return ServiceInitializer.GetRequiredService<IProgressManager>();
            }
        }

        #endregion 进度管理

        #region 系统信息

        /// <summary>
        /// 获取系统信息
        /// </summary>
        public static string GetSystemInfo()
        {
            EnsureInitialized();
            return ServiceInitializer.GenerateSystemReport();
        }

        /// <summary>
        /// 检查是否已初始化
        /// </summary>
        public static bool IsInitialized => _initialized;

        /// <summary>
        /// 版本信息
        /// </summary>
        public static string Version => "2.0.0";

        #endregion 系统信息
    }

    /// <summary>
    /// DotNetARX 快捷访问类
    /// 提供更简洁的API调用方式，直接调用ARX类的相应方法
    /// 注意：与 CAD 主类不同，CAD 主类在 CAD.cs 和 API/CAD.*.cs 中定义
    /// </summary>
    public static partial class CAD
    {
        /// <summary>
        /// 实体操作快捷方式
        /// </summary>
        public static class ARXEntity
        {
            public static bool Move(ObjectId id, Point3d from, Point3d to) => ARX.ARXEntity.Move(id, from, to);

            public static ObjectId Copy(ObjectId id, Point3d from, Point3d to) => ARX.ARXEntity.Copy(id, from, to);

            public static bool Rotate(ObjectId id, Point3d basePoint, double angle) => ARX.ARXEntity.Rotate(id, basePoint, angle);

            public static bool Scale(ObjectId id, Point3d basePoint, double factor) => ARX.ARXEntity.Scale(id, basePoint, factor);

            public static ObjectIdCollection Offset(ObjectId id, double distance) => ARX.ARXEntity.Offset(id, distance);

            public static ObjectId Mirror(ObjectId id, Point3d pt1, Point3d pt2, bool eraseSource = false) => ARX.ARXEntity.Mirror(id, pt1, pt2, eraseSource);
        }

        /// <summary>
        /// 数据库操作快捷方式
        /// </summary>
        public static class ARXDatabase
        {
            public static ObjectId Add(Autodesk.AutoCAD.DatabaseServices.Entity entity) => ARX.ARXDatabase.AddToCurrentSpace(entity);

            public static ObjectIdCollection AddBatch(IEnumerable<Autodesk.AutoCAD.DatabaseServices.Entity> entities) => ARX.ARXDatabase.AddToModelSpace(entities);

            public static bool Delete(ObjectId entityId) => ARX.ARXDatabase.DeleteEntity(entityId);

            public static int DeleteBatch(IEnumerable<ObjectId> entityIds) => ARX.ARXDatabase.DeleteEntities(entityIds);

            public static DatabaseInfo GetInfo() => ARX.ARXDatabase.GetInfo();

            // 兼容旧版本的方法
            [Obsolete("请使用 Add(Entity) 方法")]
            public static ObjectId Add(Autodesk.AutoCAD.DatabaseServices.Database db, Autodesk.AutoCAD.DatabaseServices.Entity ent) => ARX.ARXDatabase.AddToModelSpace(db, ent);

            [Obsolete("请使用 AddBatch(IEnumerable<Entity>) 方法")]
            public static ObjectIdCollection Add(Autodesk.AutoCAD.DatabaseServices.Database db, params Autodesk.AutoCAD.DatabaseServices.Entity[] ents) => ARX.ARXDatabase.AddToModelSpace(db, ents);

            public static ObjectId ModelSpace(Autodesk.AutoCAD.DatabaseServices.Database db) => ARX.ARXDatabase.GetModelSpaceId(db);

            public static ObjectId PaperSpace(Autodesk.AutoCAD.DatabaseServices.Database db) => ARX.ARXDatabase.GetPaperSpaceId(db);
        }

        /// <summary>
        /// 图层操作快捷方式
        /// </summary>
        public static class ARXLayer
        {
            public static ObjectId Create(string name, short color = 7) => ARX.ARXLayer.Create(name, color);

            public static bool SetCurrent(string name) => ARX.ARXLayer.SetCurrent(name);

            public static bool Delete(string name) => ARX.ARXLayer.Delete(name);

            public static bool Exists(string name) => ARX.ARXLayer.Exists(name);

            public static IEnumerable<string> GetNames() => ARX.ARXLayer.GetNames();

            public static bool Lock(string name) => ARX.ARXLayer.SetProperties(name, isLocked: true);

            public static bool Unlock(string name) => ARX.ARXLayer.SetProperties(name, isLocked: false);

            public static bool Freeze(string name) => ARX.ARXLayer.SetProperties(name, isFrozen: true);

            public static bool Thaw(string name) => ARX.ARXLayer.SetProperties(name, isFrozen: false);

            public static bool SetColor(string name, short colorIndex) => ARX.ARXLayer.SetProperties(name, colorIndex: colorIndex);
        }

        /// <summary>
        /// 日志记录快捷方式
        /// </summary>
        public static class Log
        {
            public static void Debug(string message) => ARX.Logger.Debug(message);

            public static void Info(string message) => ARX.Logger.Info(message);

            public static void Warning(string message) => ARX.Logger.Warning(message);

            public static void Error(string message, Exception ex = null) => ARX.Logger.Error(message, ex);
        }

        /// <summary>
        /// 性能监控快捷方式
        /// </summary>
        public static class Perf
        {
            public static void Record(string name, double value) => ARX.Performance.RecordMetric(name, value);

            public static IDisposable Timer(string name) => ARX.Performance.StartTimer(name);

            public static void Count(string name) => ARX.Performance.IncrementCounter(name);
        }

        /// <summary>
        /// 选择操作快捷方式
        /// </summary>
        public static class ARXSelection
        {
            public static List<T> ByType<T>() where T : Autodesk.AutoCAD.DatabaseServices.Entity => ARX.ARXSelection.ByType<T>();

            public static List<T> InWindow<T>(Point3d pt1, Point3d pt2) where T : Autodesk.AutoCAD.DatabaseServices.Entity => ARX.ARXSelection.InWindow<T>(pt1, pt2);

            public static List<T> CrossingWindow<T>(Point3d pt1, Point3d pt2) where T : Autodesk.AutoCAD.DatabaseServices.Entity => ARX.ARXSelection.CrossingWindow<T>(pt1, pt2);

            public static List<T> ByFilter<T>(SelectionFilter filter) where T : Autodesk.AutoCAD.DatabaseServices.Entity => ARX.ARXSelection.ByFilter<T>(filter);

            public static List<T> AtPoint<T>(Point3d point) where T : Autodesk.AutoCAD.DatabaseServices.Entity => ARX.ARXSelection.AtPoint<T>(point);

            public static ObjectIdCollection Current() => ARX.ARXSelection.GetCurrent();
        }

        /// <summary>
        /// 绘图操作快捷方式
        /// </summary>
        public static class ARXDrawing
        {
            public static ObjectId Line(Point3d start, Point3d end) => ARX.ARXDrawing.Line(start, end);

            public static ObjectId Circle(Point3d center, double radius) => ARX.ARXDrawing.Circle(center, radius);

            public static ObjectId Arc(Point3d center, double radius, double startAngle, double endAngle) => ARX.ARXDrawing.Arc(center, radius, startAngle, endAngle);

            public static ObjectId Polyline(IEnumerable<Point2d> points, bool closed = false) => ARX.ARXDrawing.Polyline(points, closed);

            public static ObjectId Text(string text, Point3d position, double height, double rotation = 0) => ARX.ARXDrawing.Text(text, position, height, rotation);

            public static ObjectId MText(string text, Point3d position, double width, double height) => ARX.ARXDrawing.MText(text, position, width, height);
        }

        /// <summary>
        /// 块操作快捷方式
        /// </summary>
        public static class ARXBlock
        {
            public static ObjectId Create(string name, IEnumerable<Autodesk.AutoCAD.DatabaseServices.Entity> entities, Point3d basePoint) => ARX.ARXBlock.CreateDefinition(name, entities, basePoint);

            public static ObjectId Insert(string name, Point3d position, double scale = 1.0, double rotation = 0) => ARX.ARXBlock.Insert(name, position, scale, rotation);

            public static ObjectId InsertWithAttribs(string name, Point3d position, Dictionary<string, string> attributes, double scale = 1.0, double rotation = 0) => ARX.ARXBlock.InsertWithAttributes(name, position, attributes, scale, rotation);

            public static bool Delete(string name) => ARX.ARXBlock.DeleteDefinition(name);

            public static IEnumerable<string> GetNames() => ARX.ARXBlock.GetNames();

            public static ObjectIdCollection Explode(ObjectId blockRefId) => ARX.ARXBlock.Explode(blockRefId);
        }
    }
}