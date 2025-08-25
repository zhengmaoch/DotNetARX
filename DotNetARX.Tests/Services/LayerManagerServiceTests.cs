namespace DotNetARX.Tests.Services
{
    [TestClass]
    public class LayerManagerServiceTests : TestBase
    {
        private LayerManagerService _layerManagerService;
        private Mock<ILogger> _mockLogger;
        private Mock<IPerformanceMonitor> _mockPerformanceMonitor;

        [TestInitialize]
        public void Setup()
        {
            base.TestInitialize();

            _mockLogger = new Mock<ILogger>();
            _mockPerformanceMonitor = new Mock<IPerformanceMonitor>();

            _layerManagerService = new LayerManagerService(
                _mockLogger.Object,
                _mockPerformanceMonitor.Object);
        }

        [TestMethod]
        public void CreateLayer_ValidName_ReturnsValidObjectId()
        {
            // Arrange
            var layerName = "TestLayer";
            short colorIndex = 1; // 红色

            // Act
            var result = _layerManagerService.CreateLayer(layerName, colorIndex);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);

            // 验证图层是否真的被创建
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var layerTable = transaction.GetObject(TestDatabase.LayerTableId, OpenMode.ForRead) as LayerTable;
                Assert.IsTrue(layerTable.Has(layerName));

                var layerRecord = transaction.GetObject(layerTable[layerName], OpenMode.ForRead) as LayerTableRecord;
                Assert.AreEqual(layerName, layerRecord.Name);
                Assert.AreEqual(colorIndex, layerRecord.Color.ColorIndex);

                transaction.Commit();
            }
        }

        [TestMethod]
        public void CreateLayer_EmptyName_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _layerManagerService.CreateLayer(""));

            Assert.ThrowsException<ArgumentException>(() =>
                _layerManagerService.CreateLayer(null));
        }

        [TestMethod]
        public void CreateLayer_DuplicateName_ReturnsExistingLayerId()
        {
            // Arrange
            var layerName = "DuplicateLayer";
            var firstId = _layerManagerService.CreateLayer(layerName);

            // Act
            var secondId = _layerManagerService.CreateLayer(layerName);

            // Assert
            Assert.AreEqual(firstId, secondId);
        }

        [TestMethod]
        public void SetCurrentLayer_ExistingLayer_ReturnsTrue()
        {
            // Arrange
            var layerName = "CurrentTestLayer";
            _layerManagerService.CreateLayer(layerName);

            // Act
            var result = _layerManagerService.SetCurrentLayer(layerName);

            // Assert
            Assert.IsTrue(result);

            // 验证当前图层设置
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var currentLayerId = TestDatabase.Clayer;
                var currentLayer = transaction.GetObject(currentLayerId, OpenMode.ForRead) as LayerTableRecord;
                Assert.AreEqual(layerName, currentLayer.Name);

                transaction.Commit();
            }
        }

        [TestMethod]
        public void SetCurrentLayer_NonExistingLayer_ReturnsFalse()
        {
            // Act
            var result = _layerManagerService.SetCurrentLayer("NonExistingLayer");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void DeleteLayer_ExistingEmptyLayer_ReturnsTrue()
        {
            // Arrange
            var layerName = "DeleteTestLayer";
            _layerManagerService.CreateLayer(layerName);

            // Act
            var result = _layerManagerService.DeleteLayer(layerName);

            // Assert
            Assert.IsTrue(result);

            // 验证图层是否被删除
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var layerTable = transaction.GetObject(TestDatabase.LayerTableId, OpenMode.ForRead) as LayerTable;
                Assert.IsFalse(layerTable.Has(layerName));

                transaction.Commit();
            }
        }

        [TestMethod]
        public void DeleteLayer_NonExistingLayer_ReturnsFalse()
        {
            // Act
            var result = _layerManagerService.DeleteLayer("NonExistingLayer");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GetAllLayers_ReturnsAllLayers()
        {
            // Arrange
            var testLayers = new[] { "Layer1", "Layer2", "Layer3" };
            foreach (var layerName in testLayers)
            {
                _layerManagerService.CreateLayer(layerName);
            }

            // Act
            var layers = _layerManagerService.GetAllLayers().ToList();

            // Assert
            Assert.IsTrue(layers.Count >= testLayers.Length);
            Assert.IsTrue(testLayers.All(name => layers.Any(layer => layer.Name == name)));
        }

        [TestMethod]
        public void LayerExists_ExistingLayer_ReturnsTrue()
        {
            // Arrange
            var layerName = "ExistTestLayer";
            _layerManagerService.CreateLayer(layerName);

            // Act
            var result = _layerManagerService.LayerExists(layerName);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void LayerExists_NonExistingLayer_ReturnsFalse()
        {
            // Act
            var result = _layerManagerService.LayerExists("NonExistingLayer");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GetLayerProperties_ExistingLayer_ReturnsProperties()
        {
            // Arrange
            var layerName = "PropTestLayer";
            short colorIndex = 3; // 绿色
            _layerManagerService.CreateLayer(layerName, colorIndex);

            // Act
            var properties = _layerManagerService.GetLayerProperties(layerName);

            // Assert
            Assert.IsNotNull(properties);
            Assert.AreEqual(layerName, properties.Name);
            Assert.AreEqual(colorIndex, properties.ColorIndex);
        }

        [TestMethod]
        public void SetLayerProperties_ExistingLayer_ReturnsTrue()
        {
            // Arrange
            var layerName = "SetPropTestLayer";
            _layerManagerService.CreateLayer(layerName);

            // Act
            var result = _layerManagerService.SetLayerProperties(layerName, colorIndex: 2, isLocked: true);

            // Assert
            Assert.IsTrue(result);

            // 验证属性是否被设置
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var layerTable = transaction.GetObject(TestDatabase.LayerTableId, OpenMode.ForRead) as LayerTable;
                var layerRecord = transaction.GetObject(layerTable[layerName], OpenMode.ForRead) as LayerTableRecord;
                Assert.AreEqual(2, layerRecord.Color.ColorIndex);
                Assert.IsTrue(layerRecord.IsLocked);

                transaction.Commit();
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            _layerManagerService?.Dispose();
            base.TestCleanup();
        }
    }
}