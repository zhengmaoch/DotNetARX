namespace DotNetARX
{
    /// <summary>
    /// CAD 统一API - 实体操作部分
    /// </summary>
    public static partial class CAD
    {
        #region 实体绘制 - 智能优化

        /// <summary>
        /// 绘制直线 - 高性能实现
        /// </summary>
        /// <param name="startPoint">起点</param>
        /// <param name="endPoint">终点</param>
        /// <returns>实体ID</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectId CreateLine(Point3d startPoint, Point3d endPoint)
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("CreateLine", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var line = new Line(startPoint, endPoint);
                    return AddToCurrentSpace(line);
                })
            );
        }

        /// <summary>
        /// 绘制直线（简化方法名）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectId Line(Point3d startPoint, Point3d endPoint) => CreateLine(startPoint, endPoint);

        /// <summary>
        /// 绘制圆 - 智能缓存
        /// </summary>
        /// <param name="center">圆心</param>
        /// <param name="radius">半径</param>
        /// <returns>实体ID</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectId CreateCircle(Point3d center, double radius)
        {
            EnsureInitialized();

            if (radius <= 0)
                throw new ArgumentException("半径必须大于0", nameof(radius));

            return PerformanceEngine.Execute("CreateCircle", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var circle = new Circle(center, Vector3d.ZAxis, radius);
                    return AddToCurrentSpace(circle);
                })
            );
        }

        /// <summary>
        /// 绘制圆（简化方法名）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectId Circle(Point3d center, double radius) => CreateCircle(center, radius);

        /// <summary>
        /// 绘制圆弧
        /// </summary>
        /// <param name="center">圆心</param>
        /// <param name="radius">半径</param>
        /// <param name="startAngle">起始角度（弧度）</param>
        /// <param name="endAngle">结束角度（弧度）</param>
        /// <returns>实体ID</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectId CreateArc(Point3d center, double radius, double startAngle, double endAngle)
        {
            EnsureInitialized();

            if (radius <= 0)
                throw new ArgumentException("半径必须大于0", nameof(radius));

            return PerformanceEngine.Execute("CreateArc", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var arc = new Arc(center, radius, startAngle, endAngle);
                    return AddToCurrentSpace(arc);
                })
            );
        }

        /// <summary>
        /// 绘制圆弧（简化方法名）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectId Arc(Point3d center, double radius, double startAngle, double endAngle)
            => CreateArc(center, radius, startAngle, endAngle);

        /// <summary>
        /// 绘制文字
        /// </summary>
        /// <param name="text">文字内容</param>
        /// <param name="position">位置</param>
        /// <param name="height">文字高度</param>
        /// <param name="rotation">旋转角度（弧度）</param>
        /// <returns>实体ID</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectId CreateText(string text, Point3d position, double height, double rotation = 0)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("文字内容不能为空", nameof(text));

            if (height <= 0)
                throw new ArgumentException("文字高度必须大于0", nameof(height));

            return PerformanceEngine.Execute("CreateText", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var dbText = new DBText
                    {
                        TextString = text,
                        Position = position,
                        Height = height,
                        Rotation = rotation
                    };
                    return AddToCurrentSpace(dbText);
                })
            );
        }

        /// <summary>
        /// 绘制文字（简化方法名）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectId Text(string text, Point3d position, double height, double rotation = 0)
            => CreateText(text, position, height, rotation);

        /// <summary>
        /// 绘制多段线
        /// </summary>
        /// <param name="points">顶点集合</param>
        /// <param name="closed">是否闭合</param>
        /// <returns>实体ID</returns>
        public static ObjectId CreatePolyline(IEnumerable<Point3d> points, bool closed = false)
        {
            EnsureInitialized();

            var pointList = points?.ToList();
            if (pointList == null || pointList.Count < 2)
                throw new ArgumentException("至少需要2个点", nameof(points));

            return PerformanceEngine.Execute("CreatePolyline", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var polyline = new Polyline();

                    for (int i = 0; i < pointList.Count; i++)
                    {
                        var point2d = new Point2d(pointList[i].X, pointList[i].Y);
                        polyline.AddVertexAt(i, point2d, 0, 0, 0);
                    }

                    polyline.Closed = closed;
                    return AddToCurrentSpace(polyline);
                })
            );
        }

        /// <summary>
        /// 绘制多段线（简化方法名）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectId Polyline(IEnumerable<Point3d> points, bool closed = false)
            => CreatePolyline(points, closed);

        /// <summary>
        /// 批量绘制实体 - 智能批处理优化
        /// </summary>
        /// <param name="entities">实体集合</param>
        /// <returns>实体ID集合</returns>
        public static List<ObjectId> CreateEntities(IEnumerable<Entity> entities)
        {
            EnsureInitialized();

            var entityList = entities?.ToList();
            if (entityList == null || !entityList.Any())
                return new List<ObjectId>();

            return PerformanceEngine.Execute("CreateEntities", () =>
            {
                // The lambda passed to ExecuteBatch must return List<ObjectId>
                return AutoCADContext.ExecuteBatch(context =>
                {
                    var ids = new List<ObjectId>();
                    var currentSpace = context.GetObject<BlockTableRecord>(
                        context.Database.GetCurrentSpaceId(), OpenMode.ForWrite);

                    foreach (var entity in entityList)
                    {
                        var id = currentSpace.AppendEntity(entity);
                        context.Transaction.AddNewlyCreatedDBObject(entity, true);
                        ids.Add(id);
                    }

                    return ids;
                });
            });
        }

        /// <summary>
        /// 批量绘制（简化方法名）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<ObjectId> Draw(IEnumerable<Entity> entities) => CreateEntities(entities);

        #endregion 实体绘制 - 智能优化

        #region 实体变换 - 智能优化

        /// <summary>
        /// 移动实体 - 智能路径选择，零配置高性能
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="fromPoint">源点</param>
        /// <param name="toPoint">目标点</param>
        /// <returns>操作是否成功</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool MoveEntity(ObjectId entityId, Point3d fromPoint, Point3d toPoint)
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("MoveEntity", () =>
            {
                return AutoCADContext.ExecuteSafely(() =>
                {
                    // 快速退出检查
                    if (fromPoint.IsEqualTo(toPoint, 1e-10))
                        return true;

                    var context = AutoCADContext.Current;
                    var entity = context.GetObject<Entity>(entityId, OpenMode.ForWrite);
                    if (entity == null) return false;

                    entity.TransformBy(Matrix3d.Displacement(toPoint - fromPoint));
                    return true;
                });
            });
        }

        /// <summary>
        /// 移动实体（简化方法名）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Move(ObjectId entityId, Point3d fromPoint, Point3d toPoint)
            => MoveEntity(entityId, fromPoint, toPoint);

        /// <summary>
        /// 复制实体 - 自动优化
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="fromPoint">源点</param>
        /// <param name="toPoint">目标点</param>
        /// <returns>复制的实体ID</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectId CopyEntity(ObjectId entityId, Point3d fromPoint, Point3d toPoint)
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("CopyEntity", () =>
            {
                return AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var original = context.GetObject<Entity>(entityId, OpenMode.ForRead);
                    if (original == null) return ObjectId.Null;

                    var displacement = toPoint - fromPoint;
                    var copy = original.GetTransformedCopy(Matrix3d.Displacement(displacement));

                    var modelSpace = context.GetObject<BlockTableRecord>(
                        context.Database.GetModelSpaceId(), OpenMode.ForWrite);

                    var copyId = modelSpace.AppendEntity(copy);
                    context.Transaction.AddNewlyCreatedDBObject(copy, true);

                    return copyId;
                });
            });
        }

        /// <summary>
        /// 复制实体（简化方法名）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectId Copy(ObjectId entityId, Point3d fromPoint, Point3d toPoint)
            => CopyEntity(entityId, fromPoint, toPoint);

        /// <summary>
        /// 旋转实体
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="basePoint">旋转基点</param>
        /// <param name="angle">旋转角度（弧度）</param>
        /// <returns>操作是否成功</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RotateEntity(ObjectId entityId, Point3d basePoint, double angle)
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("RotateEntity", () =>
            {
                return AutoCADContext.ExecuteSafely(() =>
                {
                    if (Math.Abs(angle) < 1e-10) return true;

                    var context = AutoCADContext.Current;
                    var entity = context.GetObject<Entity>(entityId, OpenMode.ForWrite);
                    if (entity == null) return false;

                    entity.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, basePoint));
                    return true;
                });
            });
        }

        /// <summary>
        /// 旋转实体（简化方法名）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Rotate(ObjectId entityId, Point3d basePoint, double angle)
            => RotateEntity(entityId, basePoint, angle);

        /// <summary>
        /// 缩放实体
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="basePoint">缩放基点</param>
        /// <param name="scaleFactor">缩放因子</param>
        /// <returns>操作是否成功</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ScaleEntity(ObjectId entityId, Point3d basePoint, double scaleFactor)
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("ScaleEntity", () =>
            {
                return AutoCADContext.ExecuteSafely(() =>
                {
                    if (scaleFactor <= 0) return false;
                    if (Math.Abs(scaleFactor - 1.0) < 1e-10) return true;

                    var context = AutoCADContext.Current;
                    var entity = context.GetObject<Entity>(entityId, OpenMode.ForWrite);
                    if (entity == null) return false;

                    entity.TransformBy(Matrix3d.Scaling(scaleFactor, basePoint));
                    return true;
                });
            });
        }

        /// <summary>
        /// 缩放实体（简化方法名）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Scale(ObjectId entityId, Point3d basePoint, double scaleFactor)
            => ScaleEntity(entityId, basePoint, scaleFactor);

        /// <summary>
        /// 批量移动实体 - 智能批处理优化
        /// </summary>
        /// <param name="operations">移动操作列表</param>
        /// <returns>成功操作的数量</returns>
        public static int MoveEntities(IEnumerable<(ObjectId Id, Point3d From, Point3d To)> operations)
        {
            EnsureInitialized();

            var ops = operations?.ToList();
            if (ops == null || ops.Count == 0) return 0;

            if (ops.Count == 1)
            {
                // 单个操作，直接调用
                var (id, from, to) = ops[0];
                return MoveEntity(id, from, to) ? 1 : 0;
            }

            // 智能批量操作优化
            return PerformanceEngine.Execute("MoveEntities", () =>
            {
                return AutoCADContext.ExecuteBatch(context =>
                {
                    int successCount = 0;

                    // 按位移向量分组优化，减少矩阵计算
                    var groups = ops.GroupBy(op => op.To - op.From);

                    foreach (var group in groups)
                    {
                        var moveMatrix = Matrix3d.Displacement(group.Key);

                        foreach (var (id, _, _) in group)
                        {
                            var entity = context.GetObject<Entity>(id, OpenMode.ForWrite);
                            if (entity != null)
                            {
                                entity.TransformBy(moveMatrix);
                                successCount++;
                            }
                        }
                    }

                    return successCount;
                });
            });
        }

        /// <summary>
        /// 批量移动（简化方法名）
        /// </summary>
        public static void Move(IEnumerable<(ObjectId Id, Point3d From, Point3d To)> operations)
        {
            MoveEntities(operations);
        }

        #endregion 实体变换 - 智能优化

        #region 实体删除

        /// <summary>
        /// 删除实体
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <returns>操作是否成功</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DeleteEntity(ObjectId entityId)
        {
            EnsureInitialized();

            return PerformanceEngine.Execute("DeleteEntity", () =>
            {
                return AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var entity = context.GetObject<Entity>(entityId, OpenMode.ForWrite);
                    if (entity == null) return false;

                    entity.Erase();
                    return true;
                });
            });
        }

        /// <summary>
        /// 删除实体（简化方法名）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Delete(ObjectId entityId) => DeleteEntity(entityId);

        /// <summary>
        /// 批量删除实体
        /// </summary>
        /// <param name="entityIds">实体ID集合</param>
        /// <returns>成功删除的数量</returns>
        public static int DeleteEntities(IEnumerable<ObjectId> entityIds)
        {
            EnsureInitialized();

            var ids = entityIds?.ToList();
            if (ids == null || !ids.Any()) return 0;

            return PerformanceEngine.Execute("DeleteEntities", () =>
            {
                return AutoCADContext.ExecuteBatch(context =>
                {
                    int successCount = 0;

                    foreach (var id in ids)
                    {
                        var entity = context.GetObject<Entity>(id, OpenMode.ForWrite);
                        if (entity != null)
                        {
                            entity.Erase();
                            successCount++;
                        }
                    }

                    return successCount;
                });
            });
        }

        #endregion 实体删除
    }
}