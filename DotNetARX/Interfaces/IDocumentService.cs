

namespace DotNetARX.Interfaces
{
    /// <summary>
    /// 文档操作接口
    /// </summary>
    public interface IDocumentService
    {
        /// <summary>
        /// 检查文档是否已保存
        /// </summary>
        bool IsDocumentSaved();

        /// <summary>
        /// 保存文档
        /// </summary>
        bool SaveDocument();

        /// <summary>
        /// 另存为文档
        /// </summary>
        bool SaveDocumentAs(string fileName);

        /// <summary>
        /// 获取文档信息
        /// </summary>
        DocumentInfo GetDocumentInfo();
    }
}