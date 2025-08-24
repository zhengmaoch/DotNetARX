using System.Runtime.CompilerServices;

namespace DotNetARX
{
    /// <summary>
    /// 智能AutoCAD上下文管理器
    /// 自动处理线程安全、文档锁定、事务管理
    /// </summary>
    public sealed class AutoCADContext : IDisposable
    {
        private static readonly ThreadLocal<AutoCADContext> _current = new();
        private readonly Document _document;
        private readonly Database _database;
        private readonly Transaction _transaction;
        private readonly DocumentLock _documentLock;
        private readonly List<IDisposable> _resources = new();
        private bool _disposed = false;

        /// <summary>
        /// 私有构造函数，通过工厂方法创建
        /// </summary>
        private AutoCADContext(Document document, bool needsLock = true)
        {
            try
            {
                _document = document ?? Application.DocumentManager.MdiActiveDocument;
                if (_document == null)
                    throw new InvalidOperationException("无活动AutoCAD文档");

                _database = _document.Database;

                // 智能线程安全检测
                if (needsLock && IsBackgroundThread())
                {
                    _documentLock = _document.LockDocument();
                    _resources.Add(_documentLock);
                }

                _transaction = _database.TransactionManager.StartTransaction();
                _resources.Add(_transaction);
            }
            catch
            {
                // 确保在构造失败时释放已分配的资源
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// 系统初始化
        /// </summary>
        public static void Initialize()
        {
            // 预热系统组件
        }

        /// <summary>
        /// 获取当前线程的上下文
        /// </summary>
        public static AutoCADContext Current
        {
            get
            {
                if (_current.Value == null || _current.Value._disposed)
                {
                    _current.Value = new AutoCADContext(null);
                }
                return _current.Value;
            }
        }

        /// <summary>
        /// 在安全上下文中执行操作 - 自动处理所有资源管理
        /// </summary>
        public static T ExecuteSafely<T>(Func<T> operation)
        {
            using var context = new AutoCADContext(null);
            try
            {
                var result = operation();
                context.Commit();
                return result;
            }
            catch
            {
                context.Abort();
                throw;
            }
        }

        /// <summary>
        /// 批量操作执行 - 优化的事务管理
        /// </summary>
        public static void ExecuteBatch(Action<AutoCADContext> batchOperation)
        {
            using var context = new AutoCADContext(null);
            try
            {
                batchOperation(context);
                context.Commit();
            }
            catch
            {
                context.Abort();
                throw;
            }
        }

        /// <summary>
        /// 异步安全执行 - 处理AutoCAD的线程限制
        /// </summary>
        public static async Task<T> ExecuteSafelyAsync<T>(Func<T> operation)
        {
            // AutoCAD不支持真正的异步，这里使用Task.FromResult包装同步操作
            // 避免Task.Run造成的线程安全问题
            return await Task.FromResult(ExecuteSafely(operation));
        }

        /// <summary>
        /// 获取数据库对象（强类型）
        /// </summary>
        public T GetObject<T>(ObjectId objectId, OpenMode mode = OpenMode.ForRead) where T : DBObject
        {
            if (objectId.IsNull || objectId.IsErased)
                return null;

            try
            {
                return _transaction.GetObject(objectId, mode) as T;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception)
            {
                // AutoCAD对象可能已被删除或无效
                return null;
            }
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public void Commit()
        {
            if (!_disposed && _transaction != null)
            {
                _transaction.Commit();
            }
        }

        /// <summary>
        /// 中止事务
        /// </summary>
        public void Abort()
        {
            if (!_disposed && _transaction != null)
            {
                _transaction.Abort();
            }
        }

        /// <summary>
        /// 检查是否在后台线程
        /// </summary>
        private static bool IsBackgroundThread()
        {
            try
            {
                // AutoCAD .NET API does not provide IsApplicationThread directly.
                // Heuristic: if current thread is not the main thread, treat as background.
                return Thread.CurrentThread.ManagedThreadId != 1;
            }
            catch
            {
                // 在某些情况下可能无法检测，默认需要锁定
                return true;
            }
        }

        #region 属性访问

        public Database Database => _database;
        public Document Document => _document;
        public Transaction Transaction => _transaction;
        public bool IsDisposed => _disposed;

        #endregion 属性访问

        #region IDisposable实现

        public void Dispose()
        {
            if (!_disposed)
            {
                // 按相反顺序释放资源
                foreach (var resource in _resources.AsEnumerable().Reverse())
                {
                    try
                    {
                        resource?.Dispose();
                    }
                    catch
                    {
                        // 忽略释放时的异常，避免掩盖原始异常
                    }
                }

                _resources.Clear();
                _current.Value = null;
                _disposed = true;
            }
        }

        #endregion IDisposable实现
    }

    /// <summary>
    /// Database扩展方法 - 高性能辅助
    /// </summary>
    public static class DatabaseExtensions
    {
        /// <summary>
        /// 获取模型空间ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectId GetModelSpaceId(this Database database)
        {
            return SymbolUtilityServices.GetBlockModelSpaceId(database);
        }

        /// <summary>
        /// 获取图纸空间ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectId GetPaperSpaceId(this Database database)
        {
            return SymbolUtilityServices.GetBlockPaperSpaceId(database);
        }

        /// <summary>
        /// 获取当前空间ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectId GetCurrentSpaceId(this Database database)
        {
            return database.CurrentSpaceId;
        }
    }
}