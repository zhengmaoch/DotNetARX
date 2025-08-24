namespace DotNetARX.Interfaces
{
    /// <summary>
    /// 进度管理接口
    /// </summary>
    public interface IProgressManager : IDisposable
    {
        /// <summary>
        /// 设置总操作数
        /// </summary>
        void SetTotalOperations(long totalOps);

        /// <summary>
        /// 更新进度
        /// </summary>
        void UpdateProgress(long currentOp, string message = null);

        /// <summary>
        /// 增量更新进度
        /// </summary>
        void Tick(string message = null);

        /// <summary>
        /// 完成进度
        /// </summary>
        void Complete(string message = "完成");

        /// <summary>
        /// 取消进度
        /// </summary>
        void Cancel();

        /// <summary>
        /// 进度是否被取消
        /// </summary>
        bool IsCancelled { get; }
    }
}