using System.Runtime.CompilerServices;

namespace DotNetARX.Extensions
{
    /// <summary>
    /// ObjectId 扩展方法
    /// 提供便捷的实体操作方法
    /// </summary>
    public static class ObjectIdExtensions
    {
        #region 实体获取

        /// <summary>
        /// 获取实体对象（强类型）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="objectId">实体ID</param>
        /// <param name="mode">打开模式</param>
        /// <returns>实体对象</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetEntity<T>(this ObjectId objectId, OpenMode mode = OpenMode.ForRead) where T : Entity
        {
            if (objectId.IsNull) return null;

            return AutoCADContext.ExecuteSafely(() =>
            {
                var context = AutoCADContext.Current;
                return context?.GetObject<T>(objectId, mode);
            });
        }

        /// <summary>
        /// 获取实体对象（非泛型）
        /// </summary>
        /// <param name="objectId">实体ID</param>
        /// <param name="mode">打开模式</param>
        /// <returns>实体对象</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity GetEntity(this ObjectId objectId, OpenMode mode = OpenMode.ForRead)
        {
            return objectId.GetEntity<Entity>(mode);
        }

        /// <summary>
        /// 安全获取实体对象（异常时返回null）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="objectId">实体ID</param>
        /// <param name="mode">打开模式</param>
        /// <returns>实体对象或null</returns>
        public static T TryGetEntity<T>(this ObjectId objectId, OpenMode mode = OpenMode.ForRead) where T : Entity
        {
            try
            {
                return objectId.GetEntity<T>(mode);
            }
            catch
            {
                return null;
            }
        }

        #endregion 实体获取

        #region 实体属性

        /// <summary>
        /// 获取实体类型名称
        /// </summary>
        /// <param name="objectId">实体ID</param>
        /// <returns>实体类型名称</returns>
        public static string GetEntityTypeName(this ObjectId objectId)
        {
            var entity = objectId.TryGetEntity<Entity>();
            return entity?.GetType().Name ?? "Unknown";
        }

        /// <summary>
        /// 获取实体所在图层名称
        /// </summary>
        /// <param name="objectId">实体ID</param>
        /// <returns>图层名称</returns>
        public static string GetLayerName(this ObjectId objectId)
        {
            var entity = objectId.TryGetEntity<Entity>();
            return entity?.Layer ?? "";
        }

        /// <summary>
        /// 获取实体颜色
        /// </summary>
        /// <param name="objectId">实体ID</param>
        /// <returns>实体颜色</returns>
        public static Color GetEntityColor(this ObjectId objectId)
        {
            var entity = objectId.TryGetEntity<Entity>();
            return entity?.Color ?? Color.FromColorIndex(ColorMethod.ByAci, 256);
        }

        /// <summary>
        /// 获取实体Handle字符串
        /// </summary>
        /// <param name="objectId">实体ID</param>
        /// <returns>Handle字符串</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetHandleString(this ObjectId objectId)
        {
            return objectId.Handle.ToString();
        }

        #endregion 实体属性

        #region 实体操作

        /// <summary>
        /// 移动实体
        /// </summary>
        /// <param name="objectId">实体ID</param>
        /// <param name="displacement">位移向量</param>
        /// <returns>操作是否成功</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Move(this ObjectId objectId, Vector3d displacement)
        {
            return CAD.MoveEntity(objectId, Point3d.Origin, Point3d.Origin + displacement);
        }

        /// <summary>
        /// 移动实体到指定位置
        /// </summary>
        /// <param name="objectId">实体ID</param>
        /// <param name="fromPoint">源点</param>
        /// <param name="toPoint">目标点</param>
        /// <returns>操作是否成功</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool MoveTo(this ObjectId objectId, Point3d fromPoint, Point3d toPoint)
        {
            return CAD.MoveEntity(objectId, fromPoint, toPoint);
        }

        /// <summary>
        /// 复制实体
        /// </summary>
        /// <param name="objectId">实体ID</param>
        /// <param name="displacement">位移向量</param>
        /// <returns>复制的实体ID</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectId Copy(this ObjectId objectId, Vector3d displacement)
        {
            return CAD.CopyEntity(objectId, Point3d.Origin, Point3d.Origin + displacement);
        }

        /// <summary>
        /// 复制实体到指定位置
        /// </summary>
        /// <param name="objectId">实体ID</param>
        /// <param name="fromPoint">源点</param>
        /// <param name="toPoint">目标点</param>
        /// <returns>复制的实体ID</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectId CopyTo(this ObjectId objectId, Point3d fromPoint, Point3d toPoint)
        {
            return CAD.CopyEntity(objectId, fromPoint, toPoint);
        }

