using System.Runtime.CompilerServices;

namespace DotNetARX.Logging
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Fatal = 4
    }

    /// <summary>
    /// 简单的日志记录器接口
    /// </summary>
    public interface ILogger
    {
        void Debug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0);

        void Info(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0);

        void Warning(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0);

        void Error(string message, Exception exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0);

        void Fatal(string message, Exception exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0);
    }

    /// <summary>
    /// 文件日志记录器实现
    /// </summary>
    public class FileLogger : ILogger, IDisposable
    {
        private readonly string _logFilePath;
        private readonly LogLevel _minLevel;
        private readonly object _lockObject = new object();
        private StreamWriter _writer;
        private bool _disposed = false;

        public FileLogger(string logFilePath = null, LogLevel minLevel = LogLevel.Info)
        {
            _minLevel = minLevel;
            _logFilePath = logFilePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DotNetARX",
                "Logs",
                $"DotNetARX_{DateTime.Now:yyyyMMdd}.log");

            // 确保日志目录存在
            var logDirectory = Path.GetDirectoryName(_logFilePath);
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            _writer = new StreamWriter(_logFilePath, append: true) { AutoFlush = true };
        }

        public void Debug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log(LogLevel.Debug, message, null, memberName, filePath, lineNumber);
        }

        public void Info(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log(LogLevel.Info, message, null, memberName, filePath, lineNumber);
        }

        public void Warning(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log(LogLevel.Warning, message, null, memberName, filePath, lineNumber);
        }

        public void Error(string message, Exception exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log(LogLevel.Error, message, exception, memberName, filePath, lineNumber);
        }

        public void Fatal(string message, Exception exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Log(LogLevel.Fatal, message, exception, memberName, filePath, lineNumber);
        }

        private void Log(LogLevel level, string message, Exception exception, string memberName, string filePath, int lineNumber)
        {
            if (level < _minLevel || _disposed) return;

            lock (_lockObject)
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] [{Thread.CurrentThread.ManagedThreadId:D2}] {fileName}.{memberName}:{lineNumber} - {message}";

                    if (exception != null)
                    {
                        logEntry += $"\n异常详情: {exception}";
                    }

                    _writer?.WriteLine(logEntry);
                }
                catch
                {
                    // 日志记录失败时不抛出异常，避免影响主程序
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_lockObject)
                {
                    _writer?.Dispose();
                    _writer = null;
                    _disposed = true;
                }
            }
        }
    }

    /// <summary>
    /// 日志管理器
    /// </summary>
    public static class LogManager
    {
        private static readonly Lazy<ILogger> _defaultLogger = new Lazy<ILogger>(() => new FileLogger());

        public static ILogger GetLogger(Type type)
        {
            return _defaultLogger.Value;
        }

        public static ILogger GetLogger<T>()
        {
            return GetLogger(typeof(T));
        }

        public static ILogger DefaultLogger => _defaultLogger.Value;
    }
}