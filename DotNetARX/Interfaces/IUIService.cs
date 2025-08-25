using CursorType = DotNetARX.Models.CursorType;

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
        /// 显示确认对话框（别名方法）
        /// </summary>
        bool ShowConfirmationDialog(string message, string title = "确认");

        /// <summary>
        /// 获取用户输入
        /// </summary>
        string GetUserInput(string prompt, string defaultValue = "");

        /// <summary>
        /// 选择文件
        /// </summary>
        string SelectFile(string title, string filter, bool saveMode = false);

        /// <summary>
        /// 显示进度条
        /// </summary>
        IProgressManager ShowProgress(string title, string message, int maxProgress);

        /// <summary>
        /// 隐藏进度条
        /// </summary>
        void HideProgress();

        /// <summary>
        /// 设置状态栏文本
        /// </summary>
        void SetStatusBarText(string text);

        /// <summary>
        /// 清除状态栏
        /// </summary>
        void ClearStatusBar();

        /// <summary>
        /// 显示工具提示
        /// </summary>
        void ShowTooltip(string message, Point3d position);

        /// <summary>
        /// 隐藏工具提示
        /// </summary>
        void HideTooltip();

        /// <summary>
        /// 刷新用户界面
        /// </summary>
        void RefreshUI();

        /// <summary>
        /// 启用/禁用用户界面
        /// </summary>
        void EnableUI(bool enable);

        /// <summary>
        /// 显示上下文菜单
        /// </summary>
        void ShowContextMenu(List<MenuItemDefinition> menuItems, Point3d position);

        /// <summary>
        /// 设置光标
        /// </summary>
        void SetCursor(CursorType cursorType);

        /// <summary>
        /// 重置光标
        /// </summary>
        void ResetCursor();

        /// <summary>
        /// 释放资源
        /// </summary>
        void Dispose();
    }
}