namespace DotNetARX.Services
{
    /// <summary>
    /// 工具服务实现
    /// </summary>
    public class UtilityService : IUtilityService
    {
        private readonly IEventBus _eventBus;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly ILogger _logger;

        public UtilityService(
            IEventBus eventBus = null,
            IPerformanceMonitor performanceMonitor = null,
            ILogger logger = null)
        {
            _eventBus = eventBus ?? ServiceContainer.Instance.GetService<IEventBus>();
            _performanceMonitor = performanceMonitor ?? ServiceContainer.Instance.GetService<IPerformanceMonitor>();
            _logger = logger ?? ServiceContainer.Instance.GetService<ILogger>();
        }

        /// <summary>
        /// 验证字符串是否为数字
        /// </summary>
        public bool IsNumeric(string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value))
                    return false;

                return Regex.IsMatch(value, @"^[+-]?\d*[.]?\d*$");
            }
            catch (Exception ex)
            {
                _logger?.Error($"验证数字失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 验证字符串是否为整数
        /// </summary>
        public bool IsInteger(string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value))
                    return false;

                return Regex.IsMatch(value, @"^[+-]?\d*$");
            }
            catch (Exception ex)
            {
                _logger?.Error($"验证整数失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 安全转换为double
        /// </summary>
        public double ToDoubleOrDefault(string value, double defaultValue = 0.0)
        {
            try
            {
                if (string.IsNullOrEmpty(value))
                    return defaultValue;

                if (double.TryParse(value, out double result))
                    return result;

                return defaultValue;
            }
            catch (Exception ex)
            {
                _logger?.Error($"转换为double失败: {ex.Message}", ex);
                return defaultValue;
            }
        }

        /// <summary>
        /// 安全转换为int
        /// </summary>
        public int ToIntOrDefault(string value, int defaultValue = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(value))
                    return defaultValue;

                if (int.TryParse(value, out int result))
                    return result;

                return defaultValue;
            }
            catch (Exception ex)
            {
                _logger?.Error($"转换为int失败: {ex.Message}", ex);
                return defaultValue;
            }
        }

        /// <summary>
        /// 获取当前程序路径
        /// </summary>
        public string GetCurrentPath()
        {
            try
            {
                var module = Assembly.GetExecutingAssembly().GetModules()[0];
                return Path.GetDirectoryName(module.FullyQualifiedName);
            }
            catch (Exception ex)
            {
                _logger?.Error($"获取当前程序路径失败: {ex.Message}", ex);
                return "";
            }
        }

        /// <summary>
        /// 句柄转ObjectId
        /// </summary>
        public ObjectId HandleToObjectId(string handleString)
        {
            using var operation = _performanceMonitor?.StartOperation("HandleToObjectId");

            try
            {
                if (string.IsNullOrEmpty(handleString))
                    throw new ArgumentException("句柄字符串不能为空");

                var database = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;
                var handle = new Handle(Convert.ToInt64(handleString, 16));
                var objectId = database.GetObjectId(false, handle, 0);

                _logger?.Debug($"句柄转换成功: {handleString} -> {objectId}");
                return objectId;
            }
            catch (Exception ex)
            {
                _logger?.Error($"句柄转ObjectId失败: {ex.Message}", ex);
                throw new UtilityOperationException($"句柄转ObjectId失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 亮显实体
        /// </summary>
        public void HighlightEntities(IEnumerable<ObjectId> entityIds)
        {
            using var operation = _performanceMonitor?.StartOperation("HighlightEntities");

            try
            {
                if (entityIds == null)
                    throw new ArgumentNullException(nameof(entityIds));

                var ids = entityIds.Where(id => !id.IsNull && id.IsValid).ToList();
                if (!ids.Any())
                {
                    _logger?.Warning("没有有效的实体ID需要亮显");
                    return;
                }

                var database = ids.First().Database;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    foreach (var id in ids)
                    {
                        try
                        {
                            var entity = transManager.GetObject<Entity>(id, OpenMode.ForRead);
                            entity?.Highlight();
                        }
                        catch (Exception ex)
                        {
                            _logger?.Warning($"亮显实体 {id} 失败: {ex.Message}");
                        }
                    }

                    transManager.Commit();
                }

                _eventBus?.Publish(new UtilityEvent("EntitiesHighlighted", $"Count: {ids.Count}"));
                _logger?.Info($"亮显实体成功，数量: {ids.Count}");
            }
            catch (Exception ex)
            {
                _logger?.Error($"亮显实体失败: {ex.Message}", ex);
                throw new UtilityOperationException($"亮显实体失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 取消亮显实体
        /// </summary>
        public void UnhighlightEntities(IEnumerable<ObjectId> entityIds)
        {
            using var operation = _performanceMonitor?.StartOperation("UnhighlightEntities");

            try
            {
                if (entityIds == null)
                    throw new ArgumentNullException(nameof(entityIds));

                var ids = entityIds.Where(id => !id.IsNull && id.IsValid).ToList();
                if (!ids.Any())
                {
                    _logger?.Warning("没有有效的实体ID需要取消亮显");
                    return;
                }

                var database = ids.First().Database;

                using (var transManager = new EnhancedTransactionManager(database))
                {
                    foreach (var id in ids)
                    {
                        try
                        {
                            var entity = transManager.GetObject<Entity>(id, OpenMode.ForRead);
                            entity?.Unhighlight();
                        }
                        catch (Exception ex)
                        {
                            _logger?.Warning($"取消亮显实体 {id} 失败: {ex.Message}");
                        }
                    }

                    transManager.Commit();
                }

                _eventBus?.Publish(new UtilityEvent("EntitiesUnhighlighted", $"Count: {ids.Count}"));
                _logger?.Info($"取消亮显实体成功，数量: {ids.Count}");
            }
            catch (Exception ex)
            {
                _logger?.Error($"取消亮显实体失败: {ex.Message}", ex);
                throw new UtilityOperationException($"取消亮显实体失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 检查字符串是否为空白
        /// </summary>
        public bool IsNullOrWhiteSpace(string value)
        {
            try
            {
                if (value == null) return true;
                return string.IsNullOrEmpty(value.Trim());
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// 生成唯一ID
        /// </summary>
        public string GenerateUniqueId()
        {
            try
            {
                return Guid.NewGuid().ToString("N");
            }
            catch (Exception ex)
            {
                _logger?.Error($"生成唯一ID失败: {ex.Message}", ex);
                return DateTime.Now.Ticks.ToString();
            }
        }

        /// <summary>
        /// 格式化文件大小
        /// </summary>
        public string FormatFileSize(long bytes)
        {
            try
            {
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                double len = bytes;
                int order = 0;

                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }

                return $"{len:0.##} {sizes[order]}";
            }
            catch (Exception ex)
            {
                _logger?.Error($"格式化文件大小失败: {ex.Message}", ex);
                return $"{bytes} B";
            }
        }

        /// <summary>
        /// 安全执行操作
        /// </summary>
        public bool SafeExecute(Action action, string operationName = "未知操作")
        {
            try
            {
                if (action == null)
                    throw new ArgumentNullException(nameof(action));

                action();
                _logger?.Debug($"安全执行成功: {operationName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"安全执行失败 ({operationName}): {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 安全执行操作并返回结果
        /// </summary>
        public T SafeExecute<T>(Func<T> func, T defaultValue = default)
        {
            try
            {
                if (func == null)
                    throw new ArgumentNullException(nameof(func));

                var result = func();
                _logger?.Debug("安全执行成功");
                return result;
            }
            catch (Exception ex)
            {
                _logger?.Error($"安全执行失败: {ex.Message}", ex);
                return defaultValue;
            }
        }

        /// <summary>
        /// 验证字符串格式
        /// </summary>
        public bool ValidateString(string value, string pattern)
        {
            try
            {
                if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(pattern))
                    return false;

                return Regex.IsMatch(value, pattern);
            }
            catch (Exception ex)
            {
                _logger?.Error($"验证字符串失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 安全转换类型
        /// </summary>
        public T SafeConvert<T>(object value, T defaultValue = default(T))
        {
            try
            {
                if (value == null)
                    return defaultValue;

                if (value is T directCast)
                    return directCast;

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                _logger?.Error($"安全转换失败: {ex.Message}", ex);
                return defaultValue;
            }
        }

        /// <summary>
        /// 亮显单个实体
        /// </summary>
        public bool HighlightEntity(ObjectId entityId, bool highlight = true)
        {
            try
            {
                if (entityId.IsNull || !entityId.IsValid)
                    return false;

                var database = entityId.Database;
                using (var transManager = new EnhancedTransactionManager(database))
                {
                    var entity = transManager.GetObject<Entity>(entityId, OpenMode.ForRead);
                    if (entity != null)
                    {
                        if (highlight)
                            entity.Highlight();
                        else
                            entity.Unhighlight();
                    }
                    transManager.Commit();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"亮显实体失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 获取AutoCAD安装路径
        /// </summary>
        public string GetAutoCADPath()
        {
            try
            {
                // 尝试从AutoCAD应用程序获取路径
                var doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    // 获取AutoCAD应用程序路径
                    var path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    return path;
                }

                // 备用方案：从环境变量或已知路径查找
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var possiblePaths = new[]
                {
                    Path.Combine(programFiles, "Autodesk"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Autodesk")
                };

                foreach (var basePath in possiblePaths)
                {
                    if (Directory.Exists(basePath))
                    {
                        var acadDirs = Directory.GetDirectories(basePath, "AutoCAD*", SearchOption.TopDirectoryOnly);
                        if (acadDirs.Any())
                            return acadDirs.First();
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger?.Error($"获取AutoCAD路径失败: {ex.Message}", ex);
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// 工具事件类
    /// </summary>
    public class UtilityEvent : Events.EventArgs
    {
        public string EventType { get; }
        public string Operation { get; }
        public string Details { get; }
        public new DateTime Timestamp { get; }

        public UtilityEvent(string eventType, string operation, string details = null)
            : base("UtilityService")
        {
            EventType = eventType;
            Operation = operation;
            Details = details;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 工具操作异常
    /// </summary>
    public class UtilityOperationException : DotNetARXException
    {
        public UtilityOperationException(string message) : base(message)
        {
        }

        public UtilityOperationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}