using DotNetARX.DependencyInjection;
using DotNetARX.Interfaces;
using System.Windows.Forms;

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