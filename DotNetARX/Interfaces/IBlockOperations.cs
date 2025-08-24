namespace DotNetARX.Interfaces
{
    /// <summary>
    /// 块操作接口
    /// </summary>
    public interface IBlockOperations
    {
        /// <summary>
        /// 创建块定义
        /// </summary>
        ObjectId CreateBlockDefinition(string blockName, IEnumerable<Entity> entities, Point3d basePoint);

        /// <summary>
        /// 插入块引用
        /// </summary>
        ObjectId InsertBlock(string blockName, Point3d position, double scale = 1.0, double rotation = 0);

        /// <summary>
        /// 插入块引用（带属性）
        /// </summary>
        ObjectId InsertBlockWithAttributes(string blockName, Point3d position, Dictionary<string, string> attributes, double scale = 1.0, double rotation = 0);

        /// <summary>
        /// 删除块定义
        /// </summary>
        bool DeleteBlockDefinition(string blockName);

        /// <summary>
        /// 获取所有块定义
        /// </summary>
        IEnumerable<string> GetBlockNames();

        /// <summary>
        /// 分解块引用
        /// </summary>
        ObjectIdCollection ExplodeBlock(ObjectId blockReferenceId);
    }
}