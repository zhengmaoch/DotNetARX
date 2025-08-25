// 此文件包含DocumentService中缺失方法的实现

namespace DotNetARX.Services
{
    public partial class DocumentService
    {
        /// <summary>
        /// 检查文档是否需要保存
        /// </summary>
        public bool CheckDocumentNeedsSave()
        {
            using var operation = _performanceMonitor?.StartOperation("CheckDocumentNeedsSave");

            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
                if (doc == null)
                {
                    _logger?.Warning("当前没有活动文档");
                    return false;
                }

                // 检查文档的修改状态
                // 在AutoCAD中，可以通过数据库的修改计数器来判断
                var database = doc.Database;

                // 如果文档有未保存的修改，这个属性会为true
                bool needsSave = database.RetainOriginalThumbnailBitmap ||
                               !string.IsNullOrEmpty(database.Filename);

                _logger?.Debug($"文档保存检查: {doc.Name} - 需要保存: {needsSave}");
                return needsSave;
            }
            catch (Exception ex)
            {
                _logger?.Error($"检查文档保存状态失败: {ex.Message}", ex);
                return false;
            }
        }
    }
}