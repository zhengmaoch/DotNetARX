// 聚合引用文件：引入所有拆分后的接口和数据模型
// 这样可以保持向后兼容性，同时提供清晰的文件组织结构

// 导入所有独立的接口文件
global using DotNetARX.Interfaces;

namespace DotNetARX.Interfaces
{
    // 注意: 所有接口和类已拆分到独立文件中
    // - IEventHandler.cs: 事件处理器接口
    // - IEntityOperations.cs: 实体操作接口
    // - ILayerManager.cs: 图层管理接口
    // - ISelectionService.cs: 选择操作接口
    // - IDatabaseOperations.cs: 数据库操作接口
    // - IDrawingOperations.cs: 绘图操作接口
    // - IBlockOperations.cs: 块操作接口
    // - IProgressManager.cs: 进度管理接口
    // - ICommandService.cs: 命令操作接口
    // - IDocumentService.cs: 文档操作接口
    // - IGeometryService.cs: 几何工具接口
    // - IStyleService.cs: 样式管理接口
    // - ITableService.cs: 表格操作接口
    // - ILayoutService.cs: 布局操作接口
    // - IUIService.cs: 用户界面操作接口
    // - IUtilityService.cs: 工具服务接口
    // - IEventBus.cs: 事件总线接口
    // - DataModels.cs: DocumentInfo 和 DatabaseInfo 数据模型
    // - OperationResult.cs: 操作结果类型
    //
    // 此文件现在作为聚合引用，确保向后兼容性
}