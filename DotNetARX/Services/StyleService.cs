namespace DotNetARX.Services
{
    /// <summary>
    /// 样式管理服务实现
    /// </summary>
    public partial class StyleService : IStyleService
    {
        private readonly IEventBus _eventBus;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly ILogger _logger;

        public StyleService(
            IEventBus eventBus = null,
            IPerformanceMonitor performanceMonitor = null,
            ILogger logger = null)
        {
            _eventBus = eventBus ?? ServiceContainer.Instance.GetService<IEventBus>();
            _performanceMonitor = performanceMonitor ?? ServiceContainer.Instance.GetService<IPerformanceMonitor>();
            _logger = logger ?? ServiceContainer.Instance.GetService<ILogger>();
        }

        /// <summary>
        /// 创建文字样式
        /// </summary>
        public ObjectId CreateTextStyle(string styleName, string fontName, double height = 0)
        {
            using var operation = _performanceMonitor?.StartOperation("CreateTextStyle");

            try
            {
                if (string.IsNullOrEmpty(styleName))
                    throw new ArgumentException("样式名称不能为空");

                if (string.IsNullOrEmpty(fontName))
                    throw new ArgumentException("字体名称不能为空");

                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;
                ObjectId styleId;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var textStyleTable = transManager.GetObject<TextStyleTable>(database.TextStyleTableId, OpenMode.ForWrite);

                    if (textStyleTable.Has(styleName))
                    {
                        _logger?.Info($"文字样式 '{styleName}' 已存在，返回现有样式");
                        styleId = textStyleTable[styleName];
                    }
                    else
                    {
                        var textStyleRecord = new TextStyleTableRecord
                        {
                            Name = styleName,
                            FileName = fontName,
                            TextSize = height
                        };

                        styleId = textStyleTable.Add(textStyleRecord);
                        transManager.AddNewlyCreatedDBObject(textStyleRecord, true);

                        _logger?.Info($"文字样式创建成功: {styleName}");
                    }

                    transManager.Commit();
                }

                _eventBus?.Publish(new StyleServiceEvent("TextStyleCreated", styleId, "TextStyleCreated", "TextStyle"));
                return styleId;
            }
            catch (Exception ex)
            {
                _logger?.Error($"创建文字样式失败: {ex.Message}", ex);
                throw new StyleOperationException($"创建文字样式失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 创建标注样式
        /// </summary>
        public ObjectId CreateDimensionStyle(string styleName)
        {
            return CreateDimStyle(styleName);
        }

        /// <summary>
        /// 创建标注样式（简化方法名）
        /// </summary>
        public ObjectId CreateDimStyle(string styleName)
        {
            using var operation = _performanceMonitor?.StartOperation("CreateDimensionStyle");

            try
            {
                if (string.IsNullOrEmpty(styleName))
                    throw new ArgumentException("样式名称不能为空");

                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;
                ObjectId styleId;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var dimStyleTable = transManager.GetObject<DimStyleTable>(database.DimStyleTableId, OpenMode.ForWrite);

                    if (dimStyleTable.Has(styleName))
                    {
                        _logger?.Info($"标注样式 '{styleName}' 已存在，返回现有样式");
                        styleId = dimStyleTable[styleName];
                    }
                    else
                    {
                        var dimStyleRecord = new DimStyleTableRecord
                        {
                            Name = styleName
                        };

                        styleId = dimStyleTable.Add(dimStyleRecord);
                        transManager.AddNewlyCreatedDBObject(dimStyleRecord, true);

                        _logger?.Info($"标注样式创建成功: {styleName}");
                    }

                    transManager.Commit();
                }

                _eventBus?.Publish(new StyleServiceEvent("DimensionStyleCreated", styleId, "DimensionStyleCreated", "DimensionStyle"));
                return styleId;
            }
            catch (Exception ex)
            {
                _logger?.Error($"创建标注样式失败: {ex.Message}", ex);
                throw new StyleOperationException($"创建标注样式失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 创建标注样式
        /// </summary>
        public ObjectId CreateDimStyle(string styleName, double textHeight, double arrowSize)
        {
            using var operation = _performanceMonitor?.StartOperation("CreateDimStyle");

            try
            {
                if (string.IsNullOrEmpty(styleName))
                    throw new ArgumentException("样式名称不能为空");

                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var dimStyleTable = transManager.GetObject<DimStyleTable>(database.DimStyleTableId, OpenMode.ForRead);

                    if (dimStyleTable.Has(styleName))
                    {
                        _logger?.Info($"标注样式 '{styleName}' 已存在");
                        return dimStyleTable[styleName];
                    }

                    dimStyleTable.UpgradeOpen();

                    var dimStyleRecord = new DimStyleTableRecord();
                    dimStyleRecord.Name = styleName;

                    // 设置文字高度
                    dimStyleRecord.Dimtxt = textHeight;

                    // 设置箭头大小
                    dimStyleRecord.Dimasz = arrowSize;

                    var dimStyleId = dimStyleTable.Add(dimStyleRecord);
                    transManager.AddNewlyCreatedDBObject(dimStyleRecord, true);

                    transManager.Commit();

                    _eventBus?.Publish(new StyleServiceEvent("DimStyleCreated", dimStyleId, "DimStyleCreated", "DimensionStyle"));
                    _logger?.Info($"标注样式创建成功: {styleName}");

                    return dimStyleId;
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"创建标注样式失败: {ex.Message}", ex);
                throw new StyleOperationException($"创建标注样式失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 创建线型
        /// </summary>
        public ObjectId CreateLineType(string typeName, string pattern, string description = "")
        {
            using var operation = _performanceMonitor?.StartOperation("CreateLineType");

            try
            {
                if (string.IsNullOrEmpty(typeName))
                    throw new ArgumentException("线型名称不能为空");

                if (string.IsNullOrEmpty(pattern))
                    throw new ArgumentException("线型模式不能为空");

                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;
                ObjectId lineTypeId;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var lineTypeTable = transManager.GetObject<LinetypeTable>(database.LinetypeTableId, OpenMode.ForWrite);

                    if (lineTypeTable.Has(typeName))
                    {
                        _logger?.Info($"线型 '{typeName}' 已存在，返回现有线型");
                        lineTypeId = lineTypeTable[typeName];
                    }
                    else
                    {
                        var lineTypeRecord = new LinetypeTableRecord
                        {
                            Name = typeName,
                            AsciiDescription = string.IsNullOrEmpty(description) ? pattern : description
                        };

                        // 解析线型模式并设置
                        ParseLineTypePattern(lineTypeRecord, pattern);

                        lineTypeId = lineTypeTable.Add(lineTypeRecord);
                        transManager.AddNewlyCreatedDBObject(lineTypeRecord, true);

                        _logger?.Info($"线型创建成功: {typeName}");
                    }

                    transManager.Commit();
                }

                _eventBus?.Publish(new StyleServiceEvent("LineTypeCreated", lineTypeId, "LineTypeCreated", "LineType"));
                return lineTypeId;
            }
            catch (Exception ex)
            {
                _logger?.Error($"创建线型失败: {ex.Message}", ex);
                throw new StyleOperationException($"创建线型失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取所有文字样式名称
        /// </summary>
        public IEnumerable<string> GetTextStyleNames()
        {
            using var operation = _performanceMonitor?.StartOperation("GetTextStyleNames");

            try
            {
                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;
                var styleNames = new List<string>();

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var textStyleTable = transManager.GetObject<TextStyleTable>(database.TextStyleTableId, OpenMode.ForRead);

                    foreach (ObjectId styleId in textStyleTable)
                    {
                        var styleRecord = transManager.GetObject<TextStyleTableRecord>(styleId, OpenMode.ForRead);
                        styleNames.Add(styleRecord.Name);
                    }

                    transManager.Commit();
                }

                return styleNames;
            }
            catch (Exception ex)
            {
                _logger?.Error($"获取文字样式名称失败: {ex.Message}", ex);
                return new List<string>();
            }
        }

        /// <summary>
        /// 获取所有标注样式名称
        /// </summary>
        public IEnumerable<string> GetDimensionStyleNames()
        {
            using var operation = _performanceMonitor?.StartOperation("GetDimensionStyleNames");

            try
            {
                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;
                var styleNames = new List<string>();

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var dimStyleTable = transManager.GetObject<DimStyleTable>(database.DimStyleTableId, OpenMode.ForRead);

                    foreach (ObjectId styleId in dimStyleTable)
                    {
                        var styleRecord = transManager.GetObject<DimStyleTableRecord>(styleId, OpenMode.ForRead);
                        styleNames.Add(styleRecord.Name);
                    }

                    transManager.Commit();
                }

                return styleNames;
            }
            catch (Exception ex)
            {
                _logger?.Error($"获取标注样式名称失败: {ex.Message}", ex);
                return new List<string>();
            }
        }

        /// <summary>
        /// 获取所有线型名称
        /// </summary>
        public IEnumerable<string> GetLineTypeNames()
        {
            using var operation = _performanceMonitor?.StartOperation("GetLineTypeNames");

            try
            {
                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;
                var lineTypeNames = new List<string>();

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var lineTypeTable = transManager.GetObject<LinetypeTable>(database.LinetypeTableId, OpenMode.ForRead);

                    foreach (ObjectId lineTypeId in lineTypeTable)
                    {
                        var lineTypeRecord = transManager.GetObject<LinetypeTableRecord>(lineTypeId, OpenMode.ForRead);
                        lineTypeNames.Add(lineTypeRecord.Name);
                    }

                    transManager.Commit();
                }

                return lineTypeNames;
            }
            catch (Exception ex)
            {
                _logger?.Error($"获取线型名称失败: {ex.Message}", ex);
                return new List<string>();
            }
        }

        /// <summary>
        /// 解析线型模式
        /// </summary>
        private void ParseLineTypePattern(LinetypeTableRecord lineTypeRecord, string pattern)
        {
            try
            {
                // 这里可以实现复杂的线型模式解析
                // 简单实现：设置为连续线
                lineTypeRecord.NumDashes = 0;

                _logger?.Debug($"线型模式解析: {pattern}");
            }
            catch (Exception ex)
            {
                _logger?.Warning($"解析线型模式失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 样式事件类
    /// </summary>
    public class StyleServiceEvent : Events.EventArgs
    {
        public string EventType { get; }
        public ObjectId StyleId { get; }
        public string StyleName { get; }
        public string StyleType { get; }
        public new DateTime Timestamp { get; }

        public StyleServiceEvent(string eventType, ObjectId styleId, string styleName, string styleType)
            : base("StyleService")
        {
            EventType = eventType;
            StyleId = styleId;
            StyleName = styleName;
            StyleType = styleType;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 样式操作异常
    /// </summary>
    public class StyleOperationException : DotNetARXException
    {
        public StyleOperationException(string message) : base(message)
        {
        }

        public StyleOperationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}