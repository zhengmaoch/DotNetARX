namespace DotNetARX.Extensions
{
    /// <summary>
    /// Database 扩展方法
    /// 提供便捷的数据库操作方法
    /// </summary>
    public static class DatabaseExtensions
    {
        #region 表格访问

        /// <summary>
        /// 获取图层表
        /// </summary>
        /// <param name="database">数据库对象</param>
        /// <param name="mode">打开模式</param>
        /// <returns>图层表</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LayerTable GetLayerTable(this Database database, OpenMode mode = OpenMode.ForRead)
        {
            return AutoCADContext.ExecuteSafely(() =>
            {
                var context = AutoCADContext.Current;
                return context.GetObject<LayerTable>(database.LayerTableId, mode);
            });
        }

        /// <summary>
        /// 获取块表
        /// </summary>
        /// <param name="database">数据库对象</param>
        /// <param name="mode">打开模式</param>
        /// <returns>块表</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BlockTable GetBlockTable(this Database database, OpenMode mode = OpenMode.ForRead)
        {
            return AutoCADContext.ExecuteSafely(() =>
            {
                var context = AutoCADContext.Current;
                return context.GetObject<BlockTable>(database.BlockTableId, mode);
            });
        }

        /// <summary>
        /// 获取线型表
        /// </summary>
        /// <param name="database">数据库对象</param>
        /// <param name="mode">打开模式</param>
        /// <returns>线型表</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LinetypeTable GetLinetypeTable(this Database database, OpenMode mode = OpenMode.ForRead)
        {
            return AutoCADContext.ExecuteSafely(() =>
            {
                var context = AutoCADContext.Current;
                return context.GetObject<LinetypeTable>(database.LinetypeTableId, mode);
            });
        }

        /// <summary>
        /// 获取文字样式表
        /// </summary>
        /// <param name="database">数据库对象</param>
        /// <param name="mode">打开模式</param>
        /// <returns>文字样式表</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TextStyleTable GetTextStyleTable(this Database database, OpenMode mode = OpenMode.ForRead)
        {
            return AutoCADContext.ExecuteSafely(() =>
            {
                var context = AutoCADContext.Current;
                return context.GetObject<TextStyleTable>(database.TextStyleTableId, mode);
            });
        }

        /// <summary>
        /// 获取标注样式表
        /// </summary>
        /// <param name="database">数据库对象</param>
        /// <param name="mode">打开模式</param>
        /// <returns>标注样式表</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DimStyleTable GetDimStyleTable(this Database database, OpenMode mode = OpenMode.ForRead)
        {
            return AutoCADContext.ExecuteSafely(() =>
            {
                var context = AutoCADContext.Current;
                return context.GetObject<DimStyleTable>(database.DimStyleTableId, mode);
            });
        }

        #endregion 表格访问

        #region 空间访问

        /// <summary>
        /// 获取模型空间
        /// </summary>
        /// <param name="database">数据库对象</param>
        /// <param name="mode">打开模式</param>
        /// <returns>模型空间块表记录</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BlockTableRecord GetModelSpace(this Database database, OpenMode mode = OpenMode.ForRead)
        {
            return AutoCADContext.ExecuteSafely(() =>
            {
                var context = AutoCADContext.Current;
                return context.GetObject<BlockTableRecord>(database.GetModelSpaceId(), mode);
            });
        }

        /// <summary>
        /// 获取图纸空间
        /// </summary>
        /// <param name="database">数据库对象</param>
        /// <param name="mode">打开模式</param>
        /// <returns>图纸空间块表记录</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BlockTableRecord GetPaperSpace(this Database database, OpenMode mode = OpenMode.ForRead)
        {
            return AutoCADContext.ExecuteSafely(() =>
            {
                var context = AutoCADContext.Current;
                return context.GetObject<BlockTableRecord>(database.GetPaperSpaceId(), mode);
            });
        }

        /// <summary>
        /// 获取当前空间
        /// </summary>
        /// <param name="database">数据库对象</param>
        /// <param name="mode">打开模式</param>
        /// <returns>当前空间块表记录</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BlockTableRecord GetCurrentSpace(this Database database, OpenMode mode = OpenMode.ForRead)
        {
            return AutoCADContext.ExecuteSafely(() =>
            {
                var context = AutoCADContext.Current;
                return context.GetObject<BlockTableRecord>(database.GetCurrentSpaceId(), mode);
            });
        }

        #endregion 空间访问

        #region 实体操作

        /// <summary>
        /// 添加实体到模型空间
        /// </summary>
        /// <param name="database">数据库对象</param>
        /// <param name="entity">要添加的实体</param>
        /// <returns>实体ID</returns>
        public static ObjectId AddEntityToModelSpace(this Database database, Entity entity)
        {
            return AutoCADContext.ExecuteSafely(() =>
            {
                var context = AutoCADContext.Current;
                var modelSpace = database.GetModelSpace(OpenMode.ForWrite);

                var entityId = modelSpace.AppendEntity(entity);
                context.Transaction.AddNewlyCreatedDBObject(entity, true);

                return entityId;
            });
        }

        /// <summary>
        /// 批量添加实体到模型空间
        /// </summary>
        /// <param name="database">数据库对象</param>
        /// <param name="entities">要添加的实体集合</param>
        /// <returns>实体ID列表</returns>
        public static List<ObjectId> AddEntitiesToModelSpace(this Database database, IEnumerable<Entity> entities)
        {
            var entityList = entities?.ToList();
            if (entityList == null || !entityList.Any())
                return new List<ObjectId>();

            List<ObjectId> entityIds = null;
            AutoCADContext.ExecuteBatch(context =>
            {
                var modelSpace = database.GetModelSpace(OpenMode.ForWrite);
                entityIds = new List<ObjectId>();

                foreach (var entity in entityList)
                {
                    var entityId = modelSpace.AppendEntity(entity);
                    context.Transaction.AddNewlyCreatedDBObject(entity, true);
                    entityIds.Add(entityId);
                }
            });

            return entityIds ?? new List<ObjectId>();
        }

        /// <summary>
        /// 添加实体到指定块
        /// </summary>
        /// <param name="database">数据库对象</param>
        /// <param name="entity">要添加的实体</param>
        /// <param name="blockName">块名称</param>
        /// <returns>实体ID</returns>
        public static ObjectId AddEntityToBlock(this Database database, Entity entity, string blockName)
        {
            return AutoCADContext.ExecuteSafely(() =>
            {
                var context = AutoCADContext.Current;
                var blockTable = database.GetBlockTable();

                if (!blockTable.Has(blockName))
                    return ObjectId.Null;

                var blockRecord = context.GetObject<BlockTableRecord>(blockTable[blockName], OpenMode.ForWrite);
                var entityId = blockRecord.AppendEntity(entity);
                context.Transaction.AddNewlyCreatedDBObject(entity, true);

                return entityId;
            });
        }

        #endregion 实体操作

        #region 图层操作

        /// <summary>
        /// 创建图层
        /// </summary>
        /// <param name="database">数据库对象</param>
        /// <param name="layerName">图层名称</param>
        /// <param name="colorIndex">颜色索引</param>
        /// <param name="lineWeight">线宽</param>
        /// <returns>图层ID</returns>
        public static ObjectId CreateLayer(this Database database, string layerName, short colorIndex = 7, LineWeight lineWeight = LineWeight.ByLayer)
        {
            if (string.IsNullOrWhiteSpace(layerName))
                throw new ArgumentException("图层名称不能为空", nameof(layerName));

            return AutoCADContext.ExecuteSafely(() =>
            {
                var context = AutoCADContext.Current;
                var layerTable = database.GetLayerTable(OpenMode.ForRead);

                if (layerTable.Has(layerName))
                    return layerTable[layerName];

                layerTable.UpgradeOpen();
                var layerRecord = new LayerTableRecord
                {
                    Name = layerName,
                    Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex),
                    LineWeight = lineWeight
                };

                var layerId = layerTable.Add(layerRecord);
                context.Transaction.AddNewlyCreatedDBObject(layerRecord, true);

                return layerId;
            });
        }

        /// <summary>
        /// 检查图层是否存在
        /// </summary>
        /// <param name="database">数据库对象</param>
        /// <param name="layerName">图层名称</param>
        /// <returns>图层是否存在</returns>
        public static bool HasLayer(this Database database, string layerName)
        {
            if (string.IsNullOrWhiteSpace(layerName))
                return false;

            return AutoCADContext.ExecuteSafely(() =>
            {
                var layerTable = database.GetLayerTable();
                return layerTable.Has(layerName);
            });
        }

        /// <summary>
        /// 获取所有图层名称
        /// </summary>
        /// <param name="database">数据库对象</param>
        /// <returns>图层名称列表</returns>
        public static List<string> GetAllLayerNames(this Database database)
        {
            return AutoCADContext.ExecuteSafely(() =>
            {
                var context = AutoCADContext.Current;
                var layerTable = database.GetLayerTable();
                var layerNames = new List<string>();

                foreach (ObjectId layerId in layerTable)
                {
                    var layerRecord = context.GetObject<LayerTableRecord>(layerId, OpenMode.ForRead);
                    layerNames.Add(layerRecord.Name);
                }

                return layerNames;
            });
        }

        #endregion 图层操作

        #region 块操作

        /// <summary>
        /// 创建块定义
        /// </summary>
        /// <param name="database">数据库对象</param>
        /// <param name="blockName">块名称</param>
        /// <param name="basePoint">插入基点</param>
        /// <param name="entities">块内实体</param>
        /// <returns>块表记录ID</returns>
        public static ObjectId CreateBlockDefinition(this Database database, string blockName, Point3d basePoint, IEnumerable<Entity> entities)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                throw new ArgumentException("块名称不能为空", nameof(blockName));

            var entityList = entities?.ToList();
            if (entityList == null || !entityList.Any())
                throw new ArgumentException("块内必须包含实体", nameof(entities));

            return AutoCADContext.ExecuteSafely(() =>
            {
                var context = AutoCADContext.Current;
                var blockTable = database.GetBlockTable(OpenMode.ForWrite);

                if (blockTable.Has(blockName))
                    return blockTable[blockName];

                var blockRecord = new BlockTableRecord
                {
                    Name = blockName,
                    Origin = basePoint
                };

                var blockId = blockTable.Add(blockRecord);
                context.Transaction.AddNewlyCreatedDBObject(blockRecord, true);

                foreach (var entity in entityList)
                {
                    blockRecord.AppendEntity(entity);
                    context.Transaction.AddNewlyCreatedDBObject(entity, true);
                }

                return blockId;
            });
        }

        /// <summary>
        /// 插入块引用
        /// </summary>
        /// <param name="database">数据库对象</param>
        /// <param name="blockName">块名称</param>
        /// <param name="insertionPoint">插入点</param>
        /// <param name="scaleX">X方向缩放</param>
        /// <param name="scaleY">Y方向缩放</param>
        /// <param name="scaleZ">Z方向缩放</param>
        /// <param name="rotation">旋转角度</param>
        /// <returns>块引用ID</returns>
        public static ObjectId InsertBlock(this Database database, string blockName, Point3d insertionPoint,
            double scaleX = 1.0, double scaleY = 1.0, double scaleZ = 1.0, double rotation = 0.0)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                throw new ArgumentException("块名称不能为空", nameof(blockName));

            return AutoCADContext.ExecuteSafely(() =>
            {
                var context = AutoCADContext.Current;
                var blockTable = database.GetBlockTable();

                if (!blockTable.Has(blockName))
                    return ObjectId.Null;

                var blockRef = new BlockReference(insertionPoint, blockTable[blockName])
                {
                    ScaleFactors = new Scale3d(scaleX, scaleY, scaleZ),
                    Rotation = rotation
                };

                return database.AddEntityToModelSpace(blockRef);
            });
        }

        /// <summary>
        /// 检查块是否存在
        /// </summary>
        /// <param name="database">数据库对象</param>
        /// <param name="blockName">块名称</param>
        /// <returns>块是否存在</returns>
        public static bool HasBlock(this Database database, string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                return false;

            return AutoCADContext.ExecuteSafely(() =>
            {
                var blockTable = database.GetBlockTable();
                return blockTable.Has(blockName);
            });
        }

        #endregion 块操作

        #region 统计信息

        /// <summary>
        /// 获取数据库统计信息
        /// </summary>
        /// <param name="database">数据库对象</param>
        /// <returns>数据库统计信息</returns>
        public static ArxDatabaseStatistics GetStatistics(this Database database)
        {
            return AutoCADContext.ExecuteSafely(() =>
            {
                var context = AutoCADContext.Current;
                var statistics = new ArxDatabaseStatistics();

                // 统计图层
                var layerTable = database.GetLayerTable();
                statistics.LayerCount = layerTable.Cast<ObjectId>().Count();

                // 统计块
                var blockTable = database.GetBlockTable();
                statistics.BlockDefinitionCount = blockTable.Cast<ObjectId>().Count();

                // 统计实体
                var modelSpace = database.GetModelSpace();
                statistics.EntityCount = modelSpace.Cast<ObjectId>().Count();

                // 统计线型
                var linetypeTable = database.GetLinetypeTable();
                statistics.LinetypeCount = linetypeTable.Cast<ObjectId>().Count();

                // 统计文字样式
                var textStyleTable = database.GetTextStyleTable();
                statistics.TextStyleCount = textStyleTable.Cast<ObjectId>().Count();

                // 统计标注样式
                var dimStyleTable = database.GetDimStyleTable();
                statistics.DimStyleCount = dimStyleTable.Cast<ObjectId>().Count();

                return statistics;
            });
        }

        /// <summary>
        /// 获取实体类型统计
        /// </summary>
        /// <param name="database">数据库对象</param>
        /// <returns>实体类型统计字典</returns>
        public static Dictionary<string, int> GetEntityTypeStatistics(this Database database)
        {
            return AutoCADContext.ExecuteSafely(() =>
            {
                var context = AutoCADContext.Current;
                var statistics = new Dictionary<string, int>();
                var modelSpace = database.GetModelSpace();

                foreach (ObjectId entityId in modelSpace)
                {
                    var entity = context.GetObject<Entity>(entityId, OpenMode.ForRead);
                    if (entity == null) continue;

                    var entityType = entity.GetType().Name;
                    statistics[entityType] = statistics.GetValueOrDefault(entityType, 0) + 1;
                }

                return statistics;
            });
        }

        #endregion 统计信息

        #region 清理操作

        /// <summary>
        /// 清理数据库（删除未使用的命名对象）
        /// </summary>
        /// <param name="database">数据库对象</param>
        /// <returns>清理的对象数量</returns>
        public static int PurgeDatabase(this Database database)
        {
            return AutoCADContext.ExecuteSafely(() =>
            {
                var purgeIds = new ObjectIdCollection();
                database.Purge(purgeIds);

                foreach (ObjectId id in purgeIds)
                {
                    var obj = id.GetObject(OpenMode.ForWrite);
                    obj.Erase();
                }

                return purgeIds.Count;
            });
        }

        #endregion 清理操作

        #region Entity扩展方法

        /// <summary>
        /// 获取实体上距离指定点最近的点
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="point">参考点</param>
        /// <returns>最近点，如果无法计算则返回null</returns>
        public static Point3d? GetClosestPointTo(this Entity entity, Point3d point)
        {
            if (entity == null) return null;

            // 对于曲线类型，使用原生方法
            if (entity is Curve curve)
            {
                try
                {
                    return curve.GetClosestPointTo(point, Vector3d.ZAxis, false);
                }
                catch
                {
                    return null;
                }
            }

            // 对于其他实体类型，使用几何外框估算
            try
            {
                var extents = entity.GeometricExtents;
                var center = new Point3d(
                    (extents.MinPoint.X + extents.MaxPoint.X) / 2,
                    (extents.MinPoint.Y + extents.MaxPoint.Y) / 2,
                    (extents.MinPoint.Z + extents.MaxPoint.Z) / 2);

                // 返回外框上距离参考点最近的点
                var corners = new[]
                {
                    extents.MinPoint,
                    extents.MaxPoint,
                    new Point3d(extents.MinPoint.X, extents.MaxPoint.Y, extents.MinPoint.Z),
                    new Point3d(extents.MaxPoint.X, extents.MinPoint.Y, extents.MinPoint.Z),
                    new Point3d(extents.MinPoint.X, extents.MinPoint.Y, extents.MaxPoint.Z),
                    new Point3d(extents.MaxPoint.X, extents.MaxPoint.Y, extents.MinPoint.Z),
                    new Point3d(extents.MinPoint.X, extents.MaxPoint.Y, extents.MaxPoint.Z),
                    new Point3d(extents.MaxPoint.X, extents.MinPoint.Y, extents.MaxPoint.Z),
                    center
                };

                return corners.OrderBy(p => p.DistanceTo(point)).First();
            }
            catch
            {
                return null;
            }
        }

        #endregion Entity扩展方法

        #region 集合扩展方法

        /// <summary>
        /// 获取字典中的值，如果不存在则返回默认值
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>值或默认值</returns>
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            if (dictionary == null) return defaultValue;
            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }

        #endregion 集合扩展方法
    }

    #region 辅助数据结构

    /// <summary>
    /// DotNetARX 数据库统计信息
    /// 避免与AutoCAD原生类型冲突
    /// </summary>
    public class ArxDatabaseStatistics
    {
        public int LayerCount { get; set; }
        public int BlockDefinitionCount { get; set; }
        public int EntityCount { get; set; }
        public int LinetypeCount { get; set; }
        public int TextStyleCount { get; set; }
        public int DimStyleCount { get; set; }

        public override string ToString()
        {
            return $"图层: {LayerCount}, 块: {BlockDefinitionCount}, 实体: {EntityCount}, " +
                   $"线型: {LinetypeCount}, 文字样式: {TextStyleCount}, 标注样式: {DimStyleCount}";
        }
    }

    #endregion 辅助数据结构
}