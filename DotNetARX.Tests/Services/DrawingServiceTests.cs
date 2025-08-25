namespace DotNetARX.Tests.Services
{
    [TestClass("DrawingServiceTests")]
    public class DrawingServiceTests : TestBase
    {
        private DrawingService _drawingService;
        private Mock<IEventBus> _mockEventBus;
        private Mock<IPerformanceMonitor> _mockPerformanceMonitor;
        private Mock<ILogger> _mockLogger;
        private Mock<IDatabaseService> _mockDatabaseOperations;
        private Mock<IOperation> _mockOperation;

        [TestInitialize]
        public void Setup()
        {
            base.TestInitialize();

            _mockEventBus = new Mock<IEventBus>();
            _mockPerformanceMonitor = new Mock<IPerformanceMonitor>();
            _mockLogger = new Mock<ILogger>();
            _mockDatabaseOperations = new Mock<IDatabaseService>();
            _mockOperation = new Mock<IOperation>();

            _mockPerformanceMonitor
                .Setup(x => x.StartOperation(It.IsAny<string>()))
                .Returns(_mockOperation.Object);

            // 模拟数据库操作返回有效的ObjectId
            _mockDatabaseOperations
                .Setup(x => x.AddToCurrentSpace(It.IsAny<Entity>()))
                .Returns(() => CreateMockObjectId());

            _drawingService = new DrawingService(
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

        [TestMethod("DrawLine_ValidPoints_ReturnsValidObjectId")]
        public void DrawLine_ValidPoints_ReturnsValidObjectId()
        {
            // Arrange
            var startPoint = new Point3d(0, 0, 0);
            var endPoint = new Point3d(100, 100, 0);

            // Act
            var result = _drawingService.DrawLine(startPoint, endPoint);

            // Assert
            Assert.IsFalse(result.IsNull);

            // 验证数据库操作被调用
            _mockDatabaseOperations.Verify(x => x.AddToCurrentSpace(It.IsAny<Line>()), Times.Once());

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DrawLine"), Times.Once());

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<DrawingEvent>()), Times.Once());

            // 验证日志记录
            _mockLogger.Verify(x => x.Info(It.IsAny<string>()), Times.Once());
        }

        [TestMethod("DrawLine_SamePoints_ThrowsArgumentException")]
        public void DrawLine_SamePoints_ThrowsArgumentException()
        {
            // Arrange
            var point = new Point3d(50, 50, 0);

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingService.DrawLine(point, point));
        }

        [TestMethod("DrawCircle_ValidParameters_ReturnsValidObjectId")]
        public void DrawCircle_ValidParameters_ReturnsValidObjectId()
        {
            // Arrange
            var center = new Point3d(50, 50, 0);
            var radius = 25.0;

            // Act
            var result = _drawingService.DrawCircle(center, radius);

            // Assert
            Assert.IsFalse(result.IsNull);

            // 验证数据库操作被调用
            _mockDatabaseOperations.Verify(x => x.AddToCurrentSpace(It.IsAny<Circle>()), Times.Once());

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DrawCircle"), Times.Once());

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<DrawingEvent>()), Times.Once());
        }

        [TestMethod("DrawCircle_ZeroRadius_ThrowsArgumentException")]
        public void DrawCircle_ZeroRadius_ThrowsArgumentException()
        {
            // Arrange
            var center = new Point3d(50, 50, 0);
            var radius = 0.0;

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingService.DrawCircle(center, radius));
        }

        [TestMethod("DrawCircle_NegativeRadius_ThrowsArgumentException")]
        public void DrawCircle_NegativeRadius_ThrowsArgumentException()
        {
            // Arrange
            var center = new Point3d(50, 50, 0);
            var radius = -10.0;

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingService.DrawCircle(center, radius));
        }

        [TestMethod("DrawArc_ValidParameters_ReturnsValidObjectId")]
        public void DrawArc_ValidParameters_ReturnsValidObjectId()
        {
            // Arrange
            var center = new Point3d(30, 30, 0);
            var radius = 15.0;
            var startAngle = 0.0;
            var endAngle = Math.PI / 2; // 90度

            // Act
            var result = _drawingService.DrawArc(center, radius, startAngle, endAngle);

            // Assert
            Assert.IsFalse(result.IsNull);

            // 验证数据库操作被调用
            _mockDatabaseOperations.Verify(x => x.AddToCurrentSpace(It.IsAny<Arc>()), Times.Once());

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DrawArc"), Times.Once());

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<DrawingEvent>()), Times.Once());
        }

        [TestMethod("DrawArc_ZeroRadius_ThrowsArgumentException")]
        public void DrawArc_ZeroRadius_ThrowsArgumentException()
        {
            // Arrange
            var center = new Point3d(30, 30, 0);
            var radius = 0.0;
            var startAngle = 0.0;
            var endAngle = Math.PI / 2;

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingService.DrawArc(center, radius, startAngle, endAngle));
        }

        [TestMethod("DrawPolyline_ValidPoints_ReturnsValidObjectId")]
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
            var result = _drawingService.DrawPolyline(points, isClosed);

            // Assert
            Assert.IsFalse(result.IsNull);

            // 验证数据库操作被调用
            _mockDatabaseOperations.Verify(x => x.AddToCurrentSpace(It.IsAny<Polyline>()), Times.Once());

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DrawPolyline"), Times.Once());

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<DrawingEvent>()), Times.Once());
        }

        [TestMethod("DrawPolyline_NullPoints_ThrowsArgumentNullException")]
        public void DrawPolyline_NullPoints_ThrowsArgumentNullException()
        {
            // Arrange
            List<Point2d> points = null;
            var isClosed = true;

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                _drawingService.DrawPolyline(points, isClosed));
        }

        [TestMethod("DrawPolyline_EmptyPoints_ThrowsArgumentException")]
        public void DrawPolyline_EmptyPoints_ThrowsArgumentException()
        {
            // Arrange
            var points = new List<Point2d>();
            var isClosed = true;

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingService.DrawPolyline(points, isClosed));
        }

        [TestMethod("DrawRectangle_ValidParameters_ReturnsValidObjectId")]
        public void DrawRectangle_ValidParameters_ReturnsValidObjectId()
        {
            // Arrange
            var corner1 = new Point3d(0, 0, 0);
            var corner2 = new Point3d(100, 50, 0);

            // Act
            var result = _drawingService.DrawRectangle(corner1, corner2);

            // Assert
            Assert.IsFalse(result.IsNull);

            // 验证数据库操作被调用（矩形由4条线组成）
            _mockDatabaseOperations.Verify(x => x.AddToCurrentSpace(It.IsAny<Line>()), Times.Exactly(4));

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DrawRectangle"), Times.Once());

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<DrawingEvent>()), Times.Once());
        }

        [TestMethod("DrawRectangle_IdenticalCorners_ThrowsArgumentException")]
        public void DrawRectangle_IdenticalCorners_ThrowsArgumentException()
        {
            // Arrange
            var corner = new Point3d(50, 50, 0);

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingService.DrawRectangle(corner, corner));
        }

        [TestMethod("DrawText_ValidParameters_ReturnsValidObjectId")]
        public void DrawText_ValidParameters_ReturnsValidObjectId()
        {
            // Arrange
            var position = new Point3d(25, 25, 0);
            var text = "测试文本";
            var height = 2.5;

            // Act
            var result = _drawingService.DrawText(position, text, height);

            // Assert
            Assert.IsFalse(result.IsNull);

            // 验证数据库操作被调用
            _mockDatabaseOperations.Verify(x => x.AddToCurrentSpace(It.IsAny<DBText>()), Times.Once());

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DrawText"), Times.Once());

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<DrawingEvent>()), Times.Once());
        }

        [TestMethod("DrawText_NullText_ThrowsArgumentNullException")]
        public void DrawText_NullText_ThrowsArgumentNullException()
        {
            // Arrange
            var position = new Point3d(25, 25, 0);
            string text = null;
            var height = 2.5;

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                _drawingService.DrawText(position, text, height));
        }

        [TestMethod("DrawText_EmptyText_ReturnsValidObjectId")]
        public void DrawText_EmptyText_ReturnsValidObjectId()
        {
            // Arrange
            var position = new Point3d(25, 25, 0);
            var text = "";
            var height = 2.5;

            // Act
            var result = _drawingService.DrawText(position, text, height);

            // Assert
            Assert.IsFalse(result.IsNull);

            // 验证数据库操作被调用
            _mockDatabaseOperations.Verify(x => x.AddToCurrentSpace(It.IsAny<DBText>()), Times.Once());

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DrawText"), Times.Once());
        }

        [TestMethod("DrawText_ZeroHeight_ThrowsArgumentException")]
        public void DrawText_ZeroHeight_ThrowsArgumentException()
        {
            // Arrange
            var position = new Point3d(25, 25, 0);
            var text = "测试文本";
            var height = 0.0;

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingService.DrawText(position, text, height));
        }

        [TestMethod("DrawText_NegativeHeight_ThrowsArgumentException")]
        public void DrawText_NegativeHeight_ThrowsArgumentException()
        {
            // Arrange
            var position = new Point3d(25, 25, 0);
            var text = "测试文本";
            var height = -1.0;

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingService.DrawText(position, text, height));
        }

        [TestMethod("DrawMText_ValidParameters_ReturnsValidObjectId")]
        public void DrawMText_ValidParameters_ReturnsValidObjectId()
        {
            // Arrange
            var position = new Point3d(50, 50, 0);
            var text = "多行文本测试\n第二行文本";
            var height = 3.0;
            var width = 50.0;

            // Act
            var result = _drawingService.DrawMText(position, text, height, width);

            // Assert
            Assert.IsFalse(result.IsNull);

            // 验证数据库操作被调用
            _mockDatabaseOperations.Verify(x => x.AddToCurrentSpace(It.IsAny<MText>()), Times.Once());

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DrawMText"), Times.Once());

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<DrawingEvent>()), Times.Once());
        }

        [TestMethod("DrawMText_NullText_ThrowsArgumentNullException")]
        public void DrawMText_NullText_ThrowsArgumentNullException()
        {
            // Arrange
            var position = new Point3d(50, 50, 0);
            string text = null;
            var height = 3.0;
            var width = 50.0;

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                _drawingService.DrawMText(position, text, height, width));
        }

        [TestMethod("DrawMText_ZeroHeight_ThrowsArgumentException")]
        public void DrawMText_ZeroHeight_ThrowsArgumentException()
        {
            // Arrange
            var position = new Point3d(50, 50, 0);
            var text = "多行文本测试";
            var height = 0.0;
            var width = 50.0;

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingService.DrawMText(position, text, height, width));
        }

        [TestMethod("DrawMText_NegativeWidth_ThrowsArgumentException")]
        public void DrawMText_NegativeWidth_ThrowsArgumentException()
        {
            // Arrange
            var position = new Point3d(50, 50, 0);
            var text = "多行文本测试";
            var height = 3.0;
            var width = -1.0;

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingService.DrawMText(position, text, height, width));
        }

        [TestMethod("DrawHatch_ValidParameters_ReturnsValidObjectId")]
        public void DrawHatch_ValidParameters_ReturnsValidObjectId()
        {
            // Arrange
            var boundaryPoints = new List<Point2d>
            {
                new Point2d(0, 0),
                new Point2d(10, 0),
                new Point2d(10, 10),
                new Point2d(0, 10)
            };
            var patternName = "SOLID";

            // Act
            var result = _drawingService.DrawHatch(boundaryPoints, patternName);

            // Assert
            Assert.IsFalse(result.IsNull);

            // 验证数据库操作被调用
            _mockDatabaseOperations.Verify(x => x.AddToCurrentSpace(It.IsAny<Hatch>()), Times.Once());

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DrawHatch"), Times.Once());

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<DrawingEvent>()), Times.Once());
        }

        [TestMethod("DrawHatch_NullBoundary_ThrowsArgumentNullException")]
        public void DrawHatch_NullBoundary_ThrowsArgumentNullException()
        {
            // Arrange
            List<Point2d> boundaryPoints = null;
            var patternName = "SOLID";

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                _drawingService.DrawHatch(boundaryPoints, patternName));
        }

        [TestMethod("DrawHatch_EmptyBoundary_ThrowsArgumentException")]
        public void DrawHatch_EmptyBoundary_ThrowsArgumentException()
        {
            // Arrange
            var boundaryPoints = new List<Point2d>();
            var patternName = "SOLID";

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingService.DrawHatch(boundaryPoints, patternName));
        }

        [TestMethod("DrawEllipse_ValidParameters_ReturnsValidObjectId")]
        public void DrawEllipse_ValidParameters_ReturnsValidObjectId()
        {
            // Arrange
            var center = new Point3d(50, 50, 0);
            var majorAxis = new Vector3d(20, 0, 0);
            var radiusRatio = 0.5;

            // Act
            var result = _drawingService.DrawEllipse(center, majorAxis, radiusRatio);

            // Assert
            Assert.IsFalse(result.IsNull);

            // 验证数据库操作被调用
            _mockDatabaseOperations.Verify(x => x.AddToCurrentSpace(It.IsAny<Ellipse>()), Times.Once());

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DrawEllipse"), Times.Once());

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<DrawingEvent>()), Times.Once());
        }

        [TestMethod("DrawEllipse_ZeroRadiusRatio_ThrowsArgumentException")]
        public void DrawEllipse_ZeroRadiusRatio_ThrowsArgumentException()
        {
            // Arrange
            var center = new Point3d(50, 50, 0);
            var majorAxis = new Vector3d(20, 0, 0);
            var radiusRatio = 0.0;

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingService.DrawEllipse(center, majorAxis, radiusRatio));
        }

        [TestMethod("DrawSpline_ValidParameters_ReturnsValidObjectId")]
        public void DrawSpline_ValidParameters_ReturnsValidObjectId()
        {
            // Arrange
            var controlPoints = new List<Point3d>
            {
                new Point3d(0, 0, 0),
                new Point3d(25, 50, 0),
                new Point3d(50, 25, 0),
                new Point3d(100, 100, 0)
            };

            // Act
            var result = _drawingService.DrawSpline(controlPoints);

            // Assert
            Assert.IsFalse(result.IsNull);

            // 验证数据库操作被调用
            _mockDatabaseOperations.Verify(x => x.AddToCurrentSpace(It.IsAny<Spline>()), Times.Once());

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DrawSpline"), Times.Once());

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<DrawingEvent>()), Times.Once());
        }

        [TestMethod("DrawSpline_NullControlPoints_ThrowsArgumentNullException")]
        public void DrawSpline_NullControlPoints_ThrowsArgumentNullException()
        {
            // Arrange
            List<Point3d> controlPoints = null;

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                _drawingService.DrawSpline(controlPoints));
        }

        [TestMethod("DrawSpline_EmptyControlPoints_ThrowsArgumentException")]
        public void DrawSpline_EmptyControlPoints_ThrowsArgumentException()
        {
            // Arrange
            var controlPoints = new List<Point3d>();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingService.DrawSpline(controlPoints));
        }

        [TestMethod("DrawDimension_ValidParameters_ReturnsValidObjectId")]
        public void DrawDimension_ValidParameters_ReturnsValidObjectId()
        {
            // Arrange
            var definitionPoint = new Point3d(0, 0, 0);
            var textPosition = new Point3d(50, 10, 0);
            var dimensionText = "50";

            // Act
            var result = _drawingService.DrawDimension(definitionPoint, textPosition, dimensionText);

            // Assert
            Assert.IsFalse(result.IsNull);

            // 验证数据库操作被调用
            _mockDatabaseOperations.Verify(x => x.AddToCurrentSpace(It.IsAny<Dimension>()), Times.Once());

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DrawDimension"), Times.Once());

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<DrawingEvent>()), Times.Once());
        }

        [TestMethod("DrawLeader_ValidParameters_ReturnsValidObjectId")]
        public void DrawLeader_ValidParameters_ReturnsValidObjectId()
        {
            // Arrange
            var points = new List<Point3d>
            {
                new Point3d(0, 0, 0),
                new Point3d(50, 50, 0),
                new Point3d(100, 50, 0)
            };
            var annotationText = "注释文本";

            // Act
            var result = _drawingService.DrawLeader(points, annotationText);

            // Assert
            Assert.IsFalse(result.IsNull);

            // 验证数据库操作被调用
            _mockDatabaseOperations.Verify(x => x.AddToCurrentSpace(It.IsAny<Leader>()), Times.Once());

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DrawLeader"), Times.Once());

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<DrawingEvent>()), Times.Once());
        }

        [TestMethod("DrawLeader_NullPoints_ThrowsArgumentNullException")]
        public void DrawLeader_NullPoints_ThrowsArgumentNullException()
        {
            // Arrange
            List<Point3d> points = null;
            var annotationText = "注释文本";

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                _drawingService.DrawLeader(points, annotationText));
        }

        [TestMethod("DrawLeader_EmptyPoints_ThrowsArgumentException")]
        public void DrawLeader_EmptyPoints_ThrowsArgumentException()
        {
            // Arrange
            var points = new List<Point3d>();
            var annotationText = "注释文本";

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _drawingService.DrawLeader(points, annotationText));
        }

        [TestCleanup]
        public void Cleanup()
        {
            _drawingService?.Dispose();
            base.TestCleanup();
        }
    }
}