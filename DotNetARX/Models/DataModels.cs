using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace DotNetARX.Models
{
    /// <summary>
    /// 菜单项目定义
    /// </summary>
    public class MenuItemDefinition
    {
        /// <summary>
        /// 菜单项文本
        /// </summary>
        public string Text { get; set; }
        
        /// <summary>
        /// 菜单项命令
        /// </summary>
        public string Command { get; set; }
        
        /// <summary>
        /// 子菜单项
        /// </summary>
        public List<MenuItemDefinition> SubItems { get; set; } = new List<MenuItemDefinition>();
    }

    /// <summary>
    /// 图层属性定义
    /// </summary>
    public class LayerProperties
    {
        /// <summary>
        /// 图层名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 颜色索引
        /// </summary>
        public short ColorIndex { get; set; }
        
        /// <summary>
        /// 是否锁定
        /// </summary>
        public bool IsLocked { get; set; }
        
        /// <summary>
        /// 是否冻结
        /// </summary>
        public bool IsFrozen { get; set; }
        
        /// <summary>
        /// 线型
        /// </summary>
        public string LineType { get; set; }
        
        /// <summary>
        /// 线宽
        /// </summary>
        public LineWeight LineWeight { get; set; }
    }

    /// <summary>
    /// 实体属性定义
    /// </summary>
    public class EntityProperties
    {
        /// <summary>
        /// 颜色索引
        /// </summary>
        public short? ColorIndex { get; set; }
        
        /// <summary>
        /// 图层名称
        /// </summary>
        public string LayerName { get; set; }
        
        /// <summary>
        /// 线型
        /// </summary>
        public string LineType { get; set; }
        
        /// <summary>
        /// 线宽
        /// </summary>
        public LineWeight? LineWeight { get; set; }
        
        /// <summary>
        /// 是否可见
        /// </summary>
        public bool? IsVisible { get; set; }
    }

    /// <summary>
    /// 光标类型枚举
    /// </summary>
    public enum CursorType
    {
        /// <summary>
        /// 默认光标
        /// </summary>
        Default,
        
        /// <summary>
        /// 等待光标
        /// </summary>
        Wait,
        
        /// <summary>
        /// 十字光标
        /// </summary>
        Crosshair,
        
        /// <summary>
        /// 手型光标
        /// </summary>
        Hand,
        
        /// <summary>
        /// 移动光标
        /// </summary>
        Move,
        
        /// <summary>
        /// 文本光标
        /// </summary>
        Text,
        
        /// <summary>
        /// 帮助光标
        /// </summary>
        Help
    }

    /// <summary>
    /// 布局信息
    /// </summary>
    public class LayoutInfo
    {
        /// <summary>
        /// 布局名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 布局ID
        /// </summary>
        public ObjectId ObjectId { get; set; }
        
        /// <summary>
        /// 是否为当前布局
        /// </summary>
        public bool IsCurrent { get; set; }
        
        /// <summary>
        /// 纸张尺寸
        /// </summary>
        public PaperSize PaperSize { get; set; }
    }

    /// <summary>
    /// 纸张尺寸
    /// </summary>
    public class PaperSize
    {
        /// <summary>
        /// 宽度
        /// </summary>
        public double Width { get; set; }
        
        /// <summary>
        /// 高度
        /// </summary>
        public double Height { get; set; }
    }
}