using EventArgs = DotNetARX.Events.EventArgs;
using Autodesk.AutoCAD.DatabaseServices;

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

        /// <summary>
        /// 检查布局是否存在
        /// </summary>
        public bool LayoutExists(string layoutName)
        {
            using var operation = _performanceMonitor?.StartOperation("LayoutExists");

            try
            {
                if (string.IsNullOrEmpty(layoutName))
                    return false;

                return LayoutManager.Current.LayoutExists(layoutName);
            }
            catch (Exception ex)
            {
                _logger?.Error($"检查布局存在性失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 获取所有布局信息
        /// </summary>
        public IEnumerable<LayoutInfo> GetAllLayouts()
        {
            using var operation = _performanceMonitor?.StartOperation("GetAllLayouts");

            try
            {
                var layouts = new List<LayoutInfo>();
                var layoutManager = LayoutManager.Current;
                var currentLayout = layoutManager.CurrentLayout;
                var database = Application.DocumentManager.MdiActiveDocument.Database;

                using (var trans = database.TransactionManager.StartTransaction())
                {
                    var layoutDict = trans.GetObject(database.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;

                    foreach (DictionaryEntry entry in layoutDict)
                    {
                        var layoutName = (string)entry.Key;
                        var layoutId = (ObjectId)entry.Value;
                        var layout = trans.GetObject(layoutId, OpenMode.ForRead) as Layout;

                        if (layout != null)
                        {
                            var layoutInfo = new LayoutInfo
                            {
                                Name = layoutName,
                                ObjectId = layoutId,
                                IsCurrent = layoutName.Equals(currentLayout, StringComparison.OrdinalIgnoreCase),
                                PaperSize = new PaperSize
                                {
                                    Width = layout.PlotPaperSize.X,
                                    Height = layout.PlotPaperSize.Y
                                }
                            };

                            layouts.Add(layoutInfo);
                        }
                    }

                    trans.Commit();
                }

                return layouts;
            }
            catch (Exception ex)
            {
                _logger?.Error($"获取所有布局信息失败: {ex.Message}", ex);
                return new List<LayoutInfo>();
            }
        }

        /// <summary>
        /// 获取布局信息
        /// </summary>
        public LayoutInfo GetLayoutInfo(string layoutName)
        {
            using var operation = _performanceMonitor?.StartOperation("GetLayoutInfo");

            try
            {
                if (string.IsNullOrEmpty(layoutName) || !LayoutManager.Current.LayoutExists(layoutName))
                    return null;

                var layoutManager = LayoutManager.Current;
                var currentLayout = layoutManager.CurrentLayout;
                var layoutId = layoutManager.GetLayoutId(layoutName);
                var database = Application.DocumentManager.MdiActiveDocument.Database;

                using (var trans = database.TransactionManager.StartTransaction())
                {
                    var layout = trans.GetObject(layoutId, OpenMode.ForRead) as Layout;

                    if (layout == null)
                        return null;

                    var layoutInfo = new LayoutInfo
                    {
                        Name = layoutName,
                        ObjectId = layoutId,
                        IsCurrent = layoutName.Equals(currentLayout, StringComparison.OrdinalIgnoreCase),
                        PaperSize = new PaperSize
                        {
                            Width = layout.PlotPaperSize.X,
                            Height = layout.PlotPaperSize.Y
                        }
                    };

                    trans.Commit();
                    return layoutInfo;
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"获取布局信息失败: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 重命名布局
        /// </summary>
        public bool RenameLayout(string oldName, string newName)
        {
            using var operation = _performanceMonitor?.StartOperation("RenameLayout");

            try
            {
                if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
                    throw new ArgumentException("布局名称不能为空");

                var layoutManager = LayoutManager.Current;

                if (!layoutManager.LayoutExists(oldName))
                {
                    _logger?.Warning($"布局 '{oldName}' 不存在");
                    return false;
                }

                if (layoutManager.LayoutExists(newName))
                {
                    _logger?.Warning($"布局 '{newName}' 已存在");
                    return false;
                }

                // 不能重命名Model布局
                if (oldName.Equals("Model", StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.Warning("不能重命名Model布局");
                    return false;
                }

                layoutManager.RenameLayout(oldName, newName);

                _eventBus?.Publish(new LayoutEvent("LayoutRenamed", newName, ObjectId.Null, $"OldName: {oldName}"));
                _logger?.Info($"布局重命名成功: {oldName} -> {newName}");

                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"重命名布局失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 复制布局
        /// </summary>
        public ObjectId CopyLayout(string sourceLayoutName, string newLayoutName)
        {
            using var operation = _performanceMonitor?.StartOperation("CopyLayout");

            try
            {
                if (string.IsNullOrEmpty(sourceLayoutName) || string.IsNullOrEmpty(newLayoutName))
                    throw new ArgumentException("布局名称不能为空");

                var layoutManager = LayoutManager.Current;

                if (!layoutManager.LayoutExists(sourceLayoutName))
                {
                    _logger?.Warning($"源布局 '{sourceLayoutName}' 不存在");
                    return ObjectId.Null;
                }

                if (layoutManager.LayoutExists(newLayoutName))
                {
                    _logger?.Warning($"目标布局 '{newLayoutName}' 已存在");
                    return ObjectId.Null;
                }

                var sourceLayoutId = layoutManager.GetLayoutId(sourceLayoutName);
                var newLayoutId = layoutManager.CopyLayout(sourceLayoutName, newLayoutName);

                _eventBus?.Publish(new LayoutEvent("LayoutCopied", newLayoutName, newLayoutId, $"Source: {sourceLayoutName}"));
                _logger?.Info($"布局复制成功: {sourceLayoutName} -> {newLayoutName}");

                return newLayoutId;
            }
            catch (Exception ex)
            {
                _logger?.Error($"复制布局失败: {ex.Message}", ex);
                return ObjectId.Null;
            }
        }

        /// <summary>
        /// 获取纸张尺寸
        /// </summary>
        public PaperSize GetPaperSize(string layoutName)
        {
            using var operation = _performanceMonitor?.StartOperation("GetPaperSize");

            try
            {
                if (string.IsNullOrEmpty(layoutName) || !LayoutManager.Current.LayoutExists(layoutName))
                    return null;

                var layoutId = LayoutManager.Current.GetLayoutId(layoutName);
                var database = Application.DocumentManager.MdiActiveDocument.Database;

                using (var trans = database.TransactionManager.StartTransaction())
                {
                    var layout = trans.GetObject(layoutId, OpenMode.ForRead) as Layout;

                    if (layout == null)
                        return null;

                    var paperSize = new PaperSize
                    {
                        Width = layout.PlotPaperSize.X,
                        Height = layout.PlotPaperSize.Y
                    };

                    trans.Commit();
                    return paperSize;
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"获取纸张尺寸失败: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 设置纸张尺寸
        /// </summary>
        public bool SetPaperSize(string layoutName, PaperSize paperSize)
        {
            using var operation = _performanceMonitor?.StartOperation("SetPaperSize");

            try
            {
                if (string.IsNullOrEmpty(layoutName))
                    throw new ArgumentException("布局名称不能为空");

                if (paperSize == null)
                    throw new ArgumentException("纸张尺寸不能为空");

                if (!LayoutManager.Current.LayoutExists(layoutName))
                {
                    _logger?.Warning($"布局 '{layoutName}' 不存在");
                    return false;
                }

                var layoutId = LayoutManager.Current.GetLayoutId(layoutName);
                var database = Application.DocumentManager.MdiActiveDocument.Database;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var layout = transManager.GetObject<Layout>(layoutId, OpenMode.ForWrite);
                    // 直接设置布局的纸张尺寸
                    layout.PlotPaperSize = new Point2d(paperSize.Width, paperSize.Height);
                    
                    // 设置其他相关属性
                    var validator = PlotSettingsValidator.Current;
                    validator.SetPlotType(layout, PlotType.Extents);
                    validator.SetUseStandardScale(layout, true);
                    validator.SetStdScaleType(layout, StdScaleType.ScaleToFit);

                    transManager.Commit();
                }

                _eventBus?.Publish(new LayoutEvent("PaperSizeSet", layoutName, layoutId));
                _logger?.Info($"纸张尺寸设置成功: {layoutName} - {paperSize.Width}x{paperSize.Height}");

                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"设置纸张尺寸失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // 当前实现中没有需要特别释放的资源
            // 但为了接口一致性，提供空实现
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