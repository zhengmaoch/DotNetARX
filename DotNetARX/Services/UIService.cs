using System.Windows.Forms;
using CursorType = DotNetARX.Models.CursorType;

namespace DotNetARX.Services
{
    /// <summary>
    /// 用户界面操作服务实现
    /// </summary>
    public class UIService : IUIService
    {
        private readonly IEventBus _eventBus;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly ILogger _logger;

        public UIService(
            IEventBus eventBus = null,
            IPerformanceMonitor performanceMonitor = null,
            ILogger logger = null)
        {
            _eventBus = eventBus ?? ServiceContainer.Instance.GetService<IEventBus>();
            _performanceMonitor = performanceMonitor ?? ServiceContainer.Instance.GetService<IPerformanceMonitor>();
            _logger = logger ?? ServiceContainer.Instance.GetService<ILogger>();
        }

        /// <summary>
        /// 显示消息
        /// </summary>
        public void ShowMessage(string message, string title = "提示")
        {
            using var operation = _performanceMonitor?.StartOperation("ShowMessage");

            try
            {
                if (string.IsNullOrEmpty(message))
                    message = "";

                if (string.IsNullOrEmpty(title))
                    title = "提示";

                // 使用AutoCAD编辑器显示消息
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    doc.Editor.WriteMessage($"\n{title}: {message}\n");
                }

                // 同时使用Windows Forms消息框
                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);

