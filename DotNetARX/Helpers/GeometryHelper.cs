using System.Runtime.CompilerServices;

namespace DotNetARX.Helpers
{
    /// <summary>
    /// 几何计算辅助工具类
    /// 提供常用的几何计算和图形处理方法
    /// </summary>
    public static class GeometryHelper
    {
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(GeometryHelper));

        #region 角度转换

        /// <summary>
        /// 弧度转角度
        /// </summary>
        /// <param name="radians">弧度值</param>
        /// <returns>角度值</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double RadiansToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }

        /// <summary>
        /// 角度转弧度
        /// </summary>
        /// <param name="degrees">角度值</param>
        /// <returns>弧度值</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        /// <summary>
        /// 规范化角度到0-2π范围
        /// </summary>
        /// <param name="angle">角度（弧度）</param>
        /// <returns>规范化后的角度</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NormalizeAngle(double angle)
        {
            while (angle < 0) angle += 2 * Math.PI;
            while (angle >= 2 * Math.PI) angle -= 2 * Math.PI;
            return angle;
        }

        #endregion 角度转换

        #region 点操作

        /// <summary>
        /// 计算两点的中点
        /// </summary>
        /// <param name="point1">点1</param>
        /// <param name="point2">点2</param>
        /// <returns>中点</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point3d GetMidPoint(Point3d point1, Point3d point2)
        {
            return new Point3d(
                (point1.X + point2.X) / 2,
                (point1.Y + point2.Y) / 2,
                (point1.Z + point2.Z) / 2);
        }

        /// <summary>
        /// 计算多个点的重心
        /// </summary>
        /// <param name="points">点集合</param>
        /// <returns>重心点</returns>
        public static Point3d GetCentroid(IEnumerable<Point3d> points)
        {
            var pointList = points?.ToList();
            if (pointList == null || !pointList.Any())
                return Point3d.Origin;

            var sumX = pointList.Sum(p => p.X);
            var sumY = pointList.Sum(p => p.Y);
            var sumZ = pointList.Sum(p => p.Z);
            var count = pointList.Count;

            return new Point3d(sumX / count, sumY / count, sumZ / count);
        }

        /// <summary>
        /// 点绕另一点旋转
        /// </summary>
        /// <param name="point">要旋转的点</param>
        /// <param name="center">旋转中心</param>
        /// <param name="angle">旋转角度（弧度）</param>
        /// <returns>旋转后的点</returns>
        public static Point3d RotatePoint(Point3d point, Point3d center, double angle)
        {
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);
            var dx = point.X - center.X;
            var dy = point.Y - center.Y;

