using DotNetARX.DependencyInjection;
using DotNetARX.Interfaces;


namespace DotNetARX.Services
{
    /// <summary>
    /// 数据库操作服务实现
    /// </summary>
    public class DatabaseOperationsService : IDatabaseOperations
    {
        private readonly IEventBus _eventBus;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly ILogger _logger;

        public DatabaseOperationsService(
            IEventBus eventBus = null,
            IPerformanceMonitor performanceMonitor = null,
            ILogger logger = null)
        {
            _eventBus = eventBus ?? ServiceContainer.Instance.GetService<IEventBus>();
            _performanceMonitor = performanceMonitor ?? ServiceContainer.Instance.GetService<IPerformanceMonitor>();
            _logger = logger ?? ServiceContainer.Instance.GetService<ILogger>();
        }

        /// <summary>
        /// 添加实体到模型空间
        /// </summary>
        public ObjectId AddToModelSpace(Entity entity)
        {
            using var operation = _performanceMonitor?.StartOperation("AddToModelSpace");

            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;
                ObjectId objectId;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var modelSpace = transManager.GetObject<BlockTableRecord>(
                        SymbolUtilityServices.GetBlockModelSpaceId(database),
                        OpenMode.ForWrite);

                    objectId = modelSpace.AppendEntity(entity);
                    transManager.AddNewlyCreatedDBObject(entity, true);
                    transManager.Commit();
                }

                _eventBus?.Publish(new EntityEvent("EntityAdded", objectId, entity.GetType().Name));
                _logger?.Info($"实体添加到模型空间成功: {objectId}");

