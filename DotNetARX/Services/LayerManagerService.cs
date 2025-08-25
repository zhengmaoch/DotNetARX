namespace DotNetARX.Services
{
    /// <summary>
    /// 图层管理服务实现
    /// </summary>
    public class LayerManagerService : ILayerManager
    {
        private readonly ILogger _logger;
        private readonly IPerformanceMonitor _performanceMonitor;

        public LayerManagerService(ILogger logger = null, IPerformanceMonitor performanceMonitor = null)
        {
            _logger = logger ?? LogManager.GetLogger(typeof(LayerManagerService));
            _performanceMonitor = performanceMonitor ?? GlobalPerformanceMonitor.Instance;
        }

        /// <summary>
        /// 创建图层（集成性能监控和事件）
        /// </summary>
        public ObjectId CreateLayer(string layerName, short colorIndex = 7)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                using (_performanceMonitor.StartTimer("CreateLayer", "LayerOperations"))
                {
                    var database = HostApplicationServices.WorkingDatabase;
                    if (database == null)
                    {
                        CADExceptionHandler.ThrowCADException("创建图层", "没有活动的数据库");
                    }

                    ObjectId layerId;

                    using (var transManager = TransactionManagerFactory.Create(database))
                    {
                        // 打开层表
                        var layerTable = transManager.GetObject<LayerTable>(database.LayerTableId, OpenMode.ForRead);

                        if (layerTable.Has(layerName))
                        {
                            // 图层已存在，返回现有图层ID
                            layerId = layerTable[layerName];
                            _logger.Info($"图层已存在: {layerName}");
                            return layerId;
                        }

                        // 创建新图层
                        var layerRecord = new LayerTableRecord
                        {
                            Name = layerName,
                            Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, colorIndex)
                        };

                        layerTable.UpgradeOpen();
                        layerId = layerTable.Add(layerRecord);
                        transManager.AddNewlyCreatedDBObject(layerRecord, true);
                        transManager.Commit();

                        _logger.Info($"成功创建图层: {layerName} (颜色索引: {colorIndex})");

                        // 触发图层创建事件
                        CADEventManager.Publisher.Publish(new LayerEventArgs(layerName, "created", "LayerManagerService"));

                        // 记录性能指标
                        _performanceMonitor.IncrementCounter("LayersCreated", "LayerOperations");
                    }

                    return layerId;
                }
            }, ObjectId.Null);
        }

        /// <summary>
        /// 设置当前图层
        /// </summary>
        public bool SetCurrentLayer(string layerName)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                var database = HostApplicationServices.WorkingDatabase;
                if (database == null)
                {
                    CADExceptionHandler.ThrowCADException("设置当前图层", "没有活动的数据库");
                }

                using (var transManager = TransactionManagerFactory.Create(database))
                {
                    var layerTable = transManager.GetObject<LayerTable>(database.LayerTableId, OpenMode.ForRead);

                    if (!layerTable.Has(layerName))
                    {
                        _logger.Warning($"图层不存在: {layerName}");
                        return false;
                    }

                    var layerId = layerTable[layerName];
                    if (database.Clayer == layerId)
                    {
                        _logger.Info($"图层 {layerName} 已经是当前图层");
                        return true;
                    }

                    database.Clayer = layerId;
                    transManager.Commit();

                    _logger.Info($"成功设置当前图层: {layerName}");

                    // 触发图层变更事件
                    CADEventManager.Publisher.PublishAsync(new LayerEventArgs(layerName, "changed", "LayerManagerService"));

                    return true;
                }
            }, false);
        }

        /// <summary>
        /// 删除图层
        /// </summary>
        public bool DeleteLayer(string layerName)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                // 检查是否为系统保留图层
                if (layerName == "0" || layerName == "Defpoints")
                {
                    _logger.Warning($"无法删除系统保留图层: {layerName}");
                    return false;
                }

                var database = HostApplicationServices.WorkingDatabase;
                if (database == null)
                {
                    CADExceptionHandler.ThrowCADException("删除图层", "没有活动的数据库");
                }

                using (var transManager = TransactionManagerFactory.Create(database))
                {
                    var layerTable = transManager.GetObject<LayerTable>(database.LayerTableId, OpenMode.ForRead);

                    if (!layerTable.Has(layerName))
                    {
                        _logger.Warning($"图层不存在: {layerName}");
                        return false;
                    }

                    var layerId = layerTable[layerName];

                    // 检查是否为当前图层
                    if (layerId == database.Clayer)
                    {
                        _logger.Warning($"无法删除当前图层: {layerName}");
                        return false;
                    }

                    var layerRecord = transManager.GetObject<LayerTableRecord>(layerId, OpenMode.ForRead);

                    // 检查图层是否被使用
                    layerTable.GenerateUsageData();
                    if (layerRecord.IsUsed)
                    {
                        _logger.Warning($"图层正在使用中，无法删除: {layerName}");
                        return false;
                    }

                    layerRecord.UpgradeOpen();
                    layerRecord.Erase(true);
                    transManager.Commit();

                    _logger.Info($"成功删除图层: {layerName}");

                    // 触发图层删除事件
                    CADEventManager.Publisher.PublishAsync(new LayerEventArgs(layerName, "deleted", "LayerManagerService"));

                    return true;
                }
            }, false);
        }

        /// <summary>
        /// 获取所有图层
        /// </summary>
        public IEnumerable<LayerTableRecord> GetAllLayers()
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                var layers = new List<LayerTableRecord>();
                var database = HostApplicationServices.WorkingDatabase;

                if (database == null)
                {
                    _logger.Warning("没有活动的数据库");
                    return layers;
                }

                using (var transManager = TransactionManagerFactory.Create(database))
                {
                    var layerTable = transManager.GetObject<LayerTable>(database.LayerTableId, OpenMode.ForRead);

                    foreach (ObjectId layerId in layerTable)
                    {
                        var layerRecord = transManager.GetObject<LayerTableRecord>(layerId, OpenMode.ForRead);
                        layers.Add(layerRecord);
                    }
                }

                _logger.Debug($"获取到 {layers.Count} 个图层");
                return layers;
            }, new List<LayerTableRecord>());
        }

        /// <summary>
        /// 获取图层名称列表
        /// </summary>
        public IEnumerable<string> GetLayerNames()
        {
            return GetAllLayers().Select(layer => layer.Name);
        }

        /// <summary>
        /// 检查图层是否存在
        /// </summary>
        public bool LayerExists(string layerName)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                var database = HostApplicationServices.WorkingDatabase;
                if (database == null) return false;

                using (var transManager = TransactionManagerFactory.Create(database))
                {
                    var layerTable = transManager.GetObject<LayerTable>(database.LayerTableId, OpenMode.ForRead);
                    return layerTable.Has(layerName);
                }
            }, false);
        }

        /// <summary>
        /// 设置图层属性
        /// </summary>
        public bool SetLayerProperties(string layerName, short? colorIndex = null, bool? isLocked = null, bool? isFrozen = null)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                var database = HostApplicationServices.WorkingDatabase;
                if (database == null)
                {
                    CADExceptionHandler.ThrowCADException("设置图层属性", "没有活动的数据库");
                }

                using (var transManager = TransactionManagerFactory.Create(database))
                {
                    var layerTable = transManager.GetObject<LayerTable>(database.LayerTableId, OpenMode.ForRead);

                    if (!layerTable.Has(layerName))
                    {
                        _logger.Warning($"图层不存在: {layerName}");
                        return false;
                    }

                    var layerId = layerTable[layerName];
                    var layerRecord = transManager.GetObject<LayerTableRecord>(layerId, OpenMode.ForWrite);

                    bool modified = false;

                    if (colorIndex.HasValue)
                    {
                        layerRecord.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(
                            Autodesk.AutoCAD.Colors.ColorMethod.ByAci, colorIndex.Value);
                        modified = true;
                    }

                    if (isLocked.HasValue)
                    {
                        layerRecord.IsLocked = isLocked.Value;
                        modified = true;
                    }

                    if (isFrozen.HasValue)
                    {
                        layerRecord.IsFrozen = isFrozen.Value;
                        modified = true;
                    }

                    if (modified)
                    {
                        transManager.Commit();
                        _logger.Info($"成功更新图层属性: {layerName}");

                        // 触发图层修改事件
                        CADEventManager.Publisher.PublishAsync(new LayerEventArgs(layerName, "modified", "LayerManagerService"));
                    }

                    return modified;
                }
            }, false);
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
}