        /// <summary>
        /// 旋转实体
        /// </summary>
        /// <param name="objectId">实体ID</param>
        /// <param name="basePoint">旋转基点</param>
        /// <param name="angle">旋转角度（弧度）</param>
        /// <returns>操作是否成功</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Rotate(this ObjectId objectId, Point3d basePoint, double angle)
        {
            return CAD.RotateEntity(objectId, basePoint, angle);
        }

        /// <summary>
        /// 缩放实体
        /// </summary>
        /// <param name="objectId">实体ID</param>
        /// <param name="basePoint">缩放基点</param>
        /// <param name="scaleFactor">缩放因子</param>
        /// <returns>操作是否成功</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Scale(this ObjectId objectId, Point3d basePoint, double scaleFactor)
        {
            return CAD.ScaleEntity(objectId, basePoint, scaleFactor);
        }

        /// <summary>
        /// 删除实体
        /// </summary>
        /// <param name="objectId">实体ID</param>
        /// <returns>操作是否成功</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Delete(this ObjectId objectId)
        {
            return CAD.DeleteEntity(objectId);
        }

        /// <summary>
        /// 变换实体
        /// </summary>
        /// <param name="objectId">实体ID</param>
        /// <param name="matrix">变换矩阵</param>
        /// <returns>操作是否成功</returns>
        public static bool Transform(this ObjectId objectId, Matrix3d matrix)
        {
            return AutoCADContext.ExecuteSafely(() =>
            {
                var entity = objectId.GetEntity(OpenMode.ForWrite);
                if (entity == null) return false;

                entity.TransformBy(matrix);
                return true;
            });
        }

        #endregion 实体操作

        #region 实体检查

        /// <summary>
        /// 检查实体是否存在且有效
        /// </summary>
        /// <param name="objectId">实体ID</param>
        /// <returns>实体是否有效</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(this ObjectId objectId)
        {
            return !objectId.IsNull && objectId.IsResident && !objectId.IsErased;
        }

        /// <summary>
        /// 检查实体是否为指定类型
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="objectId">实体ID</param>
        /// <returns>是否为指定类型</returns>
        public static bool IsOfType<T>(this ObjectId objectId) where T : Entity
        {
            var entity = objectId.TryGetEntity<Entity>();
            return entity is T;
        }

        /// <summary>
        /// 检查实体是否在指定图层
        /// </summary>
        /// <param name="objectId">实体ID</param>
        /// <param name="layerName">图层名称</param>
        /// <returns>是否在指定图层</returns>
        public static bool IsOnLayer(this ObjectId objectId, string layerName)
        {
            var entity = objectId.TryGetEntity<Entity>();
            return entity?.Layer == layerName;
        }

        /// <summary>
        /// 检查实体是否被锁定
        /// </summary>
        /// <param name="objectId">实体ID</param>
        /// <returns>是否被锁定</returns>
        public static bool IsLocked(this ObjectId objectId)
        {
            var entity = objectId.TryGetEntity<Entity>();
            return entity?.IsReadEnabled == false;
        }

        #endregion 实体检查

        #region 几何计算

        /// <summary>
        /// 获取实体的边界框
        /// </summary>
        /// <param name="objectId">实体ID</param>
        /// <returns>边界框</returns>
        public static ArxBoundingBox GetBoundingBox(this ObjectId objectId)
        {
            var entity = objectId.TryGetEntity<Entity>();
            if (entity == null)
                return ArxBoundingBox.Empty;

            try
            {
                var bounds = entity.GeometricExtents;
                return new ArxBoundingBox(bounds.MinPoint, bounds.MaxPoint);
            }
            catch
            {
                return ArxBoundingBox.Empty;
            }
        }

        /// <summary>
        /// 获取实体的中心点
        /// </summary>
        /// <param name="objectId">实体ID</param>
        /// <returns>中心点</returns>
        public static Point3d GetCenterPoint(this ObjectId objectId)
        {
            var bounds = objectId.GetBoundingBox();
            return bounds.IsEmpty ? Point3d.Origin : bounds.Center;
        }

        /// <summary>
        /// 计算实体与点的最近距离
        /// </summary>
        /// <param name="objectId">实体ID</param>
        /// <param name="point">参考点</param>
        /// <returns>最近距离</returns>
        public static double DistanceTo(this ObjectId objectId, Point3d point)
        {
            var entity = objectId.TryGetEntity<Entity>();
            if (entity == null) return double.MaxValue;

            try
            {
                var closestPoint = entity.GetClosestPointTo(point, Vector3d.ZAxis, false);
                return point.DistanceTo(closestPoint);
            }
            catch
            {
                return double.MaxValue;
            }
        }

