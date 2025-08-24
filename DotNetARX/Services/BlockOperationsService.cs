using DotNetARX.DependencyInjection;
using DotNetARX.Interfaces;

namespace DotNetARX.Services
{
    /// <summary>
    /// 块操作服务实现
    /// </summary>
    public class BlockOperationsService : IBlockOperations
    {
        private readonly IEventBus _eventBus;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly ILogger _logger;

        public BlockOperationsService(
            IEventBus eventBus = null,
            IPerformanceMonitor performanceMonitor = null,
            ILogger logger = null)
        {
            _eventBus = eventBus ?? ServiceContainer.Instance.GetService<IEventBus>();
            _performanceMonitor = performanceMonitor ?? ServiceContainer.Instance.GetService<IPerformanceMonitor>();
            _logger = logger ?? ServiceContainer.Instance.GetService<ILogger>();
        }

        /// <summary>
        /// 创建块定义
        /// </summary>
        public ObjectId CreateBlockDefinition(string blockName, IEnumerable<Entity> entities, Point3d basePoint)
        {
            using var operation = _performanceMonitor?.StartOperation("CreateBlockDefinition");

            try
            {
                if (string.IsNullOrEmpty(blockName))
                    throw new ArgumentException("块名称不能为空");

                if (entities == null || !entities.Any())
                    throw new ArgumentException("实体集合不能为空");

                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;
                ObjectId blockId;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var blockTable = transManager.GetObject<BlockTable>(database.BlockTableId, OpenMode.ForWrite);

                    // 检查块是否已存在
                    if (blockTable.Has(blockName))
                    {
                        throw new InvalidOperationException($"块 '{blockName}' 已存在");
                    }

                    // 创建块表记录
                    var blockTableRecord = new BlockTableRecord
                    {
                        Name = blockName,
                        Origin = basePoint
                    };

                    blockId = blockTable.Add(blockTableRecord);
                    transManager.AddNewlyCreatedDBObject(blockTableRecord, true);

                    // 添加实体到块定义
                    foreach (var entity in entities)
                    {
                        var clonedEntity = entity.Clone() as Entity;
                        if (clonedEntity != null)
                        {
                            blockTableRecord.AppendEntity(clonedEntity);
                            transManager.AddNewlyCreatedDBObject(clonedEntity, true);
                        }
                    }

                    transManager.Commit();
                }

                _eventBus?.Publish(new BlockEvent("BlockDefinitionCreated", blockId, blockName));
                _logger?.Info($"块定义创建成功: {blockName}, ID: {blockId}");

                return blockId;
            }
            catch (Exception ex)
            {
                _logger?.Error($"创建块定义失败: {ex.Message}", ex);
                throw new BlockOperationException($"创建块定义失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 插入块引用
        /// </summary>
        public ObjectId InsertBlock(string blockName, Point3d position, double scale = 1.0, double rotation = 0)
        {
            using var operation = _performanceMonitor?.StartOperation("InsertBlock");

            try
            {
                if (string.IsNullOrEmpty(blockName))
                    throw new ArgumentException("块名称不能为空");

                if (scale <= 0)
                    throw new ArgumentException("缩放比例必须大于0");

                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;
                ObjectId blockRefId;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var blockTable = transManager.GetObject<BlockTable>(database.BlockTableId, OpenMode.ForRead);

                    if (!blockTable.Has(blockName))
                    {
                        throw new InvalidOperationException($"块 '{blockName}' 不存在");
                    }

                    var blockId = blockTable[blockName];
                    var currentSpace = transManager.GetObject<BlockTableRecord>(
                        database.CurrentSpaceId, OpenMode.ForWrite);

                    // 创建块引用
                    var blockRef = new BlockReference(position, blockId)
                    {
                        ScaleFactors = new Scale3d(scale, scale, scale),
                        Rotation = rotation
                    };

                    blockRefId = currentSpace.AppendEntity(blockRef);
                    transManager.AddNewlyCreatedDBObject(blockRef, true);

                    transManager.Commit();
                }

                _eventBus?.Publish(new BlockEvent("BlockInserted", blockRefId,
                    $"Block: {blockName}, Position: {position}, Scale: {scale}, Rotation: {rotation}"));
                _logger?.Info($"块插入成功: {blockName}, 位置 {position}, ID: {blockRefId}");

                return blockRefId;
            }
            catch (Exception ex)
            {
                _logger?.Error($"插入块失败: {ex.Message}", ex);
                throw new BlockOperationException($"插入块失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 插入块引用（带属性）
        /// </summary>
        public ObjectId InsertBlockWithAttributes(string blockName, Point3d position, Dictionary<string, string> attributes, double scale = 1.0, double rotation = 0)
        {
            using var operation = _performanceMonitor?.StartOperation("InsertBlockWithAttributes");

            try
            {
                if (string.IsNullOrEmpty(blockName))
                    throw new ArgumentException("块名称不能为空");

                if (scale <= 0)
                    throw new ArgumentException("缩放比例必须大于0");

                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;
                ObjectId blockRefId;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var blockTable = transManager.GetObject<BlockTable>(database.BlockTableId, OpenMode.ForRead);

                    if (!blockTable.Has(blockName))
                    {
                        throw new InvalidOperationException($"块 '{blockName}' 不存在");
                    }

                    var blockId = blockTable[blockName];
                    var blockDef = transManager.GetObject<BlockTableRecord>(blockId, OpenMode.ForRead);
                    var currentSpace = transManager.GetObject<BlockTableRecord>(
                        database.CurrentSpaceId, OpenMode.ForWrite);

                    // 创建块引用
                    var blockRef = new BlockReference(position, blockId)
                    {
                        ScaleFactors = new Scale3d(scale, scale, scale),
                        Rotation = rotation
                    };

                    blockRefId = currentSpace.AppendEntity(blockRef);
                    transManager.AddNewlyCreatedDBObject(blockRef, true);

                    // 添加属性
                    if (attributes != null && attributes.Any())
                    {
                        foreach (ObjectId objId in blockDef)
                        {
                            var obj = transManager.GetObject<DBObject>(objId, OpenMode.ForRead);
                            if (obj is AttributeDefinition attDef && attributes.ContainsKey(attDef.Tag))
                            {
                                var attRef = new AttributeReference();
                                attRef.SetAttributeFromBlock(attDef, blockRef.BlockTransform);
                                attRef.TextString = attributes[attDef.Tag];

                                blockRef.AttributeCollection.AppendAttribute(attRef);
                                transManager.AddNewlyCreatedDBObject(attRef, true);
                            }
                        }
                    }

                    transManager.Commit();
                }

                _eventBus?.Publish(new BlockEvent("BlockWithAttributesInserted", blockRefId,
                    $"Block: {blockName}, Position: {position}, Attributes: {attributes?.Count ?? 0}"));
                _logger?.Info($"带属性块插入成功: {blockName}, 位置 {position}, 属性数 {attributes?.Count ?? 0}");

                return blockRefId;
            }
            catch (Exception ex)
            {
                _logger?.Error($"插入带属性块失败: {ex.Message}", ex);
                throw new BlockOperationException($"插入带属性块失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 删除块定义
        /// </summary>
        public bool DeleteBlockDefinition(string blockName)
        {
            using var operation = _performanceMonitor?.StartOperation("DeleteBlockDefinition");

            try
            {
                if (string.IsNullOrEmpty(blockName))
                    return false;

                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var blockTable = transManager.GetObject<BlockTable>(database.BlockTableId, OpenMode.ForWrite);

                    if (!blockTable.Has(blockName))
                    {
                        _logger?.Warning($"块 '{blockName}' 不存在");
                        return false;
                    }

                    var blockId = blockTable[blockName];
                    var blockDef = transManager.GetObject<BlockTableRecord>(blockId, OpenMode.ForWrite);

                    // 检查是否有引用
                    var blockRefs = blockDef.GetBlockReferenceIds(true, true);
                    if (blockRefs.Count > 0)
                    {
                        _logger?.Warning($"块 '{blockName}' 仍有引用，无法删除");
                        return false;
                    }

                    blockDef.Erase();
                    transManager.Commit();
                }

                _eventBus?.Publish(new BlockEvent("BlockDefinitionDeleted", ObjectId.Null, blockName));
                _logger?.Info($"块定义删除成功: {blockName}");

                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"删除块定义失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 获取所有块定义
        /// </summary>
        public IEnumerable<string> GetBlockNames()
        {
            using var operation = _performanceMonitor?.StartOperation("GetBlockNames");

            try
            {
                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;
                var blockNames = new List<string>();

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var blockTable = transManager.GetObject<BlockTable>(database.BlockTableId, OpenMode.ForRead);

                    foreach (ObjectId blockId in blockTable)
                    {
                        var blockDef = transManager.GetObject<BlockTableRecord>(blockId, OpenMode.ForRead);

                        // 排除系统块和匿名块
                        if (!blockDef.IsAnonymous &&
                            !blockDef.IsLayout &&
                            blockDef.Name != "*Model_Space" &&
                            blockDef.Name != "*Paper_Space")
                        {
                            blockNames.Add(blockDef.Name);
                        }
                    }

                    transManager.Commit();
                }

                return blockNames;
            }
            catch (Exception ex)
            {
                _logger?.Error($"获取块名称列表失败: {ex.Message}", ex);
                return new List<string>();
            }
        }

        /// <summary>
        /// 分解块引用
        /// </summary>
        public ObjectIdCollection ExplodeBlock(ObjectId blockReferenceId)
        {
            using var operation = _performanceMonitor?.StartOperation("ExplodeBlock");

            try
            {
                if (blockReferenceId.IsNull || !blockReferenceId.IsValid)
                    throw new ArgumentException("无效的块引用ID");

                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;
                var explodedIds = new ObjectIdCollection();

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var blockRef = transManager.GetObject<BlockReference>(blockReferenceId, OpenMode.ForWrite);
                    var currentSpace = transManager.GetObject<BlockTableRecord>(
                        database.CurrentSpaceId, OpenMode.ForWrite);

                    // 分解块引用
                    var explodedEntities = new DBObjectCollection();
                    blockRef.Explode(explodedEntities);

                    foreach (Entity entity in explodedEntities)
                    {
                        var objId = currentSpace.AppendEntity(entity);
                        transManager.AddNewlyCreatedDBObject(entity, true);
                        explodedIds.Add(objId);
                    }

                    // 删除原块引用
                    blockRef.Erase();

                    transManager.Commit();
                }

                _eventBus?.Publish(new BlockEvent("BlockExploded", blockReferenceId,
                    $"Exploded into {explodedIds.Count} entities"));
                _logger?.Info($"块分解成功: {blockReferenceId}, 分解为 {explodedIds.Count} 个实体");

                return explodedIds;
            }
            catch (Exception ex)
            {
                _logger?.Error($"分解块失败: {ex.Message}", ex);
                throw new BlockOperationException($"分解块失败: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// 块事件类
    /// </summary>
    public class BlockEvent : Events.EventArgs
    {
        public string EventType { get; }
        public ObjectId ObjectId { get; }
        public string Details { get; }
        public new DateTime Timestamp { get; }

        public BlockEvent(string eventType, ObjectId objectId, string details)
            : base("BlockOperationsService")
        {
            EventType = eventType;
            ObjectId = objectId;
            Details = details;
            Timestamp = DateTime.Now;
        }
    }
}