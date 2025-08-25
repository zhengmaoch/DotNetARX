namespace DotNetARX
{
    /// <summary>
    /// CAD 统一API - 图层操作部分
    /// </summary>
    public static partial class CAD
    {
        #region 图层缓存

        private static readonly Lazy<ISmartCache<string, ObjectId>> _layerCache =
            new(() => SmartCacheManager.GetCache<string, ObjectId>("DotNetARXLayerCache", 500, TimeSpan.FromHours(1)));

        #endregion 图层缓存

        #region 图层创建和管理

        /// <summary>
        /// 创建图层 - 智能缓存和去重
        /// </summary>
        /// <param name="layerName">图层名称</param>
        /// <param name="colorIndex">颜色索引</param>
        /// <param name="lineWeight">线宽</param>
        /// <param name="description">图层描述</param>
        /// <returns>图层ID</returns>
        public static ObjectId CreateLayer(string layerName, short colorIndex = 7, LineWeight lineWeight = LineWeight.ByLayer, string description = null)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(layerName))
                throw new ArgumentException("图层名称不能为空", nameof(layerName));

            return _layerCache.Value.GetOrAdd(layerName, name =>
                PerformanceEngine.Execute("CreateLayer", () =>
                {
                    return AutoCADContext.ExecuteSafely(() =>
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
                            Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex),
                            LineWeight = lineWeight
                        };

                        if (!string.IsNullOrEmpty(description))
                        {
                            layerRecord.Description = description;
                        }

                        var layerId = layerTable.Add(layerRecord);
                        context.Transaction.AddNewlyCreatedDBObject(layerRecord, true);

                        return layerId;
                    });
                })
            );
        }

        /// <summary>
        /// 获取或创建图层（如果不存在则创建）
        /// </summary>
        /// <param name="layerName">图层名称</param>
        /// <param name="colorIndex">颜色索引（仅在创建时使用）</param>
        /// <returns>图层ID</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectId GetOrCreateLayer(string layerName, short colorIndex = 7)
        {
            return CreateLayer(layerName, colorIndex);
        }

        /// <summary>
        /// 批量创建图层
        /// </summary>
        /// <param name="layerDefinitions">图层定义列表</param>
        /// <returns>创建的图层ID字典</returns>
        public static Dictionary<string, ObjectId> CreateLayers(IEnumerable<ArxLayerDefinition> layerDefinitions)
        {
            EnsureInitialized();

            var definitions = layerDefinitions?.ToList();
            if (definitions == null || !definitions.Any())
                return new Dictionary<string, ObjectId>();

            return PerformanceEngine.Execute("CreateLayers", () =>
            {
                return AutoCADContext.ExecuteBatch(context =>
                {
                    var result = new Dictionary<string, ObjectId>();
                    var layerTable = context.GetObject<LayerTable>(
                        context.Database.LayerTableId, OpenMode.ForRead);

                    var needsUpgrade = false;
                    var newLayers = new List<ArxLayerDefinition>();

                    // 先检查哪些图层需要创建
                    foreach (var definition in definitions)
                    {
                        if (layerTable.Has(definition.Name))
                        {
                            result[definition.Name] = layerTable[definition.Name];
                            _layerCache.Value.Set(definition.Name, layerTable[definition.Name]);
                        }
                        else
                        {
                            newLayers.Add(definition);
                            needsUpgrade = true;
                        }
                    }

                    // 批量创建新图层
                    if (needsUpgrade)
                    {
                        layerTable.UpgradeOpen();

                        foreach (var definition in newLayers)
                        {
                            var layerRecord = new LayerTableRecord
                            {
                                Name = definition.Name,
                                Color = Color.FromColorIndex(ColorMethod.ByAci, definition.ColorIndex),
                                LineWeight = definition.LineWeight
                            };

                            if (!string.IsNullOrEmpty(definition.Description))
                            {
                                layerRecord.Description = definition.Description;
                            }

                            var layerId = layerTable.Add(layerRecord);
                            context.Transaction.AddNewlyCreatedDBObject(layerRecord, true);

                            result[definition.Name] = layerId;
                            _layerCache.Value.Set(definition.Name, layerId);
                        }
                    }

                    return result;
                });
            });
        }

        #endregion 图层创建和管理

        #region 图层状态控制

        /// <summary>
        /// 设置当前图层
        /// </summary>
        /// <param name="layerName">图层名称</param>
        /// <returns>操作是否成功</returns>
        public static bool SetCurrentLayer(string layerName)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(layerName))
                return false;

            return PerformanceEngine.Execute("SetCurrentLayer", () =>
            {
                return AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var layerTable = context.GetObject<LayerTable>(
                        context.Database.LayerTableId, OpenMode.ForRead);

                    if (!layerTable.Has(layerName)) return false;

                    context.Database.Clayer = layerTable[layerName];
                    return true;
                });
            });
        }

        /// <summary>
        /// 获取当前图层名称
        /// </summary>
        /// <returns>当前图层名称</returns>
        public static string GetCurrentLayerName()
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("GetCurrentLayerName", () =>
            {
                return AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var currentLayerId = context.Database.Clayer;
                    var layerRecord = context.GetObject<LayerTableRecord>(currentLayerId, OpenMode.ForRead);
                    return layerRecord?.Name ?? "0";
                });
            });
        }

        /// <summary>
        /// 冻结图层
        /// </summary>
        /// <param name="layerName">图层名称</param>
        /// <returns>操作是否成功</returns>
        public static bool FreezeLayer(string layerName)
        {
            return SetLayerState(layerName, frozen: true);
        }

        /// <summary>
        /// 解冻图层
        /// </summary>
        /// <param name="layerName">图层名称</param>
        /// <returns>操作是否成功</returns>
        public static bool ThawLayer(string layerName)
        {
            return SetLayerState(layerName, frozen: false);
        }

        /// <summary>
        /// 关闭图层
        /// </summary>
        /// <param name="layerName">图层名称</param>
        /// <returns>操作是否成功</returns>
        public static bool TurnLayerOff(string layerName)
        {
            return SetLayerState(layerName, isOff: true);
        }

        /// <summary>
        /// 打开图层
        /// </summary>
        /// <param name="layerName">图层名称</param>
        /// <returns>操作是否成功</returns>
        public static bool TurnLayerOn(string layerName)
        {
            return SetLayerState(layerName, isOff: false);
        }

        /// <summary>
        /// 锁定图层
        /// </summary>
        /// <param name="layerName">图层名称</param>
        /// <returns>操作是否成功</returns>
        public static bool LockLayer(string layerName)
        {
            return SetLayerState(layerName, isLocked: true);
        }

        /// <summary>
        /// 解锁图层
        /// </summary>
        /// <param name="layerName">图层名称</param>
        /// <returns>操作是否成功</returns>
        public static bool UnlockLayer(string layerName)
        {
            return SetLayerState(layerName, isLocked: false);
        }

        #endregion 图层状态控制

        #region 图层查询

        /// <summary>
        /// 检查图层是否存在
        /// </summary>
        /// <param name="layerName">图层名称</param>
        /// <returns>图层是否存在</returns>
        public static bool LayerExists(string layerName)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(layerName))
                return false;

            return _layerCache.Value.ContainsKey(layerName) ||
                   PerformanceEngine.Execute("LayerExists", () =>
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

        /// <summary>
        /// 获取所有图层名称
        /// </summary>
        /// <returns>图层名称列表</returns>
        public static List<string> GetAllLayerNames()
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("GetAllLayerNames", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var layerTable = context.GetObject<LayerTable>(
                        context.Database.LayerTableId, OpenMode.ForRead);

                    var layerNames = new List<string>();
                    foreach (ObjectId layerId in layerTable)
                    {
                        var layerRecord = context.GetObject<LayerTableRecord>(layerId, OpenMode.ForRead);
                        layerNames.Add(layerRecord.Name);
                    }

                    return layerNames;
                })
            );
        }

        /// <summary>
        /// 获取图层信息
        /// </summary>
        /// <param name="layerName">图层名称</param>
        /// <returns>图层信息</returns>
        public static ArxLayerInfo GetLayerInfo(string layerName)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(layerName))
                return null;

            return PerformanceEngine.Execute("GetLayerInfo", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var layerTable = context.GetObject<LayerTable>(
                        context.Database.LayerTableId, OpenMode.ForRead);

                    if (!layerTable.Has(layerName))
                        return null;

                    var layerRecord = context.GetObject<LayerTableRecord>(
                        layerTable[layerName], OpenMode.ForRead);

                    return new ArxLayerInfo
                    {
                        Name = layerRecord.Name,
                        ColorIndex = layerRecord.Color.ColorIndex,
                        LineWeight = layerRecord.LineWeight,
                        IsFrozen = layerRecord.IsFrozen,
                        IsOff = layerRecord.IsOff,
                        IsLocked = layerRecord.IsLocked,
                        Description = layerRecord.Description
                    };
                })
            );
        }

        /// <summary>
        /// 获取所有图层信息
        /// </summary>
        /// <returns>图层信息列表</returns>
        public static List<ArxLayerInfo> GetAllLayerInfos()
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("GetAllLayerInfos", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var layerTable = context.GetObject<LayerTable>(
                        context.Database.LayerTableId, OpenMode.ForRead);

                    var layerInfos = new List<ArxLayerInfo>();
                    foreach (ObjectId layerId in layerTable)
                    {
                        var layerRecord = context.GetObject<LayerTableRecord>(layerId, OpenMode.ForRead);

                        layerInfos.Add(new ArxLayerInfo
                        {
                            Name = layerRecord.Name,
                            ColorIndex = layerRecord.Color.ColorIndex,
                            LineWeight = layerRecord.LineWeight,
                            IsFrozen = layerRecord.IsFrozen,
                            IsOff = layerRecord.IsOff,
                            IsLocked = layerRecord.IsLocked,
                            Description = layerRecord.Description
                        });
                    }

                    return layerInfos;
                })
            );
        }

        #endregion 图层查询

        #region 图层删除

        /// <summary>
        /// 删除图层（如果图层为空且不是当前图层）
        /// </summary>
        /// <param name="layerName">图层名称</param>
        /// <param name="force">是否强制删除（删除图层上的所有实体）</param>
        /// <returns>操作是否成功</returns>
        public static bool DeleteLayer(string layerName, bool force = false)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(layerName) || layerName == "0")
                return false; // 不能删除0图层

            return PerformanceEngine.Execute("DeleteLayer", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var layerTable = context.GetObject<LayerTable>(
                        context.Database.LayerTableId, OpenMode.ForWrite);

                    if (!layerTable.Has(layerName))
                        return false;

                    var layerId = layerTable[layerName];

                    // 检查是否是当前图层
                    if (context.Database.Clayer == layerId)
                        return false;

                    var layerRecord = context.GetObject<LayerTableRecord>(layerId, OpenMode.ForWrite);

                    if (force)
                    {
                        // 强制删除：先删除图层上的所有实体
                        DeleteEntitiesOnLayer(layerName);
                    }

                    try
                    {
                        layerRecord.Erase();
                        _layerCache.Value.Remove(layerName);
                        return true;
                    }
                    catch
                    {
                        return false; // 图层可能包含实体或被引用
                    }
                })
            );
        }

        #endregion 图层删除

        #region 私有辅助方法

        /// <summary>
        /// 设置图层状态
        /// </summary>
        private static bool SetLayerState(string layerName, bool? frozen = null, bool? isOff = null, bool? isLocked = null)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(layerName))
                return false;

            return PerformanceEngine.Execute("SetLayerState", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var layerTable = context.GetObject<LayerTable>(
                        context.Database.LayerTableId, OpenMode.ForRead);

                    if (!layerTable.Has(layerName))
                        return false;

                    var layerRecord = context.GetObject<LayerTableRecord>(
                        layerTable[layerName], OpenMode.ForWrite);

                    if (frozen.HasValue)
                        layerRecord.IsFrozen = frozen.Value;

                    if (isOff.HasValue)
                        layerRecord.IsOff = isOff.Value;

                    if (isLocked.HasValue)
                        layerRecord.IsLocked = isLocked.Value;

                    return true;
                })
            );
        }

        /// <summary>
        /// 删除指定图层上的所有实体
        /// </summary>
        private static void DeleteEntitiesOnLayer(string layerName)
        {
            var entitiesOnLayer = SelectByLayer(layerName);
            if (entitiesOnLayer.Any())
            {
                DeleteEntities(entitiesOnLayer);
            }
        }

        #endregion 私有辅助方法
    }

    #region 图层相关数据结构

    /// <summary>
    /// DotNetARX 图层定义
    /// 避免与AutoCAD原生类型冲突
    /// </summary>
    public class ArxLayerDefinition
    {
        public string Name { get; set; }
        public short ColorIndex { get; set; } = 7;
        public LineWeight LineWeight { get; set; } = LineWeight.ByLayer;
        public string Description { get; set; }
    }

    /// <summary>
    /// DotNetARX 图层信息
    /// 避免与AutoCAD原生类型冲突
    /// </summary>
    public class ArxLayerInfo
    {
        public string Name { get; set; }
        public short ColorIndex { get; set; }
        public LineWeight LineWeight { get; set; }
        public bool IsFrozen { get; set; }
        public bool IsOff { get; set; }
        public bool IsLocked { get; set; }
        public string Description { get; set; }
    }

    #endregion 图层相关数据结构
}