                return objectId;
            }
            catch (Exception ex)
            {
                _logger?.Error($"添加实体到模型空间失败: {ex.Message}", ex);
                throw new DatabaseOperationException($"添加实体到模型空间失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 批量添加实体到模型空间
        /// </summary>
        public ObjectIdCollection AddToModelSpace(IEnumerable<Entity> entities)
        {
            using var operation = _performanceMonitor?.StartOperation("AddToModelSpace_Batch");

            try
            {
                if (entities == null)
                    throw new ArgumentNullException(nameof(entities));

                var entityList = entities.ToList();
                if (!entityList.Any())
                    return new ObjectIdCollection();

                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;
                var objectIds = new ObjectIdCollection();

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var modelSpace = transManager.GetObject<BlockTableRecord>(
                        SymbolUtilityServices.GetBlockModelSpaceId(database),
                        OpenMode.ForWrite);

                    foreach (var entity in entityList)
                    {
                        if (entity != null)
                        {
                            var objectId = modelSpace.AppendEntity(entity);
                            transManager.AddNewlyCreatedDBObject(entity, true);
                            objectIds.Add(objectId);
                        }
                    }

                    transManager.Commit();
                }

                _eventBus?.Publish(new EntityEvent("EntitiesBatchAdded", objectIds, $"Count: {objectIds.Count}"));
                _logger?.Info($"批量添加 {objectIds.Count} 个实体到模型空间成功");

                return objectIds;
            }
            catch (Exception ex)
            {
                _logger?.Error($"批量添加实体到模型空间失败: {ex.Message}", ex);
                throw new DatabaseOperationException($"批量添加实体到模型空间失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 添加实体到图纸空间
        /// </summary>
        public ObjectId AddToPaperSpace(Entity entity)
        {
            using var operation = _performanceMonitor?.StartOperation("AddToPaperSpace");

            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;
                ObjectId objectId;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var paperSpace = transManager.GetObject<BlockTableRecord>(
                        SymbolUtilityServices.GetBlockPaperSpaceId(database),
                        OpenMode.ForWrite);

                    objectId = paperSpace.AppendEntity(entity);
                    transManager.AddNewlyCreatedDBObject(entity, true);
                    transManager.Commit();
                }

                _eventBus?.Publish(new EntityEvent("EntityAddedToPaperSpace", objectId, entity.GetType().Name));
                _logger?.Info($"实体添加到图纸空间成功: {objectId}");

                return objectId;
            }
            catch (Exception ex)
            {
                _logger?.Error($"添加实体到图纸空间失败: {ex.Message}", ex);
                throw new DatabaseOperationException($"添加实体到图纸空间失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 添加实体到当前空间
        /// </summary>
        public ObjectId AddToCurrentSpace(Entity entity)
        {
            using var operation = _performanceMonitor?.StartOperation("AddToCurrentSpace");

            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;
                ObjectId objectId;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var currentSpace = transManager.GetObject<BlockTableRecord>(
                        database.CurrentSpaceId,
                        OpenMode.ForWrite);

                    objectId = currentSpace.AppendEntity(entity);
                    transManager.AddNewlyCreatedDBObject(entity, true);
                    transManager.Commit();
                }

                _eventBus?.Publish(new EntityEvent("EntityAddedToCurrentSpace", objectId, entity.GetType().Name));
                _logger?.Info($"实体添加到当前空间成功: {objectId}");

                return objectId;
            }
            catch (Exception ex)
            {
                _logger?.Error($"添加实体到当前空间失败: {ex.Message}", ex);
                throw new DatabaseOperationException($"添加实体到当前空间失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 删除实体
        /// </summary>
        public bool DeleteEntity(ObjectId entityId)
        {
            using var operation = _performanceMonitor?.StartOperation("DeleteEntity");

            try
            {
                if (entityId.IsNull || !entityId.IsValid)
                    return false;

                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var entity = transManager.GetObject<Entity>(entityId, OpenMode.ForWrite);
                    entity.Erase();
                    transManager.Commit();
                }

                _eventBus?.Publish(new EntityEvent("EntityDeleted", entityId, ""));
                _logger?.Info($"实体删除成功: {entityId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"删除实体失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 批量删除实体
        /// </summary>
        public int DeleteEntities(IEnumerable<ObjectId> entityIds)
        {
            using var operation = _performanceMonitor?.StartOperation("DeleteEntities_Batch");

            try
            {
                if (entityIds == null)
                    return 0;

                var idList = entityIds.Where(id => !id.IsNull && id.IsValid).ToList();
                if (!idList.Any())
                    return 0;

                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;
                int deletedCount = 0;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    foreach (var entityId in idList)
                    {
                        try
                        {
                            var entity = transManager.GetObject<Entity>(entityId, OpenMode.ForWrite);
                            entity.Erase();
                            deletedCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger?.Warning($"删除实体 {entityId} 失败: {ex.Message}");
                        }
                    }

                    transManager.Commit();
                }

                _eventBus?.Publish(new EntityEvent("EntitiesBatchDeleted", new ObjectIdCollection(idList.ToArray()), $"Count: {deletedCount}"));
                _logger?.Info($"批量删除完成，成功删除 {deletedCount} 个实体");

                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger?.Error($"批量删除实体失败: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// 获取数据库信息
        /// </summary>
        public DatabaseInfo GetDatabaseInfo()
        {
            using var operation = _performanceMonitor?.StartOperation("GetDatabaseInfo");

            try
            {
                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    // 获取图层表
                    var layerTable = transManager.GetObject<LayerTable>(database.LayerTableId, OpenMode.ForRead);

                    // 获取块表
                    var blockTable = transManager.GetObject<BlockTable>(database.BlockTableId, OpenMode.ForRead);

                    // 获取模型空间
                    var modelSpace = transManager.GetObject<BlockTableRecord>(
                        SymbolUtilityServices.GetBlockModelSpaceId(database), OpenMode.ForRead);

                    var info = new DatabaseInfo
                    {
                        FileName = database.Filename ?? "未保存",
                        Version = database.Version.ToString(),
                        CreationTime = database.Tdcreate,
                        ModificationTime = database.Tdupdate,
                        EntityCount = modelSpace.Cast<ObjectId>().Count(),
                        LayerCount = layerTable.Cast<ObjectId>().Count(),
                        BlockCount = blockTable.Cast<ObjectId>().Count(),
                        CurrentLayer = database.Clayer.IsValid ?
                            transManager.GetObject<LayerTableRecord>(database.Clayer, OpenMode.ForRead).Name : "未知",
                        IsModified = database.HasSaveVersionInfo
                    };

                    transManager.Commit();
                    return info;
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"获取数据库信息失败: {ex.Message}", ex);
                throw new DatabaseOperationException($"获取数据库信息失败: {ex.Message}", ex);
            }
        }
    }
}