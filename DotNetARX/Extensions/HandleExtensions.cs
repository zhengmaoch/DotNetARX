namespace DotNetARX.Extensions
{
    /// <summary>
    /// Handle 扩展方法集合
    /// 提供便捷的Handle操作和转换功能
    /// </summary>
    public static class HandleExtensions
    {
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(HandleExtensions));

        /// <summary>
        /// 从Handle获取ObjectId
        /// </summary>
        /// <param name="handle">Handle对象</param>
        /// <param name="database">数据库对象，为null时使用当前数据库</param>
        /// <returns>对应的ObjectId，失败时返回ObjectId.Null</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectId GetObjectId(this Handle handle, Database database = null)
        {
            if (handle == new Handle(0)) return ObjectId.Null;

            return AutoCADContext.ExecuteSafely(() =>
            {
                var db = database ?? AutoCADContext.Current?.Database ?? HostApplicationServices.WorkingDatabase;
                if (db == null) return ObjectId.Null;

                if (db.TryGetObjectId(handle, out ObjectId objectId))
                {
                    return objectId;
                }

                return ObjectId.Null;
            });
        }

        /// <summary>
        /// 尝试从Handle获取ObjectId
        /// </summary>
        /// <param name="handle">Handle对象</param>
        /// <param name="objectId">输出的ObjectId</param>
        /// <param name="database">数据库对象，为null时使用当前数据库</param>
        /// <returns>是否成功获取</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetObjectId(this Handle handle, out ObjectId objectId, Database database = null)
        {
            objectId = ObjectId.Null;

            if (handle == new Handle(0)) return false;

            try
            {
                var db = database ?? AutoCADContext.Current?.Database ?? HostApplicationServices.WorkingDatabase;
                if (db == null) return false;

                return db.TryGetObjectId(handle, out objectId);
            }
            catch (Exception ex)
            {
                _logger.Debug($"从 Handle 获取 ObjectId 失败: {handle}, 错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 从Handle直接获取实体对象
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="handle">Handle对象</param>
        /// <param name="mode">打开模式</param>
        /// <param name="database">数据库对象</param>
        /// <returns>实体对象，失败时返回null</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetEntity<T>(this Handle handle, OpenMode mode = OpenMode.ForRead, Database database = null) where T : Entity
        {
            var objectId = handle.GetObjectId(database);
            return objectId.IsNull ? null : objectId.GetEntity<T>(mode);
        }

        /// <summary>
        /// 尝试从Handle获取实体对象
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="handle">Handle对象</param>
        /// <param name="entity">输出的实体对象</param>
        /// <param name="mode">打开模式</param>
        /// <param name="database">数据库对象</param>
        /// <returns>是否成功获取</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetEntity<T>(this Handle handle, out T entity, OpenMode mode = OpenMode.ForRead, Database database = null) where T : Entity
        {
            entity = null;

            if (handle.TryGetObjectId(out ObjectId objectId, database))
            {
                if (!objectId.IsNull)
                {
                    try
                    {
                        entity = objectId.GetEntity<T>(mode);
                        return entity != null;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 从Handle获取DBObject
        /// </summary>
        /// <typeparam name="T">DBObject类型</typeparam>
        /// <param name="handle">Handle对象</param>
        /// <param name="mode">打开模式</param>
        /// <param name="database">数据库对象</param>
        /// <returns>DBObject对象，失败时返回null</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetObject<T>(this Handle handle, OpenMode mode = OpenMode.ForRead, Database database = null) where T : Entity
        {
            var objectId = handle.GetObjectId(database);
            return objectId.IsNull ? null : objectId.GetEntity<T>(mode);
        }

        /// <summary>
        /// 检查Handle是否有效（对应的对象是否存在）
        /// </summary>
        /// <param name="handle">Handle对象</param>
        /// <param name="database">数据库对象</param>
        /// <returns>是否有效</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(this Handle handle, Database database = null)
        {
            if (handle == new Handle(0)) return false;

            return AutoCADContext.ExecuteSafely(() =>
            {
                var db = database ?? AutoCADContext.Current?.Database ?? HostApplicationServices.WorkingDatabase;
                if (db == null) return false;

                return db.TryGetObjectId(handle, out _);
            });
        }

        /// <summary>
        /// 检查Handle对应的对象是否已被删除
        /// </summary>
        /// <param name="handle">Handle对象</param>
        /// <param name="database">数据库对象</param>
        /// <returns>是否已删除</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsErased(this Handle handle, Database database = null)
        {
            if (handle == new Handle(0)) return true;

            return AutoCADContext.ExecuteSafely(() =>
            {
                var objectId = handle.GetObjectId(database);
                return objectId.IsNull || objectId.IsErased;
            });
        }

        /// <summary>
        /// 获取Handle对应对象的类型名称
        /// </summary>
        /// <param name="handle">Handle对象</param>
        /// <param name="database">数据库对象</param>
        /// <returns>类型名称，失败时返回null</returns>
        public static string GetObjectTypeName(this Handle handle, Database database = null)
        {
            if (handle == new Handle(0)) return null;

            return AutoCADContext.ExecuteSafely(() =>
            {
                var objectId = handle.GetObjectId(database);
                if (objectId.IsNull) return null;

                var context = AutoCADContext.Current;
                var obj = context?.GetObject<DBObject>(objectId, OpenMode.ForRead);
                return obj?.GetType().Name;
            });
        }

        /// <summary>
        /// 获取Handle的字符串表示形式（便于序列化和存储）
        /// </summary>
        /// <param name="handle">Handle对象</param>
        /// <returns>Handle的字符串表示</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToHexString(this Handle handle)
        {
            return handle == new Handle(0) ? string.Empty : handle.Value.ToString("X");
        }

        /// <summary>
        /// 从十六进制字符串创建Handle
        /// </summary>
        /// <param name="hexString">十六进制字符串</param>
        /// <returns>Handle对象</returns>
        public static Handle FromHexString(string hexString)
        {
            if (string.IsNullOrWhiteSpace(hexString))
                return new Handle(0);

            try
            {
                if (long.TryParse(hexString, System.Globalization.NumberStyles.HexNumber, null, out long value))
                {
                    return new Handle(value);
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"从十六进制字符串创建Handle失败: {hexString}, 错误: {ex.Message}");
            }

            return new Handle(0);
        }

        /// <summary>
        /// 批量验证Handle集合的有效性
        /// </summary>
        /// <param name="handles">Handle集合</param>
        /// <param name="database">数据库对象</param>
        /// <returns>有效Handle的过滤结果</returns>
        public static IEnumerable<Handle> FilterValid(this IEnumerable<Handle> handles, Database database = null)
        {
            if (handles == null) yield break;

            var db = database ?? AutoCADContext.Current?.Database ?? HostApplicationServices.WorkingDatabase;
            if (db == null) yield break;

            foreach (var handle in handles)
            {
                if (handle.IsValid(db))
                {
                    yield return handle;
                }
            }
        }

        /// <summary>
        /// 批量转换Handle到ObjectId
        /// </summary>
        /// <param name="handles">Handle集合</param>
        /// <param name="database">数据库对象</param>
        /// <param name="skipInvalid">是否跳过无效的Handle</param>
        /// <returns>ObjectId集合</returns>
        public static IEnumerable<ObjectId> ToObjectIds(this IEnumerable<Handle> handles, Database database = null, bool skipInvalid = true)
        {
            if (handles == null) yield break;

            foreach (var handle in handles)
            {
                var objectId = handle.GetObjectId(database);

                if (skipInvalid && objectId.IsNull)
                    continue;

                yield return objectId;
            }
        }

        /// <summary>
        /// 获取Handle的ArxHandleInfo信息
        /// </summary>
        /// <param name="handle">Handle对象</param>
        /// <param name="database">数据库对象</param>
        /// <returns>Handle信息</returns>
        public static ArxHandleInfo GetInfo(this Handle handle, Database database = null)
        {
            var info = new ArxHandleInfo
            {
                Handle = handle,
                IsNull = handle == new Handle(0),
                HexString = handle.ToHexString()
            };

            if (handle != new Handle(0))
            {
                info.IsValid = handle.IsValid(database);
                if (info.IsValid)
                {
                    info.ObjectId = handle.GetObjectId(database);
                    info.ObjectTypeName = handle.GetObjectTypeName(database);
                    info.IsErased = handle.IsErased(database);
                }
            }

            return info;
        }
    }

    /// <summary>
    /// Handle信息结构体 - 避免与AutoCAD原生类型冲突
    /// </summary>
    public struct ArxHandleInfo
    {
        /// <summary>
        /// Handle对象
        /// </summary>
        public Handle Handle { get; set; }

        /// <summary>
        /// 对应的ObjectId
        /// </summary>
        public ObjectId ObjectId { get; set; }

        /// <summary>
        /// 是否为空Handle
        /// </summary>
        public bool IsNull { get; set; }

        /// <summary>
        /// 是否为有效Handle
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 对应对象是否已删除
        /// </summary>
        public bool IsErased { get; set; }

        /// <summary>
        /// 对象类型名称
        /// </summary>
        public string ObjectTypeName { get; set; }

        /// <summary>
        /// Handle的十六进制字符串表示
        /// </summary>
        public string HexString { get; set; }

        /// <summary>
        /// 转换为字符串表示
        /// </summary>
        public override string ToString()
        {
            if (IsNull) return "Handle.Empty";
            return $"Handle({HexString}) -> {ObjectTypeName ?? "Unknown"} [{(IsValid ? "Valid" : "Invalid")}]";
        }
    }
}