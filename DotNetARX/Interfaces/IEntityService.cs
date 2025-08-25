namespace DotNetARX.Interfaces
{
    /// <summary>
    /// 实体操作接口
    /// </summary>
    public interface IEntityService
    {
        /// <summary>
        /// 移动实体
        /// </summary>
        bool MoveEntity(ObjectId entityId, Point3d fromPoint, Point3d toPoint);

        /// <summary>
        /// 复制实体
        /// </summary>
        ObjectId CopyEntity(ObjectId entityId, Point3d fromPoint, Point3d toPoint);

        /// <summary>
        /// 旋转实体
        /// </summary>
        bool RotateEntity(ObjectId entityId, Point3d basePoint, double angle);

        /// <summary>
        /// 缩放实体
        /// </summary>
        bool ScaleEntity(ObjectId entityId, Point3d basePoint, double scaleFactor);

        /// <summary>
        /// 偏移实体
        /// </summary>
        ObjectIdCollection OffsetEntity(ObjectId entityId, double distance);

        /// <summary>
        /// 镜像实体
        /// </summary>
        ObjectId MirrorEntity(ObjectId entityId, Point3d mirrorPt1, Point3d mirrorPt2, bool eraseSource);

        /// <summary>
        /// 验证实体的有效性
        /// </summary>
        bool ValidateEntity(ObjectId entityId);
    }
}