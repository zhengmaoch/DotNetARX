

namespace DotNetARX.Interfaces
{
    /// <summary>
    /// 布局操作接口
    /// </summary>
    public interface ILayoutService
    {
        /// <summary>
        /// 创建布局
        /// </summary>
        ObjectId CreateLayout(string layoutName);

        /// <summary>
        /// 删除布局
        /// </summary>
        bool DeleteLayout(string layoutName);

        /// <summary>
        /// 创建视口
        /// </summary>
        ObjectId CreateViewport(ObjectId layoutId, Point3d center, double width, double height);

        /// <summary>
        /// 设置视口比例
        /// </summary>
        bool SetViewportScale(ObjectId viewportId, double scale);

        /// <summary>
        /// 获取所有布局名称
        /// </summary>
        IEnumerable<string> GetLayoutNames();
    }
}