            return new Point3d(
                center.X + dx * cos - dy * sin,
                center.Y + dx * sin + dy * cos,
                point.Z);
        }

        /// <summary>
        /// 点沿向量偏移
        /// </summary>
        /// <param name="point">原点</param>
        /// <param name="vector">偏移向量</param>
        /// <returns>偏移后的点</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point3d OffsetPoint(Point3d point, Vector3d vector)
        {
            return point + vector;
        }

        /// <summary>
        /// 检查点是否近似相等
        /// </summary>
        /// <param name="point1">点1</param>
        /// <param name="point2">点2</param>
        /// <param name="tolerance">容差</param>
        /// <returns>是否近似相等</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ArePointsEqual(Point3d point1, Point3d point2, double tolerance = 1e-10)
        {
            return point1.IsEqualTo(point2, tolerance);
        }

        #endregion 点操作

        #region 向量操作

        /// <summary>
        /// 计算两个向量的夹角
        /// </summary>
        /// <param name="vector1">向量1</param>
        /// <param name="vector2">向量2</param>
        /// <returns>夹角（弧度）</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetAngleBetweenVectors(Vector3d vector1, Vector3d vector2)
        {
            return vector1.GetAngleTo(vector2);
        }

        /// <summary>
        /// 创建单位向量
        /// </summary>
        /// <param name="angle">角度（弧度）</param>
        /// <returns>单位向量</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d CreateUnitVector(double angle)
        {
            return new Vector3d(Math.Cos(angle), Math.Sin(angle), 0);
        }

        /// <summary>
        /// 向量投影到另一个向量
        /// </summary>
        /// <param name="vector">要投影的向量</param>
        /// <param name="targetVector">目标向量</param>
        /// <returns>投影向量</returns>
        public static Vector3d ProjectVector(Vector3d vector, Vector3d targetVector)
        {
            var unit = targetVector.GetNormal();
            var projection = vector.DotProduct(unit);
            return unit * projection;
        }

        #endregion 向量操作

        #region 直线操作

        /// <summary>
        /// 计算两条直线的交点（2D）
        /// </summary>
        /// <param name="line1Start">直线1起点</param>
        /// <param name="line1End">直线1终点</param>
        /// <param name="line2Start">直线2起点</param>
        /// <param name="line2End">直线2终点</param>
        /// <returns>交点，无交点时返回null</returns>
        public static Point3d? GetLineIntersection(Point3d line1Start, Point3d line1End,
            Point3d line2Start, Point3d line2End)
        {
            var x1 = line1Start.X; var y1 = line1Start.Y;
            var x2 = line1End.X; var y2 = line1End.Y;
            var x3 = line2Start.X; var y3 = line2Start.Y;
            var x4 = line2End.X; var y4 = line2End.Y;

            var denominator = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            if (Math.Abs(denominator) < 1e-10) return null; // 平行线

            var t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denominator;

            var intersectionX = x1 + t * (x2 - x1);
            var intersectionY = y1 + t * (y2 - y1);

            return new Point3d(intersectionX, intersectionY, 0);
        }

        /// <summary>
        /// 计算点到直线的垂足
        /// </summary>
        /// <param name="point">点</param>
        /// <param name="lineStart">直线起点</param>
        /// <param name="lineEnd">直线终点</param>
        /// <returns>垂足点</returns>
        public static Point3d GetPerpendicularFoot(Point3d point, Point3d lineStart, Point3d lineEnd)
        {
            var lineVector = lineEnd - lineStart;
            var pointVector = point - lineStart;

            var projection = pointVector.DotProduct(lineVector) / lineVector.DotProduct(lineVector);

            return lineStart + lineVector * projection;
        }

        /// <summary>
        /// 检查点是否在线段上
        /// </summary>
        /// <param name="point">检查的点</param>
        /// <param name="segmentStart">线段起点</param>
        /// <param name="segmentEnd">线段终点</param>
        /// <param name="tolerance">容差</param>
        /// <returns>是否在线段上</returns>
        public static bool IsPointOnSegment(Point3d point, Point3d segmentStart, Point3d segmentEnd,
            double tolerance = 1e-10)
        {
            var distance = EntityHelper.DistanceToLine(point, segmentStart, segmentEnd);
            if (distance > tolerance) return false;

            var segmentLength = segmentStart.DistanceTo(segmentEnd);
            var dist1 = point.DistanceTo(segmentStart);
            var dist2 = point.DistanceTo(segmentEnd);

            return Math.Abs(dist1 + dist2 - segmentLength) < tolerance;
        }

        #endregion 直线操作

        #region 圆形操作

        /// <summary>
        /// 计算直线与圆的交点
        /// </summary>
        /// <param name="lineStart">直线起点</param>
        /// <param name="lineEnd">直线终点</param>
        /// <param name="circleCenter">圆心</param>
        /// <param name="radius">半径</param>
        /// <returns>交点集合</returns>
        public static List<Point3d> GetLineCircleIntersections(Point3d lineStart, Point3d lineEnd,
            Point3d circleCenter, double radius)
        {
            var intersections = new List<Point3d>();

            var dx = lineEnd.X - lineStart.X;
            var dy = lineEnd.Y - lineStart.Y;
            var fx = lineStart.X - circleCenter.X;
            var fy = lineStart.Y - circleCenter.Y;

            var a = dx * dx + dy * dy;
            var b = 2 * (fx * dx + fy * dy);
            var c = fx * fx + fy * fy - radius * radius;

            var discriminant = b * b - 4 * a * c;

            if (discriminant < 0) return intersections; // 无交点

            if (Math.Abs(discriminant) < 1e-10) // 切点
            {
                var t = -b / (2 * a);
                var x = lineStart.X + t * dx;
                var y = lineStart.Y + t * dy;
                intersections.Add(new Point3d(x, y, 0));
            }
            else // 两个交点
            {
                var sqrtD = Math.Sqrt(discriminant);
                var t1 = (-b - sqrtD) / (2 * a);
                var t2 = (-b + sqrtD) / (2 * a);

                var x1 = lineStart.X + t1 * dx;
                var y1 = lineStart.Y + t1 * dy;
                intersections.Add(new Point3d(x1, y1, 0));

                var x2 = lineStart.X + t2 * dx;
                var y2 = lineStart.Y + t2 * dy;
                intersections.Add(new Point3d(x2, y2, 0));
            }

            return intersections;
        }

        /// <summary>
        /// 检查点是否在圆内
        /// </summary>
        /// <param name="point">检查的点</param>
        /// <param name="circleCenter">圆心</param>
        /// <param name="radius">半径</param>
        /// <returns>是否在圆内</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPointInCircle(Point3d point, Point3d circleCenter, double radius)
        {
            return point.DistanceTo(circleCenter) <= radius;
        }

        /// <summary>
        /// 计算圆上指定角度的点
        /// </summary>
        /// <param name="center">圆心</param>
        /// <param name="radius">半径</param>
        /// <param name="angle">角度（弧度）</param>
        /// <returns>圆上的点</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point3d GetPointOnCircle(Point3d center, double radius, double angle)
        {
            return new Point3d(
                center.X + radius * Math.Cos(angle),
                center.Y + radius * Math.Sin(angle),
                center.Z);
        }

        #endregion 圆形操作

        #region 多边形操作

        /// <summary>
        /// 计算多边形面积（2D）
        /// </summary>
        /// <param name="vertices">顶点集合</param>
        /// <returns>面积</returns>
        public static double CalculatePolygonArea(IEnumerable<Point3d> vertices)
        {
            var points = vertices?.ToArray();
            if (points == null || points.Length < 3) return 0;

            var area = 0.0;
            var count = points.Length;

            for (int i = 0; i < count; i++)
            {
                var j = (i + 1) % count;
                area += points[i].X * points[j].Y;
                area -= points[j].X * points[i].Y;
            }

            return Math.Abs(area) / 2.0;
        }

        /// <summary>
        /// 计算多边形周长
        /// </summary>
        /// <param name="vertices">顶点集合</param>
        /// <param name="closed">是否闭合</param>
        /// <returns>周长</returns>
        public static double CalculatePolygonPerimeter(IEnumerable<Point3d> vertices, bool closed = true)
        {
            var points = vertices?.ToArray();
            if (points == null || points.Length < 2) return 0;

            var perimeter = 0.0;
            var count = points.Length;

            for (int i = 0; i < count - 1; i++)
            {
                perimeter += points[i].DistanceTo(points[i + 1]);
            }

            if (closed && count > 2)
            {
                perimeter += points[count - 1].DistanceTo(points[0]);
            }

            return perimeter;
        }

        /// <summary>
        /// 检查多边形是否为凸多边形
        /// </summary>
        /// <param name="vertices">顶点集合</param>
        /// <returns>是否为凸多边形</returns>
        public static bool IsConvexPolygon(IEnumerable<Point3d> vertices)
        {
            var points = vertices?.ToArray();
            if (points == null || points.Length < 3) return false;

            var count = points.Length;
            var isPositive = false;
            var isNegative = false;

            for (int i = 0; i < count; i++)
            {
                var p1 = points[i];
                var p2 = points[(i + 1) % count];
                var p3 = points[(i + 2) % count];

                var cross = (p2.X - p1.X) * (p3.Y - p2.Y) - (p2.Y - p1.Y) * (p3.X - p2.X);

                if (cross > 0) isPositive = true;
                if (cross < 0) isNegative = true;

                if (isPositive && isNegative) return false;
            }

            return true;
        }

        #endregion 多边形操作

        #region 变换矩阵

        /// <summary>
        /// 创建平移矩阵
        /// </summary>
        /// <param name="displacement">位移向量</param>
        /// <returns>平移矩阵</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3d CreateTranslationMatrix(Vector3d displacement)
        {
            return Matrix3d.Displacement(displacement);
        }

        /// <summary>
        /// 创建旋转矩阵
        /// </summary>
        /// <param name="angle">旋转角度（弧度）</param>
        /// <param name="axis">旋转轴</param>
        /// <param name="center">旋转中心</param>
        /// <returns>旋转矩阵</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3d CreateRotationMatrix(double angle, Vector3d axis, Point3d center)
        {
            return Matrix3d.Rotation(angle, axis, center);
        }

        /// <summary>
        /// 创建缩放矩阵
        /// </summary>
        /// <param name="scaleFactor">缩放因子</param>
        /// <param name="center">缩放中心</param>
        /// <returns>缩放矩阵</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3d CreateScalingMatrix(double scaleFactor, Point3d center)
        {
            return Matrix3d.Scaling(scaleFactor, center);
        }

        /// <summary>
        /// 创建镜像矩阵
        /// </summary>
        /// <param name="mirrorPlane">镜像平面</param>
        /// <returns>镜像矩阵</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3d CreateMirrorMatrix(Plane mirrorPlane)
        {
            return Matrix3d.Mirroring(mirrorPlane);
        }

        #endregion 变换矩阵

        #region 几何工具

        /// <summary>
        /// 创建ArxGeometryInfo几何信息对象
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <returns>几何信息</returns>
        public static ArxGeometryInfo GetGeometryInfo(ObjectId entityId)
        {
            var entity = entityId.TryGetEntity();
            if (entity == null) return new ArxGeometryInfo { IsValid = false };

            var info = new ArxGeometryInfo
            {
                IsValid = true,
                EntityType = entity.GetType().Name,
                BoundingBox = EntityHelper.GetBoundingBox(entityId)
            };

            try
            {
                var extents = entity.GeometricExtents;
                info.HasGeometricExtents = true;

                switch (entity)
                {
                    case Line line:
                        info.Length = line.Length;
                        info.StartPoint = line.StartPoint;
                        info.EndPoint = line.EndPoint;
                        info.Angle = line.Angle;
                        break;

                    case Circle circle:
                        info.Area = Math.PI * circle.Radius * circle.Radius;
                        info.Perimeter = 2 * Math.PI * circle.Radius;
                        info.CenterPoint = circle.Center;
                        info.Radius = circle.Radius;
                        break;

                    case Arc arc:
                        info.Length = arc.Length;
                        info.CenterPoint = arc.Center;
                        info.Radius = arc.Radius;
                        info.StartAngle = arc.StartAngle;
                        info.EndAngle = arc.EndAngle;
                        break;

                    case Polyline polyline:
                        info.Length = polyline.Length;
                        info.Area = polyline.Area;
                        info.IsClosed = polyline.Closed;
                        break;
                }
            }
            catch (Exception ex)
            {
                info.HasGeometricExtents = false;
                _logger.Debug($"获取几何信息失败: {entityId}", ex);
            }

            return info;
        }

        #endregion 几何工具
    }

    /// <summary>
    /// 几何信息结构体 - 避免与AutoCAD原生类型冲突
    /// </summary>
    public struct ArxGeometryInfo
    {
        public bool IsValid { get; set; }
        public string EntityType { get; set; }
        public bool HasGeometricExtents { get; set; }
        public ArxBoundingBox? BoundingBox { get; set; }

        // 通用几何属性
        public double? Length { get; set; }

        public double? Area { get; set; }
        public double? Perimeter { get; set; }
        public Point3d? CenterPoint { get; set; }
        public Point3d? StartPoint { get; set; }
        public Point3d? EndPoint { get; set; }

        // 圆形属性
        public double? Radius { get; set; }

        public double? StartAngle { get; set; }
        public double? EndAngle { get; set; }

        // 其他属性
        public double? Angle { get; set; }

        public bool? IsClosed { get; set; }

        public override string ToString()
        {
            return $"{EntityType} - {(IsValid ? "Valid" : "Invalid")}";
        }
    }
}