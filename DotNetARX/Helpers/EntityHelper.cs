namespace DotNetARX.Helpers
{
    /// <summary>
    /// 实体操作辅助工具类
    /// 提供便捷的实体创建、查询和操作方法
    /// </summary>
    public static class EntityHelper
    {
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(EntityHelper));

        #region 实体创建辅助方法

        /// <summary>
        /// 快速创建矩形
        /// </summary>
        /// <param name="corner1">矩形对角点1</param>
        /// <param name="corner2">矩形对角点2</param>
        /// <returns>多段线实体ID</returns>
        public static ObjectId CreateRectangle(Point3d corner1, Point3d corner2)
        {
            var points = new[]
            {
                new Point3d(corner1.X, corner1.Y, 0),
                new Point3d(corner2.X, corner1.Y, 0),
                new Point3d(corner2.X, corner2.Y, 0),
                new Point3d(corner1.X, corner2.Y, 0)
            };

            return CAD.CreatePolyline(points, true);
        }

        /// <summary>
        /// 创建多边形（正多边形）
        /// </summary>
        /// <param name="center">中心点</param>
        /// <param name="radius">半径</param>
        /// <param name="sides">边数</param>
        /// <param name="startAngle">起始角度（弧度）</param>
        /// <returns>多段线实体ID</returns>
        public static ObjectId CreatePolygon(Point3d center, double radius, int sides, double startAngle = 0)
        {
            if (sides < 3) throw new ArgumentException("边数必须大于等于3", nameof(sides));
            if (radius <= 0) throw new ArgumentException("半径必须大于0", nameof(radius));

            var points = new List<Point3d>();
            var angleStep = 2 * Math.PI / sides;

            for (int i = 0; i < sides; i++)
            {
                var angle = startAngle + i * angleStep;
                var x = center.X + radius * Math.Cos(angle);
                var y = center.Y + radius * Math.Sin(angle);
                points.Add(new Point3d(x, y, center.Z));
            }

            return CAD.CreatePolyline(points, true);
        }

        /// <summary>
        /// 创建椭圆
        /// </summary>
        /// <param name="center">椭圆中心</param>
        /// <param name="majorAxis">长轴向量</param>
        /// <param name="radiusRatio">短轴与长轴的比例</param>
        /// <returns>椭圆实体ID</returns>
        public static ObjectId CreateEllipse(Point3d center, Vector3d majorAxis, double radiusRatio)
        {
            return PerformanceEngine.Execute("CreateEllipse", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var ellipse = new Ellipse(center, Vector3d.ZAxis, majorAxis, radiusRatio, 0, 2 * Math.PI);
                    return CAD.AddToCurrentSpace(ellipse);
                })
            );
        }

        /// <summary>
        /// 创建样条曲线
        /// </summary>
        /// <param name="controlPoints">控制点</param>
        /// <param name="degree">次数</param>
        /// <returns>样条曲线实体ID</returns>
        public static ObjectId CreateSpline(IEnumerable<Point3d> controlPoints, int degree = 3)
        {
            var points = controlPoints?.ToArray();
            if (points == null || points.Length < 2)
                throw new ArgumentException("至少需要2个控制点", nameof(controlPoints));

            return PerformanceEngine.Execute("CreateSpline", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var pointCollection = new Point3dCollection();
                    foreach (var point in points)
                    {
                        pointCollection.Add(point);
                    }
                    var spline = new Spline(pointCollection, degree, 0);
                    return CAD.AddToCurrentSpace(spline);
                })
            );
        }

        #endregion 实体创建辅助方法

        #region 实体查询辅助方法

        /// <summary>
        /// 获取实体的边界框
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <returns>边界框信息</returns>
        public static ArxBoundingBox? GetBoundingBox(ObjectId entityId)
        {
            var entity = entityId.TryGetEntity<Entity>();
            if (entity == null) return null;

            try
            {
                var extents = entity.GeometricExtents;
                return new ArxBoundingBox(extents.MinPoint, extents.MaxPoint);
            }
            catch (Exception ex)
            {
                _logger.Debug($"获取实体边界框失败: {entityId}, 错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取多个实体的总边界框
        /// </summary>
        /// <param name="entityIds">实体ID集合</param>
        /// <returns>总边界框</returns>
        public static ArxBoundingBox? GetCombinedBoundingBox(IEnumerable<ObjectId> entityIds)
        {
            var ids = entityIds?.ToList();
            if (ids == null || !ids.Any()) return null;

            var boxes = ids.Select(GetBoundingBox).Where(box => box.HasValue).Select(box => box.Value).ToList();
            if (!boxes.Any()) return null;

            var minX = boxes.Min(b => b.MinPoint.X);
            var minY = boxes.Min(b => b.MinPoint.Y);
            var minZ = boxes.Min(b => b.MinPoint.Z);
            var maxX = boxes.Max(b => b.MaxPoint.X);
            var maxY = boxes.Max(b => b.MaxPoint.Y);
            var maxZ = boxes.Max(b => b.MaxPoint.Z);

            return new ArxBoundingBox(new Point3d(minX, minY, minZ), new Point3d(maxX, maxY, maxZ));
        }

        /// <summary>
        /// 计算两点之间的距离
        /// </summary>
        /// <param name="point1">点1</param>
        /// <param name="point2">点2</param>
        /// <returns>距离</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Distance(Point3d point1, Point3d point2)
        {
            return point1.DistanceTo(point2);
        }

        /// <summary>
        /// 计算点到直线的距离
        /// </summary>
        /// <param name="point">点</param>
        /// <param name="lineStart">直线起点</param>
        /// <param name="lineEnd">直线终点</param>
        /// <returns>距离</returns>
        public static double DistanceToLine(Point3d point, Point3d lineStart, Point3d lineEnd)
        {
            var line = new Line3d(lineStart, lineEnd);
            return line.GetDistanceTo(point);
        }

        /// <summary>
        /// 检查点是否在多边形内（2D）
        /// </summary>
        /// <param name="point">检查的点</param>
        /// <param name="polygon">多边形顶点</param>
        /// <returns>是否在多边形内</returns>
        public static bool IsPointInPolygon(Point3d point, IEnumerable<Point3d> polygon)
        {
            var vertices = polygon?.ToArray();
            if (vertices == null || vertices.Length < 3) return false;

            var x = point.X;
            var y = point.Y;
            var inside = false;

            for (int i = 0, j = vertices.Length - 1; i < vertices.Length; j = i++)
            {
                var xi = vertices[i].X;
                var yi = vertices[i].Y;
                var xj = vertices[j].X;
                var yj = vertices[j].Y;

                if (((yi > y) != (yj > y)) && (x < (xj - xi) * (y - yi) / (yj - yi) + xi))
                    inside = !inside;
            }

            return inside;
        }

        #endregion 实体查询辅助方法

        #region 实体分析方法

        /// <summary>
        /// 获取实体详细信息
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <returns>实体信息</returns>
        public static ArxEntityInfo GetEntityInfo(ObjectId entityId)
        {
            var entity = entityId.TryGetEntity<Entity>();

            var info = new ArxEntityInfo
            {
                ObjectId = entityId,
                Handle = entityId.Handle,
                IsNull = entityId.IsNull,
                IsValid = entityId.IsValid,
                IsErased = entityId.IsErased
            };

            if (entity != null)
            {
                info.EntityType = entity.GetType().Name;
                info.LayerName = entity.Layer;
                info.Color = entity.Color;
                info.LineWeight = entity.LineWeight;
                info.LinetypeId = entity.LinetypeId;
                info.BoundingBox = GetBoundingBox(entityId);

                // 特定类型的额外信息
                switch (entity)
                {
                    case Line line:
                        info.AdditionalProperties = new Dictionary<string, object>
                        {
                            ["StartPoint"] = line.StartPoint,
                            ["EndPoint"] = line.EndPoint,
                            ["Length"] = line.Length,
                            ["Angle"] = line.Angle
                        };
                        break;

                    case Circle circle:
                        info.AdditionalProperties = new Dictionary<string, object>
                        {
                            ["Center"] = circle.Center,
                            ["Radius"] = circle.Radius,
                            ["Area"] = Math.PI * circle.Radius * circle.Radius,
                            ["Circumference"] = 2 * Math.PI * circle.Radius
                        };
                        break;

                    case Arc arc:
                        info.AdditionalProperties = new Dictionary<string, object>
                        {
                            ["Center"] = arc.Center,
                            ["Radius"] = arc.Radius,
                            ["StartAngle"] = arc.StartAngle,
                            ["EndAngle"] = arc.EndAngle,
                            ["TotalAngle"] = arc.TotalAngle,
                            ["Length"] = arc.Length
                        };
                        break;

                    case DBText text:
                        info.AdditionalProperties = new Dictionary<string, object>
                        {
                            ["TextString"] = text.TextString,
                            ["Position"] = text.Position,
                            ["Height"] = text.Height,
                            ["Rotation"] = text.Rotation,
                            ["WidthFactor"] = text.WidthFactor
                        };
                        break;
                }
            }

            return info;
        }

        /// <summary>
        /// 批量获取实体信息
        /// </summary>
        /// <param name="entityIds">实体ID集合</param>
        /// <returns>实体信息集合</returns>
        public static List<ArxEntityInfo> GetEntitiesInfo(IEnumerable<ObjectId> entityIds)
        {
            return entityIds?.Select(GetEntityInfo).ToList() ?? new List<ArxEntityInfo>();
        }

        /// <summary>
        /// 统计实体类型分布
        /// </summary>
        /// <param name="entityIds">实体ID集合</param>
        /// <returns>类型统计字典</returns>
        public static Dictionary<string, int> GetEntityTypeStatistics(IEnumerable<ObjectId> entityIds)
        {
            var stats = new Dictionary<string, int>();

            foreach (var id in entityIds ?? Enumerable.Empty<ObjectId>())
            {
                var typeName = id.GetEntityTypeName();
                stats[typeName] = stats.TryGetValue(typeName, out var count) ? count + 1 : 1;
            }

            return stats;
        }

        #endregion 实体分析方法

        #region 实体变换辅助方法

        /// <summary>
        /// 围绕指定点旋转实体指定角度
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="center">旋转中心</param>
        /// <param name="degrees">旋转角度（度）</param>
        /// <returns>操作是否成功</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RotateByDegrees(ObjectId entityId, Point3d center, double degrees)
        {
            return CAD.RotateEntity(entityId, center, degrees * Math.PI / 180.0);
        }

        /// <summary>
        /// 镜像实体
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="mirrorStart">镜像轴起点</param>
        /// <param name="mirrorEnd">镜像轴终点</param>
        /// <param name="eraseSource">是否删除原实体</param>
        /// <returns>镜像后的实体ID</returns>
        public static ObjectId Mirror(ObjectId entityId, Point3d mirrorStart, Point3d mirrorEnd, bool eraseSource = false)
        {
            return PerformanceEngine.Execute("Mirror", () =>
                AutoCADContext.ExecuteSafely(() =>
                {
                    var context = AutoCADContext.Current;
                    var entity = context.GetObject<Entity>(entityId, OpenMode.ForRead);
                    if (entity == null) return ObjectId.Null;

                    // 计算镜像矩阵
                    var mirrorLine = new Line3d(mirrorStart, mirrorEnd);
                    var mirrorPlane = new Plane(mirrorStart, mirrorLine.Direction.CrossProduct(Vector3d.ZAxis));
                    var mirrorMatrix = Matrix3d.Mirroring(mirrorPlane);

                    var mirrored = entity.GetTransformedCopy(mirrorMatrix);
                    var modelSpace = context.GetObject<BlockTableRecord>(
                        context.Database.GetModelSpaceId(), OpenMode.ForWrite);

                    var mirroredId = modelSpace.AppendEntity(mirrored);
                    context.Transaction.AddNewlyCreatedDBObject(mirrored, true);

                    if (eraseSource)
                    {
                        entity.UpgradeOpen();
                        entity.Erase();
                    }

                    return mirroredId;
                })
            );
        }

        /// <summary>
        /// 阵列实体（矩形阵列）
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="rows">行数</param>
        /// <param name="columns">列数</param>
        /// <param name="rowSpacing">行间距</param>
        /// <param name="columnSpacing">列间距</param>
        /// <returns>阵列生成的实体ID集合</returns>
        public static List<ObjectId> ArrayRectangular(ObjectId entityId, int rows, int columns,
    double rowSpacing, double columnSpacing)
        {
            var result = new List<ObjectId>();

            if (rows <= 0 || columns <= 0) return result;

            return PerformanceEngine.Execute("ArrayRectangular", () =>
            {
                AutoCADContext.ExecuteBatch(context =>
                {
                    var entity = context.GetObject<Entity>(entityId, OpenMode.ForRead);
                    if (entity == null)
                        return;

                    var modelSpace = context.GetObject<BlockTableRecord>(
                        context.Database.GetModelSpaceId(), OpenMode.ForWrite);

                    for (int i = 0; i < rows; i++)
                    {
                        for (int j = 0; j < columns; j++)
                        {
                            if (i == 0 && j == 0) continue; // 跳过原始实体

                            var displacement = new Vector3d(j * columnSpacing, i * rowSpacing, 0);
                            var copy = entity.GetTransformedCopy(Matrix3d.Displacement(displacement));

                            var copyId = modelSpace.AppendEntity(copy);
                            context.Transaction.AddNewlyCreatedDBObject(copy, true);
                            result.Add(copyId);
                        }
                    }
                });
                return result;
            });
        }

        #endregion 实体变换辅助方法
    }

    /// <summary>
    /// 实体信息结构体 - 避免与AutoCAD原生类型冲突
    /// </summary>
    public struct ArxEntityInfo
    {
        public ObjectId ObjectId { get; set; }
        public Handle Handle { get; set; }
        public bool IsNull { get; set; }
        public bool IsValid { get; set; }
        public bool IsErased { get; set; }
        public string EntityType { get; set; }
        public string LayerName { get; set; }
        public Color Color { get; set; }
        public LineWeight LineWeight { get; set; }
        public ObjectId LinetypeId { get; set; }
        public ArxBoundingBox? BoundingBox { get; set; }
        public Dictionary<string, object> AdditionalProperties { get; set; }

        public override string ToString()
        {
            return $"{EntityType} on {LayerName} [{Handle}]";
        }
    }
}