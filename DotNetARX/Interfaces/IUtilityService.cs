namespace DotNetARX.Interfaces
{
    /// <summary>
    /// 工具服务接口
    /// </summary>
    public interface IUtilityService : IDisposable
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

        /// <summary>
        /// 验证字符串是否匹配指定模式
        /// </summary>
        bool ValidateString(string value, string pattern);

        /// <summary>
        /// 安全类型转换
        /// </summary>
        T SafeConvert<T>(object value, T defaultValue = default);

        /// <summary>
        /// 获取AutoCAD安装路径
        /// </summary>
        string GetAutoCADPath();

        /// <summary>
        /// 亮显单个实体
        /// </summary>
        bool HighlightEntity(ObjectId entityId, bool highlight = true);

        /// <summary>
        /// 安全执行操作
        /// </summary>
        T SafeExecute<T>(Func<T> operation, T defaultValue = default);
    }
}