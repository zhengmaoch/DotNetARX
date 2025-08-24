using DotNetARX.Events;
using DotNetARX.Interfaces;

namespace DotNetARX.Async
{
    /// <summary>
    /// 异步操作结果
    /// </summary>
    public class AsyncOperationResult<T>
    {
        public bool Success { get; set; }
        public T Result { get; set; }
        public Exception Exception { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }

        public static AsyncOperationResult<T> Successful(T result, TimeSpan duration)
        {
            return new AsyncOperationResult<T>
            {
                Success = true,
                Result = result,
                Duration = duration
            };
        }

        public static AsyncOperationResult<T> Failed(Exception exception, TimeSpan duration)
        {
            return new AsyncOperationResult<T>
            {
                Success = false,
                Exception = exception,
                ErrorMessage = exception?.Message,
                Duration = duration
            };
        }
    }

    /// <summary>
    /// 异步进度报告
    /// </summary>
    public class AsyncProgress
    {
        public int Current { get; set; }
        public int Total { get; set; }
        public string Message { get; set; }
        public double Percentage => Total > 0 ? (double)Current / Total * 100 : 0;
        public bool IsCompleted => Current >= Total;
    }

    /// <summary>
    /// 异步实体操作服务
    /// </summary>
    public class AsyncEntityOperationService : IEntityOperations
    {
        private readonly ILogger _logger;
        private readonly IPerformanceMonitor _performanceMonitor;

        public AsyncEntityOperationService(
            ILogger logger = null,
            IPerformanceMonitor performanceMonitor = null)
        {
            _logger = logger ?? LogManager.GetLogger(typeof(AsyncEntityOperationService));
            _performanceMonitor = performanceMonitor ?? GlobalPerformanceMonitor.Instance;
        }

        /// <summary>
        /// 异步移动实体
        /// </summary>
        public async Task<AsyncOperationResult<bool>> MoveEntityAsync(
            ObjectId entityId,
            Point3d fromPoint,
            Point3d toPoint,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;

            try
            {
                using (_performanceMonitor.StartTimer("AsyncMoveEntity", "EntityOperations"))
                {
                    // 在主线程上执行，避免Task.Run的线程安全问题
                    cancellationToken.ThrowIfCancellationRequested();
                    var result = MoveEntity(entityId, fromPoint, toPoint);

                    await CADEventManager.OnEntityModifiedAsync(entityId, "Entity", "AsyncEntityOperationService");

                    var duration = DateTime.Now - startTime;
                    return AsyncOperationResult<bool>.Successful(result, duration);
                }
            }
            catch (OperationCanceledException)
            {
                var duration = DateTime.Now - startTime;
                _logger.Info($"异步移动实体操作被取消 - EntityId: {entityId}");
                return AsyncOperationResult<bool>.Failed(new OperationCanceledException("操作被取消"), duration);
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                _logger.Error($"异步移动实体失败 - EntityId: {entityId}", ex);
                await CADEventManager.OnErrorOccurredAsync(ex, "AsyncMoveEntity", "AsyncEntityOperationService");
                return AsyncOperationResult<bool>.Failed(ex, duration);
            }
        }

        /// <summary>
        /// 批量异步操作实体
        /// </summary>
        public async Task<List<AsyncOperationResult<ObjectId>>> BatchOperationAsync<T>(
            IEnumerable<T> items,
            Func<T, ObjectId> operation,
            IProgress<AsyncProgress> progress = null,
            CancellationToken cancellationToken = default,
            int maxConcurrency = 4)
        {
            var itemList = items.ToList();
            var results = new List<AsyncOperationResult<ObjectId>>();
            var completed = 0;

            using (_performanceMonitor.StartTimer("AsyncBatchOperation", "EntityOperations"))
            {
                var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
                var tasks = itemList.Select(async item =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        var startTime = DateTime.Now;
                        try
                        {
                            var result = await Task.Run(() => operation(item), cancellationToken);
                            var duration = DateTime.Now - startTime;

                            Interlocked.Increment(ref completed);
                            progress?.Report(new AsyncProgress
                            {
                                Current = completed,
                                Total = itemList.Count,
                                Message = $"已完成 {completed}/{itemList.Count} 项操作"
                            });

                            return AsyncOperationResult<ObjectId>.Successful(result, duration);
                        }
                        catch (Exception ex)
                        {
                            var duration = DateTime.Now - startTime;
                            _logger.Error($"批量操作项失败: {item}", ex);

                            Interlocked.Increment(ref completed);
                            progress?.Report(new AsyncProgress
                            {
                                Current = completed,
                                Total = itemList.Count,
                                Message = $"已完成 {completed}/{itemList.Count} 项操作 (含失败)"
                            });

                            return AsyncOperationResult<ObjectId>.Failed(ex, duration);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }).ToList();

                results.AddRange(await Task.WhenAll(tasks));
            }

            var successCount = results.Count(r => r.Success);
            _logger.Info($"批量异步操作完成 - 总计: {itemList.Count}, 成功: {successCount}, 失败: {itemList.Count - successCount}");

            return results;
        }

