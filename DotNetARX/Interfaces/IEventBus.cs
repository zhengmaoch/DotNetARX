

namespace DotNetARX.Interfaces
{
    /// <summary>
    /// 事件总线接口
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 发布事件
        /// </summary>
        Task PublishAsync<T>(T eventArgs) where T : Events.EventArgs;

        /// <summary>
        /// 同步发布事件
        /// </summary>
        void Publish<T>(T eventArgs) where T : Events.EventArgs;

        /// <summary>
        /// 订阅事件
        /// </summary>
        void Subscribe<T>(IEventHandler<T> handler) where T : Events.EventArgs;

        /// <summary>
        /// 订阅事件（使用委托）
        /// </summary>
        void Subscribe<T>(Func<T, Task> handler, int priority = 1) where T : Events.EventArgs;

        /// <summary>
        /// 取消订阅
        /// </summary>
        void Unsubscribe<T>(IEventHandler<T> handler) where T : Events.EventArgs;

        /// <summary>
        /// 清除所有订阅
        /// </summary>
        void ClearSubscriptions();
    }
}