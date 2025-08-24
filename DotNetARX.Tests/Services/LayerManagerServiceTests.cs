using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetARX.Tests.Services
{
    [TestClass]
    public class LayerManagerServiceTests : TestBase
    {
        private LayerManagerService _layerManagerService;
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

            _layerManagerService = new LayerManagerService(
                _mockEventBus.Object,
                _mockPerformanceMonitor.Object,
                _mockLogger.Object);
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

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("CreateLayer"), Times.Once);

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<LayerEvent>()), Times.Once);

            // 验证日志记录
            _mockLogger.Verify(x => x.Info(It.IsAny<string>()), Times.Once);
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
        public void CreateLayer_DuplicateName_ThrowsLayerOperationException()
        {
            // Arrange
            var layerName = "DuplicateLayer";
            _layerManagerService.CreateLayer(layerName);

            // Act & Assert
            Assert.ThrowsException<LayerOperationException>(() =>
                _layerManagerService.CreateLayer(layerName));
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

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("SetCurrentLayer"), Times.Once);
        }

        [TestMethod]
        public void SetCurrentLayer_NonExistingLayer_ReturnsFalse()
        {
            // Act
            var result = _layerManagerService.SetCurrentLayer("NonExistingLayer");

            // Assert
            Assert.IsFalse(result);

            // 验证警告日志
            _mockLogger.Verify(x => x.Warning(It.IsAny<string>()), Times.Once);
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

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<LayerEvent>()), Times.AtLeast(1));
        }

        [TestMethod]
        public void DeleteLayer_CurrentLayer_ReturnsFalse()
        {
            // Arrange
            var layerName = "CurrentLayerToDelete";
            _layerManagerService.CreateLayer(layerName);
            _layerManagerService.SetCurrentLayer(layerName);

            // Act
            var result = _layerManagerService.DeleteLayer(layerName);

            // Assert
            Assert.IsFalse(result);

            // 验证警告日志
            _mockLogger.Verify(x => x.Warning(It.IsAny<string>()), Times.Once);
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

            foreach (var layerName in testLayers)
            {
                Assert.IsTrue(layers.Any(l => l.Name == layerName));
            }
        }

        [TestMethod]
        public void GetLayerNames_ReturnsLayerNames()
        {
            // Arrange
            var testLayers = new[] { "NameLayer1", "NameLayer2", "NameLayer3" };
            foreach (var layerName in testLayers)
            {
                _layerManagerService.CreateLayer(layerName);
            }

            // Act
            var layerNames = _layerManagerService.GetLayerNames().ToList();

            // Assert
            Assert.IsTrue(layerNames.Count >= testLayers.Length);

            foreach (var layerName in testLayers)
            {
                Assert.IsTrue(layerNames.Contains(layerName));
            }
        }

        [TestMethod]
        public void LayerExists_ExistingLayer_ReturnsTrue()
        {
            // Arrange
            var layerName = "ExistingLayer";
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
        public void SetLayerProperties_ExistingLayer_ReturnsTrue()
        {
            // Arrange
            var layerName = "PropertiesLayer";
            _layerManagerService.CreateLayer(layerName);
            short newColorIndex = 3; // 绿色

            // Act
            var result = _layerManagerService.SetLayerProperties(layerName, newColorIndex, true, false);

            // Assert
            Assert.IsTrue(result);

            // 验证属性设置
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var layerTable = transaction.GetObject(TestDatabase.LayerTableId, OpenMode.ForRead) as LayerTable;
                var layerRecord = transaction.GetObject(layerTable[layerName], OpenMode.ForRead) as LayerTableRecord;

                Assert.AreEqual(newColorIndex, layerRecord.Color.ColorIndex);
                Assert.IsTrue(layerRecord.IsLocked);
                Assert.IsFalse(layerRecord.IsFrozen);

                transaction.Commit();
            }
        }

        [TestMethod]
        public void SetLayerProperties_NonExistingLayer_ReturnsFalse()
        {
            // Act
            var result = _layerManagerService.SetLayerProperties("NonExistingLayer", 1);

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