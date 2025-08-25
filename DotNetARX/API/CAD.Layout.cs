using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetARX
{
    public static partial class CAD
    {
        #region 布局操作

        /// <summary>
        /// 创建布局
        /// </summary>
        public static ObjectId CreateLayout(string layoutName)
        {
            EnsureInitialized();
            var layoutService = ServiceInitializer.GetRequiredService<ILayoutService>();
            return layoutService.CreateLayout(layoutName);
        }

        /// <summary>
        /// 删除布局
        /// </summary>
        public static bool DeleteLayout(string layoutName)
        {
            EnsureInitialized();
            var layoutService = ServiceInitializer.GetRequiredService<ILayoutService>();
            return layoutService.DeleteLayout(layoutName);
        }

        /// <summary>
        /// 创建视口
        /// </summary>
        public static ObjectId CreateViewport(Point3d center, double width, double height)
        {
            EnsureInitialized();
            var layoutService = ServiceInitializer.GetRequiredService<ILayoutService>();
            // 获取当前布局ID
            var layoutId = LayoutManager.Current.GetLayoutId(LayoutManager.Current.CurrentLayout);
            return layoutService.CreateViewport(layoutId, center, width, height);
        }

        /// <summary>
        /// 设置视口比例
        /// </summary>
        public static bool SetViewportScale(ObjectId viewportId, double scale)
        {
            EnsureInitialized();
            var layoutService = ServiceInitializer.GetRequiredService<ILayoutService>();
            return layoutService.SetViewportScale(viewportId, scale);
        }

        #endregion 布局操作
    }
}