namespace DotNetARX.Interfaces
{
    /// <summary>
    /// 数据库操作接口
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>
        /// 添加实体到模型空间
        /// </summary>
        ObjectId AddToModelSpace(Entity entity);

        /// <summary>
        /// 批量添加实体到模型空间
        /// </summary>
        ObjectIdCollection AddToModelSpace(IEnumerable<Entity> entities);

        /// <summary>
        /// 添加实体到图纸空间
        /// </summary>
        ObjectId AddToPaperSpace(Entity entity);

        /// <summary>
        /// 添加实体到当前空间
        /// </summary>
        ObjectId AddToCurrentSpace(Entity entity);

        /// <summary>
        /// 删除实体
        /// </summary>
        bool DeleteEntity(ObjectId entityId);

        /// <summary>
        /// 批量删除实体
        /// </summary>
        int DeleteEntities(IEnumerable<ObjectId> entityIds);

        /// <summary>
        /// 获取数据库信息
        /// </summary>
        DatabaseInfo GetDatabaseInfo();
    }
}