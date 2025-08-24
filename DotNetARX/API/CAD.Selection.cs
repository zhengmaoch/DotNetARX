using System.Runtime.CompilerServices;

namespace DotNetARX
{
    /// <summary>
    /// CAD 统一API - 选择和查询操作部分
    /// </summary>
    public static partial class CAD
    {
        #region 按类型选择

        /// <summary>
        /// 按类型选择实体 - 高性能查询
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns>实体ID列表</returns>
        public static List<ObjectId> SelectByType<T>() where T : Entity
        {
            EnsureInitialized();

            return PerformanceEngine.Execute($"SelectByType<{typeof(T).Name}>", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var results = new List<ObjectId>();

                    var modelSpace = context.GetObject<BlockTableRecord>(
                        context.Database.GetModelSpaceId(), OpenMode.ForRead);

                    foreach (ObjectId id in modelSpace)
                    {
                        var entity = context.GetObject<Entity>(id, OpenMode.ForRead);
                        if (entity is T)
                        {
                            results.Add(id);
                        }
                    }

                    return results;
                })
            );
        }

        /// <summary>
        /// 获取指定类型的实体对象（而非ID）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns>实体对象列表</returns>
        public static List<T> GetEntitiesByType<T>() where T : Entity
        {
            EnsureInitialized();

            return PerformanceEngine.Execute($"GetEntitiesByType<{typeof(T).Name}>", () =>
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

        #endregion 按类型选择

        #region 按图层选择

        /// <summary>
        /// 选择指定图层上的所有实体
        /// </summary>
        /// <param name="layerName">图层名称</param>
        /// <returns>实体ID列表</returns>
        public static List<ObjectId> SelectByLayer(string layerName)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(layerName))
                return new List<ObjectId>();

            return PerformanceEngine.Execute("SelectByLayer", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var results = new List<ObjectId>();

                    var modelSpace = context.GetObject<BlockTableRecord>(
                        context.Database.GetModelSpaceId(), OpenMode.ForRead);

                    foreach (ObjectId id in modelSpace)
                    {
                        var entity = context.GetObject<Entity>(id, OpenMode.ForRead);
                        if (entity != null && entity.Layer == layerName)
                        {
                            results.Add(id);
                        }
                    }

                    return results;
                })
            );
        }

        /// <summary>
        /// 选择指定图层上的指定类型实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="layerName">图层名称</param>
        /// <returns>实体ID列表</returns>
        public static List<ObjectId> SelectByLayerAndType<T>(string layerName) where T : Entity
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(layerName))
                return new List<ObjectId>();

            return PerformanceEngine.Execute($"SelectByLayerAndType<{typeof(T).Name}>", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var results = new List<ObjectId>();

                    var modelSpace = context.GetObject<BlockTableRecord>(
                        context.Database.GetModelSpaceId(), OpenMode.ForRead);

                    foreach (ObjectId id in modelSpace)
                    {
                        var entity = context.GetObject<Entity>(id, OpenMode.ForRead);
                        if (entity is T && entity.Layer == layerName)
                        {
                            results.Add(id);
                        }
                    }

                    return results;
                })
            );
        }

        #endregion 按图层选择

        #region 智能过滤选择

        /// <summary>
        /// 智能过滤选择 - 支持LINQ表达式
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">过滤条件</param>
        /// <returns>实体ID列表</returns>
        public static List<ObjectId> SelectWhere<T>(Func<T, bool> predicate = null) where T : Entity
        {
            EnsureInitialized();

            return PerformanceEngine.Execute($"SelectWhere<{typeof(T).Name}>", () =>
            {
                var allEntities = SelectByType<T>();

                if (predicate == null)
                    return allEntities;

                return AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var results = new List<ObjectId>();

                    foreach (var id in allEntities)
                    {
                        var entity = context.GetObject<T>(id, OpenMode.ForRead);
                        if (entity != null && predicate(entity))
                        {
                            results.Add(id);
                        }
                    }

                    return results;
                });
            });
        }

        /// <summary>
        /// 简化的智能选择方法
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">过滤条件</param>
        /// <returns>实体ID列表</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<ObjectId> Select<T>(Func<T, bool> predicate = null) where T : Entity
        {
            return SelectWhere(predicate);
        }

        #endregion 智能过滤选择

        #region 按区域选择

        /// <summary>
        /// 选择矩形区域内的实体
        /// </summary>
        /// <param name="corner1">矩形角点1</param>
        /// <param name="corner2">矩形角点2</param>
        /// <param name="crossingMode">是否包含相交的实体</param>
        /// <returns>实体ID列表</returns>
        public static List<ObjectId> SelectByRectangle(Point3d corner1, Point3d corner2, bool crossingMode = true)
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("SelectByRectangle", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var editor = context.Document.Editor;

                    var selectionOptions = new SelectionFilter(null);
                    var selectionMethod = crossingMode ?
                        editor.SelectCrossingWindow(corner1, corner2, selectionOptions) :
                        editor.SelectWindow(corner1, corner2, selectionOptions);

                    if (selectionMethod.Status == PromptStatus.OK)
                    {
                        return selectionMethod.Value.GetObjectIds().ToList();
                    }

                    return new List<ObjectId>();
                })
            );
        }

        /// <summary>
        /// 选择多边形区域内的实体
        /// </summary>
        /// <param name="polygonPoints">多边形顶点</param>
        /// <param name="crossingMode">是否包含相交的实体</param>
        /// <returns>实体ID列表</returns>
        public static List<ObjectId> SelectByPolygon(IEnumerable<Point3d> polygonPoints, bool crossingMode = true)
        {
            EnsureInitialized();

            var points = polygonPoints?.ToArray();
            if (points == null || points.Length < 3)
                return new List<ObjectId>();

            return PerformanceEngine.Execute("SelectByPolygon", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var editor = context.Document.Editor;

                    var selectionOptions = new SelectionFilter(null);
                    var selectionMethod = crossingMode ?
                        editor.SelectCrossingPolygon(points, selectionOptions) :
                        editor.SelectPolygon(points, selectionOptions);

                    if (selectionMethod.Status == PromptStatus.OK)
                    {
                        return selectionMethod.Value.GetObjectIds().ToList();
                    }

                    return new List<ObjectId>();
                })
            );
        }

        #endregion 按区域选择

        #region 选择集操作

        /// <summary>
        /// 获取所有实体ID
        /// </summary>
        /// <returns>所有实体ID列表</returns>
        public static List<ObjectId> SelectAll()
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("SelectAll", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var results = new List<ObjectId>();

                    var modelSpace = context.GetObject<BlockTableRecord>(
                        context.Database.GetModelSpaceId(), OpenMode.ForRead);

                    foreach (ObjectId id in modelSpace)
                    {
                        results.Add(id);
                    }

                    return results;
                })
            );
        }

        /// <summary>
        /// 获取当前选择集
        /// </summary>
        /// <returns>当前选择的实体ID列表</returns>
        public static List<ObjectId> GetCurrentSelection()
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("GetCurrentSelection", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var editor = context.Document.Editor;

                    var selection = editor.SelectImplied();
                    if (selection.Status == PromptStatus.OK)
                    {
                        return selection.Value.GetObjectIds().ToList();
                    }

                    return new List<ObjectId>();
                })
            );
        }

        /// <summary>
        /// 设置当前选择集
        /// </summary>
        /// <param name="entityIds">要选择的实体ID</param>
        /// <returns>操作是否成功</returns>
        public static bool SetCurrentSelection(IEnumerable<ObjectId> entityIds)
        {
            EnsureInitialized();

            var ids = entityIds?.ToArray();
            if (ids == null || ids.Length == 0)
                return false;

            return PerformanceEngine.Execute("SetCurrentSelection", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var editor = context.Document.Editor;

                    editor.SetImpliedSelection(ids);
                    return true;
                })
            );
        }

        /// <summary>
        /// 清除当前选择集
        /// </summary>
        /// <returns>操作是否成功</returns>
        public static bool ClearCurrentSelection()
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("ClearCurrentSelection", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var editor = context.Document.Editor;

                    editor.SetImpliedSelection(new ObjectId[0]);
                    return true;
                })
            );
        }

        #endregion 选择集操作

        #region 高级查询

        /// <summary>
        /// 查找最近的实体
        /// </summary>
        /// <param name="point">参考点</param>
        /// <param name="searchRadius">搜索半径（可选）</param>
        /// <returns>最近的实体ID，如果未找到返回ObjectId.Null</returns>
        public static ObjectId FindNearestEntity(Point3d point, double? searchRadius = null)
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("FindNearestEntity", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var modelSpace = context.GetObject<BlockTableRecord>(
                        context.Database.GetModelSpaceId(), OpenMode.ForRead);

                    ObjectId nearestId = ObjectId.Null;
                    double minDistance = double.MaxValue;

                    foreach (ObjectId id in modelSpace)
                    {
                        var entity = context.GetObject<Entity>(id, OpenMode.ForRead);
                        if (entity == null) continue;

                        try
                        {
                            var closestPoint = entity.GetClosestPointTo(point, false);
                            var distance = point.DistanceTo(closestPoint);

                            if (distance < minDistance &&
                                (!searchRadius.HasValue || distance <= searchRadius.Value))
                            {
                                minDistance = distance;
                                nearestId = id;
                            }
                        }
                        catch
                        {
                            // 某些实体可能不支持GetClosestPointTo方法
                            continue;
                        }
                    }

                    return nearestId;
                })
            );
        }

        /// <summary>
        /// 查找与指定实体相交的实体
        /// </summary>
        /// <param name="entityId">参考实体ID</param>
        /// <returns>相交的实体ID列表</returns>
        public static List<ObjectId> FindIntersectingEntities(ObjectId entityId)
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("FindIntersectingEntities", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var referenceEntity = context.GetObject<Entity>(entityId, OpenMode.ForRead);
                    if (referenceEntity == null)
                        return new List<ObjectId>();

                    var results = new List<ObjectId>();
                    var modelSpace = context.GetObject<BlockTableRecord>(
                        context.Database.GetModelSpaceId(), OpenMode.ForRead);

                    foreach (ObjectId id in modelSpace)
                    {
                        if (id == entityId) continue; // 跳过自身

                        var entity = context.GetObject<Entity>(id, OpenMode.ForRead);
                        if (entity == null) continue;

                        try
                        {
                            var intersectionPoints = new Point3dCollection();
                            referenceEntity.IntersectWith(entity, Intersect.OnBothOperands,
                                intersectionPoints, IntPtr.Zero, IntPtr.Zero);

                            if (intersectionPoints.Count > 0)
                            {
                                results.Add(id);
                            }
                        }
                        catch
                        {
                            // 某些实体可能不支持IntersectWith方法
                            continue;
                        }
                    }

                    return results;
                })
            );
        }

        /// <summary>
        /// 统计各类型实体的数量
        /// </summary>
        /// <returns>实体类型统计字典</returns>
        public static Dictionary<string, int> GetEntityTypeStatistics()
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("GetEntityTypeStatistics", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var statistics = new Dictionary<string, int>();

                    var modelSpace = context.GetObject<BlockTableRecord>(
                        context.Database.GetModelSpaceId(), OpenMode.ForRead);

                    foreach (ObjectId id in modelSpace)
                    {
                        var entity = context.GetObject<Entity>(id, OpenMode.ForRead);
                        if (entity == null) continue;

                        var entityType = entity.GetType().Name;
                        statistics[entityType] = statistics.GetValueOrDefault(entityType, 0) + 1;
                    }

                    return statistics;
                })
            );
        }

        #endregion 高级查询
    }
}