        /// <summary>
        /// 获取实体上离指定点最近的点
        /// </summary>
        /// <param name="objectId">实体ID</param>
        /// <param name="point">参考点</param>
        /// <returns>最近的点</returns>
        public static Point3d GetClosestPointTo(this ObjectId objectId, Point3d point)
        {
            var entity = objectId.TryGetEntity<Entity>();
            if (entity == null) return Point3d.Origin;

            try
            {
                return entity.GetClosestPointTo(point, Vector3d.ZAxis, false);
            }
            catch
            {
                return Point3d.Origin;
            }
        }

        #endregion 几何计算

        #region 批量操作

        /// <summary>
        /// 批量移动实体
        /// </summary>
        /// <param name="objectIds">实体ID集合</param>
        /// <param name="displacement">位移向量</param>
        /// <returns>成功操作的数量</returns>
        public static int MoveAll(this IEnumerable<ObjectId> objectIds, Vector3d displacement)
        {
            var operations = objectIds.Select(id => (id, Point3d.Origin, Point3d.Origin + displacement));
            return CAD.MoveEntities(operations);
        }

        /// <summary>
        /// 批量删除实体
        /// </summary>
        /// <param name="objectIds">实体ID集合</param>
        /// <returns>成功删除的数量</returns>
        public static int DeleteAll(this IEnumerable<ObjectId> objectIds)
        {
            return CAD.DeleteEntities(objectIds);
        }

        /// <summary>
        /// 批量获取实体类型统计
        /// </summary>
        /// <param name="objectIds">实体ID集合</param>
        /// <returns>实体类型统计</returns>
        public static Dictionary<string, int> GetTypeStatistics(this IEnumerable<ObjectId> objectIds)
        {
            var statistics = new Dictionary<string, int>();

            foreach (var id in objectIds)
            {
                var typeName = id.GetEntityTypeName();
                statistics[typeName] = statistics.GetValueOrDefault(typeName, 0) + 1;
            }

            return statistics;
        }

        /// <summary>
        /// 过滤有效的实体ID
        /// </summary>
        /// <param name="objectIds">实体ID集合</param>
        /// <returns>有效的实体ID集合</returns>
        public static IEnumerable<ObjectId> WhereValid(this IEnumerable<ObjectId> objectIds)
        {
            return objectIds.Where(id => id.IsValid());
        }

        /// <summary>
        /// 过滤指定类型的实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="objectIds">实体ID集合</param>
        /// <returns>指定类型的实体ID集合</returns>
        public static IEnumerable<ObjectId> WhereOfType<T>(this IEnumerable<ObjectId> objectIds) where T : Entity
        {
            return objectIds.Where(id => id.IsOfType<T>());
        }

        /// <summary>
        /// 过滤指定图层的实体
        /// </summary>
        /// <param name="objectIds">实体ID集合</param>
        /// <param name="layerName">图层名称</param>
        /// <returns>指定图层的实体ID集合</returns>
        public static IEnumerable<ObjectId> WhereOnLayer(this IEnumerable<ObjectId> objectIds, string layerName)
        {
            return objectIds.Where(id => id.IsOnLayer(layerName));
        }

        #endregion 批量操作
    }

    #region 辅助数据结构

    /// <summary>
    /// DotNetARX 边界框
    /// 避免与AutoCAD原生类型冲突
    /// </summary>
    public struct ArxBoundingBox
    {
        public Point3d MinPoint { get; }
        public Point3d MaxPoint { get; }
        public bool IsEmpty { get; }

        public ArxBoundingBox(Point3d minPoint, Point3d maxPoint)
        {
            MinPoint = minPoint;
            MaxPoint = maxPoint;
            IsEmpty = false;
        }

        private ArxBoundingBox(bool isEmpty)
        {
            MinPoint = Point3d.Origin;
            MaxPoint = Point3d.Origin;
            IsEmpty = isEmpty;
        }

        public Point3d Center => new Point3d(
            (MinPoint.X + MaxPoint.X) / 2,
            (MinPoint.Y + MaxPoint.Y) / 2,
            (MinPoint.Z + MaxPoint.Z) / 2);

        public double Width => MaxPoint.X - MinPoint.X;
        public double Height => MaxPoint.Y - MinPoint.Y;
        public double Depth => MaxPoint.Z - MinPoint.Z;

        public static ArxBoundingBox Empty => new ArxBoundingBox(true);
    }

    #endregion 辅助数据结构
}