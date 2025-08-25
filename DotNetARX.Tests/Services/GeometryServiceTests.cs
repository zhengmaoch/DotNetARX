namespace DotNetARX.Tests.Services
{
    [TestClass]
    public class GeometryServiceTests : TestBase
    {
        private GeometryService _geometryService;
        private Mock<IEventBus> _mockEventBus;
        private Mock<IPerformanceMonitor> _mockPerformanceMonitor;
        private Mock<ILogger> _mockLogger;
        private Mock<IOperation> _mockOperation;

        [TestInitialize]
        public void Setup()
        {
            base.TestInitialize();

            _mockEventBus = new Mock<IEventBus>();
            _mockPerformanceMonitor = new Mock<IPerformanceMonitor>();
            _mockLogger = new Mock<ILogger>();
            _mockOperation = new Mock<IOperation>();

            _mockPerformanceMonitor
                .Setup(x => x.StartOperation(It.IsAny<string>()))
                .Returns(_mockOperation.Object);

            _geometryService = new GeometryService(
                _mockEventBus.Object,
                _mockPerformanceMonitor.Object,
                _mockLogger.Object);
        }

        [TestMethod]
        public void CalculateDistance_TwoPoints_ReturnsCorrectDistance()
        {
            // Arrange
            var pt1 = new Point3d(0, 0, 0);
            var pt2 = new Point3d(3, 4, 0);
            var expectedDistance = 5.0; // 3-4-5 三角形

            // Act
            var result = _geometryService.CalculateDistance(pt1, pt2);

            // Assert
            Assert.AreEqual(expectedDistance, result, 1e-6);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("CalculateDistance"), Times.Once);
        }

        [TestMethod]
        public void CalculateDistance_SamePoints_ReturnsZero()
        {
            // Arrange
            var pt1 = new Point3d(10, 20, 30);
            var pt2 = new Point3d(10, 20, 30);

            // Act
            var result = _geometryService.CalculateDistance(pt1, pt2);

            // Assert
            Assert.AreEqual(0.0, result, 1e-10);
        }

        [TestMethod]
        public void CalculateDistance_3DPoints_ReturnsCorrectDistance()
        {
            // Arrange
            var pt1 = new Point3d(0, 0, 0);
            var pt2 = new Point3d(1, 1, 1);
            var expectedDistance = Math.Sqrt(3); // √(1² + 1² + 1²)

            // Act
            var result = _geometryService.CalculateDistance(pt1, pt2);

            // Assert
            Assert.AreEqual(expectedDistance, result, 1e-6);
        }

        [TestMethod]
        public void CalculateAngle_ThreePoints_ReturnsCorrectAngle()
        {
            // Arrange
            var pt1 = new Point3d(1, 0, 0);  // 起点
            var pt2 = new Point3d(0, 0, 0);  // 顶点
            var pt3 = new Point3d(0, 1, 0);  // 终点
            var expectedAngle = Math.PI / 2; // 90度

            // Act
            var result = _geometryService.CalculateAngle(pt1, pt2, pt3);

            // Assert
            Assert.AreEqual(expectedAngle, result, 1e-6);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("CalculateAngle"), Times.Once);
        }

        [TestMethod]
        public void CalculateAngle_StraightLine_ReturnsZero()
        {
            // Arrange
            var pt1 = new Point3d(0, 0, 0);
            var pt2 = new Point3d(1, 0, 0);
            var pt3 = new Point3d(2, 0, 0);

            // Act
            var result = _geometryService.CalculateAngle(pt1, pt2, pt3);

            // Assert
            Assert.AreEqual(0.0, result, 1e-6);
        }

        [TestMethod]
        public void CalculateAngle_OppositeDirection_ReturnsPI()
        {
            // Arrange
            var pt1 = new Point3d(-1, 0, 0);
            var pt2 = new Point3d(0, 0, 0);
            var pt3 = new Point3d(1, 0, 0);

            // Act
            var result = _geometryService.CalculateAngle(pt1, pt2, pt3);

            // Assert
            Assert.AreEqual(Math.PI, result, 1e-6);
        }

        [TestMethod]
        public void IsPointInPolygon_PointInside_ReturnsTrue()
        {
            // Arrange - 创建一个正方形多边形
            var polygon = new List<Point3d>
            {
                new Point3d(0, 0, 0),
                new Point3d(10, 0, 0),
                new Point3d(10, 10, 0),
                new Point3d(0, 10, 0)
            };
            var testPoint = new Point3d(5, 5, 0); // 在正方形内部

            // Act
            var result = _geometryService.IsPointInPolygon(testPoint, polygon);

            // Assert
            Assert.IsTrue(result);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("IsPointInPolygon"), Times.Once);
        }

        [TestMethod]
        public void IsPointInPolygon_PointOutside_ReturnsFalse()
        {
            // Arrange - 创建一个正方形多边形
            var polygon = new List<Point3d>
            {
                new Point3d(0, 0, 0),
                new Point3d(10, 0, 0),
                new Point3d(10, 10, 0),
                new Point3d(0, 10, 0)
            };
            var testPoint = new Point3d(15, 15, 0); // 在正方形外部

            // Act
            var result = _geometryService.IsPointInPolygon(testPoint, polygon);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsPointInPolygon_PointOnEdge_ReturnsTrue()
        {
            // Arrange - 创建一个正方形多边形
            var polygon = new List<Point3d>
            {
                new Point3d(0, 0, 0),
                new Point3d(10, 0, 0),
                new Point3d(10, 10, 0),
                new Point3d(0, 10, 0)
            };
            var testPoint = new Point3d(5, 0, 0); // 在边上

            // Act
            var result = _geometryService.IsPointInPolygon(testPoint, polygon);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsPointInPolygon_TrianglePolygon_WorksCorrectly()
        {
            // Arrange - 创建一个三角形多边形
            var polygon = new List<Point3d>
            {
                new Point3d(0, 0, 0),
                new Point3d(10, 0, 0),
                new Point3d(5, 10, 0)
            };

            var pointInside = new Point3d(5, 3, 0);
            var pointOutside = new Point3d(5, 12, 0);

            // Act
            var resultInside = _geometryService.IsPointInPolygon(pointInside, polygon);
            var resultOutside = _geometryService.IsPointInPolygon(pointOutside, polygon);

            // Assert
            Assert.IsTrue(resultInside);
            Assert.IsFalse(resultOutside);
        }

        [TestMethod]
        public void GetEntityBounds_ValidEntity_ReturnsCorrectBounds()
        {
            // Arrange
            var circle = new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, 10);
            var entityId = AddEntityToModelSpace(circle);

            // Act
            var result = _geometryService.GetEntityBounds(entityId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(-10, result.MinPoint.X, 1e-6);
            Assert.AreEqual(-10, result.MinPoint.Y, 1e-6);
            Assert.AreEqual(10, result.MaxPoint.X, 1e-6);
            Assert.AreEqual(10, result.MaxPoint.Y, 1e-6);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("GetEntityBounds"), Times.Once);
        }

        [TestMethod]
        public void GetEntityBounds_Line_ReturnsCorrectBounds()
        {
            // Arrange
            var line = new Line(new Point3d(0, 0, 0), new Point3d(20, 30, 0));
            var entityId = AddEntityToModelSpace(line);

            // Act
            var result = _geometryService.GetEntityBounds(entityId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.MinPoint.X, 1e-6);
            Assert.AreEqual(0, result.MinPoint.Y, 1e-6);
            Assert.AreEqual(20, result.MaxPoint.X, 1e-6);
            Assert.AreEqual(30, result.MaxPoint.Y, 1e-6);
        }

        [TestMethod]
        public void GetEntityBounds_InvalidEntityId_ThrowsException()
        {
            // Arrange
            var invalidId = ObjectId.Null;

            // Act & Assert
            try
            {
                var result = _geometryService.GetEntityBounds(invalidId);
                Assert.Fail("应该抛出异常");
            }
            catch (Exception)
            {
                // 预期的异常
                Assert.IsTrue(true);
            }
        }

        [TestMethod]
        public void IsPointInPolygon_EmptyPolygon_ReturnsFalse()
        {
            // Arrange
            var polygon = new List<Point3d>();
            var testPoint = new Point3d(5, 5, 0);

            // Act
            var result = _geometryService.IsPointInPolygon(testPoint, polygon);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsPointInPolygon_NullPolygon_ReturnsFalse()
        {
            // Arrange
            List<Point3d> polygon = null;
            var testPoint = new Point3d(5, 5, 0);

            // Act
            var result = _geometryService.IsPointInPolygon(testPoint, polygon);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CalculateDistance_LargeNumbers_ReturnsCorrectResult()
        {
            // Arrange
            var pt1 = new Point3d(1000000, 2000000, 3000000);
            var pt2 = new Point3d(1000003, 2000004, 3000000);
            var expectedDistance = 5.0; // 3-4-5 三角形

            // Act
            var result = _geometryService.CalculateDistance(pt1, pt2);

            // Assert
            Assert.AreEqual(expectedDistance, result, 1e-6);
        }

        [TestMethod]
        public void CalculateDistance_SmallNumbers_ReturnsCorrectResult()
        {
            // Arrange
            var pt1 = new Point3d(0.0001, 0.0002, 0.0003);
            var pt2 = new Point3d(0.0004, 0.0006, 0.0003);
            var expectedDistance = Math.Sqrt(0.0003 * 0.0003 + 0.0004 * 0.0004);

            // Act
            var result = _geometryService.CalculateDistance(pt1, pt2);

            // Assert
            Assert.AreEqual(expectedDistance, result, 1e-10);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _geometryService?.Dispose();
            base.TestCleanup();
        }
    }
}