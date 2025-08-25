namespace DotNetARX.Services
{
    /// <summary>
    /// 绘图操作服务实现
    /// </summary>
    public class DrawingOperationsService : IDrawingOperations
    {
        private readonly IEventBus _eventBus;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly ILogger _logger;
        private readonly IDatabaseOperations _databaseOperations;

        public DrawingOperationsService(
            IEventBus eventBus = null,
            IPerformanceMonitor performanceMonitor = null,
            ILogger logger = null,
            IDatabaseOperations databaseOperations = null)
        {
            _eventBus = eventBus ?? ServiceContainer.Instance.GetService<IEventBus>();
            _performanceMonitor = performanceMonitor ?? ServiceContainer.Instance.GetService<IPerformanceMonitor>();
            _logger = logger ?? ServiceContainer.Instance.GetService<ILogger>();
            _databaseOperations = databaseOperations ?? ServiceContainer.Instance.GetService<IDatabaseOperations>();
        }

        /// <summary>
        /// 绘制直线
        /// </summary>
        public ObjectId DrawLine(Point3d startPoint, Point3d endPoint)
        {
            using var operation = _performanceMonitor?.StartOperation("DrawLine");

            try
            {
                if (startPoint.IsEqualTo(endPoint))
                    throw new ArgumentException("起点和终点不能相同");

                var line = new Line(startPoint, endPoint);
                var objectId = _databaseOperations.AddToCurrentSpace(line);

                _eventBus?.Publish(new DrawingEvent("LineDrawn", objectId, $"From: {startPoint} To: {endPoint}"));
                _logger?.Info($"直线绘制成功: {objectId}, 从 {startPoint} 到 {endPoint}");

                return objectId;
            }
            catch (Exception ex)
            {
                _logger?.Error($"绘制直线失败: {ex.Message}", ex);
                throw new DotNetARXException($"绘制直线失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 绘制圆
        /// </summary>
        public ObjectId DrawCircle(Point3d center, double radius)
        {
            using var operation = _performanceMonitor?.StartOperation("DrawCircle");

            try
            {
                if (radius <= 0)
                    throw new ArgumentException("半径必须大于0");

                var circle = new Circle(center, Vector3d.ZAxis, radius);
                var objectId = _databaseOperations.AddToCurrentSpace(circle);

                _eventBus?.Publish(new DrawingEvent("CircleDrawn", objectId, $"Center: {center}, Radius: {radius}"));
                _logger?.Info($"圆形绘制成功: {objectId}, 中心 {center}, 半径 {radius}");

                return objectId;
            }
            catch (Exception ex)
            {
                _logger?.Error($"绘制圆形失败: {ex.Message}", ex);
                throw new DotNetARXException($"绘制圆形失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 绘制圆弧
        /// </summary>
        public ObjectId DrawArc(Point3d center, double radius, double startAngle, double endAngle)
        {
            using var operation = _performanceMonitor?.StartOperation("DrawArc");

            try
            {
                if (radius <= 0)
                    throw new ArgumentException("半径必须大于0");

                var arc = new Arc(center, radius, startAngle, endAngle);
                var objectId = _databaseOperations.AddToCurrentSpace(arc);

                _eventBus?.Publish(new DrawingEvent("ArcDrawn", objectId,
                    $"Center: {center}, Radius: {radius}, StartAngle: {startAngle}, EndAngle: {endAngle}"));
                _logger?.Info($"圆弧绘制成功: {objectId}, 中心 {center}, 半径 {radius}");

                return objectId;
            }
            catch (Exception ex)
            {
                _logger?.Error($"绘制圆弧失败: {ex.Message}", ex);
                throw new DotNetARXException($"绘制圆弧失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 绘制多段线
        /// </summary>
        public ObjectId DrawPolyline(IEnumerable<Point2d> points, bool isClosed = false)
        {
            using var operation = _performanceMonitor?.StartOperation("DrawPolyline");

            try
            {
                if (points == null)
                    throw new ArgumentNullException(nameof(points));

                var pointList = points.ToList();
                if (pointList.Count < 2)
                    throw new ArgumentException("至少需要2个点来绘制多段线");

                var polyline = new Polyline();

                for (int i = 0; i < pointList.Count; i++)
                {
                    polyline.AddVertexAt(i, pointList[i], 0, 0, 0);
                }

                polyline.Closed = isClosed;
                var objectId = _databaseOperations.AddToCurrentSpace(polyline);

                _eventBus?.Publish(new DrawingEvent("PolylineDrawn", objectId,
                    $"Points: {pointList.Count}, Closed: {isClosed}"));
                _logger?.Info($"多段线绘制成功: {objectId}, 点数 {pointList.Count}, 闭合 {isClosed}");

                return objectId;
            }
            catch (Exception ex)
            {
                _logger?.Error($"绘制多段线失败: {ex.Message}", ex);
                throw new DotNetARXException($"绘制多段线失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 绘制文本
        /// </summary>
        public ObjectId DrawText(string text, Point3d position, double height, double rotation = 0)
        {
            using var operation = _performanceMonitor?.StartOperation("DrawText");

            try
            {
                if (string.IsNullOrEmpty(text))
                    throw new ArgumentException("文本内容不能为空");

                if (height <= 0)
                    throw new ArgumentException("文本高度必须大于0");

                var dbText = new DBText
                {
                    TextString = text,
                    Position = position,
                    Height = height,
                    Rotation = rotation
                };

                var objectId = _databaseOperations.AddToCurrentSpace(dbText);

                _eventBus?.Publish(new DrawingEvent("TextDrawn", objectId,
                    $"Text: {text}, Position: {position}, Height: {height}"));
                _logger?.Info($"文本绘制成功: {objectId}, 内容 '{text}', 位置 {position}");

                return objectId;
            }
            catch (Exception ex)
            {
                _logger?.Error($"绘制文本失败: {ex.Message}", ex);
                throw new DotNetARXException($"绘制文本失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 绘制多行文本
        /// </summary>
        public ObjectId DrawMText(string text, Point3d position, double width, double height)
        {
            using var operation = _performanceMonitor?.StartOperation("DrawMText");

            try
            {
                if (string.IsNullOrEmpty(text))
                    throw new ArgumentException("文本内容不能为空");

                if (width <= 0 || height <= 0)
                    throw new ArgumentException("宽度和高度必须大于0");

                var mText = new MText
                {
                    Contents = text,
                    Location = position,
                    Width = width,
                    TextHeight = height
                };

                var objectId = _databaseOperations.AddToCurrentSpace(mText);

                _eventBus?.Publish(new DrawingEvent("MTextDrawn", objectId,
                    $"Text: {text}, Position: {position}, Width: {width}, Height: {height}"));
                _logger?.Info($"多行文本绘制成功: {objectId}, 内容 '{text}', 位置 {position}");

                return objectId;
            }
            catch (Exception ex)
            {
                _logger?.Error($"绘制多行文本失败: {ex.Message}", ex);
                throw new DotNetARXException($"绘制多行文本失败: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// 绘图事件类
    /// </summary>
    public class DrawingEvent : Events.EventArgs
    {
        public string EventType { get; }
        public ObjectId EntityId { get; }
        public string EntityType { get; }
        public string Details { get; }
        public new DateTime Timestamp { get; }

        public DrawingEvent(string eventType, ObjectId entityId, string entityType, string details = null)
            : base("DrawingOperationsService")
        {
            EventType = eventType;
            EntityId = entityId;
            EntityType = entityType;
            Details = details;
            Timestamp = DateTime.Now;
        }
    }
}