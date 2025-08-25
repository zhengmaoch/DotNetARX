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

        /// <summary>
        /// 设置当前布局
        /// </summary>
        bool SetCurrentLayout(string layoutName);

        /// <summary>
        /// 检查布局是否存在
        /// </summary>
        bool LayoutExists(string layoutName);

        /// <summary>
        /// 获取所有布局信息
        /// </summary>
        IEnumerable<LayoutInfo> GetAllLayouts();

        /// <summary>
        /// 获取布局信息
        /// </summary>
        LayoutInfo GetLayoutInfo(string layoutName);

        /// <summary>
        /// 重命名布局
        /// </summary>
        bool RenameLayout(string oldName, string newName);

        /// <summary>
        /// 复制布局
        /// </summary>
        ObjectId CopyLayout(string sourceLayoutName, string newLayoutName);

        /// <summary>
        /// 获取纸张尺寸
        /// </summary>
        PaperSize GetPaperSize(string layoutName);

        /// <summary>
        /// 设置纸张尺寸
        /// </summary>
        bool SetPaperSize(string layoutName, PaperSize paperSize);

        /// <summary>
        /// 释放资源
        /// </summary>
        void Dispose();
    }
}