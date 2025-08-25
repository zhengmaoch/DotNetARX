namespace DotNetARX.Tests.Services
{
    [TestClass]
    public class SelectionServiceTests : TestBase
    {
        private SelectionService _selectionService;
        private Mock<ILogger> _mockLogger;
        private Mock<IPerformanceMonitor> _mockPerformanceMonitor;

        [TestInitialize]
        public void Setup()
        {
            base.TestInitialize();

            _mockLogger = new Mock<ILogger>();
            _mockPerformanceMonitor = new Mock<IPerformanceMonitor>();

            _selectionService = new SelectionService(
                _mockLogger.Object,
                _mockPerformanceMonitor.Object);
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
        }

        [TestMethod]
        public void SelectByFilter_TypeFilter_ReturnsEntitiesOfType()
        {
            // Arrange
            CreateTestEntities();

            // 创建类型过滤器
            var filterValues = new TypedValue[]
            {
                new TypedValue((int)DxfCode.Start, "LINE")
            };
            var filter = new SelectionFilter(filterValues);

            // Act
            var entities = _selectionService.SelectByFilter<Entity>(filter);

            // Assert
            Assert.IsNotNull(entities);
            Assert.IsTrue(entities.All(entity => entity is Line));
        }

        [TestMethod]
        public void SelectAtPoint_ValidPoint_ReturnsEntitiesAtPoint()
        {
            // Arrange
            CreateTestEntities();
            var point = new Point3d(5, 5, 0);

            // Act
            var entities = _selectionService.SelectAtPoint<Entity>(point);

            // Assert
            Assert.IsNotNull(entities);
            Assert.IsTrue(entities.Count > 0);
        }

        [TestMethod]
        public void GetCurrentSelection_NoSelection_ReturnsEmptyCollection()
        {
            // Arrange
            // 不创建任何选择

            // Act
            var selection = _selectionService.GetCurrentSelection();

            // Assert
            Assert.IsNotNull(selection);
            Assert.AreEqual(0, selection.Count);
        }

        [TestMethod]
        public void SelectLast_SelectedEntities_ReturnsLastSelectedEntities()
        {
            // Arrange
            CreateTestEntities();

            // Act
            var entities = _selectionService.SelectLast<Entity>();

            // Assert
            Assert.IsNotNull(entities);
        }

        [TestMethod]
        public void SelectPrevious_SelectedEntities_ReturnsPreviousSelectedEntities()
        {
            // Arrange
            CreateTestEntities();

            // Act
            var entities = _selectionService.SelectPrevious<Entity>();

            // Assert
            Assert.IsNotNull(entities);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _selectionService?.Dispose();
            base.TestCleanup();
        }
    }
}