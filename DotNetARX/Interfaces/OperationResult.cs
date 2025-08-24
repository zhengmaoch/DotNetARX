

namespace DotNetARX.Interfaces
{
    /// <summary>
    /// 操作结果基类
    /// </summary>
    public class OperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }

        public OperationResult(bool success, string message = null, Exception exception = null)
        {
            Success = success;
            Message = message;
            Exception = exception;
        }

        public static OperationResult Successful(string message = "操作成功")
        {
            return new OperationResult(true, message);
        }

        public static OperationResult Failed(string message, Exception exception = null)
        {
            return new OperationResult(false, message, exception);
        }
    }

    /// <summary>
    /// 泛型操作结果
    /// </summary>
    public class OperationResult<T> : OperationResult
    {
        public T Data { get; set; }

        public OperationResult(bool success, T data = default, string message = null, Exception exception = null)
            : base(success, message, exception)
        {
            Data = data;
        }

        public static OperationResult<T> Successful(T data, string message = "操作成功")
        {
            return new OperationResult<T>(true, data, message);
        }

        public new static OperationResult<T> Failed(string message, Exception exception = null)
        {
            return new OperationResult<T>(false, default, message, exception);
        }
    }
}