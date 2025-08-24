namespace DotNetARX.Interfaces
{
    /// <summary>
    /// 绘图操作接口
    /// </summary>
    public interface IDrawingOperations
    {
        /// <summary>
        /// 绘制直线
        /// </summary>
        ObjectId DrawLine(Point3d startPoint, Point3d endPoint);

        /// <summary>
        /// 绘制圆
        /// </summary>
        ObjectId DrawCircle(Point3d center, double radius);

        /// <summary>
        /// 绘制圆弧
        /// </summary>
        ObjectId DrawArc(Point3d center, double radius, double startAngle, double endAngle);

        /// <summary>
        /// 绘制多段线
        /// </summary>
        ObjectId DrawPolyline(IEnumerable<Point2d> points, bool isClosed = false);

        /// <summary>
        /// 绘制文本
        /// </summary>
        ObjectId DrawText(string text, Point3d position, double height, double rotation = 0);

        /// <summary>
        /// 绘制多行文本
        /// </summary>
        ObjectId DrawMText(string text, Point3d position, double width, double height);
    }
}