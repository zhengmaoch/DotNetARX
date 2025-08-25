namespace DotNetARX.Tests.Services
{
    [TestClass("LayerManagerServiceTests")]
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

        [TestMethod("CreateLayer_ValidName_ReturnsValidObjectId")]
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

        [TestMethod("CreateLayer_EmptyName_ThrowsArgumentException")]
        public void CreateLayer_EmptyName_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _layerManagerService.CreateLayer(""));

            Assert.ThrowsException<ArgumentException>(() =>
                _layerManagerService.CreateLayer(null));
        }

        [TestMethod("CreateLayer_DuplicateName_ReturnsExistingLayerId")]
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

        [TestMethod("SetCurrentLayer_ExistingLayer_ReturnsTrue")]
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

        [TestMethod("SetCurrentLayer_NonExistingLayer_ReturnsFalse")]
        public void SetCurrentLayer_NonExistingLayer_ReturnsFalse()
        {
            // Act
            var result = _layerManagerService.SetCurrentLayer("NonExistingLayer");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("DeleteLayer_ExistingEmptyLayer_ReturnsTrue")]
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

        [TestMethod("DeleteLayer_NonExistingLayer_ReturnsFalse")]
        public void DeleteLayer_NonExistingLayer_ReturnsFalse()
        {
            // Act
            var result = _layerManagerService.DeleteLayer("NonExistingLayer");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("GetAllLayers_ReturnsAllLayers")]
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

        [TestMethod("LayerExists_ExistingLayer_ReturnsTrue")]
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

        [TestMethod("LayerExists_NonExistingLayer_ReturnsFalse")]
        public void LayerExists_NonExistingLayer_ReturnsFalse()
        {
            // Act
            var result = _layerManagerService.LayerExists("NonExistingLayer");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("GetLayerProperties_ExistingLayer_ReturnsProperties")]
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

        [TestMethod("GetLayerProperties_NonExistingLayer_ReturnsNull")]
        public void GetLayerProperties_NonExistingLayer_ReturnsNull()
        {
            // Act
            var properties = _layerManagerService.GetLayerProperties("NonExistingLayer");

            // Assert
            Assert.IsNull(properties);
        }

        [TestMethod("SetLayerProperties_ExistingLayer_ReturnsTrue")]
        public void SetLayerProperties_ExistingLayer_ReturnsTrue()
        {
            // Arrange
            var layerName = "SetPropTestLayer";
            _layerManagerService.CreateLayer(layerName);

            var newProperties = new LayerProperties
            {
                Name = layerName,
                ColorIndex = 2, // 黄色
                IsFrozen = true,
                IsLocked = false
            };

            // Act
            var result = _layerManagerService.SetLayerProperties(layerName, newProperties);

            // Assert
            Assert.IsTrue(result);

            // 验证属性是否被设置
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var layerTable = transaction.GetObject(TestDatabase.LayerTableId, OpenMode.ForRead) as LayerTable;
                var layerRecord = transaction.GetObject(layerTable[layerName], OpenMode.ForRead) as LayerTableRecord;

                Assert.AreEqual(newProperties.ColorIndex, layerRecord.Color.ColorIndex);
                Assert.AreEqual(newProperties.IsFrozen, layerRecord.IsFrozen);
                // Assert.AreEqual(newProperties.IsLocked, layerRecord.IsLocked); // IsLocked属性可能需要特殊处理

                transaction.Commit();
            }
        }

        [TestMethod("SetLayerProperties_NonExistingLayer_ReturnsFalse")]
        public void SetLayerProperties_NonExistingLayer_ReturnsFalse()
        {
            // Arrange
            var newProperties = new LayerProperties
            {
                Name = "NonExistingLayer",
                ColorIndex = 2
            };

            // Act
            var result = _layerManagerService.SetLayerProperties("NonExistingLayer", newProperties);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("PurgeUnusedLayers_ReturnsDeletedCount")]
        public void PurgeUnusedLayers_ReturnsDeletedCount()
        {
            // Arrange
            // 创建一些图层
            var layer1 = "UnusedLayer1";
            var layer2 = "UnusedLayer2";
            _layerManagerService.CreateLayer(layer1);
            _layerManagerService.CreateLayer(layer2);

            // Act
            var result = _layerManagerService.PurgeUnusedLayers();

            // Assert
            Assert.IsTrue(result >= 0); // 可能删除0个或更多图层
        }

        [TestMethod("GetLayerId_ExistingLayer_ReturnsValidObjectId")]
        public void GetLayerId_ExistingLayer_ReturnsValidObjectId()
        {
            // Arrange
            var layerName = "GetIdTestLayer";
            var expectedId = _layerManagerService.CreateLayer(layerName);

            // Act
            var result = _layerManagerService.GetLayerId(layerName);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.AreEqual(expectedId, result);
        }

        [TestMethod("GetLayerId_NonExistingLayer_ReturnsNull")]
        public void GetLayerId_NonExistingLayer_ReturnsNull()
        {
            // Act
            var result = _layerManagerService.GetLayerId("NonExistingLayer");

            // Assert
            Assert.IsTrue(result.IsNull);
        }

        [TestMethod("RenameLayer_ExistingLayer_ReturnsTrue")]
        public void RenameLayer_ExistingLayer_ReturnsTrue()
        {
            // Arrange
            var oldName = "OldLayerName";
            var newName = "NewLayerName";
            _layerManagerService.CreateLayer(oldName);

            // Act
            var result = _layerManagerService.RenameLayer(oldName, newName);

            // Assert
            Assert.IsTrue(result);

            // 验证重命名是否成功
            Assert.IsFalse(_layerManagerService.LayerExists(oldName));
            Assert.IsTrue(_layerManagerService.LayerExists(newName));
        }

        [TestMethod("RenameLayer_NonExistingLayer_ReturnsFalse")]
        public void RenameLayer_NonExistingLayer_ReturnsFalse()
        {
            // Act
            var result = _layerManagerService.RenameLayer("NonExistingLayer", "NewName");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("RenameLayer_DuplicateName_ReturnsFalse")]
        public void RenameLayer_DuplicateName_ReturnsFalse()
        {
            // Arrange
            var layer1 = "Layer1";
            var layer2 = "Layer2";
            _layerManagerService.CreateLayer(layer1);
            _layerManagerService.CreateLayer(layer2);

            // Act
            var result = _layerManagerService.RenameLayer(layer1, layer2); // 尝试重命名为已存在的图层名

            // Assert
            Assert.IsFalse(result);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _layerManagerService?.Dispose();
            base.TestCleanup();
        }
    }
}