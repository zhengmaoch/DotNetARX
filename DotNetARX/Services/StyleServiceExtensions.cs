// 此文件包含StyleService中缺失方法的实现

namespace DotNetARX.Services
{
    public partial class StyleService
    {
        /// <summary>
        /// 创建标注样式
        /// </summary>
        public ObjectId CreateDimStyle(string styleName, double textHeight, double arrowSize)
        {
            using var operation = _performanceMonitor?.StartOperation("CreateDimStyle");

            try
            {
                if (string.IsNullOrEmpty(styleName))
                    throw new ArgumentException("样式名称不能为空");

                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var dimStyleTable = transManager.GetObject<DimStyleTable>(database.DimStyleTableId, OpenMode.ForRead);

                    if (dimStyleTable.Has(styleName))
                    {
                        _logger?.Info($"标注样式 '{styleName}' 已存在");
                        return dimStyleTable[styleName];
                    }

                    dimStyleTable.UpgradeOpen();

                    var dimStyleRecord = new DimStyleTableRecord();
                    dimStyleRecord.Name = styleName;

                    // 设置文字高度
                    dimStyleRecord.Dimtxt = textHeight;

                    // 设置箭头大小
                    dimStyleRecord.Dimasz = arrowSize;

                    var dimStyleId = dimStyleTable.Add(dimStyleRecord);
                    transManager.AddNewlyCreatedDBObject(dimStyleRecord, true);

                    transManager.Commit();

                    _eventBus?.Publish(new StyleEvent("DimStyleCreated", dimStyleId, "DimStyleCreated", "DimensionStyle"));
                    _logger?.Info($"标注样式创建成功: {styleName}");

                    return dimStyleId;
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"创建标注样式失败: {ex.Message}", ex);
                throw new StyleOperationException($"创建标注样式失败: {ex.Message}", ex);
            }
        }
    }
}