namespace DotNetARX
{
    /// <summary>
    /// 简化的日志管理器 - 支持Serilog集成
    /// </summary>
    public static class LogManager
    {
        private static readonly ConcurrentDictionary<Type, ILogger> _loggers = new();
        private static ILogger _defaultLogger;
        private static bool _initialized = false;

        /// <summary>
        /// 初始化日志系统
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                // 尝试配置Serilog（如果可用）
                ConfigureSerilog();
                _initialized = true;
            }
            catch
            {
                // 如果Serilog不可用，使用默认日志
                _defaultLogger = new DefaultLogger();
                _initialized = true;
            }
        }

        /// <summary>
        /// 配置Serilog
        /// </summary>
        private static void ConfigureSerilog()
        {
            try
            {
                // 检查Serilog是否可用
                var serilogType = Type.GetType("Serilog.Log, Serilog");
                if (serilogType != null)
                {
                    _defaultLogger = new SerilogLogger();
                }
                else
                {
                    _defaultLogger = new DefaultLogger();
                }
            }
            catch
            {
                _defaultLogger = new DefaultLogger();
            }
        }

        /// <summary>
        /// 获取类型对应的日志器
        /// </summary>
        public static ILogger GetLogger(Type type)
        {
            if (!_initialized) Initialize();

            return _loggers.GetOrAdd(type, t => new TypedLogger(t, _defaultLogger));
        }

        /// <summary>
        /// 获取泛型日志器
        /// </summary>
        public static ILogger GetLogger<T>()
        {
            return GetLogger(typeof(T));
        }

        /// <summary>
        /// 默认日志器
        /// </summary>
        public static ILogger DefaultLogger
        {
            get
            {
                if (!_initialized) Initialize();
                return _defaultLogger;
            }
        }
    }

    /// <summary>
    /// 日志接口
    /// </summary>
    public interface ILogger
    {
        void Debug(string message);

        void Info(string message);

        void Warning(string message);

        void Error(string message, Exception exception = null);
    }

    /// <summary>
    /// 带类型信息的日志器
    /// </summary>
    public class TypedLogger : ILogger
    {
        private readonly Type _type;
        private readonly ILogger _baseLogger;

        public TypedLogger(Type type, ILogger baseLogger)
        {
            _type = type;
            _baseLogger = baseLogger;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug(string message) => _baseLogger.Debug($"[{_type.Name}] {message}");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info(string message) => _baseLogger.Info($"[{_type.Name}] {message}");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning(string message) => _baseLogger.Warning($"[{_type.Name}] {message}");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(string message, Exception exception = null) =>
            _baseLogger.Error($"[{_type.Name}] {message}", exception);
    }

    /// <summary>
    /// Serilog集成日志器
    /// </summary>
    public class SerilogLogger : ILogger
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug(string message)
        {
            try
            {
                // 使用反射调用Serilog.Log.Debug
                var logType = Type.GetType("Serilog.Log, Serilog");
                var debugMethod = logType?.GetMethod("Debug", new[] { typeof(string) });
                debugMethod?.Invoke(null, new object[] { message });
            }
            catch
            {
                // 如果Serilog调用失败，回退到控制台
                Console.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss} {message}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info(string message)
        {
            try
            {
                var logType = Type.GetType("Serilog.Log, Serilog");
                var infoMethod = logType?.GetMethod("Information", new[] { typeof(string) });
                infoMethod?.Invoke(null, new object[] { message });
            }
            catch
            {
                Console.WriteLine($"[INFO ] {DateTime.Now:HH:mm:ss} {message}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning(string message)
        {
            try
            {
                var logType = Type.GetType("Serilog.Log, Serilog");
                var warningMethod = logType?.GetMethod("Warning", new[] { typeof(string) });
                warningMethod?.Invoke(null, new object[] { message });
            }
            catch
            {
                Console.WriteLine($"[WARN ] {DateTime.Now:HH:mm:ss} {message}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(string message, Exception exception = null)
        {
            try
            {
                var logType = Type.GetType("Serilog.Log, Serilog");
                if (exception != null)
                {
                    var errorMethod = logType?.GetMethod("Error", new[] { typeof(Exception), typeof(string) });
                    errorMethod?.Invoke(null, new object[] { exception, message });
                }
                else
                {
                    var errorMethod = logType?.GetMethod("Error", new[] { typeof(string) });
                    errorMethod?.Invoke(null, new object[] { message });
                }
            }
            catch
            {
                var exceptionInfo = exception != null ? $" - {exception.Message}" : "";
                Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} {message}{exceptionInfo}");
            }
        }
    }

    /// <summary>
    /// 默认控制台日志器
    /// </summary>
    public class DefaultLogger : ILogger
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug(string message) =>
            Console.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss} {message}");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info(string message) =>
            Console.WriteLine($"[INFO ] {DateTime.Now:HH:mm:ss} {message}");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning(string message) =>
            Console.WriteLine($"[WARN ] {DateTime.Now:HH:mm:ss} {message}");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(string message, Exception exception = null)
        {
            var exceptionInfo = exception != null ? $" - {exception.Message}" : "";
            Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} {message}{exceptionInfo}");
        }
    }
}