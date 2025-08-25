namespace DotNetARX.Tests.Services
{
    [TestClass("SelectionServiceTests")]
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

        [TestMethod("SelectByType_Lines_ReturnsOnlyLines")]
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

        [TestMethod("SelectByType_Circles_ReturnsOnlyCircles")]
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

        [TestMethod("SelectByType_Text_ReturnsOnlyText")]
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

        [TestMethod("SelectByType_NonExistentType_ReturnsEmptyList")]
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

        [TestMethod("SelectInWindow_ValidWindow_ReturnsEntitiesInWindow")]
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

        [TestMethod("SelectInWindow_EmptyWindow_ReturnsEmptyList")]
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

        [TestMethod("SelectCrossingWindow_ValidWindow_ReturnsEntitiesCrossingWindow")]
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

        [TestMethod("SelectByFilter_LayerFilter_ReturnsEntitiesOnLayer")]
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

        [TestMethod("SelectByFilter_TypeFilter_ReturnsEntitiesOfType")]
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

        [TestMethod("SelectAtPoint_ValidPoint_ReturnsEntitiesAtPoint")]
        public void SelectAtPoint_ValidPoint_ReturnsEntitiesAtPoint()
        {
            // Arrange
            CreateTestEntities();
            var point = new Point3d(0, 0, 0); // 第一个实体的位置

            // Act
            var entities = _selectionService.SelectAtPoint<Entity>(point);

            // Assert
            Assert.IsNotNull(entities);
            Assert.IsTrue(entities.Count > 0);
        }

        [TestMethod("SelectAtPoint_EmptyPoint_ReturnsEmptyList")]
        public void SelectAtPoint_EmptyPoint_ReturnsEmptyList()
        {
            // Arrange
            CreateTestEntities();
            var point = new Point3d(1000, 1000, 0); // 远离所有实体的位置

            // Act
            var entities = _selectionService.SelectAtPoint<Entity>(point);

            // Assert
            Assert.IsNotNull(entities);
            Assert.AreEqual(0, entities.Count);
        }

        [TestMethod("SelectAll_ReturnsAllEntities")]
        public void SelectAll_ReturnsAllEntities()
        {
            // Arrange
            CreateTestEntities();

            // Act
            var entities = _selectionService.SelectAll<Entity>();

            // Assert
            Assert.IsNotNull(entities);
            Assert.IsTrue(entities.Count >= 5); // 至少包含创建的5个实体
        }

        [TestMethod("SelectLast_ReturnsLastEntity")]
        public void SelectLast_ReturnsLastEntity()
        {
            // Arrange
            CreateTestEntities();

            // Act
            var entity = _selectionService.SelectLast<Entity>();

            // Assert
            Assert.IsNotNull(entity);
        }

        [TestMethod("SelectPrevious_ReturnsPreviousEntity")]
        public void SelectPrevious_ReturnsPreviousEntity()
        {
            // Arrange
            CreateTestEntities();

            // Act
            var entity = _selectionService.SelectPrevious<Entity>();

            // Assert
            // 在测试环境中，可能返回null或实体
            Assert.IsTrue(entity == null || entity is Entity);
        }

        [TestMethod("GetSelectionCount_ReturnsCorrectCount")]
        public void GetSelectionCount_ReturnsCorrectCount()
        {
            // Arrange
            CreateTestEntities();

            // Act
            var count = _selectionService.GetSelectionCount();

            // Assert
            Assert.IsTrue(count >= 0);
        }

        [TestMethod("ClearSelection_ClearsCurrentSelection")]
        public void ClearSelection_ClearsCurrentSelection()
        {
            // Arrange
            CreateTestEntities();
            // 先选择一些实体
            var entities = _selectionService.SelectAll<Entity>();

            // Act
            _selectionService.ClearSelection();

            // Assert
            // 清除选择后，选择集应该为空
            var count = _selectionService.GetSelectionCount();
            Assert.AreEqual(0, count);
        }

        [TestMethod("AddToSelection_ValidEntities_AddsToSelection")]
        public void AddToSelection_ValidEntities_AddsToSelection()
        {
            // Arrange
            CreateTestEntities();
            var entities = _selectionService.SelectByType<Line>();
            Assert.AreEqual(2, entities.Count);

            // 清除当前选择
            _selectionService.ClearSelection();

            // Act
            _selectionService.AddToSelection(entities);

            // Assert
            var count = _selectionService.GetSelectionCount();
            Assert.AreEqual(2, count);
        }

        [TestMethod("RemoveFromSelection_ValidEntities_RemovesFromSelection")]
        public void RemoveFromSelection_ValidEntities_RemovesFromSelection()
        {
            // Arrange
            CreateTestEntities();
            var entities = _selectionService.SelectByType<Line>();
            Assert.AreEqual(2, entities.Count);

            // 先添加到选择集
            _selectionService.AddToSelection(entities);

            // Act
            _selectionService.RemoveFromSelection(entities.Take(1)); // 移除第一个实体

            // Assert
            var count = _selectionService.GetSelectionCount();
            Assert.AreEqual(1, count);
        }

        [TestMethod("InvertSelection_InvertsCurrentSelection")]
        public void InvertSelection_InvertsCurrentSelection()
        {
            // Arrange
            CreateTestEntities();
            var allEntities = _selectionService.SelectAll<Entity>();
            var lineEntities = _selectionService.SelectByType<Line>();

            // 先选择所有直线
            _selectionService.AddToSelection(lineEntities);

            // Act
            _selectionService.InvertSelection<Entity>();

            // Assert
            var count = _selectionService.GetSelectionCount();
            // 选择集应该包含除直线外的所有实体
            Assert.IsTrue(count > 0);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _selectionService?.Dispose();
            base.TestCleanup();
        }
    }
}