namespace DotNetARX.Interfaces
{
    /// <summary>
    /// 图层管理接口
    /// </summary>
    public interface ILayerManager : IDisposable
    {
        /// <summary>
        /// 创建图层
        /// </summary>
        ObjectId CreateLayer(string layerName, short colorIndex = 7);

        /// <summary>
        /// 设置当前图层
        /// </summary>
        bool SetCurrentLayer(string layerName);

        /// <summary>
        /// 删除图层
        /// </summary>
        bool DeleteLayer(string layerName);

        /// <summary>
        /// 获取所有图层
        /// </summary>
        IEnumerable<LayerTableRecord> GetAllLayers();

        /// <summary>
        /// 获取图层名称列表
        /// </summary>
        IEnumerable<string> GetLayerNames();

        /// <summary>
        /// 检查图层是否存在
        /// </summary>
        bool LayerExists(string layerName);

        /// <summary>
        /// 设置图层属性
        /// </summary>
        bool SetLayerProperties(string layerName, short? colorIndex = null, bool? isLocked = null, bool? isFrozen = null);
    }
}