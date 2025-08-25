using System.Windows.Forms;
using ProgressBar = System.Windows.Forms.ProgressBar;

namespace DotNetARX.Services
{
    /// <summary>
    /// 进度管理器实现
    /// </summary>
    public class ProgressManagerService : IProgressManager
    {
        private readonly IEventBus _eventBus;
        private readonly ILogger _logger;
        private ProgressBar _progressBar;
        private Form _progressForm;
        private Label _statusLabel;
        private Button _cancelButton;
        private long _totalOperations;
        private long _currentOperation;
        private bool _isCancelled;
        private bool _disposed;

        public bool IsCancelled => _isCancelled;

        public ProgressManagerService(
            IEventBus eventBus = null,
            ILogger logger = null)
        {
            _eventBus = eventBus ?? ServiceContainer.Instance.GetService<IEventBus>();
            _logger = logger ?? ServiceContainer.Instance.GetService<ILogger>();

            InitializeProgressForm();
        }

        /// <summary>
        /// 初始化进度窗口
        /// </summary>
        private void InitializeProgressForm()
        {
            try
            {
                _progressForm = new Form
                {
                    Text = "操作进度",
                    Size = new System.Drawing.Size(400, 150),
                    StartPosition = FormStartPosition.CenterScreen,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    TopMost = true
                };

                _progressBar = new ProgressBar
                {
                    Location = new System.Drawing.Point(20, 20),
                    Size = new System.Drawing.Size(340, 25),
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0
                };

                _statusLabel = new Label
                {
                    Location = new System.Drawing.Point(20, 55),
                    Size = new System.Drawing.Size(340, 20),
                    Text = "准备开始...",
                    TextAlign = System.Drawing.ContentAlignment.MiddleLeft
                };

                _cancelButton = new Button
                {
                    Location = new System.Drawing.Point(160, 85),
                    Size = new System.Drawing.Size(80, 25),
                    Text = "取消",
                    DialogResult = DialogResult.Cancel
                };

                _cancelButton.Click += (sender, e) =>
                {
                    _isCancelled = true;
                    _eventBus?.Publish(new ProgressEvent("ProgressCancelled", "用户取消操作", 0));
                    _logger?.Info("用户取消了操作进度");
                    _progressForm.Hide();
                };

                _progressForm.Controls.AddRange(new Control[] { _progressBar, _statusLabel, _cancelButton });

                _progressForm.FormClosing += (sender, e) =>
                {
                    if (!_isCancelled && !_disposed)
                    {
                        e.Cancel = true; // 防止用户直接关闭窗口
                        _isCancelled = true;
                        _eventBus?.Publish(new ProgressEvent("ProgressCancelled", "用户关闭窗口", 0));
                    }
                };
            }
            catch (Exception ex)
            {
                _logger?.Error($"初始化进度窗口失败: {ex.Message}", ex);
                throw new ProgressManagerException($"初始化进度窗口失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 设置总操作数
        /// </summary>
        public void SetTotalOperations(long totalOps)
        {
            try
            {
                if (totalOps <= 0)
                    throw new ArgumentException("总操作数必须大于0");

                _totalOperations = totalOps;
                _currentOperation = 0;

                if (_progressForm != null && !_progressForm.IsDisposed)
                {
                    _progressForm.Invoke(new Action(() =>
                    {
                        _progressBar.Value = 0;
                        _statusLabel.Text = $"总共 {_totalOperations} 个操作，准备开始...";
                        if (!_progressForm.Visible)
                        {
                            _progressForm.Show();
                        }
                    }));
                }

                _eventBus?.Publish(new ProgressEvent("ProgressInitialized", "进度初始化", 0));
                _logger?.Info($"进度管理器初始化：总操作数 {_totalOperations}");
            }
            catch (Exception ex)
            {
                _logger?.Error($"设置总操作数失败: {ex.Message}", ex);
                throw new ProgressManagerException($"设置总操作数失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 更新进度
        /// </summary>
        public void UpdateProgress(long currentOp, string message = null)
        {
            try
            {
                if (_isCancelled || _disposed)
                    return;

                _currentOperation = currentOp;
                var percentage = _totalOperations > 0 ? (int)(currentOp * 100 / _totalOperations) : 0;
                percentage = Math.Min(100, Math.Max(0, percentage));

                var statusMessage = message ?? $"正在处理第 {currentOp} 个操作...";

                if (_progressForm != null && !_progressForm.IsDisposed && _progressForm.Visible)
                {
                    _progressForm.Invoke(new Action(() =>
                    {
                        _progressBar.Value = percentage;
                        _statusLabel.Text = statusMessage;
                        _progressForm.Update();
                    }));
                }

                _eventBus?.Publish(new ProgressEvent("ProgressUpdated", statusMessage, percentage));

                // 处理Windows消息，确保界面响应
                System.Windows.Forms.Application.DoEvents();
            }
            catch (Exception ex)
            {
                _logger?.Error($"更新进度失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 增量更新进度
        /// </summary>
        public void Tick(string message = null)
        {
            UpdateProgress(_currentOperation + 1, message);
        }

        /// <summary>
        /// 完成进度
        /// </summary>
        public void Complete(string message = "完成")
        {
            try
            {
                if (_disposed)
                    return;

                _currentOperation = _totalOperations;

                if (_progressForm != null && !_progressForm.IsDisposed)
                {
                    _progressForm.Invoke(new Action(() =>
                    {
                        _progressBar.Value = 100;
                        _statusLabel.Text = message;
                        _cancelButton.Text = "关闭";
                        _cancelButton.DialogResult = DialogResult.OK;
                        _progressForm.Update();
                    }));

                    // 延迟一秒后自动关闭
                    var timer = new System.Windows.Forms.Timer();
                    timer.Interval = 1000;
                    timer.Tick += (sender, e) =>
                    {
                        timer.Stop();
                        timer.Dispose();
                        if (_progressForm != null && !_progressForm.IsDisposed)
                        {
                            _progressForm.Invoke(new Action(() => _progressForm.Hide()));
                        }
                    };
                    timer.Start();
                }

                _eventBus?.Publish(new ProgressEvent("ProgressCompleted", message, 100));
                _logger?.Info($"操作完成：{message}");
            }
            catch (Exception ex)
            {
                _logger?.Error($"完成进度失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 取消进度
        /// </summary>
        public void Cancel()
        {
            try
            {
                _isCancelled = true;

                if (_progressForm != null && !_progressForm.IsDisposed)
                {
                    _progressForm.Invoke(new Action(() =>
                    {
                        _statusLabel.Text = "操作已取消";
                        _cancelButton.Text = "关闭";
                        _progressForm.Hide();
                    }));
                }

                _eventBus?.Publish(new ProgressEvent("ProgressCancelled", "操作被取消", 0));
                _logger?.Info("操作进度被取消");
            }
            catch (Exception ex)
            {
                _logger?.Error($"取消进度失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    if (_progressForm != null && !_progressForm.IsDisposed)
                    {
                        if (_progressForm.InvokeRequired)
                        {
                            _progressForm.Invoke(new Action(() =>
                            {
                                _progressForm.Close();
                                _progressForm.Dispose();
                            }));
                        }
                        else
                        {
                            _progressForm.Close();
                            _progressForm.Dispose();
                        }
                    }

                    _progressBar?.Dispose();
                    _statusLabel?.Dispose();
                    _cancelButton?.Dispose();

                    _logger?.Info("进度管理器资源已释放");
                }
                catch (Exception ex)
                {
                    _logger?.Error($"释放进度管理器资源失败: {ex.Message}", ex);
                }

                _disposed = true;
            }
        }

        ~ProgressManagerService()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// 进度事件类
    /// </summary>
    public class ProgressEvent : Events.EventArgs
    {
        public string EventType { get; }
        public string Message { get; }
        public int Percentage { get; }
        public new DateTime Timestamp { get; }

        public ProgressEvent(string eventType, string message, int percentage = 0)
            : base("ProgressManagerService")
        {
            EventType = eventType;
            Message = message;
            Percentage = percentage;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 进度管理器异常
    /// </summary>
    public class ProgressManagerException : DotNetARXException
    {
        public ProgressManagerException(string message) : base(message)
        {
        }

        public ProgressManagerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}