

namespace DotNetARX.Interfaces
{
    /// <summary>
    /// 命令操作接口
    /// </summary>
    public interface ICommandService
    {
        /// <summary>
        /// 发送命令（COM方式）
        /// </summary>
        void SendCommand(params string[] args);

        /// <summary>
        /// 投递命令（异步方式）
        /// </summary>
        void PostCommand(string expression);

        /// <summary>
        /// 队列表达式
        /// </summary>
        void QueueExpression(string expression);

        /// <summary>
        /// 执行ARX命令
        /// </summary>
        int ExecuteCommand(ResultBuffer args);
    }
}