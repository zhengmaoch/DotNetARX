namespace DotNetARX.Interfaces
{
    /// <summary>
    /// 选择操作接口
    /// </summary>
    public interface ISelectionService : IDisposable
    {
        /// <summary>
        /// 按类型选择实体
        /// </summary>
        List<T> SelectByType<T>() where T : Entity;

        /// <summary>
        /// 在窗口内选择实体
        /// </summary>
        List<T> SelectInWindow<T>(Point3d pt1, Point3d pt2) where T : Entity;

        /// <summary>
        /// 交叉窗口选择实体
        /// </summary>
        List<T> SelectCrossingWindow<T>(Point3d pt1, Point3d pt2) where T : Entity;

        /// <summary>
        /// 通过过滤器选择实体
        /// </summary>
        List<T> SelectByFilter<T>(SelectionFilter filter) where T : Entity;

        /// <summary>
        /// 选择指定点处的实体
        /// </summary>
        List<T> SelectAtPoint<T>(Point3d point) where T : Entity;

        /// <summary>
        /// 获取当前选择集
        /// </summary>
        ObjectIdCollection GetCurrentSelection();
    }
}