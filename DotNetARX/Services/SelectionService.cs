namespace DotNetARX.Services
{
    /// <summary>
    /// 选择操作服务实现
    /// </summary>
    public class SelectionService : ISelectionService
    {
        private readonly ILogger _logger;
        private readonly IPerformanceMonitor _performanceMonitor;

        public SelectionService(ILogger logger = null, IPerformanceMonitor performanceMonitor = null)
        {
            _logger = logger ?? LogManager.GetLogger(typeof(SelectionService));
            _performanceMonitor = performanceMonitor ?? GlobalPerformanceMonitor.Instance;
        }

        /// <summary>
        /// 按类型选择实体
        /// </summary>
        public List<T> SelectByType<T>() where T : Entity
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                using (_performanceMonitor.StartTimer("SelectByType", "SelectionOperations"))
                {
                    var editor = GetActiveEditor();
                    var dxfName = RXObject.GetClass(typeof(T)).DxfName;

                    var filter = new SelectionFilter(new[]
                    {
                        new TypedValue((int)DxfCode.Start, dxfName)
                    });

                    var result = editor.GetSelection(filter);
                    var entities = new List<T>();

                    if (result.Status == PromptStatus.OK)
                    {
                        var database = HostApplicationServices.WorkingDatabase;
                        using (var transManager = TransactionManagerFactory.Create(database))
                        {
                            foreach (var objectId in result.Value.GetObjectIds())
                            {
                                if (transManager.TryGetObject<T>(objectId, out var entity))
                                {
                                    entities.Add(entity);
                                }
                            }
                        }

                        _logger.Info($"按类型选择完成 - 类型: {typeof(T).Name}, 数量: {entities.Count}");
                        _performanceMonitor.RecordMetric("SelectedEntities", entities.Count, MetricType.OperationCount);
                    }
                    else
                    {
                        _logger.Info($"选择操作取消或失败 - 状态: {result.Status}");
                    }

                    return entities;
                }
            }, new List<T>());
        }

        /// <summary>
        /// 在窗口内选择实体
        /// </summary>
        public List<T> SelectInWindow<T>(Point3d pt1, Point3d pt2) where T : Entity
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                using (_performanceMonitor.StartTimer("SelectInWindow", "SelectionOperations"))
                {
                    var editor = GetActiveEditor();
                    var dxfName = RXObject.GetClass(typeof(T)).DxfName;

                    var filter = new SelectionFilter(new[]
                    {
                        new TypedValue((int)DxfCode.Start, dxfName)
                    });

                    var result = editor.SelectWindow(pt1, pt2, filter);
                    var entities = ProcessSelectionResult<T>(result, "窗口选择");

                    _logger.Info($"窗口选择完成 - 类型: {typeof(T).Name}, 数量: {entities.Count}");
                    return entities;
                }
            }, new List<T>());
        }

        /// <summary>
        /// 交叉窗口选择实体
        /// </summary>
        public List<T> SelectCrossingWindow<T>(Point3d pt1, Point3d pt2) where T : Entity
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                using (_performanceMonitor.StartTimer("SelectCrossingWindow", "SelectionOperations"))
                {
                    var editor = GetActiveEditor();
                    var dxfName = RXObject.GetClass(typeof(T)).DxfName;

                    var filter = new SelectionFilter(new[]
                    {
                        new TypedValue((int)DxfCode.Start, dxfName)
                    });

                    var result = editor.SelectCrossingWindow(pt1, pt2, filter);
                    var entities = ProcessSelectionResult<T>(result, "交叉窗口选择");

                    _logger.Info($"交叉窗口选择完成 - 类型: {typeof(T).Name}, 数量: {entities.Count}");
                    return entities;
                }
            }, new List<T>());
        }

        /// <summary>
        /// 通过过滤器选择实体
        /// </summary>
        public List<T> SelectByFilter<T>(SelectionFilter filter) where T : Entity
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                using (_performanceMonitor.StartTimer("SelectByFilter", "SelectionOperations"))
                {
                    var editor = GetActiveEditor();
                    var result = editor.GetSelection(filter);
                    var entities = ProcessSelectionResult<T>(result, "过滤器选择");

                    _logger.Info($"过滤器选择完成 - 类型: {typeof(T).Name}, 数量: {entities.Count}");
                    return entities;
                }
            }, new List<T>());
        }

        /// <summary>
        /// 选择指定点处的实体
        /// </summary>
        public List<T> SelectAtPoint<T>(Point3d point) where T : Entity
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                using (_performanceMonitor.StartTimer("SelectAtPoint", "SelectionOperations"))
                {
                    var editor = GetActiveEditor();
                    var dxfName = RXObject.GetClass(typeof(T)).DxfName;

                    var filter = new SelectionFilter(new[]
                    {
                        new TypedValue((int)DxfCode.Start, dxfName)
                    });

                    // 使用很小的窗口来模拟点选择
                    var tolerance = 0.001;
                    var pt1 = new Point3d(point.X - tolerance, point.Y - tolerance, point.Z);
                    var pt2 = new Point3d(point.X + tolerance, point.Y + tolerance, point.Z);

                    var result = editor.SelectCrossingWindow(pt1, pt2, filter);
                    var entities = ProcessSelectionResult<T>(result, "点选择");

                    _logger.Info($"点选择完成 - 类型: {typeof(T).Name}, 数量: {entities.Count}");
                    return entities;
                }
            }, new List<T>());
        }

        /// <summary>
        /// 获取当前选择集
        /// </summary>
        public ObjectIdCollection GetCurrentSelection()
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                using (_performanceMonitor.StartTimer("GetCurrentSelection", "SelectionOperations"))
                {
                    var editor = GetActiveEditor();
                    var result = editor.SelectImplied();

                    if (result.Status == PromptStatus.OK)
                    {
                        var objectIds = new ObjectIdCollection();
                        foreach (var id in result.Value.GetObjectIds())
                        {
                            objectIds.Add(id);
                        }

                        _logger.Info($"获取当前选择集完成 - 数量: {objectIds.Count}");
                        return objectIds;
                    }
                    else
                    {
                        _logger.Info("当前没有选择集");
                        return new ObjectIdCollection();
                    }
                }
            }, new ObjectIdCollection());
        }

        /// <summary>
        /// 处理选择结果
        /// </summary>
        private List<T> ProcessSelectionResult<T>(PromptSelectionResult result, string operationType) where T : Entity
        {
            var entities = new List<T>();

            if (result.Status == PromptStatus.OK)
            {
                var database = HostApplicationServices.WorkingDatabase;
                using (var transManager = TransactionManagerFactory.Create(database))
                {
                    foreach (var objectId in result.Value.GetObjectIds())
                    {
                        if (transManager.TryGetObject<T>(objectId, out var entity))
                        {
                            entities.Add(entity);

                            // 触发实体选择事件
                            CADEventManager.Publisher.PublishAsync(new EntityEventArgs(
                                objectId, "selected", typeof(T).Name, "SelectionService"));
                        }
                    }
                }

                _performanceMonitor.RecordMetric($"{operationType}_Count", entities.Count, MetricType.OperationCount);
            }
            else if (result.Status == PromptStatus.Cancel)
            {
                _logger.Info($"{operationType}被用户取消");
            }
            else
            {
                _logger.Warning($"{operationType}失败 - 状态: {result.Status}");
            }

            return entities;
        }

        /// <summary>
        /// 获取活动编辑器
        /// </summary>
        private Editor GetActiveEditor()
        {
            var document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                CADExceptionHandler.ThrowCADException("获取编辑器", "没有活动文档");
            }

            return document.Editor;
        }

        /// <summary>
        /// 选择所有指定类型的实体
        /// </summary>
        public List<T> SelectAllByType<T>() where T : Entity
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                using (_performanceMonitor.StartTimer("SelectAllByType", "SelectionOperations"))
                {
                    var editor = GetActiveEditor();
                    var dxfName = RXObject.GetClass(typeof(T)).DxfName;

                    var filter = new SelectionFilter(new[]
                    {
                        new TypedValue((int)DxfCode.Start, dxfName)
                    });

                    var result = editor.SelectAll(filter);
                    var entities = ProcessSelectionResult<T>(result, "全选");

                    _logger.Info($"全选完成 - 类型: {typeof(T).Name}, 数量: {entities.Count}");
                    return entities;
                }
            }, new List<T>());
        }

        /// <summary>
        /// 多边形选择实体
        /// </summary>
        public List<T> SelectByPolygon<T>(Point3dCollection polygon, bool crossingMode = false) where T : Entity
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                using (_performanceMonitor.StartTimer("SelectByPolygon", "SelectionOperations"))
                {
                    var editor = GetActiveEditor();
                    var dxfName = RXObject.GetClass(typeof(T)).DxfName;

                    var filter = new SelectionFilter(new[]
                    {
                        new TypedValue((int)DxfCode.Start, dxfName)
                    });

                    PromptSelectionResult result;
                    if (crossingMode)
                    {
                        result = editor.SelectCrossingPolygon(polygon, filter);
                    }
                    else
                    {
                        result = editor.SelectWindowPolygon(polygon, filter);
                    }

                    var entities = ProcessSelectionResult<T>(result, "多边形选择");
                    _logger.Info($"多边形选择完成 - 类型: {typeof(T).Name}, 数量: {entities.Count}");
                    return entities;
                }
            }, new List<T>());
        }
    }
}