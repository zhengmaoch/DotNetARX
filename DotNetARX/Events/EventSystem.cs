namespace DotNetARX.Events
{
    /// <summary>
    /// 事件优先级
    /// </summary>
    public enum EventPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// 基础事件参数
    /// </summary>
    public abstract class EventArgs : System.EventArgs
    {
        public DateTime Timestamp { get; }
        public string Source { get; set; }
        public bool Handled { get; set; }
        public Dictionary<string, object> Properties { get; }

        protected EventArgs(string source = null)
        {
            Timestamp = DateTime.Now;
            Source = source;
            Properties = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// 实体事件参数
    /// </summary>
    public class EntityEventArgs : EventArgs
    {
        public ObjectId EntityId { get; }
        public string EntityType { get; }
        public string Operation { get; }

        public EntityEventArgs(ObjectId entityId, string operation, string entityType = null, string source = null)
            : base(source)
        {
            EntityId = entityId;
            Operation = operation;
            EntityType = entityType;
        }
    }

    /// <summary>
    /// 数据库事件参数
    /// </summary>
    public class DatabaseEventArgs : EventArgs
    {
        public Database Database { get; }
        public string Operation { get; }

        public DatabaseEventArgs(Database database, string operation, string source = null)
            : base(source)
        {
            Database = database;
            Operation = operation;
        }
    }

    /// <summary>
    /// 图层事件参数
    /// </summary>
    public class LayerEventArgs : EventArgs
    {
        public string LayerName { get; }
        public string Operation { get; }

        public LayerEventArgs(string layerName, string operation, string source = null)
            : base(source)
        {
            LayerName = layerName;
            Operation = operation;
        }
    }

    /// <summary>
    /// 错误事件参数
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public string ErrorMessage { get; }
        public string Operation { get; }

        public ErrorEventArgs(Exception exception, string operation, string source = null)
            : base(source)
        {
            Exception = exception;
            ErrorMessage = exception?.Message;
            Operation = operation;
        }
    }

    /// <summary>
    /// 实体事件
    /// </summary>
    public class EntityEvent
    {
        public ObjectId EntityId { get; set; }
        public string EntityType { get; set; }
        public string Operation { get; set; }
        public DateTime Timestamp { get; set; }
        public string Source { get; set; }

        public EntityEvent(ObjectId entityId, string operation, string entityType = null, string source = null)
        {
            EntityId = entityId;
            Operation = operation;
            EntityType = entityType;
            Timestamp = DateTime.Now;
            Source = source;
        }
    }

    /// <summary>
    /// 样式事件
    /// </summary>
    public class StyleEvent
    {
        public string StyleName { get; set; }
        public ObjectId StyleId { get; set; }
        public string Operation { get; set; }
        public string StyleType { get; set; }
        public DateTime Timestamp { get; set; }

        public StyleEvent(string styleName, ObjectId styleId, string operation, string styleType)
        {
            StyleName = styleName;
            StyleId = styleId;
            Operation = operation;
            StyleType = styleType;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 进度事件
    /// </summary>
    public class ProgressEvent
    {
        public string OperationName { get; set; }
        public double Progress { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }

        public ProgressEvent(string operationName, double progress, string message = null)
        {
            OperationName = operationName;
            Progress = progress;
            Message = message;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 事件发布器接口
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        /// 发布事件
        /// </summary>
        Task PublishAsync<T>(T eventArgs) where T : EventArgs;

        /// <summary>
        /// 同步发布事件
        /// </summary>
        void Publish<T>(T eventArgs) where T : EventArgs;

        /// <summary>
        /// 订阅事件
        /// </summary>
        void Subscribe<T>(IEventHandler<T> handler) where T : EventArgs;

        /// <summary>
        /// 订阅事件（使用委托）
        /// </summary>
        void Subscribe<T>(Func<T, Task> handler, int priority = (int)EventPriority.Normal) where T : EventArgs;

        /// <summary>
        /// 取消订阅
        /// </summary>
        void Unsubscribe<T>(IEventHandler<T> handler) where T : EventArgs;

        /// <summary>
        /// 清除所有订阅
        /// </summary>
        void ClearSubscriptions();
    }

    /// <summary>
    /// 委托事件处理器
    /// </summary>
    internal class DelegateEventHandler<T> : IEventHandler<T> where T : EventArgs
    {
        private readonly Func<T, Task> _handler;

        public int Priority { get; }

        public DelegateEventHandler(Func<T, Task> handler, int priority)
        {
            _handler = handler;
            Priority = priority;
        }

        public Task HandleAsync(T eventArgs)
        {
            return _handler(eventArgs);
        }
    }

    /// <summary>
    /// 事件发布器实现
    /// </summary>
    public class EventPublisher : IEventPublisher, DotNetARX.Interfaces.IEventBus, IDisposable
    {
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<Type, List<object>> _handlers;
        private readonly object _lockObject = new object();
        private bool _disposed = false;

        public EventPublisher(ILogger logger = null)
        {
            _logger = logger ?? LogManager.GetLogger(typeof(EventPublisher));
            _handlers = new ConcurrentDictionary<Type, List<object>>();
        }

        public async Task PublishAsync<T>(T eventArgs) where T : EventArgs
        {
            if (eventArgs == null || _disposed) return;

            var eventType = typeof(T);
            if (!_handlers.TryGetValue(eventType, out var handlerList))
            {
                _logger.Debug($"没有找到事件处理器: {eventType.Name}");
                return;
            }

            List<IEventHandler<T>> handlers;
            lock (_lockObject)
            {
                handlers = handlerList.Cast<IEventHandler<T>>()
                                    .OrderByDescending(h => h.Priority)
                                    .ToList();
            }

            _logger.Debug($"发布事件: {eventType.Name}, 处理器数量: {handlers.Count}");

            var tasks = new List<Task>();
            foreach (var handler in handlers)
            {
                if (eventArgs.Handled) break;

                try
                {
                    var task = handler.HandleAsync(eventArgs);
                    tasks.Add(task);
                }
                catch (Exception ex)
                {
                    _logger.Error($"事件处理器执行失败: {handler.GetType().Name}", ex);
                }
            }

            if (tasks.Count > 0)
            {
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    _logger.Error("等待事件处理器完成时发生异常", ex);
                }
            }
        }

        public void Publish<T>(T eventArgs) where T : EventArgs
        {
            Task.Run(async () => await PublishAsync(eventArgs));
        }

        public void Subscribe<T>(IEventHandler<T> handler) where T : EventArgs
        {
            if (handler == null || _disposed) return;

            var eventType = typeof(T);
            _handlers.AddOrUpdate(eventType,
                new List<object> { handler },
                (key, existing) =>
                {
                    lock (_lockObject)
                    {
                        existing.Add(handler);
                        return existing;
                    }
                });

            _logger.Debug($"订阅事件: {eventType.Name}, 处理器: {handler.GetType().Name}");
        }

        public void Subscribe<T>(Func<T, Task> handler, int priority = (int)EventPriority.Normal) where T : EventArgs
        {
            if (handler == null) return;

            var delegateHandler = new DelegateEventHandler<T>(handler, priority);
            Subscribe(delegateHandler);
        }

        public void Unsubscribe<T>(IEventHandler<T> handler) where T : EventArgs
        {
            if (handler == null || _disposed) return;

            var eventType = typeof(T);
            if (_handlers.TryGetValue(eventType, out var handlerList))
            {
                lock (_lockObject)
                {
                    handlerList.Remove(handler);
                    if (handlerList.Count == 0)
                    {
                        _handlers.TryRemove(eventType, out _);
                    }
                }

                _logger.Debug($"取消订阅事件: {eventType.Name}, 处理器: {handler.GetType().Name}");
            }
        }

        public void ClearSubscriptions()
        {
            lock (_lockObject)
            {
                _handlers.Clear();
                _logger.Info("已清除所有事件订阅");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                ClearSubscriptions();
                _disposed = true;
                _logger.Debug("事件发布器已释放");
            }
        }
    }

    /// <summary>
    /// CAD事件管理器
    /// </summary>
    public static class CADEventManager
    {
        private static readonly Lazy<EventPublisher> _publisher =
            new Lazy<EventPublisher>(() => new EventPublisher());

        public static IEventPublisher Publisher => _publisher.Value;

        /// <summary>
        /// 默认事件总线
        /// </summary>
        public static IEventPublisher DefaultBus => Publisher;

        // 实体相关事件
        public static event Func<EntityEventArgs, Task> EntityCreated;

        public static event Func<EntityEventArgs, Task> EntityModified;

        public static event Func<EntityEventArgs, Task> EntityDeleted;

        public static event Func<EntityEventArgs, Task> EntitySelected;

        // 图层相关事件
        public static event Func<LayerEventArgs, Task> LayerCreated;

        public static event Func<LayerEventArgs, Task> LayerModified;

        public static event Func<LayerEventArgs, Task> LayerDeleted;

        public static event Func<LayerEventArgs, Task> LayerChanged;

        // 数据库相关事件
        public static event Func<DatabaseEventArgs, Task> DatabaseOpened;

        public static event Func<DatabaseEventArgs, Task> DatabaseClosed;

        public static event Func<DatabaseEventArgs, Task> DatabaseSaved;

        // 错误事件
        public static event Func<ErrorEventArgs, Task> ErrorOccurred;

        static CADEventManager()
        {
            InitializeDefaultSubscriptions();
        }

        /// <summary>
        /// 初始化默认事件订阅
        /// </summary>
        private static void InitializeDefaultSubscriptions()
        {
            // 订阅实体事件
            Publisher.Subscribe<EntityEventArgs>(async args =>
            {
                switch (args.Operation?.ToLower())
                {
                    case "created":
                        if (EntityCreated != null)
                            await EntityCreated(args);
                        break;

                    case "modified":
                        if (EntityModified != null)
                            await EntityModified(args);
                        break;

                    case "deleted":
                        if (EntityDeleted != null)
                            await EntityDeleted(args);
                        break;

                    case "selected":
                        if (EntitySelected != null)
                            await EntitySelected(args);
                        break;
                }
            }, (int)EventPriority.Normal);

            // 订阅图层事件
            Publisher.Subscribe<LayerEventArgs>(async args =>
            {
                switch (args.Operation?.ToLower())
                {
                    case "created":
                        if (LayerCreated != null)
                            await LayerCreated(args);
                        break;

                    case "modified":
                        if (LayerModified != null)
                            await LayerModified(args);
                        break;

                    case "deleted":
                        if (LayerDeleted != null)
                            await LayerDeleted(args);
                        break;

                    case "changed":
                        if (LayerChanged != null)
                            await LayerChanged(args);
                        break;
                }
            }, (int)EventPriority.Normal);

            // 订阅数据库事件
            Publisher.Subscribe<DatabaseEventArgs>(async args =>
            {
                switch (args.Operation?.ToLower())
                {
                    case "opened":
                        if (DatabaseOpened != null)
                            await DatabaseOpened(args);
                        break;

                    case "closed":
                        if (DatabaseClosed != null)
                            await DatabaseClosed(args);
                        break;

                    case "saved":
                        if (DatabaseSaved != null)
                            await DatabaseSaved(args);
                        break;
                }
            }, (int)EventPriority.Normal);

            // 订阅错误事件
            Publisher.Subscribe<ErrorEventArgs>(async args =>
            {
                if (ErrorOccurred != null)
                    await ErrorOccurred(args);
            }, (int)EventPriority.High);
        }

        /// <summary>
        /// 触发实体创建事件
        /// </summary>
        public static async Task OnEntityCreatedAsync(ObjectId entityId, string entityType = null, string source = null)
        {
            var args = new EntityEventArgs(entityId, "created", entityType, source);
            await Publisher.PublishAsync(args);
        }

        /// <summary>
        /// 触发实体修改事件
        /// </summary>
        public static async Task OnEntityModifiedAsync(ObjectId entityId, string entityType = null, string source = null)
        {
            var args = new EntityEventArgs(entityId, "modified", entityType, source);
            await Publisher.PublishAsync(args);
        }

        /// <summary>
        /// 触发实体删除事件
        /// </summary>
        public static async Task OnEntityDeletedAsync(ObjectId entityId, string entityType = null, string source = null)
        {
            var args = new EntityEventArgs(entityId, "deleted", entityType, source);
            await Publisher.PublishAsync(args);
        }

        /// <summary>
        /// 触发图层创建事件
        /// </summary>
        public static async Task OnLayerCreatedAsync(string layerName, string source = null)
        {
            var args = new LayerEventArgs(layerName, "created", source);
            await Publisher.PublishAsync(args);
        }

        /// <summary>
        /// 触发错误事件
        /// </summary>
        public static async Task OnErrorOccurredAsync(Exception exception, string operation, string source = null)
        {
            var args = new ErrorEventArgs(exception, operation, source);
            await Publisher.PublishAsync(args);
        }

        /// <summary>
        /// 同步版本的事件触发方法
        /// </summary>
        public static void OnEntityCreated(ObjectId entityId, string entityType = null, string source = null)
        {
            Task.Run(async () => await OnEntityCreatedAsync(entityId, entityType, source));
        }

        public static void OnEntityModified(ObjectId entityId, string entityType = null, string source = null)
        {
            Task.Run(async () => await OnEntityModifiedAsync(entityId, entityType, source));
        }

        public static void OnErrorOccurred(Exception exception, string operation, string source = null)
        {
            Task.Run(async () => await OnErrorOccurredAsync(exception, operation, source));
        }
    }
}