        /// <summary>
        /// 异步获取实体集合
        /// </summary>
        public async Task<AsyncOperationResult<List<T>>> GetEntitiesAsync<T>(
            Database database,
            Func<T, bool> predicate = null,
            CancellationToken cancellationToken = default) where T : Entity
        {
            var startTime = DateTime.Now;

            try
            {
                using (_performanceMonitor.StartTimer("AsyncGetEntities", "DatabaseOperations"))
                {
                    var result = await Task.Run(() =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var entities = database.GetEntsInDatabase<T>();

                        if (predicate != null)
                        {
                            entities = entities.Where(predicate).ToList();
                        }

                        return entities;
                    }, cancellationToken);

                    var duration = DateTime.Now - startTime;
                    _logger.Info($"异步获取实体完成 - 类型: {typeof(T).Name}, 数量: {result.Count}");

                    return AsyncOperationResult<List<T>>.Successful(result, duration);
                }
            }
            catch (OperationCanceledException)
            {
                var duration = DateTime.Now - startTime;
                _logger.Info($"异步获取实体操作被取消 - 类型: {typeof(T).Name}");
                return AsyncOperationResult<List<T>>.Failed(new OperationCanceledException("操作被取消"), duration);
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                _logger.Error($"异步获取实体失败 - 类型: {typeof(T).Name}", ex);
                return AsyncOperationResult<List<T>>.Failed(ex, duration);
            }
        }

        #region IEntityOperations Implementation

        public bool MoveEntity(ObjectId entityId, Point3d fromPoint, Point3d toPoint)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                if (entityId.IsNull || entityId.IsErased)
                {
                    CADExceptionHandler.ThrowEntityException("移动实体", entityId, "实体ID无效或已被删除");
                }

                if (fromPoint.IsEqualTo(toPoint))
                {
                    _logger.Info("源点与目标点相同，无需移动");
                    return true;
                }

