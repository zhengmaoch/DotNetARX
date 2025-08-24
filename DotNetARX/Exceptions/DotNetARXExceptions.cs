namespace DotNetARX.Exceptions
{
    /// <summary>
    /// DotNetARX自定义异常基类
    /// </summary>
    public abstract class DotNetARXException : Exception
    {
        public string Operation { get; }
        public DateTime Timestamp { get; }

        protected DotNetARXException(string message) : base(message)
        {
            Operation = "General";
            Timestamp = DateTime.Now;
        }

        protected DotNetARXException(string operation, string message) : base(message)
        {
            Operation = operation;
            Timestamp = DateTime.Now;
        }

        protected DotNetARXException(string operation, string message, Exception innerException)
            : base(message, innerException)
        {
            Operation = operation;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// CAD操作异常
    /// </summary>
    public class CADOperationException : DotNetARXException
    {
        public ErrorStatus? ErrorStatus { get; }

        public CADOperationException(string message) : base(message)
        {
        }

        public CADOperationException(string operation, string message, ErrorStatus? errorStatus = null)
            : base(operation, message)
        {
            ErrorStatus = errorStatus;
        }

        public CADOperationException(string operation, string message, Exception innerException, ErrorStatus? errorStatus = null)
            : base(operation, message, innerException)
        {
            ErrorStatus = errorStatus;
        }
    }

    /// <summary>
    /// 实体操作异常
    /// </summary>
    public class EntityOperationException : CADOperationException
    {
        public ObjectId? EntityId { get; }

        public EntityOperationException(string message) : base(message)
        {
        }

        public EntityOperationException(string operation, ObjectId entityId, string message)
            : base(operation, message)
        {
            EntityId = entityId;
        }

        public EntityOperationException(string operation, ObjectId entityId, string message, Exception innerException)
            : base(operation, message, innerException)
        {
            EntityId = entityId;
        }
    }

    /// <summary>
    /// 资源管理异常
    /// </summary>
    public class ResourceManagementException : DotNetARXException
    {
        public string ResourceType { get; }

        public ResourceManagementException(string message) : base(message)
        {
            ResourceType = "Unknown";
        }

        public ResourceManagementException(string operation, string resourceType, string message)
            : base(operation, message)
        {
            ResourceType = resourceType;
        }

        public ResourceManagementException(string operation, string resourceType, string message, Exception innerException)
            : base(operation, message, innerException)
        {
            ResourceType = resourceType;
        }
    }

    /// <summary>
    /// 数据库操作异常
    /// </summary>
    public class DatabaseOperationException : CADOperationException
    {
        public DatabaseOperationException(string message) : base(message)
        {
        }

        public DatabaseOperationException(string operation, string message) : base(operation, message)
        {
        }

        public DatabaseOperationException(string operation, string message, Exception innerException) : base(operation, message, innerException)
        {
        }
    }

    /// <summary>
    /// 块操作异常
    /// </summary>
    public class BlockOperationException : CADOperationException
    {
        public BlockOperationException(string message) : base(message)
        {
        }

        public BlockOperationException(string operation, string message) : base(operation, message)
        {
        }

        public BlockOperationException(string operation, string message, Exception innerException) : base(operation, message, innerException)
        {
        }
    }

    /// <summary>
    /// 绘图操作异常
    /// </summary>
    public class DrawingOperationException : CADOperationException
    {
        public DrawingOperationException(string message) : base(message)
        {
        }

        public DrawingOperationException(string operation, string message) : base(operation, message)
        {
        }

        public DrawingOperationException(string operation, string message, Exception innerException) : base(operation, message, innerException)
        {
        }
    }

    /// <summary>
    /// 几何操作异常
    /// </summary>
    public class GeometryOperationException : CADOperationException
    {
        public GeometryOperationException(string message) : base(message)
        {
        }

        public GeometryOperationException(string operation, string message) : base(operation, message)
        {
        }

        public GeometryOperationException(string operation, string message, Exception innerException) : base(operation, message, innerException)
        {
        }
    }

    /// <summary>
    /// 样式操作异常
    /// </summary>
    public class StyleOperationException : CADOperationException
    {
        public StyleOperationException(string message) : base(message)
        {
        }

        public StyleOperationException(string operation, string message) : base(operation, message)
        {
        }

        public StyleOperationException(string operation, string message, Exception innerException) : base(operation, message, innerException)
        {
        }
    }

    /// <summary>
    /// 表格操作异常
    /// </summary>
    public class TableOperationException : CADOperationException
    {
        public TableOperationException(string message) : base(message)
        {
        }

        public TableOperationException(string operation, string message) : base(operation, message)
        {
        }

        public TableOperationException(string operation, string message, Exception innerException) : base(operation, message, innerException)
        {
        }
    }

    /// <summary>
    /// 布局操作异常
    /// </summary>
    public class LayoutOperationException : CADOperationException
    {
        public LayoutOperationException(string message) : base(message)
        {
        }

        public LayoutOperationException(string operation, string message) : base(operation, message)
        {
        }

        public LayoutOperationException(string operation, string message, Exception innerException) : base(operation, message, innerException)
        {
        }
    }

    /// <summary>
    /// 进度管理异常
    /// </summary>
    public class ProgressManagerException : DotNetARXException
    {
        public ProgressManagerException(string message) : base(message)
        {
        }

        public ProgressManagerException(string operation, string message) : base(operation, message)
        {
        }

        public ProgressManagerException(string operation, string message, Exception innerException) : base(operation, message, innerException)
        {
        }
    }
}