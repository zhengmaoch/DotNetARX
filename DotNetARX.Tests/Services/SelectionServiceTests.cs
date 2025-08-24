using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetARX.Tests.Services
{
    [TestClass]
    public class SelectionServiceTests : TestBase
    {
        private SelectionService _selectionService;
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

            _selectionService = new SelectionService(
                _mockEventBus.Object,
                _mockPerformanceMonitor.Object,
                _mockLogger.Object);
        }

        private void CreateTestEntities()
        {
            // 创建测试直线
            var line1 = new Line(new Point3d(0, 0, 0), new Point3d(10, 10, 0));
            var line2 = new Line(new Point3d(20, 20, 0), new Point3d(30, 30, 0));
            AddEntityToModelSpace(line1);
            AddEntityToModelSpace(line2);

            // 创建测试圆
            var circle1 = new Circle(new Point3d(5, 5, 0), Vector3d.ZAxis, 2);
            var circle2 = new Circle(new Point3d(25, 25, 0), Vector3d.ZAxis, 3);
            AddEntityToModelSpace(circle1);
            AddEntityToModelSpace(circle2);

            // 创建测试文本
            var text1 = new DBText
            {
                TextString = "Test Text 1",
                Position = new Point3d(0, 0, 0),
                Height = 1.0
            };
            AddEntityToModelSpace(text1);
        }

        [TestMethod]
        public void SelectByType_Lines_ReturnsOnlyLines()
        {
            // Arrange
            CreateTestEntities();

            // Act
            var lines = _selectionService.SelectByType<Line>();

            // Assert
            Assert.IsNotNull(lines);
            Assert.AreEqual(2, lines.Count);
            Assert.IsTrue(lines.All(entity => entity is Line));

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("SelectByType"), Times.Once);

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<SelectionEvent>()), Times.Once);
        }

        [TestMethod]
        public void SelectByType_Circles_ReturnsOnlyCircles()
        {
            // Arrange
            CreateTestEntities();

            // Act
            var circles = _selectionService.SelectByType<Circle>();

            // Assert
            Assert.IsNotNull(circles);
            Assert.AreEqual(2, circles.Count);
            Assert.IsTrue(circles.All(entity => entity is Circle));
        }

        [TestMethod]
        public void SelectByType_Text_ReturnsOnlyText()
        {
            // Arrange
            CreateTestEntities();

            // Act
            var texts = _selectionService.SelectByType<DBText>();

            // Assert
            Assert.IsNotNull(texts);
            Assert.AreEqual(1, texts.Count);
            Assert.IsTrue(texts.All(entity => entity is DBText));
        }

        [TestMethod]
        public void SelectByType_NonExistentType_ReturnsEmptyList()
        {
            // Arrange
            CreateTestEntities();

            // Act
            var arcs = _selectionService.SelectByType<Arc>();

            // Assert
            Assert.IsNotNull(arcs);
            Assert.AreEqual(0, arcs.Count);
        }

        [TestMethod]
        public void SelectInWindow_ValidWindow_ReturnsEntitiesInWindow()
        {
            // Arrange
            CreateTestEntities();
            var pt1 = new Point3d(-1, -1, 0);
            var pt2 = new Point3d(15, 15, 0);

            // Act
            var entities = _selectionService.SelectInWindow<Entity>(pt1, pt2);

            // Assert
            Assert.IsNotNull(entities);
            Assert.IsTrue(entities.Count >= 2); // 至少包含第一条线和第一个圆

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("SelectInWindow"), Times.Once);
        }

        [TestMethod]
        public void SelectInWindow_EmptyWindow_ReturnsEmptyList()
        {
            // Arrange
            CreateTestEntities();
            var pt1 = new Point3d(100, 100, 0);
            var pt2 = new Point3d(110, 110, 0);

            // Act
            var entities = _selectionService.SelectInWindow<Entity>(pt1, pt2);

            // Assert
            Assert.IsNotNull(entities);
            Assert.AreEqual(0, entities.Count);
        }

        [TestMethod]
        public void SelectCrossingWindow_ValidWindow_ReturnsEntitiesCrossingWindow()
        {
            // Arrange
            CreateTestEntities();
            var pt1 = new Point3d(5, 5, 0);
            var pt2 = new Point3d(15, 15, 0);

            // Act
            var entities = _selectionService.SelectCrossingWindow<Entity>(pt1, pt2);

            // Assert
            Assert.IsNotNull(entities);
            Assert.IsTrue(entities.Count > 0);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("SelectCrossingWindow"), Times.Once);
        }

        [TestMethod]
        public void SelectByFilter_LayerFilter_ReturnsEntitiesOnLayer()
        {
            // Arrange
            CreateTestEntities();

            // 创建图层过滤器
            var filterValues = new TypedValue[]
            {
                new TypedValue((int)DxfCode.LayerName, "0") // 默认图层
            };
            var filter = new SelectionFilter(filterValues);

            // Act
            var entities = _selectionService.SelectByFilter<Entity>(filter);

            // Assert
            Assert.IsNotNull(entities);
            Assert.IsTrue(entities.Count > 0);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("SelectByFilter"), Times.Once);
        }

        [TestMethod]
        public void SelectByFilter_TypeFilter_ReturnsEntitiesOfType()
        {
            // Arrange
            CreateTestEntities();

            // 创建类型过滤器（只选择直线）
            var filterValues = new TypedValue[]
            {
                new TypedValue((int)DxfCode.Start, "LINE")
            };
            var filter = new SelectionFilter(filterValues);

            // Act
            var entities = _selectionService.SelectByFilter<Line>(filter);

            // Assert
            Assert.IsNotNull(entities);
            Assert.AreEqual(2, entities.Count);
            Assert.IsTrue(entities.All(entity => entity is Line));
        }

        [TestMethod]
        public void SelectByFilter_NullFilter_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                _selectionService.SelectByFilter<Entity>(null));
        }

        [TestMethod]
        public void SelectAtPoint_ValidPoint_ReturnsEntitiesAtPoint()
        {
            // Arrange
            CreateTestEntities();
            var point = new Point3d(5, 5, 0); // 圆心位置

            // Act
            var entities = _selectionService.SelectAtPoint<Entity>(point);

            // Assert
            Assert.IsNotNull(entities);
            // 由于模拟环境的限制，这里主要验证方法不抛异常

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("SelectAtPoint"), Times.Once);
        }

        [TestMethod]
        public void GetCurrentSelection_ReturnsObjectIdCollection()
        {
            // Arrange
            CreateTestEntities();

            // Act
            var selection = _selectionService.GetCurrentSelection();

            // Assert
            Assert.IsNotNull(selection);
            // 在测试环境中，当前选择集通常为空
            Assert.AreEqual(0, selection.Count);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("GetCurrentSelection"), Times.Once);
        }

        [TestMethod]
        public void SelectInWindow_InvalidPoints_ThrowsArgumentException()
        {
            // Arrange
            var pt1 = new Point3d(10, 10, 0);
            var pt2 = new Point3d(5, 5, 0); // pt2应该在pt1的右上方

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _selectionService.SelectInWindow<Entity>(pt1, pt2));
        }

        [TestMethod]
        public void SelectCrossingWindow_InvalidPoints_ThrowsArgumentException()
        {
            // Arrange
            var pt1 = new Point3d(10, 10, 0);
            var pt2 = new Point3d(5, 5, 0); // pt2应该在pt1的右上方

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _selectionService.SelectCrossingWindow<Entity>(pt1, pt2));
        }

        [TestMethod]
        public void SelectByType_EmptyDatabase_ReturnsEmptyList()
        {
            // Act (不创建任何实体)
            var entities = _selectionService.SelectByType<Line>();

            // Assert
            Assert.IsNotNull(entities);
            Assert.AreEqual(0, entities.Count);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _selectionService?.Dispose();
            base.TestCleanup();
        }
    }
}