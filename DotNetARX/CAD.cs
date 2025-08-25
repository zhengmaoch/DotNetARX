namespace DotNetARX
{
    /// <summary>
    /// DotNetARX 终极统一API
    /// 易用性与高性能的完美结合 - 默认即最优实现
    /// </summary>
    public static partial class CAD
    {
        private static readonly Lazy<bool> _initialized = new(() =>
        {
            InitializeCADSystem();
            return true;
        });

        /// <summary>
        /// 确保系统已初始化
        /// </summary>
        public static void EnsureInitialized() => _ = _initialized.Value;

        /// <summary>
        /// 初始化CAD系统
        /// </summary>
        private static void InitializeCADSystem()
        {
            // 初始化智能服务定位器
            var container = SmartServiceLocator.Current;

            // 注册DotNetARX核心服务
            container.RegisterDotNetARXServices();

            // 初始化性能监控
            PerformanceEngine.Initialize();

            // 初始化智能上下文管理器
            AutoCADContext.Initialize();

            // 初始化日志系统
            LogManager.Initialize();

            // 记录容器信息
            var logger = LogManager.GetLogger(typeof(CAD));
            logger.Info($"CAD系统初始化完成 - {SmartServiceLocator.GetContainerInfo()}");
        }

        #region 内部辅助方法

        /// <summary>
        /// 添加实体到当前空间 - 内联优化
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ObjectId AddToCurrentSpace(Entity entity)
        {
            var context = AutoCADContext.Current;
            var currentSpace = context.GetObject<BlockTableRecord>(
                context.Database.GetCurrentSpaceId(), OpenMode.ForWrite);

            var id = currentSpace.AppendEntity(entity);
            context.Transaction.AddNewlyCreatedDBObject(entity, true);
            return id;
        }

        #endregion 内部辅助方法

        /// <summary>
        /// 获取系统状态
        /// </summary>
        public static bool IsInitialized => _initialized.IsValueCreated && _initialized.Value;

    }
}