                _eventBus?.Publish(new UIEvent("MessageShown", $"Title: {title}, Message: {message}"));
                _logger?.Info($"显示消息: {title} - {message}");
            }
            catch (Exception ex)
            {
                _logger?.Error($"显示消息失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        public bool ShowConfirmDialog(string message, string title = "确认")
        {
            using var operation = _performanceMonitor?.StartOperation("ShowConfirmDialog");

            try
            {
                if (string.IsNullOrEmpty(message))
                    message = "确认执行此操作？";

                if (string.IsNullOrEmpty(title))
                    title = "确认";

                var result = MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                bool confirmed = result == DialogResult.Yes;

                _eventBus?.Publish(new UIEvent("ConfirmDialog", $"Title: {title}, Result: {confirmed}"));
                _logger?.Info($"确认对话框: {title} - 结果: {confirmed}");

                return confirmed;
            }
            catch (Exception ex)
            {
                _logger?.Error($"显示确认对话框失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 显示确认对话框（别名方法）
        /// </summary>
        public bool ShowConfirmationDialog(string message, string title = "确认")
        {
            return ShowConfirmDialog(message, title);
        }

        /// <summary>
        /// 获取用户输入
        /// </summary>
        public string GetUserInput(string prompt, string defaultValue = "")
        {
            using var operation = _performanceMonitor?.StartOperation("GetUserInput");

            try
            {
                if (string.IsNullOrEmpty(prompt))
                    prompt = "请输入：";

                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null)
                {
                    _logger?.Warning("当前没有活动文档，使用输入框");
                    return ShowInputDialog(prompt, defaultValue);
                }

                var editor = doc.Editor;
                var options = new PromptStringOptions(prompt);
                if (!string.IsNullOrEmpty(defaultValue))
                {
                    options.DefaultValue = defaultValue;
                }

                var result = editor.GetString(options);

                if (result.Status == PromptStatus.OK)
                {
                    _eventBus?.Publish(new UIEvent("UserInputReceived", $"Prompt: {prompt}, Input: {result.StringResult}"));
                    _logger?.Debug($"用户输入: {prompt} - {result.StringResult}");
                    return result.StringResult;
                }
                else
                {
                    _logger?.Debug($"用户取消输入: {prompt}");
                    return defaultValue;
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"获取用户输入失败: {ex.Message}", ex);
                return defaultValue;
            }
        }

        /// <summary>
        /// 选择文件
        /// </summary>
        public string SelectFile(string title, string filter, bool saveMode = false)
        {
            using var operation = _performanceMonitor?.StartOperation("SelectFile");

            try
            {
                FileDialog dialog;

                if (saveMode)
                {
                    dialog = new SaveFileDialog();
                }
                else
                {
                    dialog = new OpenFileDialog();
                }

                dialog.Title = title ?? "选择文件";
                dialog.Filter = filter ?? "所有文件|*.*";

                var result = dialog.ShowDialog();
                string fileName = "";

                if (result == DialogResult.OK)
                {
                    fileName = dialog.FileName;
                }

                dialog.Dispose();

                _eventBus?.Publish(new UIEvent("FileSelected", $"Mode: {(saveMode ? "Save" : "Open")}, File: {fileName}"));
                _logger?.Info($"文件选择: {(saveMode ? "保存" : "打开")} - {fileName}");

                return fileName;
            }
            catch (Exception ex)
            {
                _logger?.Error($"选择文件失败: {ex.Message}", ex);
                return "";
            }
        }

        /// <summary>
        /// 显示进度条
        /// </summary>
        public IProgressManager ShowProgress(string title, string message, int maxProgress)
        {
            using var operation = _performanceMonitor?.StartOperation("ShowProgress");

            try
            {
                var progressManager = ServiceContainer.Instance.GetService<IProgressManager>();
                if (progressManager != null)
                {
                    progressManager.SetTotalOperations(maxProgress);
                    _eventBus?.Publish(new UIEvent("ProgressStarted", $"Title: {title}, Message: {message}"));
                    _logger?.Info($"进度条显示: {title} - {message}");
                }

                return progressManager;
            }
            catch (Exception ex)
            {
                _logger?.Error($"显示进度条失败: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 隐藏进度条
        /// </summary>
        public void HideProgress()
        {
            using var operation = _performanceMonitor?.StartOperation("HideProgress");

            try
            {
                var progressManager = ServiceContainer.Instance.GetService<IProgressManager>();
                progressManager?.Complete();

                _eventBus?.Publish(new UIEvent("ProgressStopped", "Progress hidden"));
                _logger?.Info("进度条隐藏");
            }
            catch (Exception ex)
            {
                _logger?.Error($"隐藏进度条失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 设置状态栏文本
        /// </summary>
        public void SetStatusBarText(string text)
        {
            using var operation = _performanceMonitor?.StartOperation("SetStatusBarText");

            try
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    doc.Editor.WriteMessage($"\n{text ?? ""}\n");
                }

                _eventBus?.Publish(new UIEvent("StatusBarTextSet", $"Text: {text}"));
                _logger?.Info($"状态栏文本设置: {text}");
            }
            catch (Exception ex)
            {
                _logger?.Error($"设置状态栏文本失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 清除状态栏
        /// </summary>
        public void ClearStatusBar()
        {
            using var operation = _performanceMonitor?.StartOperation("ClearStatusBar");

            try
            {
                // 在AutoCAD中，通常通过写入空行来"清除"状态栏
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    doc.Editor.WriteMessage("\n");
                }

                _eventBus?.Publish(new UIEvent("StatusBarCleared", "Status bar cleared"));
                _logger?.Info("状态栏清除");
            }
            catch (Exception ex)
            {
                _logger?.Error($"清除状态栏失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 显示工具提示
        /// </summary>
        public void ShowTooltip(string message, Point3d position)
        {
            using var operation = _performanceMonitor?.StartOperation("ShowTooltip");

            try
            {
                // 工具提示通常通过状态栏显示
                SetStatusBarText(message);

                _eventBus?.Publish(new UIEvent("TooltipShown", $"Message: {message}, Position: {position}"));
                _logger?.Info($"工具提示显示: {message} at {position}");
            }
            catch (Exception ex)
            {
                _logger?.Error($"显示工具提示失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 隐藏工具提示
        /// </summary>
        public void HideTooltip()
        {
            using var operation = _performanceMonitor?.StartOperation("HideTooltip");

            try
            {
                ClearStatusBar();

                _eventBus?.Publish(new UIEvent("TooltipHidden", "Tooltip hidden"));
                _logger?.Info("工具提示隐藏");
            }
            catch (Exception ex)
            {
                _logger?.Error($"隐藏工具提示失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 刷新用户界面
        /// </summary>
        public void RefreshUI()
        {
            using var operation = _performanceMonitor?.StartOperation("RefreshUI");

            try
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    doc.Editor.UpdateScreen();
                }

                _eventBus?.Publish(new UIEvent("UIRefreshed", "UI refreshed"));
                _logger?.Info("用户界面刷新");
            }
            catch (Exception ex)
            {
                _logger?.Error($"刷新用户界面失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 启用/禁用用户界面
        /// </summary>
        public void EnableUI(bool enable)
        {
            using var operation = _performanceMonitor?.StartOperation("EnableUI");

            try
            {
                // 在AutoCAD环境中，我们通过启用/禁用命令来控制UI
                // 这里我们只记录日志和事件
                _eventBus?.Publish(new UIEvent("UIEnabled", $"Enabled: {enable}"));
                _logger?.Info($"用户界面启用状态设置: {enable}");
            }
            catch (Exception ex)
            {
                _logger?.Error($"设置用户界面启用状态失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 显示上下文菜单
        /// </summary>
        public void ShowContextMenu(List<MenuItemDefinition> menuItems, Point3d position)
        {
            using var operation = _performanceMonitor?.StartOperation("ShowContextMenu");

            try
            {
                // 在AutoCAD环境中，上下文菜单通常由AutoCAD自身处理
                // 这里我们只记录日志和事件
                _eventBus?.Publish(new UIEvent("ContextMenuShown", $"Items: {menuItems?.Count ?? 0}, Position: {position}"));
                _logger?.Info($"上下文菜单显示: {menuItems?.Count ?? 0} items at {position}");
            }
            catch (Exception ex)
            {
                _logger?.Error($"显示上下文菜单失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 设置光标
        /// </summary>
        public void SetCursor(CursorType cursorType)
        {
            using var operation = _performanceMonitor?.StartOperation("SetCursor");

            try
            {
                // 在AutoCAD环境中，光标通常由AutoCAD自身管理
                // 这里我们只记录日志和事件
                _eventBus?.Publish(new UIEvent("CursorSet", $"CursorType: {cursorType}"));
                _logger?.Info($"光标设置: {cursorType}");
            }
            catch (Exception ex)
            {
                _logger?.Error($"设置光标失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 重置光标
        /// </summary>
        public void ResetCursor()
        {
            using var operation = _performanceMonitor?.StartOperation("ResetCursor");

            try
            {
                // 重置为默认光标
                SetCursor(CursorType.Default);

                _eventBus?.Publish(new UIEvent("CursorReset", "Cursor reset to default"));
                _logger?.Info("光标重置");
            }
            catch (Exception ex)
            {
                _logger?.Error($"重置光标失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 显示输入对话框
        /// </summary>
        private string ShowInputDialog(string prompt, string defaultValue)
        {
            try
            {
                using (var form = new Form())
                {
                    form.Text = "输入";
                    form.Size = new System.Drawing.Size(300, 150);
                    form.StartPosition = FormStartPosition.CenterScreen;
                    form.FormBorderStyle = FormBorderStyle.FixedDialog;
                    form.MaximizeBox = false;
                    form.MinimizeBox = false;

                    var label = new Label()
                    {
                        Text = prompt,
                        Location = new System.Drawing.Point(10, 20),
                        Size = new System.Drawing.Size(260, 20)
                    };

                    var textBox = new TextBox()
                    {
                        Text = defaultValue,
                        Location = new System.Drawing.Point(10, 50),
                        Size = new System.Drawing.Size(260, 20)
                    };

                    var buttonOk = new Button()
                    {
                        Text = "确定",
                        Location = new System.Drawing.Point(110, 80),
                        Size = new System.Drawing.Size(75, 23),
                        DialogResult = DialogResult.OK
                    };

                    var buttonCancel = new Button()
                    {
                        Text = "取消",
                        Location = new System.Drawing.Point(195, 80),
                        Size = new System.Drawing.Size(75, 23),
                        DialogResult = DialogResult.Cancel
                    };

                    form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
                    form.AcceptButton = buttonOk;
                    form.CancelButton = buttonCancel;

                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        return textBox.Text;
                    }
                    else
                    {
                        return defaultValue;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"显示输入对话框失败: {ex.Message}", ex);
                return defaultValue;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // 当前实现中没有需要特别释放的资源
            // 但为了接口一致性，提供空实现
        }
    }

    /// <summary>
    /// UI事件类
    /// </summary>
    public class UIEvent : Events.EventArgs
    {
        public string EventType { get; }
        public string Action { get; }
        public string Details { get; }
        public new DateTime Timestamp { get; }

        public UIEvent(string eventType, string action, string details = null)
            : base("UIService")
        {
            EventType = eventType;
            Action = action;
            Details = details;
            Timestamp = DateTime.Now;
        }
    }
}