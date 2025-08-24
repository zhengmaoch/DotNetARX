

namespace DotNetARX.Interfaces
{
    /// <summary>
    /// 用户界面操作接口
    /// </summary>
    public interface IUIService
    {
        /// <summary>
        /// 显示消息
        /// </summary>
        void ShowMessage(string message, string title = "提示");

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        bool ShowConfirmDialog(string message, string title = "确认");

        /// <summary>
        /// 获取用户输入
        /// </summary>
        string GetUserInput(string prompt, string defaultValue = "");

        /// <summary>
        /// 选择文件
        /// </summary>
        string SelectFile(string title, string filter, bool saveMode = false);
    }
}