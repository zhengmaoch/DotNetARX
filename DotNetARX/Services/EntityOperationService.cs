using DotNetARX.Interfaces;


namespace DotNetARX.Services
{
    /// <summary>
    /// 实体操作服务实现
    /// </summary>
    public class EntityOperationService : IEntityOperations
    {
        private readonly ILogger _logger;
        private readonly IConfigurationManager _config;

        public EntityOperationService(ILogger logger = null, IConfigurationManager config = null)
        {
            _logger = logger ?? LogManager.GetLogger(typeof(EntityOperationService));
            _config = config ?? GlobalConfiguration.Instance;
        }

        public bool MoveEntity(ObjectId entityId, Point3d fromPoint, Point3d toPoint)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                ValidateEntityId(entityId, "移动实体");

                if (fromPoint.IsEqualTo(toPoint))
                {
                    _logger.Info("源点与目标点相同，无需移动");
                    return true;
                }

                using (var transManager = TransactionManagerFactory.Create(entityId.Database))
                {
                    var entity = transManager.GetObject<Entity>(entityId, OpenMode.ForWrite);

                    var displacement = toPoint - fromPoint;
                    var moveMatrix = Matrix3d.Displacement(displacement);
                    entity.TransformBy(moveMatrix);

                    if (ValidateEntityTransform(entity))
                    {
                        transManager.Commit();
                        _logger.Info($"实体移动成功 - EntityId: {entityId}");
                        return true;
                    }
                    else
                    {
                        CADExceptionHandler.ThrowEntityException("移动实体", entityId, "移动操作验证失败");
                        return false;
                    }
                }
            }, false);
        }

        public ObjectId CopyEntity(ObjectId entityId, Point3d fromPoint, Point3d toPoint)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                ValidateEntityId(entityId, "复制实体");

                using (var transManager = TransactionManagerFactory.Create(entityId.Database))
                {
                    var originalEntity = transManager.GetObject<Entity>(entityId, OpenMode.ForRead);

                    var displacement = toPoint - fromPoint;
                    var moveMatrix = Matrix3d.Displacement(displacement);
                    var copiedEntity = originalEntity.GetTransformedCopy(moveMatrix);

                    var modelSpace = transManager.GetObject<BlockTableRecord>(
                        entityId.Database.GetModelSpaceId(),
                        OpenMode.ForWrite);

                    var copyId = modelSpace.AppendEntity(copiedEntity);
                    transManager.AddNewlyCreatedDBObject(copiedEntity, true);
                    transManager.Commit();

                    _logger.Info($"实体复制成功 - 原实体: {entityId}, 新实体: {copyId}");
                    return copyId;
                }
            }, ObjectId.Null);
        }

        public bool RotateEntity(ObjectId entityId, Point3d basePoint, double angle)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                ValidateEntityId(entityId, "旋转实体");

                if (Math.Abs(angle) < 1e-10)
                {
                    _logger.Info("旋转角度过小，无需操作");
                    return true;
                }

                using (var transManager = TransactionManagerFactory.Create(entityId.Database))
                {
                    var entity = transManager.GetObject<Entity>(entityId, OpenMode.ForWrite);

                    var rotationMatrix = Matrix3d.Rotation(angle, Vector3d.ZAxis, basePoint);
                    entity.TransformBy(rotationMatrix);

                    if (ValidateEntityTransform(entity))
                    {
                        transManager.Commit();
                        _logger.Info($"实体旋转成功 - EntityId: {entityId}, 角度: {angle * 180 / Math.PI}度");
                        return true;
                    }
                    else
                    {
                        CADExceptionHandler.ThrowEntityException("旋转实体", entityId, "旋转操作验证失败");
                        return false;
                    }
                }
            }, false);
        }

        public bool ScaleEntity(ObjectId entityId, Point3d basePoint, double scaleFactor)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                ValidateEntityId(entityId, "缩放实体");

                if (scaleFactor <= 0)
                {
                    CADExceptionHandler.ThrowEntityException("缩放实体", entityId, "缩放系数必须大于0");
                }

                if (Math.Abs(scaleFactor - 1.0) < 1e-10)
                {
                    _logger.Info("缩放系数接近1，无需操作");
                    return true;
                }

                using (var transManager = TransactionManagerFactory.Create(entityId.Database))
                {
                    var entity = transManager.GetObject<Entity>(entityId, OpenMode.ForWrite);

                    var scaleMatrix = Matrix3d.Scaling(scaleFactor, basePoint);
                    entity.TransformBy(scaleMatrix);

                    if (ValidateEntityTransform(entity))
                    {
                        transManager.Commit();
                        _logger.Info($"实体缩放成功 - EntityId: {entityId}, 缩放系数: {scaleFactor}");
                        return true;
                    }
                    else
                    {
                        CADExceptionHandler.ThrowEntityException("缩放实体", entityId, "缩放操作验证失败");
                        return false;
                    }
                }
            }, false);
        }

        public ObjectIdCollection OffsetEntity(ObjectId entityId, double distance)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                ValidateEntityId(entityId, "偏移实体");

                if (Math.Abs(distance) < 1e-10)
                {
                    _logger.Info("偏移距离过小，无需操作");
                    return new ObjectIdCollection();
                }

                using (var transManager = TransactionManagerFactory.Create(entityId.Database))
                {
                    var entity = transManager.GetObject<Entity>(entityId, OpenMode.ForRead);

                    if (!(entity is Curve curve))
                    {
                        CADExceptionHandler.ThrowEntityException("偏移实体", entityId, "只能偏移曲线类型的实体");
                    }

                    var offsetCurves = curve.GetOffsetCurves(distance);
                    var resultIds = new ObjectIdCollection();

                    if (offsetCurves.Count > 0)
                    {
                        var modelSpace = transManager.GetObject<BlockTableRecord>(
                            entityId.Database.GetModelSpaceId(),
                            OpenMode.ForWrite);

                        foreach (Entity offsetEntity in offsetCurves)
                        {
                            var offsetId = modelSpace.AppendEntity(offsetEntity);
                            transManager.AddNewlyCreatedDBObject(offsetEntity, true);
                            resultIds.Add(offsetId);
                        }

                        transManager.Commit();
                        _logger.Info($"实体偏移成功 - 原实体: {entityId}, 生成实体数: {resultIds.Count}");
                    }

                    return resultIds;
                }
            }, new ObjectIdCollection());
        }

        public ObjectId MirrorEntity(ObjectId entityId, Point3d mirrorPt1, Point3d mirrorPt2, bool eraseSource)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                ValidateEntityId(entityId, "镜像实体");

                using (var transManager = TransactionManagerFactory.Create(entityId.Database))
                {
                    var entity = transManager.GetObject<Entity>(entityId, OpenMode.ForWrite);

                    var mirrorLine = new Line3d(mirrorPt1, mirrorPt2);
                    var mirrorMatrix = Matrix3d.Mirroring(mirrorLine);

                    ObjectId resultId = entityId;

                    if (eraseSource)
                    {
                        // 直接变换原实体
                        entity.TransformBy(mirrorMatrix);
                        _logger.Info($"实体镜像成功（替换原实体） - EntityId: {entityId}");
                    }
                    else
                    {
                        // 创建镜像副本
                        var mirroredEntity = entity.GetTransformedCopy(mirrorMatrix);

                        var modelSpace = transManager.GetObject<BlockTableRecord>(
                            entityId.Database.GetModelSpaceId(),
                            OpenMode.ForWrite);

                        resultId = modelSpace.AppendEntity(mirroredEntity);
                        transManager.AddNewlyCreatedDBObject(mirroredEntity, true);

                        _logger.Info($"实体镜像成功（创建副本） - 原实体: {entityId}, 新实体: {resultId}");
                    }

                    transManager.Commit();
                    return resultId;
                }
            }, ObjectId.Null);
        }

        public bool ValidateEntity(ObjectId entityId)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                if (entityId.IsNull || entityId.IsErased)
                    return false;

                using (var transManager = TransactionManagerFactory.Create(entityId.Database))
                {
                    return transManager.TryGetObject<Entity>(entityId, out var entity) &&
                           ValidateEntityTransform(entity);
                }
            }, false);
        }

        private void ValidateEntityId(ObjectId entityId, string operation)
        {
            if (entityId.IsNull)
            {
                CADExceptionHandler.ThrowEntityException(operation, entityId, "实体ID为空");
            }

            if (entityId.IsErased)
            {
                CADExceptionHandler.ThrowEntityException(operation, entityId, "实体已被删除");
            }
        }

        private bool ValidateEntityTransform(Entity entity)
        {
            try
            {
                var bounds = entity.GeometricExtents;
                return bounds.MinPoint.IsValidCoordinate() && bounds.MaxPoint.IsValidCoordinate();
            }
            catch
            {
                return false;
            }
        }
    }
}