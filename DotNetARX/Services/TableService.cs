namespace DotNetARX.Services
{
    /// <summary>
    /// 表格操作服务实现
    /// </summary>
    public class TableService : ITableService
    {
        private readonly IEventBus _eventBus;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly ILogger _logger;

        public TableService(
            IEventBus eventBus = null,
            IPerformanceMonitor performanceMonitor = null,
            ILogger logger = null)
        {
            _eventBus = eventBus ?? ServiceContainer.Instance.GetService<IEventBus>();
            _performanceMonitor = performanceMonitor ?? ServiceContainer.Instance.GetService<IPerformanceMonitor>();
            _logger = logger ?? ServiceContainer.Instance.GetService<ILogger>();
        }

        /// <summary>
        /// 创建表格
        /// </summary>
        public ObjectId CreateTable(Point3d position, int rows, int columns, double rowHeight, double columnWidth)
        {
            using var operation = _performanceMonitor?.StartOperation("CreateTable");

            try
            {
                if (rows <= 0)
                    throw new ArgumentException("行数必须大于0");

                if (columns <= 0)
                    throw new ArgumentException("列数必须大于0");

                if (rowHeight <= 0)
                    throw new ArgumentException("行高必须大于0");

                if (columnWidth <= 0)
                    throw new ArgumentException("列宽必须大于0");

                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;
                ObjectId tableId;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    // 创建表格对象
                    var table = new Table();
                    table.Position = position;
                    table.SetSize(rows, columns);

                    // 设置所有行高
                    for (int row = 0; row < rows; row++)
                    {
                        table.Rows[row].Height = rowHeight;
                    }

                    // 设置所有列宽
                    for (int col = 0; col < columns; col++)
                    {
                        table.Columns[col].Width = columnWidth;
                    }

                    // 将表格添加到模型空间
                    var modelSpace = transManager.GetObject<BlockTableRecord>(
                        SymbolUtilityServices.GetBlockModelSpaceId(database), OpenMode.ForWrite);

                    tableId = modelSpace.AppendEntity(table);
                    transManager.AddNewlyCreatedDBObject(table, true);

                    transManager.Commit();
                }

                _eventBus?.Publish(new TableEvent("TableCreated", tableId, $"Rows: {rows}, Columns: {columns}"));
                _logger?.Info($"表格创建成功: {tableId}, 行数: {rows}, 列数: {columns}");

                return tableId;
            }
            catch (Exception ex)
            {
                _logger?.Error($"创建表格失败: {ex.Message}", ex);
                throw new TableOperationException($"创建表格失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 设置单元格文本
        /// </summary>
        public bool SetCellText(ObjectId tableId, int row, int column, string text)
        {
            using var operation = _performanceMonitor?.StartOperation("SetCellText");

            try
            {
                if (tableId.IsNull || !tableId.IsValid)
                    throw new ArgumentException("无效的表格ID");

                if (string.IsNullOrEmpty(text))
                    text = "";

                var database = tableId.Database;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var table = transManager.GetObject<Table>(tableId, OpenMode.ForWrite);

                    // 检查行列索引是否有效
                    if (row < 0 || row >= table.Rows.Count)
                        throw new ArgumentOutOfRangeException(nameof(row), "行索引超出范围");

                    if (column < 0 || column >= table.Columns.Count)
                        throw new ArgumentOutOfRangeException(nameof(column), "列索引超出范围");

                    // 设置单元格文本
                    table.Cells[row, column].TextString = text;

                    transManager.Commit();
                }

                _eventBus?.Publish(new TableEvent("CellTextSet", tableId, $"Row: {row}, Column: {column}, Text: {text}"));
                _logger?.Debug($"设置单元格文本成功: 表格 {tableId}, 行 {row}, 列 {column}, 文本 '{text}'");

                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"设置单元格文本失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 获取单元格文本
        /// </summary>
        public string GetCellText(ObjectId tableId, int row, int column)
        {
            using var operation = _performanceMonitor?.StartOperation("GetCellText");

            try
            {
                if (tableId.IsNull || !tableId.IsValid)
                    throw new ArgumentException("无效的表格ID");

                var database = tableId.Database;
                string text;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var table = transManager.GetObject<Table>(tableId, OpenMode.ForRead);

                    // 检查行列索引是否有效
                    if (row < 0 || row >= table.Rows.Count)
                        throw new ArgumentOutOfRangeException(nameof(row), "行索引超出范围");

                    if (column < 0 || column >= table.Columns.Count)
                        throw new ArgumentOutOfRangeException(nameof(column), "列索引超出范围");

                    // 获取单元格文本
                    text = table.Cells[row, column].Contents[0].TextString;

                    transManager.Commit();
                }

                _logger?.Debug($"获取单元格文本成功: 表格 {tableId}, 行 {row}, 列 {column}, 文本 '{text}'");
                return text ?? "";
            }
            catch (Exception ex)
            {
                _logger?.Error($"获取单元格文本失败: {ex.Message}", ex);
                return "";
            }
        }

        /// <summary>
        /// 合并单元格
        /// </summary>
        public bool MergeCells(ObjectId tableId, int startRow, int startColumn, int endRow, int endColumn)
        {
            using var operation = _performanceMonitor?.StartOperation("MergeCells");

            try
            {
                if (tableId.IsNull || !tableId.IsValid)
                    throw new ArgumentException("无效的表格ID");

                if (startRow > endRow)
                    throw new ArgumentException("起始行不能大于结束行");

                if (startColumn > endColumn)
                    throw new ArgumentException("起始列不能大于结束列");

                var database = tableId.Database;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var table = transManager.GetObject<Table>(tableId, OpenMode.ForWrite);

                    // 检查索引是否有效
                    if (startRow < 0 || endRow >= table.Rows.Count)
                        throw new ArgumentOutOfRangeException("行索引超出范围");

                    if (startColumn < 0 || endColumn >= table.Columns.Count)
                        throw new ArgumentOutOfRangeException("列索引超出范围");

                    // 创建单元格范围
                    var cellRange = CellRange.Create(table, startRow, startColumn, endRow, endColumn);

                    // 合并单元格
                    table.MergeCells(cellRange);

                    transManager.Commit();
                }

                _eventBus?.Publish(new TableEvent("CellsMerged", tableId,
                    $"Range: ({startRow},{startColumn}) to ({endRow},{endColumn})"));
                _logger?.Info($"合并单元格成功: 表格 {tableId}, 范围 ({startRow},{startColumn}) 到 ({endRow},{endColumn})");

                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"合并单元格失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 设置表格样式
        /// </summary>
        public bool SetTableStyle(ObjectId tableId, ObjectId tableStyleId)
        {
            using var operation = _performanceMonitor?.StartOperation("SetTableStyle");

            try
            {
                if (tableId.IsNull || !tableId.IsValid)
                    throw new ArgumentException("无效的表格ID");

                if (tableStyleId.IsNull || !tableStyleId.IsValid)
                    throw new ArgumentException("无效的表格样式ID");

                var database = tableId.Database;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var table = transManager.GetObject<Table>(tableId, OpenMode.ForWrite);
                    table.TableStyle = tableStyleId;

                    transManager.Commit();
                }

                _eventBus?.Publish(new TableEvent("TableStyleSet", tableId, $"StyleId: {tableStyleId}"));
                _logger?.Info($"设置表格样式成功: 表格 {tableId}, 样式 {tableStyleId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"设置表格样式失败: {ex.Message}", ex);
                return false;
            }
        }
    }

    /// <summary>
    /// 表格事件类
    /// </summary>
    public class TableEvent : Events.EventArgs
    {
        public string EventType { get; }
        public ObjectId TableId { get; }
        public string Details { get; }
        public new DateTime Timestamp { get; }

        public TableEvent(string eventType, ObjectId tableId, string details = null)
            : base("TableService")
        {
            EventType = eventType;
            TableId = tableId;
            Details = details;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 表格操作异常
    /// </summary>
    public class TableOperationException : DotNetARXException
    {
        public TableOperationException(string message) : base(message)
        {
        }

        public TableOperationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}