namespace DotNetARX.Services
{
    /// <summary>
    /// 几何工具服务实现
    /// </summary>
    public partial class GeometryService : IGeometryService
    {
        private readonly IEventBus _eventBus;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly ILogger _logger;

        public GeometryService(
            IEventBus eventBus = null,
            IPerformanceMonitor performanceMonitor = null,
            ILogger logger = null)
        {
            _eventBus = eventBus ?? ServiceContainer.Instance.GetService<IEventBus>();
            _performanceMonitor = performanceMonitor ?? ServiceContainer.Instance.GetService<IPerformanceMonitor>();
            _logger = logger ?? ServiceContainer.Instance.GetService<ILogger>();
        }

        /// <summary>
        /// 计算两点间距离
        /// </summary>
        public double CalculateDistance(Point3d pt1, Point3d pt2)
        {
            using var operation = _performanceMonitor?.StartOperation("CalculateDistance");

            try
            {
                double distance = pt1.DistanceTo(pt2);
                _logger?.Debug($"计算距离: {pt1} 到 {pt2} = {distance}");
                return distance;
            }
            catch (Exception ex)
            {
                _logger?.Error($"计算距离失败: {ex.Message}", ex);
                throw new GeometryOperationException($"计算距离失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 计算角度
        /// </summary>
        public double CalculateAngle(Point3d pt1, Point3d pt2, Point3d pt3)
        {
            using var operation = _performanceMonitor?.StartOperation("CalculateAngle");

            try
            {
                // 创建两个向量
                Vector3d vector1 = pt1.GetVectorTo(pt2);
                Vector3d vector2 = pt2.GetVectorTo(pt3);

                // 计算角度
                double angle = vector1.GetAngleTo(vector2);

                _logger?.Debug($"计算角度: {pt1}-{pt2}-{pt3} = {angle} 弧度 ({angle * 180 / Math.PI} 度)");
                return angle;
            }
            catch (Exception ex)
            {
                _logger?.Error($"计算角度失败: {ex.Message}", ex);
                throw new GeometryOperationException($"计算角度失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 检查点是否在多边形内
        /// </summary>
        public bool IsPointInPolygon(Point3d point, IEnumerable<Point3d> polygon)
        {
            using var operation = _performanceMonitor?.StartOperation("IsPointInPolygon");

            try
            {
                if (polygon == null)
                    throw new ArgumentNullException(nameof(polygon));

                var points = polygon.ToList();
                if (points.Count < 3)
                    throw new ArgumentException("多边形至少需要3个点");

                // 使用射线法判断点是否在多边形内
                bool inside = false;
                int j = points.Count - 1;

                for (int i = 0; i < points.Count; i++)
                {
                    double xi = points[i].X, yi = points[i].Y;
                    double xj = points[j].X, yj = points[j].Y;

                    if (yi > point.Y != yj > point.Y &&
                        point.X < (xj - xi) * (point.Y - yi) / (yj - yi) + xi)
                    {
                        inside = !inside;
                    }
                    j = i;
                }

                _logger?.Debug($"点在多边形内检查: {point} = {inside}");
                return inside;
            }
            catch (Exception ex)
            {
                _logger?.Error($"检查点是否在多边形内失败: {ex.Message}", ex);
                throw new GeometryOperationException($"检查点是否在多边形内失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取边界框
        /// </summary>
        public Extents3d GetBoundingBox(IEnumerable<ObjectId> entityIds)
        {
            using var operation = _performanceMonitor?.StartOperation("GetBoundingBox");

            try
            {
                if (entityIds == null)
                    throw new ArgumentNullException(nameof(entityIds));

                var ids = entityIds.Where(id => !id.IsNull && id.IsValid).ToList();
                if (!ids.Any())
                    throw new ArgumentException("没有有效的实体ID");

                var database = ids.First().Database;
                Extents3d? totalExtents = null;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    foreach (var id in ids)
                    {
                        try
                        {
                            var entity = transManager.GetObject<Entity>(id, OpenMode.ForRead);
                            if (entity != null)
                            {
                                var bounds = entity.GeometricExtents;
                                if (totalExtents.HasValue)
                                {
                                    totalExtents = new Extents3d(
                                        new Point3d(
                                            Math.Min(totalExtents.Value.MinPoint.X, bounds.MinPoint.X),
                                            Math.Min(totalExtents.Value.MinPoint.Y, bounds.MinPoint.Y),
                                            Math.Min(totalExtents.Value.MinPoint.Z, bounds.MinPoint.Z)
                                        ),
                                        new Point3d(
                                            Math.Max(totalExtents.Value.MaxPoint.X, bounds.MaxPoint.X),
                                            Math.Max(totalExtents.Value.MaxPoint.Y, bounds.MaxPoint.Y),
                                            Math.Max(totalExtents.Value.MaxPoint.Z, bounds.MaxPoint.Z)
                                        )
                                    );
                                }
                                else
                                {
                                    totalExtents = bounds;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.Warning($"获取实体 {id} 的边界框失败: {ex.Message}");
                        }
                    }

                    transManager.Commit();
                }

                if (!totalExtents.HasValue)
                    throw new InvalidOperationException("无法获取任何实体的边界框");

                _logger?.Debug($"获取边界框成功: {totalExtents.Value.MinPoint} 到 {totalExtents.Value.MaxPoint}");
                return totalExtents.Value;
            }
            catch (Exception ex)
            {
                _logger?.Error($"获取边界框失败: {ex.Message}", ex);
                throw new GeometryOperationException($"获取边界框失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取实体边界
        /// </summary>
        public Extents3d GetEntityBounds(ObjectId entityId)
        {
            using var operation = _performanceMonitor?.StartOperation("GetEntityBounds");

            try
            {
                if (entityId.IsNull || !entityId.IsValid)
                    throw new ArgumentException("实体ID无效");

                var database = entityId.Database;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var entity = transManager.GetObject<Entity>(entityId, OpenMode.ForRead);
                    if (entity == null)
                        throw new InvalidOperationException($"无法获取实体: {entityId}");

                    var bounds = entity.GeometricExtents;
                    transManager.Commit();

                    _logger?.Debug($"获取实体边界成功: {bounds.MinPoint} 到 {bounds.MaxPoint}");
                    return bounds;
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"获取实体边界失败: {ex.Message}", ex);
                throw new GeometryOperationException($"获取实体边界失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 计算多边形面积
        /// </summary>
        public double CalculatePolygonArea(IEnumerable<Point2d> vertices)
        {
            using var operation = _performanceMonitor?.StartOperation("CalculatePolygonArea");

            try
            {
                if (vertices == null)
                    throw new ArgumentNullException(nameof(vertices));

                var points = vertices.ToList();
                if (points.Count < 3)
                    throw new ArgumentException("多边形至少需要3个顶点");

                // 使用鞋带公式计算面积
                double area = 0.0;
                int j = points.Count - 1;

                for (int i = 0; i < points.Count; i++)
                {
                    area += (points[j].X + points[i].X) * (points[j].Y - points[i].Y);
                    j = i;
                }

                area = Math.Abs(area) / 2.0;
                _logger?.Debug($"计算多边形面积: {area}");
                return area;
            }
            catch (Exception ex)
            {
                _logger?.Error($"计算多边形面积失败: {ex.Message}", ex);
                throw new GeometryOperationException($"计算多边形面积失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 检查坐标是否有效
        /// </summary>
        public bool IsValidCoordinate(Point3d point)
        {
            try
            {
                return !double.IsNaN(point.X) && !double.IsNaN(point.Y) && !double.IsNaN(point.Z) &&
                       !double.IsInfinity(point.X) && !double.IsInfinity(point.Y) && !double.IsInfinity(point.Z);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查两点是否相等（在指定容差内）
        /// </summary>
        public bool IsEqualTo(Point3d point1, Point3d point2, double tolerance = 1e-10)
        {
            try
            {
                return point1.IsEqualTo(point2, new Tolerance(tolerance, tolerance));
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// 几何操作异常
    /// </summary>
    public class GeometryOperationException : DotNetARXException
    {
        public GeometryOperationException(string message) : base(message)
        {
        }

        public GeometryOperationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}