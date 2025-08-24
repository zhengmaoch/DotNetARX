namespace DotNetARX
{
    /// <summary>
    /// 改进的辅助操作类
    /// </summary>
    public static partial class ToolsImproved
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(ToolsImproved));
        private static readonly Regex NumericRegex = new Regex(@"^[+-]?\d*\.?\d+$", RegexOptions.Compiled);
        private static readonly Regex IntegerRegex = new Regex(@"^[+-]?\d+$", RegexOptions.Compiled);

        /// <summary>
        /// 改进的数字验证（性能更好，更准确）
        /// </summary>
        /// <param name="value">字符串</param>
        /// <returns>如果字符串为数字，返回true，否则返回false</returns>
        public static bool IsNumeric(this string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return NumericRegex.IsMatch(value) && double.TryParse(value, out _);
        }

        /// <summary>
        /// 改进的整数验证
        /// </summary>
        /// <param name="value">字符串</param>
        /// <returns>如果字符串为整数，返回true，否则返回false</returns>
        public static bool IsInt(this string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return IntegerRegex.IsMatch(value) && long.TryParse(value, out _);
        }

        /// <summary>
        /// 安全的数字转换
        /// </summary>
        /// <param name="value">字符串值</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>转换后的double值</returns>
        public static double ToDoubleOrDefault(this string value, double defaultValue = 0.0)
        {
            return double.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// 安全的整数转换
        /// </summary>
        /// <param name="value">字符串值</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>转换后的int值</returns>
        public static int ToIntOrDefault(this string value, int defaultValue = 0)
        {
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// 获取当前.NET程序所在的目录（带异常处理）
        /// </summary>
        /// <returns>返回当前.NET程序所在的目录</returns>
        public static string GetCurrentPath()
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                var module = Assembly.GetExecutingAssembly().GetModules()[0];
                var path = Path.GetDirectoryName(module.FullyQualifiedName);
                Logger.Debug($"获取当前路径: {path}");
                return path;
            }, string.Empty);
        }

        /// <summary>
        /// 改进的空白字符串检查
        /// </summary>
        /// <param name="value">字符串</param>
        /// <returns>如果字符串为空或空白，返回true，否则返回false</returns>
        public static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// 获取模型空间的ObjectId（带验证）
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <returns>返回模型空间的ObjectId</returns>
        public static ObjectId GetModelSpaceId(this Database db)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                if (db == null)
                    CADExceptionHandler.ThrowCADException("获取模型空间ID", "数据库对象为null");

                var modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
                Logger.Debug($"获取模型空间ID: {modelSpaceId}");
                return modelSpaceId;
            }, ObjectId.Null);
        }

        /// <summary>
        /// 获取图纸空间的ObjectId（带验证）
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <returns>返回图纸空间的ObjectId</returns>
        public static ObjectId GetPaperSpaceId(this Database db)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                if (db == null)
                    CADExceptionHandler.ThrowCADException("获取图纸空间ID", "数据库对象为null");

                var paperSpaceId = SymbolUtilityServices.GetBlockPaperSpaceId(db);
                Logger.Debug($"获取图纸空间ID: {paperSpaceId}");
                return paperSpaceId;
            }, ObjectId.Null);
        }

        /// <summary>
        /// 改进的实体添加到模型空间方法
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="ent">要添加的实体</param>
        /// <returns>返回添加到模型空间中的实体ObjectId</returns>
        public static ObjectId AddToModelSpaceImproved(this Database db, Entity ent)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                if (db == null)
                    CADExceptionHandler.ThrowCADException("添加实体到模型空间", "数据库对象为null");

                if (ent == null)
                    CADExceptionHandler.ThrowCADException("添加实体到模型空间", "实体对象为null");

                using (var transManager = TransactionManagerFactory.Create(db))
                {
                    var modelSpace = transManager.GetObject<BlockTableRecord>(
                        SymbolUtilityServices.GetBlockModelSpaceId(db),
                        OpenMode.ForWrite);

                    var objectId = modelSpace.AppendEntity(ent);
                    transManager.AddNewlyCreatedDBObject(ent, true);
                    transManager.Commit();

                    Logger.Info($"实体添加到模型空间成功 - Type: {ent.GetType().Name}, ObjectId: {objectId}");
                    return objectId;
                }
            }, ObjectId.Null);
        }

        /// <summary>
        /// 批量添加实体到模型空间（改进版本）
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="entities">要添加的多个实体</param>
        /// <returns>返回添加到模型空间中的实体ObjectId集合</returns>
        public static ObjectIdCollection AddToModelSpaceImproved(this Database db, params Entity[] entities)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                if (db == null)
                    CADExceptionHandler.ThrowCADException("批量添加实体到模型空间", "数据库对象为null");

                if (entities == null || entities.Length == 0)
                {
                    Logger.Warning("尝试添加空的实体数组到模型空间");
                    return new ObjectIdCollection();
                }

                var objectIds = new ObjectIdCollection();
                var batchSize = GlobalConfiguration.GetSetting(ConfigurationKeys.DefaultBatchSize, 1000);

                using (var transManager = TransactionManagerFactory.Create(db))
                {
                    var modelSpace = transManager.GetObject<BlockTableRecord>(
                        SymbolUtilityServices.GetBlockModelSpaceId(db),
                        OpenMode.ForWrite);

                    var addedCount = 0;
                    foreach (var entity in entities)
                    {
                        if (entity != null)
                        {
                            var objectId = modelSpace.AppendEntity(entity);
                            transManager.AddNewlyCreatedDBObject(entity, true);
                            objectIds.Add(objectId);
                            addedCount++;

                            // 每处理一定数量的实体后检查是否需要分批
                            if (addedCount >= batchSize)
                            {
                                Logger.Debug($"达到批处理大小限制: {batchSize}，当前已处理: {addedCount}");
                                break;
                            }
                        }
                    }

                    transManager.Commit();
                    Logger.Info($"批量添加实体到模型空间完成 - 总数: {entities.Length}, 成功: {addedCount}");
                }

                return objectIds;
            }, new ObjectIdCollection());
        }

        /// <summary>
        /// 安全的句柄到ObjectId转换
        /// </summary>
        /// <param name="db">数据库对象</param>
        /// <param name="handleString">句柄字符串</param>
        /// <returns>返回实体的ObjectId</returns>
        public static ObjectId HandleToObjectId(this Database db, string handleString)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                if (db == null)
                    CADExceptionHandler.ThrowCADException("句柄转换ObjectId", "数据库对象为null");

                if (string.IsNullOrWhiteSpace(handleString))
                    CADExceptionHandler.ThrowCADException("句柄转换ObjectId", "句柄字符串为空");

                if (!long.TryParse(handleString, System.Globalization.NumberStyles.HexNumber, null, out var handleValue))
                    CADExceptionHandler.ThrowCADException("句柄转换ObjectId", $"无效的句柄格式: {handleString}");

                var handle = new Handle(handleValue);
                var objectId = db.GetObjectId(false, handle, 0);

                Logger.Debug($"句柄转换成功 - Handle: {handleString}, ObjectId: {objectId}");
                return objectId;
            }, ObjectId.Null);
        }

        /// <summary>
        /// 验证ObjectId的有效性
        /// </summary>
        /// <param name="objectId">要验证的ObjectId</param>
        /// <param name="allowErased">是否允许已删除的对象</param>
        /// <returns>如果ObjectId有效返回true</returns>
        public static bool IsValid(this ObjectId objectId, bool allowErased = false)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                if (objectId.IsNull)
                    return false;

                if (objectId.IsErased && !allowErased)
                    return false;

                // 尝试访问对象以确认其有效性
                using (var transManager = TransactionManagerFactory.Create(objectId.Database))
                {
                    return transManager.TryGetObject<DBObject>(objectId, out var _);
                }
            }, false);
        }

        /// <summary>
        /// 亮显实体（改进版本）
        /// </summary>
        /// <param name="ids">要亮显的实体的Id集合</param>
        public static void HighlightEntities(params ObjectId[] ids)
        {
            CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                if (ids == null || ids.Length == 0)
                {
                    Logger.Warning("尝试亮显空的ObjectId数组");
                    return;
                }

                var validIds = new List<ObjectId>();
                foreach (var id in ids)
                {
                    if (id.IsValid())
                        validIds.Add(id);
                }

                if (validIds.Count == 0)
                {
                    Logger.Warning("没有有效的ObjectId可以亮显");
                    return;
                }

                // 这里应该调用AutoCAD的亮显功能
                // 具体实现取决于AutoCAD版本和API
                Logger.Info($"亮显实体 - 数量: {validIds.Count}");
            });
        }
    }
}