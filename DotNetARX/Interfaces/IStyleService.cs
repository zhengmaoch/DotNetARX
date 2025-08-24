namespace DotNetARX.Interfaces
{
    /// <summary>
    /// 样式管理接口
    /// </summary>
    public interface IStyleService
    {
        /// <summary>
        /// 创建文字样式
        /// </summary>
        ObjectId CreateTextStyle(string styleName, string fontName, double height = 0);

        /// <summary>
        /// 创建标注样式
        /// </summary>
        ObjectId CreateDimensionStyle(string styleName);

        /// <summary>
        /// 创建标注样式（简化方法名）
        /// </summary>
        ObjectId CreateDimStyle(string styleName);

        ObjectId CreateDimStyle(string styleName, double textHeight, double arrowSize);

        /// <summary>
        /// 创建线型
        /// </summary>
        ObjectId CreateLineType(string typeName, string pattern, string description = "");

        /// <summary>
        /// 获取所有文字样式
        /// </summary>
        IEnumerable<string> GetTextStyleNames();

        /// <summary>
        /// 获取所有标注样式
        /// </summary>
        IEnumerable<string> GetDimensionStyleNames();

        /// <summary>
        /// 获取所有线型
        /// </summary>
        IEnumerable<string> GetLineTypeNames();
    }
}