                using (var transManager = TransactionManagerFactory.Create(entityId.Database))
                {
                    var entity = transManager.GetObject<Entity>(entityId, OpenMode.ForWrite);

                    var displacement = toPoint - fromPoint;
                    var moveMatrix = Matrix3d.Displacement(displacement);
                    entity.TransformBy(moveMatrix);

                    transManager.Commit();
                    _logger.Info($"实体移动成功 - EntityId: {entityId}");
                    return true;
                }
            }, false);
        }

        public ObjectId CopyEntity(ObjectId entityId, Point3d fromPoint, Point3d toPoint)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                if (entityId.IsNull || entityId.IsErased)
                {
                    CADExceptionHandler.ThrowEntityException("复制实体", entityId, "实体ID无效或已被删除");
                }

                using (var transManager = TransactionManagerFactory.Create(entityId.Database))
                {
                    var originalEntity = transManager.GetObject<Entity>(entityId, OpenMode.ForRead);

                    var displacement = toPoint - fromPoint;
                    var moveMatrix = Matrix3d.Displacement(displacement);
                    var copiedEntity = originalEntity.GetTransformedCopy(moveMatrix);

                    var modelSpace = transManager.GetObject<BlockTableRecord>(
                        entityId.Database.GetModelSpaceId(),
                        OpenMode.ForWrite);

                    var copyId = modelSpace.AppendEntity(copiedEntity);
                    transManager.AddNewlyCreatedDBObject(copiedEntity, true);
                    transManager.Commit();

                    _logger.Info($"实体复制成功 - 原实体: {entityId}, 新实体: {copyId}");
                    return copyId;
                }
            }, ObjectId.Null);
        }

        public bool RotateEntity(ObjectId entityId, Point3d basePoint, double angle)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                if (entityId.IsNull || entityId.IsErased)
                {
                    CADExceptionHandler.ThrowEntityException("旋转实体", entityId, "实体ID无效或已被删除");
                }

                if (Math.Abs(angle) < 1e-10)
                {
                    _logger.Info("旋转角度过小，无需操作");
                    return true;
                }

                using (var transManager = TransactionManagerFactory.Create(entityId.Database))
                {
                    var entity = transManager.GetObject<Entity>(entityId, OpenMode.ForWrite);

                    var rotationMatrix = Matrix3d.Rotation(angle, Vector3d.ZAxis, basePoint);
                    entity.TransformBy(rotationMatrix);

                    transManager.Commit();
                    _logger.Info($"实体旋转成功 - EntityId: {entityId}");
                    return true;
                }
            }, false);
        }

        public bool ScaleEntity(ObjectId entityId, Point3d basePoint, double scaleFactor)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                if (entityId.IsNull || entityId.IsErased)
                {
                    CADExceptionHandler.ThrowEntityException("缩放实体", entityId, "实体ID无效或已被删除");
                }

                if (scaleFactor <= 0)
                {
                    CADExceptionHandler.ThrowEntityException("缩放实体", entityId, "缩放系数必须大于0");
                }

                if (Math.Abs(scaleFactor - 1.0) < 1e-10)
                {
                    _logger.Info("缩放系数接近1，无需操作");
                    return true;
                }

                using (var transManager = TransactionManagerFactory.Create(entityId.Database))
                {
                    var entity = transManager.GetObject<Entity>(entityId, OpenMode.ForWrite);

                    var scaleMatrix = Matrix3d.Scaling(scaleFactor, basePoint);
                    entity.TransformBy(scaleMatrix);

                    transManager.Commit();
                    _logger.Info($"实体缩放成功 - EntityId: {entityId}");
                    return true;
                }
            }, false);
        }

        public ObjectIdCollection OffsetEntity(ObjectId entityId, double distance)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                if (entityId.IsNull || entityId.IsErased)
                {
                    CADExceptionHandler.ThrowEntityException("偏移实体", entityId, "实体ID无效或已被删除");
                }

                if (Math.Abs(distance) < 1e-10)
                {
                    _logger.Info("偏移距离过小，无需操作");
                    return new ObjectIdCollection();
                }

                using (var transManager = TransactionManagerFactory.Create(entityId.Database))
                {
                    var entity = transManager.GetObject<Entity>(entityId, OpenMode.ForRead);

                    if (!(entity is Curve curve))
                    {
                        CADExceptionHandler.ThrowEntityException("偏移实体", entityId, "只能偏移曲线类型的实体");
                    }

                    var offsetCurves = curve.GetOffsetCurves(distance);
                    var resultIds = new ObjectIdCollection();

                    if (offsetCurves.Count > 0)
                    {
                        var modelSpace = transManager.GetObject<BlockTableRecord>(
                            entityId.Database.GetModelSpaceId(),
                            OpenMode.ForWrite);

                        foreach (Entity offsetEntity in offsetCurves)
                        {
                            var offsetId = modelSpace.AppendEntity(offsetEntity);
                            transManager.AddNewlyCreatedDBObject(offsetEntity, true);
                            resultIds.Add(offsetId);
                        }

                        transManager.Commit();
                        _logger.Info($"实体偏移成功 - 原实体: {entityId}, 生成实体数: {resultIds.Count}");
                    }

                    return resultIds;
                }
            }, new ObjectIdCollection());
        }

        public ObjectId MirrorEntity(ObjectId entityId, Point3d mirrorPt1, Point3d mirrorPt2, bool eraseSource)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                if (entityId.IsNull || entityId.IsErased)
                {
                    CADExceptionHandler.ThrowEntityException("镜像实体", entityId, "实体ID无效或已被删除");
                }

                using (var transManager = TransactionManagerFactory.Create(entityId.Database))
                {
                    var entity = transManager.GetObject<Entity>(entityId, OpenMode.ForWrite);

                    var mirrorLine = new Line3d(mirrorPt1, mirrorPt2);
                    var mirrorMatrix = Matrix3d.Mirroring(mirrorLine);

                    ObjectId resultId = entityId;

                    if (eraseSource)
                    {
                        entity.TransformBy(mirrorMatrix);
                        _logger.Info($"实体镜像成功（替换原实体） - EntityId: {entityId}");
                    }
                    else
                    {
                        var mirroredEntity = entity.GetTransformedCopy(mirrorMatrix);

                        var modelSpace = transManager.GetObject<BlockTableRecord>(
                            entityId.Database.GetModelSpaceId(),
                            OpenMode.ForWrite);

                        resultId = modelSpace.AppendEntity(mirroredEntity);
                        transManager.AddNewlyCreatedDBObject(mirroredEntity, true);

                        _logger.Info($"实体镜像成功（创建副本） - 原实体: {entityId}, 新实体: {resultId}");
                    }

                    transManager.Commit();
                    return resultId;
                }
            }, ObjectId.Null);
        }

        public bool ValidateEntity(ObjectId entityId)
        {
            return CADExceptionHandler.ExecuteWithExceptionHandling(() =>
            {
                if (entityId.IsNull || entityId.IsErased)
                    return false;

                using (var transManager = TransactionManagerFactory.Create(entityId.Database))
                {
                    return transManager.TryGetObject<Entity>(entityId, out var _);
                }
            }, false);
        }

        #endregion IEntityOperations Implementation
    }

    /// <summary>
    /// 异步任务管理器
    /// </summary>
    public class AsyncTaskManager : IDisposable
    {
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly List<Task> _runningTasks;
        private readonly object _lockObject = new object();
        private bool _disposed = false;

        public AsyncTaskManager(ILogger logger = null)
        {
            _logger = logger ?? LogManager.GetLogger(typeof(AsyncTaskManager));
            _cancellationTokenSource = new CancellationTokenSource();
            _runningTasks = new List<Task>();
        }

        /// <summary>
        /// 运行异步任务
        /// </summary>
        public async Task<T> RunAsync<T>(
            Func<CancellationToken, Task<T>> taskFactory,
            TimeSpan? timeout = null,
            string taskName = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AsyncTaskManager));

            var task = taskFactory(_cancellationTokenSource.Token);

            lock (_lockObject)
            {
                _runningTasks.Add(task);
            }

            try
            {
                if (timeout.HasValue)
                {
                    using (var timeoutCts = new CancellationTokenSource(timeout.Value))
                    using (var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                        _cancellationTokenSource.Token, timeoutCts.Token))
                    {
                        var completedTask = await Task.WhenAny(task, Task.Delay(timeout.Value, combinedCts.Token));

                        if (completedTask == task)
                        {
                            return await task;
                        }
                        else
                        {
                            throw new TimeoutException($"任务执行超时: {taskName ?? "未命名任务"}");
                        }
                    }
                }
                else
                {
                    return await task;
                }
            }
            finally
            {
                lock (_lockObject)
                {
                    _runningTasks.Remove(task);
                }
            }
        }

        /// <summary>
        /// 取消所有任务
        /// </summary>
        public void CancelAll()
        {
            if (!_disposed)
            {
                _cancellationTokenSource.Cancel();
                _logger.Info("已请求取消所有异步任务");
            }
        }

        /// <summary>
        /// 等待所有任务完成
        /// </summary>
        public async Task WaitAllAsync(TimeSpan? timeout = null)
        {
            Task[] tasksToWait;
            lock (_lockObject)
            {
                tasksToWait = _runningTasks.ToArray();
            }

            if (tasksToWait.Length == 0) return;

            try
            {
                if (timeout.HasValue)
                {
                    var waitTask = Task.WhenAll(tasksToWait);
                    var completedTask = await Task.WhenAny(waitTask, Task.Delay(timeout.Value));

                    if (completedTask != waitTask)
                    {
                        _logger.Warning($"等待所有任务完成超时，剩余 {tasksToWait.Length} 个任务");
                    }
                }
                else
                {
                    await Task.WhenAll(tasksToWait);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("等待异步任务完成时发生异常", ex);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cancellationTokenSource.Cancel();

                // 等待一段时间让任务完成
                try
                {
                    WaitAllAsync(TimeSpan.FromSeconds(5)).Wait();
                }
                catch
                {
                    // 忽略等待超时的异常
                }

                _cancellationTokenSource.Dispose();
                _disposed = true;

                _logger.Debug("异步任务管理器已释放");
            }
        }
    }
}