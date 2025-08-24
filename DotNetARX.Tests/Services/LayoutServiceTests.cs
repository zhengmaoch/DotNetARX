using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetARX.Tests.Services
{
    [TestClass]
    public class LayoutServiceTests : TestBase
    {
        private LayoutService _layoutService;
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

            _layoutService = new LayoutService(
                _mockEventBus.Object,
                _mockPerformanceMonitor.Object,
                _mockLogger.Object);
        }

        [TestMethod]
        public void CreateLayout_ValidName_ReturnsValidObjectId()
        {
            // Arrange
            var layoutName = "TestLayout";

            // Act
            var result = _layoutService.CreateLayout(layoutName);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);

            // 验证布局是否创建成功
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var layoutDict = transaction.GetObject(TestDatabase.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;
                Assert.IsTrue(layoutDict.Contains(layoutName));

                var layoutId = layoutDict.GetAt(layoutName);
                var layout = transaction.GetObject(layoutId, OpenMode.ForRead) as Layout;
                Assert.IsNotNull(layout);
                Assert.AreEqual(layoutName, layout.LayoutName);

                transaction.Commit();
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("CreateLayout"), Times.Once);

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<LayoutEvent>()), Times.Once);
        }

        [TestMethod]
        public void CreateLayout_DuplicateName_ReturnsExistingLayout()
        {
            // Arrange
            var layoutName = "DuplicateTestLayout";

            // Act - 创建第一个布局
            var result1 = _layoutService.CreateLayout(layoutName);

            // Act - 尝试创建同名布局
            var result2 = _layoutService.CreateLayout(layoutName);

            // Assert
            Assert.IsFalse(result1.IsNull);
            Assert.IsFalse(result2.IsNull);
            Assert.AreEqual(result1, result2); // 应该返回相同的ObjectId
        }

        [TestMethod]
        public void CreateLayout_NullName_ReturnsFalse()
        {
            // Arrange
            string layoutName = null;

            // Act
            var result = _layoutService.CreateLayout(layoutName);

            // Assert
            Assert.IsTrue(result.IsNull);
        }

        [TestMethod]
        public void CreateLayout_EmptyName_ReturnsFalse()
        {
            // Arrange
            var layoutName = string.Empty;

            // Act
            var result = _layoutService.CreateLayout(layoutName);

            // Assert
            Assert.IsTrue(result.IsNull);
        }

        [TestMethod]
        public void CreateLayout_InvalidCharacters_HandledGracefully()
        {
            // Arrange
            var layoutName = "Test<>Layout"; // 包含无效字符

            // Act
            var result = _layoutService.CreateLayout(layoutName);

            // Assert
            // 可能失败或成功，取决于AutoCAD的处理方式
            // 但不应抛出异常
        }

        [TestMethod]
        public void DeleteLayout_ExistingLayout_ReturnsTrue()
        {
            // Arrange
            var layoutName = "LayoutToDelete";
            var layoutId = _layoutService.CreateLayout(layoutName);
            Assert.IsFalse(layoutId.IsNull); // 确保布局创建成功

            // Act
            var result = _layoutService.DeleteLayout(layoutName);

            // Assert
            Assert.IsTrue(result);

            // 验证布局是否被删除
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var layoutDict = transaction.GetObject(TestDatabase.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;
                Assert.IsFalse(layoutDict.Contains(layoutName));

                transaction.Commit();
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DeleteLayout"), Times.Once);
        }

        [TestMethod]
        public void DeleteLayout_NonExistentLayout_ReturnsFalse()
        {
            // Arrange
            var layoutName = "NonExistentLayout";

            // Act
            var result = _layoutService.DeleteLayout(layoutName);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void DeleteLayout_ModelTab_ReturnsFalse()
        {
            // Arrange
            var layoutName = "Model"; // 模型空间不能删除

            // Act
            var result = _layoutService.DeleteLayout(layoutName);

            // Assert
            Assert.IsFalse(result); // 模型空间不应该被删除
        }

        [TestMethod]
        public void CreateViewport_ValidParameters_ReturnsValidObjectId()
        {
            // Arrange
            var center = new Point3d(100, 100, 0);
            var width = 200.0;
            var height = 150.0;

            // Act
            var result = _layoutService.CreateViewport(center, width, height);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);

            // 验证视口属性
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var viewport = transaction.GetObject(result, OpenMode.ForRead) as Viewport;
                Assert.IsNotNull(viewport);
                Assert.AreEqual(center, viewport.CenterPoint);
                Assert.AreEqual(width, viewport.Width, 1e-6);
                Assert.AreEqual(height, viewport.Height, 1e-6);

                transaction.Commit();
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("CreateViewport"), Times.Once);
        }

        [TestMethod]
        public void CreateViewport_MinimumSize_WorksCorrectly()
        {
            // Arrange
            var center = new Point3d(50, 50, 0);
            var width = 10.0;
            var height = 10.0;

            // Act
            var result = _layoutService.CreateViewport(center, width, height);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void CreateViewport_LargeSize_WorksCorrectly()
        {
            // Arrange
            var center = new Point3d(500, 500, 0);
            var width = 1000.0;
            var height = 800.0;

            // Act
            var result = _layoutService.CreateViewport(center, width, height);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void CreateViewport_ZeroSize_ReturnsFalse()
        {
            // Arrange
            var center = new Point3d(100, 100, 0);
            var width = 0.0;
            var height = 100.0;

            // Act
            var result = _layoutService.CreateViewport(center, width, height);

            // Assert
            Assert.IsTrue(result.IsNull);
        }

        [TestMethod]
        public void CreateViewport_NegativeSize_ReturnsFalse()
        {
            // Arrange
            var center = new Point3d(100, 100, 0);
            var width = 100.0;
            var height = -50.0;

            // Act
            var result = _layoutService.CreateViewport(center, width, height);

            // Assert
            Assert.IsTrue(result.IsNull);
        }

        [TestMethod]
        public void SetViewportScale_ValidViewport_ReturnsTrue()
        {
            // Arrange
            var viewportId = CreateTestViewport();
            var scale = 0.5; // 1:2 比例

            // Act
            var result = _layoutService.SetViewportScale(viewportId, scale);

            // Assert
            Assert.IsTrue(result);

            // 验证比例设置
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var viewport = transaction.GetObject(viewportId, OpenMode.ForRead) as Viewport;
                Assert.AreEqual(scale, viewport.CustomScale, 1e-6);

                transaction.Commit();
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("SetViewportScale"), Times.Once);
        }

        [TestMethod]
        public void SetViewportScale_DifferentScales_AllWork()
        {
            // Arrange
            var scales = new[] { 0.25, 0.5, 1.0, 2.0, 4.0 };

            foreach (var scale in scales)
            {
                var viewportId = CreateTestViewport();

                // Act
                var result = _layoutService.SetViewportScale(viewportId, scale);

                // Assert
                Assert.IsTrue(result, $"Scale {scale} should work");

                // 验证比例设置
                using (var transaction = TestDatabase.TransactionManager.StartTransaction())
                {
                    var viewport = transaction.GetObject(viewportId, OpenMode.ForRead) as Viewport;
                    Assert.AreEqual(scale, viewport.CustomScale, 1e-6);

                    transaction.Commit();
                }
            }
        }

        [TestMethod]
        public void SetViewportScale_InvalidViewportId_ReturnsFalse()
        {
            // Arrange
            var invalidViewportId = ObjectId.Null;
            var scale = 1.0;

            // Act
            var result = _layoutService.SetViewportScale(invalidViewportId, scale);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SetViewportScale_ZeroScale_ReturnsFalse()
        {
            // Arrange
            var viewportId = CreateTestViewport();
            var scale = 0.0;

            // Act
            var result = _layoutService.SetViewportScale(viewportId, scale);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SetViewportScale_NegativeScale_ReturnsFalse()
        {
            // Arrange
            var viewportId = CreateTestViewport();
            var scale = -1.0;

            // Act
            var result = _layoutService.SetViewportScale(viewportId, scale);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CreateMultipleLayouts_AllSucceed()
        {
            // Arrange
            var layoutNames = new[] { "Layout1", "Layout2", "Layout3", "Layout4" };
            var layoutIds = new ObjectId[layoutNames.Length];

            // Act
            for (int i = 0; i < layoutNames.Length; i++)
            {
                layoutIds[i] = _layoutService.CreateLayout(layoutNames[i]);
            }

            // Assert
            for (int i = 0; i < layoutNames.Length; i++)
            {
                Assert.IsFalse(layoutIds[i].IsNull, $"Layout {layoutNames[i]} creation failed");
                Assert.IsTrue(layoutIds[i].IsValid, $"Layout {layoutNames[i]} is not valid");
            }

            // 验证所有布局都存在
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var layoutDict = transaction.GetObject(TestDatabase.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;

                foreach (var layoutName in layoutNames)
                {
                    Assert.IsTrue(layoutDict.Contains(layoutName), $"Layout {layoutName} not found in dictionary");
                }

                transaction.Commit();
            }
        }

        [TestMethod]
        public void CreateViewportInLayout_WorksCorrectly()
        {
            // Arrange
            var layoutName = "ViewportTestLayout";
            var layoutId = _layoutService.CreateLayout(layoutName);
            Assert.IsFalse(layoutId.IsNull);

            var center = new Point3d(150, 150, 0);
            var width = 250.0;
            var height = 200.0;

            // Act
            var viewportId = _layoutService.CreateViewport(center, width, height);

            // Assert
            Assert.IsFalse(viewportId.IsNull);
            Assert.IsTrue(viewportId.IsValid);
        }

        private ObjectId CreateTestViewport()
        {
            var center = new Point3d(100, 100, 0);
            var width = 200.0;
            var height = 150.0;

            return _layoutService.CreateViewport(center, width, height);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _layoutService?.Dispose();
            base.TestCleanup();
        }
    }
}