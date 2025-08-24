namespace DotNetARX
{
    /// <summary>
    /// 改进的实体操作类
    /// </summary>
    public static class EntityOperationsImproved
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(EntityOperationsImproved));

        /// <summary>
        /// 安全的实体移动操作（支持验证和回滚）
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="fromPoint">源点</param>
        /// <param name="toPoint">目标点</param>
        /// <returns>移动操作是否成功</returns>
        public static bool MoveEntitySafe(this ObjectId entityId, Point3d fromPoint, Point3d toPoint)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                // 参数验证
                if (entityId.IsNull || entityId.IsErased)
                {
                    CADExceptionHandler.ThrowEntityException("移动实体", entityId, "实体ID无效或已被删除");
                }

                if (fromPoint.IsEqualTo(toPoint))
                {
                    Logger.Info("源点与目标点相同，无需移动");
                    return true;
                }

                using (var transManager = TransactionManagerFactory.Create(entityId.Database))
                {
                    var entity = transManager.GetObject<Entity>(entityId, OpenMode.ForWrite);

                    // 记录原始边界用于验证
                    var originalBounds = entity.GeometricExtents;

                    // 执行移动
                    var displacement = toPoint - fromPoint;
                    var moveMatrix = Matrix3d.Displacement(displacement);
                    entity.TransformBy(moveMatrix);

                    // 验证移动结果
                    if (ValidateEntityTransform(entity))
                    {
                        transManager.Commit();
                        Logger.Info($"实体移动成功 - EntityId: {entityId}, 位移: {displacement}");
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

        /// <summary>
        /// 安全的实体复制操作
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="fromPoint">复制源点</param>
        /// <param name="toPoint">复制目标点</param>
        /// <returns>复制后的实体ObjectId</returns>
        public static ObjectId CopyEntitySafe(this ObjectId entityId, Point3d fromPoint, Point3d toPoint)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                if (entityId.IsNull || entityId.IsErased)
                {
                    CADExceptionHandler.ThrowEntityException("复制实体", entityId, "实体ID无效或已被删除");
                }

                using (var transManager = TransactionManagerFactory.Create(entityId.Database))
                {
                    var originalEntity = transManager.GetObject<Entity>(entityId, OpenMode.ForRead);

                    // 计算变换矩阵
                    var displacement = toPoint - fromPoint;
                    var moveMatrix = Matrix3d.Displacement(displacement);

                    // 创建变换后的副本
                    var copiedEntity = originalEntity.GetTransformedCopy(moveMatrix);

                    // 添加到数据库
                    var modelSpace = transManager.GetObject<BlockTableRecord>(
                        entityId.Database.GetModelSpaceId(),
                        OpenMode.ForWrite);

                    var copyId = modelSpace.AppendEntity(copiedEntity);
                    transManager.AddNewlyCreatedDBObject(copiedEntity, true);
                    transManager.Commit();

                    Logger.Info($"实体复制成功 - 原实体: {entityId}, 新实体: {copyId}");
                    return copyId;
                }
            }, ObjectId.Null);
        }

        /// <summary>
        /// 安全的实体旋转操作
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="basePoint">旋转基点</param>
        /// <param name="angle">旋转角度（弧度）</param>
        /// <returns>旋转操作是否成功</returns>
        public static bool RotateEntitySafe(this ObjectId entityId, Point3d basePoint, double angle)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                if (entityId.IsNull || entityId.IsErased)
                {
                    CADExceptionHandler.ThrowEntityException("旋转实体", entityId, "实体ID无效或已被删除");
                }

                if (Math.Abs(angle) < 1e-10) // 角度太小，无需旋转
                {
                    Logger.Info("旋转角度过小，无需操作");
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
                        Logger.Info($"实体旋转成功 - EntityId: {entityId}, 角度: {angle * 180 / Math.PI}度");
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

        /// <summary>
        /// 安全的实体缩放操作
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="basePoint">缩放基点</param>
        /// <param name="scaleFactor">缩放系数</param>
        /// <returns>缩放操作是否成功</returns>
        public static bool ScaleEntitySafe(this ObjectId entityId, Point3d basePoint, double scaleFactor)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                if (entityId.IsNull || entityId.IsErased)
                {
                    CADExceptionHandler.ThrowEntityException("缩放实体", entityId, "实体ID无效或已被删除");
                }

                if (scaleFactor <= 0)
                {
                    CADExceptionHandler.ThrowEntityException("缩放实体", entityId, "缩放系数必须大于0");
                }

                if (Math.Abs(scaleFactor - 1.0) < 1e-10) // 缩放系数接近1，无需缩放
                {
                    Logger.Info("缩放系数接近1，无需操作");
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
                        Logger.Info($"实体缩放成功 - EntityId: {entityId}, 缩放系数: {scaleFactor}");
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

        /// <summary>
        /// 安全的实体偏移操作
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="offsetDistance">偏移距离</param>
        /// <returns>偏移后的实体ID集合</returns>
        public static ObjectIdCollection OffsetEntitySafe(this ObjectId entityId, double offsetDistance)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                if (entityId.IsNull || entityId.IsErased)
                {
                    CADExceptionHandler.ThrowEntityException("偏移实体", entityId, "实体ID无效或已被删除");
                }

                if (Math.Abs(offsetDistance) < 1e-10)
                {
                    Logger.Info("偏移距离过小，无需操作");
                    return new ObjectIdCollection();
                }

                using (var transManager = TransactionManagerFactory.Create(entityId.Database))
                {
                    var entity = transManager.GetObject<Entity>(entityId, OpenMode.ForRead);

                    if (!(entity is Curve curve))
                    {
                        CADExceptionHandler.ThrowEntityException("偏移实体", entityId, "只能偏移曲线类型的实体");
                        return new ObjectIdCollection();
                    }

                    var offsetCurves = curve.GetOffsetCurves(offsetDistance);
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
                        Logger.Info($"实体偏移成功 - 原实体: {entityId}, 生成实体数: {resultIds.Count}");
                    }

                    return resultIds;
                }
            }, new ObjectIdCollection());
        }

        /// <summary>
        /// 验证实体变换的有效性
        /// </summary>
        /// <param name="entity">要验证的实体</param>
        /// <returns>如果变换有效返回true</returns>
        private static bool ValidateEntityTransform(Entity entity)
        {
            try
            {
                // 检查实体是否仍然有效
                var bounds = entity.GeometricExtents;
                return bounds.MinPoint.IsValidCoordinate() && bounds.MaxPoint.IsValidCoordinate();
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Point3d扩展方法
    /// </summary>
    public static class Point3dExtensions
    {
        /// <summary>
        /// 检查坐标是否有效
        /// </summary>
        /// <param name="point">点坐标</param>
        /// <returns>如果坐标有效返回true</returns>
        public static bool IsValidCoordinate(this Point3d point)
        {
            return !double.IsNaN(point.X) && !double.IsNaN(point.Y) && !double.IsNaN(point.Z) &&
                   !double.IsInfinity(point.X) && !double.IsInfinity(point.Y) && !double.IsInfinity(point.Z);
        }

        /// <summary>
        /// 检查两点是否足够接近
        /// </summary>
        /// <param name="point1">第一个点</param>
        /// <param name="point2">第二个点</param>
        /// <param name="tolerance">容差</param>
        /// <returns>如果两点足够接近返回true</returns>
        public static bool IsEqualTo(this Point3d point1, Point3d point2, double tolerance = 1e-10)
        {
            return point1.DistanceTo(point2) <= tolerance;
        }
    }
}