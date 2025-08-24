

namespace DotNetARX.Interfaces
{
    /// <summary>
    /// 工具服务接口
    /// </summary>
    public interface IUtilityService
    {
        /// <summary>
        /// 验证字符串是否为数字
        /// </summary>
        bool IsNumeric(string value);

        /// <summary>
        /// 验证字符串是否为整数
        /// </summary>
        bool IsInteger(string value);

        /// <summary>
        /// 安全转换为double
        /// </summary>
        double ToDoubleOrDefault(string value, double defaultValue = 0.0);

        /// <summary>
        /// 安全转换为int
        /// </summary>
        int ToIntOrDefault(string value, int defaultValue = 0);

        /// <summary>
        /// 获取当前程序路径
        /// </summary>
        string GetCurrentPath();

        /// <summary>
        /// 句柄转ObjectId
        /// </summary>
        ObjectId HandleToObjectId(string handleString);

        /// <summary>
        /// 亮显实体
        /// </summary>
        void HighlightEntities(IEnumerable<ObjectId> entityIds);

        /// <summary>
        /// 取消亮显实体
        /// </summary>
        void UnhighlightEntities(IEnumerable<ObjectId> entityIds);
    }
}