using EventArgs = DotNetARX.Events.EventArgs;

namespace DotNetARX.Services
{
    /// <summary>
    /// 布局操作服务实现
    /// </summary>
    public class LayoutService : ILayoutService
    {
        private readonly IEventBus _eventBus;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly ILogger _logger;

        public LayoutService(
            IEventBus eventBus = null,
            IPerformanceMonitor performanceMonitor = null,
            ILogger logger = null)
        {
            _eventBus = eventBus ?? ServiceContainer.Instance.GetService<IEventBus>();
            _performanceMonitor = performanceMonitor ?? ServiceContainer.Instance.GetService<IPerformanceMonitor>();
            _logger = logger ?? ServiceContainer.Instance.GetService<ILogger>();
        }

        /// <summary>
        /// 创建布局
        /// </summary>
        public ObjectId CreateLayout(string layoutName)
        {
            using var operation = _performanceMonitor?.StartOperation("CreateLayout");

            try
            {
                if (string.IsNullOrEmpty(layoutName))
                    throw new ArgumentException("布局名称不能为空");

                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;
                ObjectId layoutId;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var layoutManager = LayoutManager.Current;

                    // 检查布局是否已存在
                    if (layoutManager.LayoutExists(layoutName))
                    {
                        _logger?.Info($"布局 '{layoutName}' 已存在");
                        layoutId = layoutManager.GetLayoutId(layoutName);
                    }
                    else
                    {
                        // 创建新布局
                        layoutId = layoutManager.CreateLayout(layoutName);
                        _logger?.Info($"布局创建成功: {layoutName}");
                    }

                    transManager.Commit();
                }

                _eventBus?.Publish(new LayoutEvent("LayoutCreated", layoutName, layoutId));
                return layoutId;
            }
            catch (Exception ex)
            {
                _logger?.Error($"创建布局失败: {ex.Message}", ex);
                throw new LayoutOperationException($"创建布局失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 删除布局
        /// </summary>
        public bool DeleteLayout(string layoutName)
        {
            using var operation = _performanceMonitor?.StartOperation("DeleteLayout");

            try
            {
                if (string.IsNullOrEmpty(layoutName))
                    throw new ArgumentException("布局名称不能为空");

                // 不能删除Model布局
                if (layoutName.Equals("Model", StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.Warning("不能删除Model布局");
                    return false;
                }

                var layoutManager = LayoutManager.Current;

                if (!layoutManager.LayoutExists(layoutName))
                {
                    _logger?.Warning($"布局 '{layoutName}' 不存在");
                    return false;
                }

                // 如果是当前布局，先切换到其他布局
                if (layoutManager.CurrentLayout.Equals(layoutName, StringComparison.OrdinalIgnoreCase))
                {
                    var layouts = GetLayoutNames().Where(name => !name.Equals(layoutName, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (layouts.Any())
                    {
                        layoutManager.CurrentLayout = layouts.First();
                    }
                }

                layoutManager.DeleteLayout(layoutName);

                _eventBus?.Publish(new LayoutEvent("LayoutDeleted", layoutName, ObjectId.Null));
                _logger?.Info($"布局删除成功: {layoutName}");

                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"删除布局失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 创建视口
        /// </summary>
        public ObjectId CreateViewport(ObjectId layoutId, Point3d center, double width, double height)
        {
            using var operation = _performanceMonitor?.StartOperation("CreateViewport");

            try
            {
                if (layoutId.IsNull || !layoutId.IsValid)
                    throw new ArgumentException("无效的布局ID");

                if (width <= 0 || height <= 0)
                    throw new ArgumentException("视口宽度和高度必须大于0");

                var database = layoutId.Database;
                ObjectId viewportId;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var layout = transManager.GetObject<Layout>(layoutId, OpenMode.ForRead);
                    var paperSpace = transManager.GetObject<BlockTableRecord>(layout.BlockTableRecordId, OpenMode.ForWrite);

                    // 创建视口
                    var viewport = new Viewport();
                    viewport.CenterPoint = center;
                    viewport.Width = width;
                    viewport.Height = height;

                    // 设置视口为开启状态
                    viewport.On = true;

                    viewportId = paperSpace.AppendEntity(viewport);
                    transManager.AddNewlyCreatedDBObject(viewport, true);

                    transManager.Commit();
                }

                _eventBus?.Publish(new LayoutEvent("ViewportCreated", "", viewportId));
                _logger?.Info($"视口创建成功: {viewportId}, 中心 {center}, 尺寸 {width}x{height}");

                return viewportId;
            }
            catch (Exception ex)
            {
                _logger?.Error($"创建视口失败: {ex.Message}", ex);
                throw new LayoutOperationException($"创建视口失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 设置视口比例
        /// </summary>
        public bool SetViewportScale(ObjectId viewportId, double scale)
        {
            using var operation = _performanceMonitor?.StartOperation("SetViewportScale");

            try
            {
                if (viewportId.IsNull || !viewportId.IsValid)
                    throw new ArgumentException("无效的视口ID");

                if (scale <= 0)
                    throw new ArgumentException("比例必须大于0");

                var database = viewportId.Database;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var viewport = transManager.GetObject<Viewport>(viewportId, OpenMode.ForWrite);

                    // 设置视口的缩放比例
                    viewport.CustomScale = scale;

                    transManager.Commit();
                }

                _eventBus?.Publish(new LayoutEvent("ViewportScaleSet", "", viewportId));
                _logger?.Info($"视口比例设置成功: {viewportId}, 比例 {scale}");

                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"设置视口比例失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 获取所有布局名称
        /// </summary>
        public IEnumerable<string> GetLayoutNames()
        {
            using var operation = _performanceMonitor?.StartOperation("GetLayoutNames");

            try
            {
                var layoutNames = new List<string>();
                var database = Application.DocumentManager.MdiActiveDocument.Database;

                using (var trans = database.TransactionManager.StartTransaction())
                {
                    var layoutDict = trans.GetObject(database.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;

                    foreach (DictionaryEntry entry in layoutDict)
                    {
                        var layoutId = (ObjectId)entry.Value;
                        var layout = trans.GetObject(layoutId, OpenMode.ForRead) as Layout;
                        if (layout != null)
                        {
                            layoutNames.Add(layout.LayoutName);
                        }
                    }

                    trans.Commit();
                }

                return layoutNames;
            }
            catch (Exception ex)
            {
                _logger?.Error($"获取布局名称失败: {ex.Message}", ex);
                return new List<string>();
            }
        }

        /// <summary>
        /// 设置当前布局
        /// </summary>
        public bool SetCurrentLayout(string layoutName)
        {
            using var operation = _performanceMonitor?.StartOperation("SetCurrentLayout");

            try
            {
                if (string.IsNullOrEmpty(layoutName))
                    throw new ArgumentException("布局名称不能为空");

                var layoutManager = LayoutManager.Current;

                if (!layoutManager.LayoutExists(layoutName))
                {
                    _logger?.Warning($"布局 '{layoutName}' 不存在");
                    return false;
                }

                layoutManager.CurrentLayout = layoutName;

                _eventBus?.Publish(new LayoutEvent("CurrentLayoutSet", layoutName, ObjectId.Null));
                _logger?.Info($"当前布局设置成功: {layoutName}");

                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"设置当前布局失败: {ex.Message}", ex);
                return false;
            }
        }
    }

    /// <summary>
    /// 布局事件类
    /// </summary>
    public class LayoutEvent : EventArgs
    {
        public string EventType { get; }
        public string LayoutName { get; }
        public ObjectId ObjectId { get; }
        public string Details { get; }
        public new DateTime Timestamp { get; }

        public LayoutEvent(string eventType, string layoutName, ObjectId objectId = default, string details = null)
            : base("LayoutService")
        {
            EventType = eventType;
            LayoutName = layoutName;
            ObjectId = objectId;
            Details = details;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 布局操作异常
    /// </summary>
    public class LayoutOperationException : DotNetARXException
    {
        public LayoutOperationException(string message) : base(message)
        {
        }

        public LayoutOperationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}