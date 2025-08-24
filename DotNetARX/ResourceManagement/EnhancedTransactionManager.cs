namespace DotNetARX.ResourceManagement
{
    /// <summary>
    /// 增强的事务管理器
    /// </summary>
    public class EnhancedTransactionManager : IDisposable
    {
        private readonly Database _database;
        private readonly Transaction _transaction;
        private readonly ILogger _logger;
        private readonly List<IDisposable> _resources;
        private bool _committed = false;
        private bool _disposed = false;

        public Database Database => _database;
        public bool IsCommitted => _committed;
        public bool IsDisposed => _disposed;

        public EnhancedTransactionManager(Database database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _logger = LogManager.GetLogger(typeof(EnhancedTransactionManager));
            _resources = new List<IDisposable>();

            try
            {
                _transaction = _database.TransactionManager.StartTransaction();
                _logger.Debug($"事务已启动 - Database: {_database.Filename}");
            }
            catch (Exception ex)
            {
                _logger.Error("启动事务失败", ex);
                CADExceptionHandler.ThrowCADException("启动事务", "无法启动数据库事务", null);
            }
        }

        /// <summary>
        /// 获取数据库对象
        /// </summary>
        public T GetObject<T>(ObjectId objectId, OpenMode openMode = OpenMode.ForRead, bool openErased = false)
            where T : DBObject
        {
            if (objectId.IsNull)
            {
                CADExceptionHandler.ThrowCADException("获取对象", "对象ID为空", ErrorStatus.InvalidObjectId);
            }

            if (objectId.IsErased && !openErased)
            {
                CADExceptionHandler.ThrowCADException("获取对象", "对象已被删除", ErrorStatus.WasErased);
            }

            try
            {
                var obj = (T)_transaction.GetObject(objectId, openMode, openErased);
                _logger.Debug($"获取对象成功 - Type: {typeof(T).Name}, ObjectId: {objectId}, OpenMode: {openMode}");
                return obj;
            }
            catch (Exception ex)
            {
                _logger.Error($"获取对象失败 - ObjectId: {objectId}, Type: {typeof(T).Name}", ex);
                throw;
            }
        }

        /// <summary>
        /// 尝试获取数据库对象
        /// </summary>
        public bool TryGetObject<T>(ObjectId objectId, out T dbObject, OpenMode openMode = OpenMode.ForRead, bool openErased = false)
            where T : DBObject
        {
            dbObject = null;

            if (objectId.IsNull || objectId.IsErased && !openErased)
            {
                return false;
            }

            try
            {
                dbObject = GetObject<T>(objectId, openMode, openErased);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 添加新创建的数据库对象
        /// </summary>
        public void AddNewlyCreatedDBObject(DBObject dbObject, bool add)
        {
            if (dbObject == null)
            {
                CADExceptionHandler.ThrowCADException("添加对象", "数据库对象为空", null);
            }

            try
            {
                _transaction.AddNewlyCreatedDBObject(dbObject, add);
                _logger.Debug($"添加新对象成功 - Type: {dbObject.GetType().Name}, Add: {add}");
            }
            catch (Exception ex)
            {
                _logger.Error($"添加新对象失败 - Type: {dbObject.GetType().Name}", ex);
                throw;
            }
        }

        /// <summary>
        /// 注册资源以便自动释放
        /// </summary>
        public void RegisterResource(IDisposable resource)
        {
            if (resource != null && !_disposed)
            {
                _resources.Add(resource);
            }
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public void Commit()
        {
            if (_disposed)
            {
                CADExceptionHandler.ThrowCADException("提交事务", "事务管理器已被释放", null);
            }

            if (_committed)
            {
                _logger.Warning("尝试重复提交事务");
                return;
            }

            try
            {
                _transaction.Commit();
                _committed = true;
                _logger.Debug("事务提交成功");
            }
            catch (Exception ex)
            {
                _logger.Error("事务提交失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 中止事务
        /// </summary>
        public void Abort()
        {
            if (_disposed || _committed)
            {
                return;
            }

            try
            {
                _transaction.Abort();
                _logger.Debug("事务已中止");
            }
            catch (Exception ex)
            {
                _logger.Error("事务中止失败", ex);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                // 释放注册的资源
                foreach (var resource in _resources)
                {
                    try
                    {
                        resource?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"释放资源时发生异常: {ex.Message}");
                    }
                }
                _resources.Clear();

                // 如果事务未提交，则中止
                if (!_committed)
                {
                    _transaction?.Abort();
                    _logger.Debug("事务未提交，已自动中止");
                }

                _transaction?.Dispose();
                _logger.Debug("事务管理器已释放");
            }
            catch (Exception ex)
            {
                _logger.Error("释放事务管理器时发生异常", ex);
            }
            finally
            {
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 事务管理器工厂
    /// </summary>
    public static class TransactionManagerFactory
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(TransactionManagerFactory));

        /// <summary>
        /// 创建事务管理器
        /// </summary>
        public static EnhancedTransactionManager Create(Database database)
        {
            if (database == null)
            {
                CADExceptionHandler.ThrowCADException("创建事务管理器", "数据库对象为空", null);
            }

            Logger.Debug($"创建事务管理器 - Database: {database.Filename}");
            return new EnhancedTransactionManager(database);
        }

        /// <summary>
        /// 创建当前文档的事务管理器
        /// </summary>
        public static EnhancedTransactionManager CreateForCurrentDocument()
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                CADExceptionHandler.ThrowCADException("创建事务管理器", "没有活动文档", null);
            }

            return Create(doc.Database);
        }
    }
}