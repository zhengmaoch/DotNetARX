namespace DotNetARX.Exceptions
{
    /// <summary>
    /// DotNetARX自定义异常基类
    /// </summary>
    public class DotNetARXException : Exception
    {
        public string Operation { get; }
        public DateTime Timestamp { get; }

        public DotNetARXException(string message) : base(message)
        {
            Operation = "General";
            Timestamp = DateTime.Now;
        }

        public DotNetARXException(string operation, string message) : base(message)
        {
            Operation = operation;
            Timestamp = DateTime.Now;
        }

        public DotNetARXException(string message, Exception innerException)
            : base(message, innerException)
        {
            Operation = "General";
            Timestamp = DateTime.Now;
        }

        public DotNetARXException(string operation, string message, Exception innerException)
            : base(message, innerException)
        {
            Operation = operation;
            Timestamp = DateTime.Now;
        }
    }
}