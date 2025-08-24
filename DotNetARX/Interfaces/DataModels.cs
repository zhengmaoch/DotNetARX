

namespace DotNetARX.Interfaces
{
    /// <summary>
    /// 文档信息类
    /// </summary>
    public class DocumentInfo
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public bool IsModified { get; set; }
        public bool IsSaved { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastModifiedTime { get; set; }
        public string Version { get; set; }
        public long FileSize { get; set; }
    }

    /// <summary>
    /// 数据库信息类
    /// </summary>
    public class DatabaseInfo
    {
        public string FileName { get; set; }
        public string Version { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime ModificationTime { get; set; }
        public long EntityCount { get; set; }
        public long LayerCount { get; set; }
        public long BlockCount { get; set; }
        public string CurrentLayer { get; set; }
        public bool IsModified { get; set; }
    }
}