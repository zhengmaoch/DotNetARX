namespace DotNetARX.Tests.Services
{
    [TestClass("GeometryServiceTests")]
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

        [TestMethod("CalculateDistance_TwoPoints_ReturnsCorrectDistance")]
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
            _mockPerformanceMonitor.Verify(x => x.StartOperation("CalculateDistance"), Times.Once());
        }

        [TestMethod("CalculateDistance_SamePoints_ReturnsZero")]
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

        [TestMethod("CalculateDistance_3DPoints_ReturnsCorrectDistance")]
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

        [TestMethod("CalculateAngle_ThreePoints_ReturnsCorrectAngle")]
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
            _mockPerformanceMonitor.Verify(x => x.StartOperation("CalculateAngle"), Times.Once());
        }

        [TestMethod("CalculateAngle_StraightLine_ReturnsZero")]
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

        [TestMethod("CalculateAngle_OppositeDirection_ReturnsPI")]
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

        [TestMethod("IsPointInPolygon_PointInside_ReturnsTrue")]
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
            _mockPerformanceMonitor.Verify(x => x.StartOperation("IsPointInPolygon"), Times.Once());
        }

        [TestMethod("IsPointInPolygon_PointOutside_ReturnsFalse")]
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

        [TestMethod("IsPointInPolygon_PointOnEdge_ReturnsTrue")]
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

        [TestMethod("IsPointInPolygon_TrianglePolygon_WorksCorrectly")]
        public void IsPointInPolygon_TrianglePolygon_WorksCorrectly()
        {
            // Arrange - 创建一个三角形多边形
            var polygon = new List<Point3d>
            {
                new Point3d(0, 0, 0),
                new Point3d(10, 0, 0),
                new Point3d(5, 10, 0)
            };

            // 测试点在三角形内部
            var insidePoint = new Point3d(5, 3, 0);
            // 测试点在三角形外部
            var outsidePoint = new Point3d(15, 15, 0);

            // Act
            var insideResult = _geometryService.IsPointInPolygon(insidePoint, polygon);
            var outsideResult = _geometryService.IsPointInPolygon(outsidePoint, polygon);

            // Assert
            Assert.IsTrue(insideResult);
            Assert.IsFalse(outsideResult);
        }

        [TestMethod("CalculateArea_PolygonPoints_ReturnsCorrectArea")]
        public void CalculateArea_PolygonPoints_ReturnsCorrectArea()
        {
            // Arrange - 创建一个正方形
            var polygon = new List<Point3d>
            {
                new Point3d(0, 0, 0),
                new Point3d(10, 0, 0),
                new Point3d(10, 10, 0),
                new Point3d(0, 10, 0)
            };
            var expectedArea = 100.0; // 10×10正方形

            // Act
            var result = _geometryService.CalculateArea(polygon);

            // Assert
            Assert.AreEqual(expectedArea, result, 1e-6);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("CalculateArea"), Times.Once());
        }

        [TestMethod("CalculateArea_TrianglePoints_ReturnsCorrectArea")]
        public void CalculateArea_TrianglePoints_ReturnsCorrectArea()
        {
            // Arrange - 创建一个三角形
            var polygon = new List<Point3d>
            {
                new Point3d(0, 0, 0),
                new Point3d(10, 0, 0),
                new Point3d(0, 10, 0)
            };
            var expectedArea = 50.0; // 三角形面积 = 1/2 × 10 × 10

            // Act
            var result = _geometryService.CalculateArea(polygon);

            // Assert
            Assert.AreEqual(expectedArea, result, 1e-6);
        }

        [TestMethod("CalculateArea_InvalidPolygon_ReturnsZero")]
        public void CalculateArea_InvalidPolygon_ReturnsZero()
        {
            // Arrange - 创建一个无效的多边形（少于3个点）
            var polygon = new List<Point3d>
            {
                new Point3d(0, 0, 0),
                new Point3d(10, 0, 0)
            };

            // Act
            var result = _geometryService.CalculateArea(polygon);

            // Assert
            Assert.AreEqual(0.0, result, 1e-6);
        }

        [TestMethod("FindIntersection_TwoLines_ReturnsIntersectionPoint")]
        public void FindIntersection_TwoLines_ReturnsIntersectionPoint()
        {
            // Arrange
            var line1Start = new Point3d(0, 0, 0);
            var line1End = new Point3d(10, 10, 0);
            var line2Start = new Point3d(0, 10, 0);
            var line2End = new Point3d(10, 0, 0);
            var expectedIntersection = new Point3d(5, 5, 0);

            // Act
            var result = _geometryService.FindIntersection(line1Start, line1End, line2Start, line2End);

            // Assert
            Assert.IsTrue(result.HasValue);
            Assert.IsTrue(result.Value.IsEqualTo(expectedIntersection, new Tolerance(1e-6, 1e-6)));

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("FindIntersection"), Times.Once());
        }

        [TestMethod("FindIntersection_ParallelLines_ReturnsNull")]
        public void FindIntersection_ParallelLines_ReturnsNull()
        {
            // Arrange
            var line1Start = new Point3d(0, 0, 0);
            var line1End = new Point3d(10, 0, 0);
            var line2Start = new Point3d(0, 5, 0);
            var line2End = new Point3d(10, 5, 0);

            // Act
            var result = _geometryService.FindIntersection(line1Start, line1End, line2Start, line2End);

            // Assert
            Assert.IsFalse(result.HasValue);
        }

        [TestMethod("FindIntersection_CoincidentLines_ReturnsNull")]
        public void FindIntersection_CoincidentLines_ReturnsNull()
        {
            // Arrange
            var line1Start = new Point3d(0, 0, 0);
            var line1End = new Point3d(10, 10, 0);
            var line2Start = new Point3d(5, 5, 0);
            var line2End = new Point3d(15, 15, 0);

            // Act
            var result = _geometryService.FindIntersection(line1Start, line1End, line2Start, line2End);

            // Assert
            Assert.IsFalse(result.HasValue);
        }

        [TestMethod("OffsetPoint_PointAndDistance_ReturnsOffsetPoint")]
        public void OffsetPoint_PointAndDistance_ReturnsOffsetPoint()
        {
            // Arrange
            var originalPoint = new Point3d(0, 0, 0);
            var direction = new Vector3d(1, 0, 0);
            var distance = 10.0;
            var expectedPoint = new Point3d(10, 0, 0);

            // Act
            var result = _geometryService.OffsetPoint(originalPoint, direction, distance);

            // Assert
            Assert.IsTrue(result.IsEqualTo(expectedPoint, new Tolerance(1e-6, 1e-6)));

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("OffsetPoint"), Times.Once());
        }

        [TestMethod("OffsetPoint_ZeroDistance_ReturnsOriginalPoint")]
        public void OffsetPoint_ZeroDistance_ReturnsOriginalPoint()
        {
            // Arrange
            var originalPoint = new Point3d(5, 10, 15);
            var direction = new Vector3d(1, 1, 1);
            var distance = 0.0;

            // Act
            var result = _geometryService.OffsetPoint(originalPoint, direction, distance);

            // Assert
            Assert.IsTrue(result.IsEqualTo(originalPoint, new Tolerance(1e-10, 1e-10)));
        }

        [TestMethod("RotatePoint_PointAndAngle_ReturnsRotatedPoint")]
        public void RotatePoint_PointAndAngle_ReturnsRotatedPoint()
        {
            // Arrange
            var pointToRotate = new Point3d(1, 0, 0);
            var centerPoint = new Point3d(0, 0, 0);
            var angle = Math.PI / 2; // 90度
            var expectedPoint = new Point3d(0, 1, 0); // 旋转90度后的位置

            // Act
            var result = _geometryService.RotatePoint(pointToRotate, centerPoint, angle);

            // Assert
            Assert.IsTrue(result.IsEqualTo(expectedPoint, new Tolerance(1e-6, 1e-6)));

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("RotatePoint"), Times.Once());
        }

        [TestMethod("RotatePoint_ZeroAngle_ReturnsOriginalPoint")]
        public void RotatePoint_ZeroAngle_ReturnsOriginalPoint()
        {
            // Arrange
            var pointToRotate = new Point3d(5, 10, 15);
            var centerPoint = new Point3d(1, 1, 1);
            var angle = 0.0;

            // Act
            var result = _geometryService.RotatePoint(pointToRotate, centerPoint, angle);

            // Assert
            Assert.IsTrue(result.IsEqualTo(pointToRotate, new Tolerance(1e-10, 1e-10)));
        }

        [TestMethod("ScalePoint_PointAndFactor_ReturnsScaledPoint")]
        public void ScalePoint_PointAndFactor_ReturnsScaledPoint()
        {
            // Arrange
            var pointToScale = new Point3d(2, 3, 4);
            var basePoint = new Point3d(0, 0, 0);
            var scaleFactor = 2.0;
            var expectedPoint = new Point3d(4, 6, 8);

            // Act
            var result = _geometryService.ScalePoint(pointToScale, basePoint, scaleFactor);

            // Assert
            Assert.IsTrue(result.IsEqualTo(expectedPoint, new Tolerance(1e-6, 1e-6)));

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("ScalePoint"), Times.Once());
        }

        [TestMethod("ScalePoint_UnitFactor_ReturnsOriginalPoint")]
        public void ScalePoint_UnitFactor_ReturnsOriginalPoint()
        {
            // Arrange
            var pointToScale = new Point3d(5, 10, 15);
            var basePoint = new Point3d(1, 1, 1);
            var scaleFactor = 1.0;

            // Act
            var result = _geometryService.ScalePoint(pointToScale, basePoint, scaleFactor);

            // Assert
            Assert.IsTrue(result.IsEqualTo(pointToScale, new Tolerance(1e-10, 1e-10)));
        }

        [TestMethod("GetBoundingBox_Points_ReturnsExtents3d")]
        public void GetBoundingBox_Points_ReturnsExtents3d()
        {
            // Arrange
            var points = new List<Point3d>
            {
                new Point3d(0, 0, 0),
                new Point3d(10, 5, 2),
                new Point3d(-5, 15, -3),
                new Point3d(3, -2, 1)
            };
            var expectedMin = new Point3d(-5, -2, -3);
            var expectedMax = new Point3d(10, 15, 2);

            // Act
            var result = _geometryService.GetBoundingBox(points);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Value.MinPoint.IsEqualTo(expectedMin, new Tolerance(1e-6, 1e-6)));
            Assert.IsTrue(result.Value.MaxPoint.IsEqualTo(expectedMax, new Tolerance(1e-6, 1e-6)));

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("GetBoundingBox"), Times.Once());
        }

        [TestMethod("GetBoundingBox_EmptyPoints_ReturnsNull")]
        public void GetBoundingBox_EmptyPoints_ReturnsNull()
        {
            // Arrange
            var points = new List<Point3d>();

            // Act
            var result = _geometryService.GetBoundingBox(points);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod("GetBoundingBox_SinglePoint_ReturnsPointExtents")]
        public void GetBoundingBox_SinglePoint_ReturnsPointExtents()
        {
            // Arrange
            var points = new List<Point3d> { new Point3d(5, 10, 15) };

            // Act
            var result = _geometryService.GetBoundingBox(points);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Value.MinPoint.IsEqualTo(points[0], new Tolerance(1e-10, 1e-10)));
            Assert.IsTrue(result.Value.MaxPoint.IsEqualTo(points[0], new Tolerance(1e-10, 1e-10)));
        }

        [TestCleanup]
        public void Cleanup()
        {
            _geometryService?.Dispose();
            base.TestCleanup();
        }
    }
}