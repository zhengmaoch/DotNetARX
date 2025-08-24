

namespace DotNetARX.Interfaces
{
    /// <summary>
    /// 表格操作接口
    /// </summary>
    public interface ITableService
    {
        /// <summary>
        /// 创建表格
        /// </summary>
        ObjectId CreateTable(Point3d position, int rows, int columns, double rowHeight, double columnWidth);

        /// <summary>
        /// 设置单元格文本
        /// </summary>
        bool SetCellText(ObjectId tableId, int row, int column, string text);

        /// <summary>
        /// 获取单元格文本
        /// </summary>
        string GetCellText(ObjectId tableId, int row, int column);

        /// <summary>
        /// 合并单元格
        /// </summary>
        bool MergeCells(ObjectId tableId, int startRow, int startColumn, int endRow, int endColumn);
    }
}