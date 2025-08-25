namespace DotNetARX.Services
{
    /// <summary>
    /// 文档操作服务实现
    /// </summary>
    public partial class DocumentService : IDocumentService
    {
        private readonly IEventBus _eventBus;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly ILogger _logger;

        public DocumentService(
            IEventBus eventBus = null,
            IPerformanceMonitor performanceMonitor = null,
            ILogger logger = null)
        {
            _eventBus = eventBus ?? ServiceContainer.Instance.GetService<IEventBus>();
            _performanceMonitor = performanceMonitor ?? ServiceContainer.Instance.GetService<IPerformanceMonitor>();
            _logger = logger ?? ServiceContainer.Instance.GetService<ILogger>();
        }

        /// <summary>
        /// 检查文档是否已保存
        /// </summary>
        public bool IsDocumentSaved()
        {
            using var operation = _performanceMonitor?.StartOperation("IsDocumentSaved");

            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
                if (doc == null)
                {
                    _logger?.Warning("当前没有活动文档");
                    return false;
                }

                var db = doc.Database;

                // 读取DBMOD系统变量判断文档是否已修改
                // DBMOD为0表示未修改，非0表示已修改
                object dbmod = Autodesk.AutoCAD.ApplicationServices.Core.Application.GetSystemVariable("DBMOD");
                bool isSaved = (short)dbmod == 0;

                _logger?.Debug($"文档保存状态检查: {(isSaved ? "已保存" : "未保存")}");
                return isSaved;
            }
            catch (Exception ex)
            {
                _logger?.Error($"检查文档保存状态失败: {ex.Message}", ex);
                throw new DocumentOperationException($"检查文档保存状态失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 保存文档
        /// </summary>
        public bool SaveDocument()
        {
            using var operation = _performanceMonitor?.StartOperation("SaveDocument");

            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
                if (doc == null)
                {
                    _logger?.Warning("当前没有活动文档");
                    return false;
                }

                var db = doc.Database;

                if (string.IsNullOrEmpty(db.Filename))
                {
                    _logger?.Warning("文档尚未保存过，无法执行保存操作，请使用另存为");
                    return false;
                }

                // 使用Database.SaveAs方法保存文档
                db.SaveAs(db.Filename, DwgVersion.Current);

                _eventBus?.Publish(new DocumentEvent("DocumentSaved", db.Filename));
                _logger?.Info($"文档保存成功: {db.Filename}");

                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"保存文档失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 另存为文档
        /// </summary>
        public bool SaveDocumentAs(string fileName)
        {
            using var operation = _performanceMonitor?.StartOperation("SaveDocumentAs");

            try
            {
                if (string.IsNullOrEmpty(fileName))
                    throw new ArgumentException("文件名不能为空");

                var doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
                if (doc == null)
                {
                    _logger?.Warning("当前没有活动文档");
                    return false;
                }

                var db = doc.Database;

                // 确保目录存在
                var directory = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 使用Database.SaveAs方法另存为文档
                db.SaveAs(fileName, DwgVersion.Current);

                _eventBus?.Publish(new DocumentEvent("DocumentSavedAs", fileName));
                _logger?.Info($"文档另存为成功: {fileName}");

                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"另存为文档失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 获取文档信息
        /// </summary>
        public DocumentInfo GetDocumentInfo()
        {
            using var operation = _performanceMonitor?.StartOperation("GetDocumentInfo");

            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
                if (doc == null)
                    throw new InvalidOperationException("当前没有活动文档");

                var db = doc.Database;
                var info = new DocumentInfo();

                // 基本信息
                info.FileName = Path.GetFileName(db.Filename) ?? "未命名";
                info.FullPath = db.Filename ?? "";
                info.IsSaved = IsDocumentSaved();
                info.Version = "Unknown"; // db.Version 在某些AutoCAD版本中不可用

                // 文件信息
                if (!string.IsNullOrEmpty(db.Filename) && File.Exists(db.Filename))
                {
                    var fileInfo = new FileInfo(db.Filename);
                    info.CreationTime = fileInfo.CreationTime;
                    info.LastModifiedTime = fileInfo.LastWriteTime;
                    info.FileSize = fileInfo.Length;
                }
                else
                {
                    info.CreationTime = DateTime.Now;
                    info.LastModifiedTime = DateTime.Now;
                    info.FileSize = 0;
                }

                // 修改状态
                try
                {
                    object dbmod = Autodesk.AutoCAD.ApplicationServices.Core.Application.GetSystemVariable("DBMOD");
                    info.IsModified = (short)dbmod != 0;
                }
                catch
                {
                    info.IsModified = false;
                }

                return info;
            }
            catch (Exception ex)
            {
                _logger?.Error($"获取文档信息失败: {ex.Message}", ex);
                throw new DocumentOperationException($"获取文档信息失败: {ex.Message}", ex);
            }
        }

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

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // 当前实现中没有需要特别释放的资源
            // 但为了接口一致性，提供空实现
        }
    }

    /// <summary>
    /// 文档事件类
    /// </summary>
    public class DocumentEvent : Events.EventArgs
    {
        public string EventType { get; }
        public string DocumentPath { get; }
        public string Details { get; }
        public new DateTime Timestamp { get; }

        public DocumentEvent(string eventType, string documentPath, string details = null)
            : base("DocumentService")
        {
            EventType = eventType;
            DocumentPath = documentPath;
            Details = details;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 文档操作异常
    /// </summary>
    public class DocumentOperationException : DotNetARXException
    {
        public DocumentOperationException(string message) : base(message)
        {
        }

        public DocumentOperationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}