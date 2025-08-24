

namespace DotNetARX.Interfaces
{
    /// <summary>
    /// 事件处理器接口
    /// </summary>
    public interface IEventHandler<in T> where T : Events.EventArgs
    {
        Task HandleAsync(T eventArgs);

        int Priority { get; }
    }
}