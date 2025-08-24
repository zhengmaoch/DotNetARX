namespace DotNetARX.Interfaces
{
    /// <summary>
    /// 几何工具接口
    /// </summary>
    public interface IGeometryService
    {
        /// <summary>
        /// 计算两点间距离
        /// </summary>
        double CalculateDistance(Point3d pt1, Point3d pt2);

        /// <summary>
        /// 计算角度
        /// </summary>
        double CalculateAngle(Point3d pt1, Point3d pt2, Point3d pt3);

        /// <summary>
        /// 检查点是否在多边形内
        /// </summary>
        bool IsPointInPolygon(Point3d point, IEnumerable<Point3d> polygon);

        /// <summary>
        /// 获取边界框
        /// </summary>
        Extents3d GetBoundingBox(IEnumerable<ObjectId> entityIds);

        /// <summary>
        /// 获取实体边界
        /// </summary>
        Extents3d GetEntityBounds(ObjectId entityId);
    }
}