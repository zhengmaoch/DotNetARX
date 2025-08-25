namespace DotNetARX.Tests.Services
{
    [TestClass]
    public class DrawingOperationsServiceTests : TestBase
    {
        private DrawingOperationsService _drawingOperationsService;
        private Mock<IEventBus> _mockEventBus;
        private Mock<IPerformanceMonitor> _mockPerformanceMonitor;
        private Mock<ILogger> _mockLogger;
        private Mock<IDatabaseOperations> _mockDatabaseOperations;
        private Mock<IOperation> _mockOperation;

        [TestInitialize]
        public void Setup()
        {
            base.TestInitialize();

            _mockEventBus = new Mock<IEventBus>();
            _mockPerformanceMonitor = new Mock<IPerformanceMonitor>();
            _mockLogger = new Mock<ILogger>();
            _mockDatabaseOperations = new Mock<IDatabaseOperations>();
            _mockOperation = new Mock<IOperation>();

            _mockPerformanceMonitor
                .Setup(x => x.StartOperation(It.IsAny<string>()))
                .Returns(_mockOperation.Object);

            // 模拟数据库操作返回有效的ObjectId
            _mockDatabaseOperations
                .Setup(x => x.AddToCurrentSpace(It.IsAny<Entity>()))
                .Returns(() => CreateMockObjectId());

            _drawingOperationsService = new DrawingOperationsService(
                _mockEventBus.Object,
                _mockPerformanceMonitor.Object,
                _mockLogger.Object,
                _mockDatabaseOperations.Object);
        }

        private ObjectId CreateMockObjectId()
        {
            // 创建一个临时实体并添加到测试数据库中，返回其ObjectId
            var line = new Line(Point3d.Origin, new Point3d(1, 1, 0));
            return AddEntityToModelSpace(line);
        }

        [TestMethod]
        public void DrawLine_ValidPoints_ReturnsValidObjectId()
        {
            // Arrange
            var startPoint = new Point3d(0, 0, 0);
            var endPoint = new Point3d(100, 100, 0);

            // Act
            var result = _drawingOperationsService.DrawLine(startPoint, endPoint);

            // Assert
            Assert.IsFalse(result.IsNull);

            // 验证数据库操作被调用
            _mockDatabaseOperations.Verify(x => x.AddToCurrentSpace(It.IsAny<Line>()), Times.Once);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DrawLine"), Times.Once);

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<DrawingEvent>()), Times.Once);

            // 验证日志记录
            _mockLogger.Verify(x => x.Info(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void DrawLine_SamePoints_ThrowsArgumentException()
        {
            // Arrange
            var point = new Point3d(50, 50, 0);

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingOperationsService.DrawLine(point, point));
        }

        [TestMethod]
        public void DrawCircle_ValidParameters_ReturnsValidObjectId()
        {
            // Arrange
            var center = new Point3d(50, 50, 0);
            var radius = 25.0;

            // Act
            var result = _drawingOperationsService.DrawCircle(center, radius);

            // Assert
            Assert.IsFalse(result.IsNull);

            // 验证数据库操作被调用
            _mockDatabaseOperations.Verify(x => x.AddToCurrentSpace(It.IsAny<Circle>()), Times.Once);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DrawCircle"), Times.Once);

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<DrawingEvent>()), Times.Once);
        }

        [TestMethod]
        public void DrawCircle_ZeroRadius_ThrowsArgumentException()
        {
            // Arrange
            var center = new Point3d(50, 50, 0);
            var radius = 0.0;

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingOperationsService.DrawCircle(center, radius));
        }

        [TestMethod]
        public void DrawCircle_NegativeRadius_ThrowsArgumentException()
        {
            // Arrange
            var center = new Point3d(50, 50, 0);
            var radius = -10.0;

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingOperationsService.DrawCircle(center, radius));
        }

        [TestMethod]
        public void DrawArc_ValidParameters_ReturnsValidObjectId()
        {
            // Arrange
            var center = new Point3d(30, 30, 0);
            var radius = 15.0;
            var startAngle = 0.0;
            var endAngle = Math.PI / 2; // 90度

            // Act
            var result = _drawingOperationsService.DrawArc(center, radius, startAngle, endAngle);

            // Assert
            Assert.IsFalse(result.IsNull);

            // 验证数据库操作被调用
            _mockDatabaseOperations.Verify(x => x.AddToCurrentSpace(It.IsAny<Arc>()), Times.Once);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DrawArc"), Times.Once);

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<DrawingEvent>()), Times.Once);
        }

        [TestMethod]
        public void DrawArc_ZeroRadius_ThrowsArgumentException()
        {
            // Arrange
            var center = new Point3d(30, 30, 0);
            var radius = 0.0;
            var startAngle = 0.0;
            var endAngle = Math.PI / 2;

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingOperationsService.DrawArc(center, radius, startAngle, endAngle));
        }

        [TestMethod]
        public void DrawPolyline_ValidPoints_ReturnsValidObjectId()
        {
            // Arrange
            var points = new List<Point2d>
            {
                new Point2d(0, 0),
                new Point2d(10, 0),
                new Point2d(10, 10),
                new Point2d(0, 10)
            };
            var isClosed = true;

            // Act
            var result = _drawingOperationsService.DrawPolyline(points, isClosed);

            // Assert
            Assert.IsFalse(result.IsNull);

            // 验证数据库操作被调用
            _mockDatabaseOperations.Verify(x => x.AddToCurrentSpace(It.IsAny<Polyline>()), Times.Once);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DrawPolyline"), Times.Once);

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<DrawingEvent>()), Times.Once);
        }

        [TestMethod]
        public void DrawPolyline_NullPoints_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                _drawingOperationsService.DrawPolyline(null));
        }

        [TestMethod]
        public void DrawPolyline_InsufficientPoints_ThrowsArgumentException()
        {
            // Arrange
            var points = new List<Point2d> { new Point2d(0, 0) }; // 只有一个点

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingOperationsService.DrawPolyline(points));
        }

        [TestMethod]
        public void DrawText_ValidParameters_ReturnsValidObjectId()
        {
            // Arrange
            var text = "Test Text";
            var position = new Point3d(20, 20, 0);
            var height = 2.5;
            var rotation = Math.PI / 4; // 45度

            // Act
            var result = _drawingOperationsService.DrawText(text, position, height, rotation);

            // Assert
            Assert.IsFalse(result.IsNull);

            // 验证数据库操作被调用
            _mockDatabaseOperations.Verify(x => x.AddToCurrentSpace(It.IsAny<DBText>()), Times.Once);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DrawText"), Times.Once);

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<DrawingEvent>()), Times.Once);
        }

        [TestMethod]
        public void DrawText_EmptyText_ThrowsArgumentException()
        {
            // Arrange
            var position = new Point3d(20, 20, 0);
            var height = 2.5;

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingOperationsService.DrawText("", position, height));

            Assert.ThrowsException<ArgumentException>(() =>
                _drawingOperationsService.DrawText(null, position, height));
        }

        [TestMethod]
        public void DrawText_ZeroHeight_ThrowsArgumentException()
        {
            // Arrange
            var text = "Test Text";
            var position = new Point3d(20, 20, 0);
            var height = 0.0;

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingOperationsService.DrawText(text, position, height));
        }

        [TestMethod]
        public void DrawMText_ValidParameters_ReturnsValidObjectId()
        {
            // Arrange
            var text = "Multi-line\nTest Text";
            var position = new Point3d(40, 40, 0);
            var width = 100.0;
            var height = 2.0;

            // Act
            var result = _drawingOperationsService.DrawMText(text, position, width, height);

            // Assert
            Assert.IsFalse(result.IsNull);

            // 验证数据库操作被调用
            _mockDatabaseOperations.Verify(x => x.AddToCurrentSpace(It.IsAny<MText>()), Times.Once);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DrawMText"), Times.Once);

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<DrawingEvent>()), Times.Once);
        }

        [TestMethod]
        public void DrawMText_EmptyText_ThrowsArgumentException()
        {
            // Arrange
            var position = new Point3d(40, 40, 0);
            var width = 100.0;
            var height = 2.0;

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingOperationsService.DrawMText("", position, width, height));

            Assert.ThrowsException<ArgumentException>(() =>
                _drawingOperationsService.DrawMText(null, position, width, height));
        }

        [TestMethod]
        public void DrawMText_ZeroWidth_ThrowsArgumentException()
        {
            // Arrange
            var text = "Multi-line Text";
            var position = new Point3d(40, 40, 0);
            var width = 0.0;
            var height = 2.0;

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingOperationsService.DrawMText(text, position, width, height));
        }

        [TestMethod]
        public void DrawMText_ZeroHeight_ThrowsArgumentException()
        {
            // Arrange
            var text = "Multi-line Text";
            var position = new Point3d(40, 40, 0);
            var width = 100.0;
            var height = 0.0;

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingOperationsService.DrawMText(text, position, width, height));
        }

        [TestMethod]
        public void DrawPolyline_OpenPolyline_ReturnsValidObjectId()
        {
            // Arrange
            var points = new List<Point2d>
            {
                new Point2d(0, 0),
                new Point2d(20, 0),
                new Point2d(20, 20)
            };
            var isClosed = false;

            // Act
            var result = _drawingOperationsService.DrawPolyline(points, isClosed);

            // Assert
            Assert.IsFalse(result.IsNull);

            // 验证数据库操作被调用
            _mockDatabaseOperations.Verify(x => x.AddToCurrentSpace(It.IsAny<Polyline>()), Times.Once);
        }

        [TestMethod]
        public void DrawText_DefaultRotation_ReturnsValidObjectId()
        {
            // Arrange
            var text = "Default Rotation";
            var position = new Point3d(60, 60, 0);
            var height = 3.0;

            // Act (不指定旋转角度，使用默认值0)
            var result = _drawingOperationsService.DrawText(text, position, height);

            // Assert
            Assert.IsFalse(result.IsNull);

            // 验证数据库操作被调用
            _mockDatabaseOperations.Verify(x => x.AddToCurrentSpace(It.IsAny<DBText>()), Times.Once);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _drawingOperationsService?.Dispose();
            base.TestCleanup();
        }
    }
}