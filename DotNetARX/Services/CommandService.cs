namespace DotNetARX.Services
{
    /// <summary>
    /// 命令操作服务实现
    /// </summary>
    public class CommandService : ICommandService
    {
        private readonly IEventBus _eventBus;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly ILogger _logger;

        [DllImport("acad.exe", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "?acedPostCommand@@YAHPB_W@Z")]
        private static extern int acedPostCommand(string strExpr);

        [DllImport("acad.exe", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static extern int ads_queueexpr(string strExpr);

        [DllImport("acad.exe", EntryPoint = "acedCmd", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static extern int acedCmd(IntPtr rbp);

        public CommandService(
            IEventBus eventBus = null,
            IPerformanceMonitor performanceMonitor = null,
            ILogger logger = null)
        {
            _eventBus = eventBus ?? ServiceContainer.Instance.GetService<IEventBus>();
            _performanceMonitor = performanceMonitor ?? ServiceContainer.Instance.GetService<IPerformanceMonitor>();
            _logger = logger ?? ServiceContainer.Instance.GetService<ILogger>();
        }

        /// <summary>
        /// 发送命令（COM方式）
        /// </summary>
        public void SendCommand(params string[] args)
        {
            using var operation = _performanceMonitor?.StartOperation("SendCommand");

            try
            {
                if (args == null || args.Length == 0)
                    throw new ArgumentException("命令参数不能为空");

                var doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
                if (doc == null)
                    throw new InvalidOperationException("当前没有活动文档");

                Type acadDocumentType = Type.GetTypeFromHandle(Type.GetTypeHandle(doc.GetAcadDocument()));

                // 通过后期绑定的方式调用SendCommand命令
                acadDocumentType.InvokeMember("SendCommand",
                    BindingFlags.InvokeMethod,
                    null,
                    doc.GetAcadDocument(),
                    args);

                _eventBus?.Publish(new CommandEvent("CommandSent", string.Join(" ", args)));
                _logger?.Info($"COM命令发送成功: {string.Join(" ", args)}");
            }
            catch (Exception ex)
            {
                _logger?.Error($"发送COM命令失败: {ex.Message}", ex);
                throw new CommandOperationException($"发送COM命令失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 投递命令（异步方式）
        /// </summary>
        public void PostCommand(string expression)
        {
            using var operation = _performanceMonitor?.StartOperation("PostCommand");

            try
            {
                if (string.IsNullOrEmpty(expression))
                    throw new ArgumentException("命令表达式不能为空");

                int result = acedPostCommand(expression);

                if (result != 0)
                {
                    _logger?.Warning($"投递命令返回非零值: {result}");
                }

                _eventBus?.Publish(new CommandEvent("CommandPosted", expression));
                _logger?.Info($"投递命令成功: {expression}");
            }
            catch (Exception ex)
            {
                _logger?.Error($"投递命令失败: {ex.Message}", ex);
                throw new CommandOperationException($"投递命令失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 队列表达式
        /// </summary>
        public void QueueExpression(string expression)
        {
            using var operation = _performanceMonitor?.StartOperation("QueueExpression");

            try
            {
                if (string.IsNullOrEmpty(expression))
                    throw new ArgumentException("表达式不能为空");

                int result = ads_queueexpr(expression);

                if (result != 0)
                {
                    _logger?.Warning($"队列表达式返回非零值: {result}");
                }

                _eventBus?.Publish(new CommandEvent("ExpressionQueued", expression));
                _logger?.Info($"队列表达式成功: {expression}");
            }
            catch (Exception ex)
            {
                _logger?.Error($"队列表达式失败: {ex.Message}", ex);
                throw new CommandOperationException($"队列表达式失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 执行ARX命令
        /// </summary>
        public int ExecuteCommand(ResultBuffer args)
        {
            using var operation = _performanceMonitor?.StartOperation("ExecuteCommand");

            try
            {
                if (args == null)
                    throw new ArgumentNullException(nameof(args));

                // 由于acedCmd只能在程序环境下运行，因此需调用此语句
                if (!Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.IsApplicationContext)
                {
                    int result = acedCmd(args.UnmanagedObject);

                    _eventBus?.Publish(new CommandEvent("ARXCommandExecuted", $"Result: {result}"));
                    _logger?.Info($"ARX命令执行完成，返回值: {result}");

                    return result;
                }
                else
                {
                    _logger?.Warning("当前处于应用程序上下文中，无法执行ARX命令");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"执行ARX命令失败: {ex.Message}", ex);
                throw new CommandOperationException($"执行ARX命令失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 执行命令（COM方式）
        /// </summary>
        public bool ExecuteCommandCOM(string command)
        {
            using var operation = _performanceMonitor?.StartOperation("ExecuteCommandCOM");

            try
            {
                if (string.IsNullOrEmpty(command))
                {
                    _logger?.Warning("命令不能为空");
                    return false;
                }

                SendCommand(command);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"执行COM命令失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 异步执行命令
        /// </summary>
        public bool ExecuteCommandAsync(string command)
        {
            using var operation = _performanceMonitor?.StartOperation("ExecuteCommandAsync");

            try
            {
                if (string.IsNullOrEmpty(command))
                {
                    _logger?.Warning("命令不能为空");
                    return false;
                }

                PostCommand(command);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"异步执行命令失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 队列执行命令
        /// </summary>
        public bool ExecuteCommandQueue(string command)
        {
            using var operation = _performanceMonitor?.StartOperation("ExecuteCommandQueue");

            try
            {
                if (string.IsNullOrEmpty(command))
                {
                    _logger?.Warning("命令不能为空");
                    return false;
                }

                QueueExpression(command);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"队列执行命令失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 执行ARX命令（简单版本）
        /// </summary>
        public bool ExecuteARXCommand(string command)
        {
            return ExecuteARXCommand(command, null);
        }

        /// <summary>
        /// 执行ARX命令（带参数）
        /// </summary>
        public bool ExecuteARXCommand(string command, params object[] args)
        {
            using var operation = _performanceMonitor?.StartOperation("ExecuteARXCommand");

            try
            {
                if (string.IsNullOrEmpty(command))
                {
                    _logger?.Warning("命令不能为空");
                    return false;
                }

                var resultBuffer = new ResultBuffer();
                resultBuffer.Add(new TypedValue((int)LispDataType.Text, command));

                if (args != null)
                {
                    foreach (var arg in args)
                    {
                        if (arg is string str)
                            resultBuffer.Add(new TypedValue((int)LispDataType.Text, str));
                        else if (arg is int intVal)
                            resultBuffer.Add(new TypedValue((int)LispDataType.Int32, intVal));
                        else if (arg is double doubleVal)
                            resultBuffer.Add(new TypedValue((int)LispDataType.Double, doubleVal));
                        else if (arg is Point3d point)
                            resultBuffer.Add(new TypedValue((int)LispDataType.Point3d, point));
                        else
                            resultBuffer.Add(new TypedValue((int)LispDataType.Text, arg?.ToString() ?? ""));
                    }
                }

                var result = ExecuteCommand(resultBuffer);
                return result == 0; // 0 通常表示成功
            }
            catch (Exception ex)
            {
                _logger?.Error($"执行ARX命令失败: {ex.Message}", ex);
                return false;
            }
        }
    }

    /// <summary>
    /// 命令事件类
    /// </summary>
    public class CommandEvent : Events.EventArgs
    {
        public string EventType { get; }
        public string Command { get; }
        public new DateTime Timestamp { get; }

        public CommandEvent(string eventType, string command)
            : base("CommandService")
        {
            EventType = eventType;
            Command = command;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 命令操作异常
    /// </summary>
    public class CommandOperationException : DotNetARXException
    {
        public CommandOperationException(string message) : base(message)
        {
        }

        public CommandOperationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}