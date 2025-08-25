using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Moq;
using NUnit.Framework;

namespace DotNetARX.Tests.Services
{
    [TestClass("LayoutServiceTests")]
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

        [TestMethod("CreateLayout_ValidName_ReturnsValidObjectId")]
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
                Assert.AreEqual<string>(layoutName, layout.LayoutName);

                transaction.Commit();
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("CreateLayout"), Times.Once());

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<LayoutEvent>()), Times.Once());
        }

        [TestMethod("CreateLayout_DuplicateName_ReturnsExistingLayout")]
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
            Assert.AreEqual<ObjectId>(result1, result2); // 应该返回相同的ObjectId
        }

        [TestMethod("CreateLayout_NullName_ReturnsFalse")]
        public void CreateLayout_NullName_ReturnsFalse()
        {
            // Arrange
            string layoutName = null;

            // Act
            var result = _layoutService.CreateLayout(layoutName);

            // Assert
            Assert.IsTrue(result.IsNull);
        }

        [TestMethod("CreateLayout_EmptyName_ReturnsFalse")]
        public void CreateLayout_EmptyName_ReturnsFalse()
        {
            // Arrange
            var layoutName = string.Empty;

            // Act
            var result = _layoutService.CreateLayout(layoutName);

            // Assert
            Assert.IsTrue(result.IsNull);
        }

        [TestMethod("CreateLayout_InvalidCharacters_HandledGracefully")]
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

        [TestMethod("DeleteLayout_ExistingLayout_ReturnsTrue")]
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
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DeleteLayout"), Times.Once());
        }

        [TestMethod("DeleteLayout_NonExistentLayout_ReturnsFalse")]
        public void DeleteLayout_NonExistentLayout_ReturnsFalse()
        {
            // Arrange
            var layoutName = "NonExistentLayout";

            // Act
            var result = _layoutService.DeleteLayout(layoutName);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("DeleteLayout_ModelTab_ReturnsFalse")]
        public void DeleteLayout_ModelTab_ReturnsFalse()
        {
            // Arrange
            var layoutName = "Model"; // 模型空间不能删除

            // Act
            var result = _layoutService.DeleteLayout(layoutName);

            // Assert
            Assert.IsFalse(result); // 模型空间不应该被删除
        }

        [TestMethod("CreateViewport_ValidParameters_ReturnsValidObjectId")]
        public void CreateViewport_ValidParameters_ReturnsValidObjectId()
        {
            // Arrange
            var layoutId = _layoutService.CreateLayout("ViewportTestLayout");
            var center = new Point3d(100, 100, 0);
            var width = 200.0;
            var height = 150.0;

            // Act
            var result = _layoutService.CreateViewport(layoutId, center, width, height);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);

            // 验证视口属性
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var viewport = transaction.GetObject(result, OpenMode.ForRead) as Viewport;
                Assert.IsNotNull(viewport);
                Assert.AreEqual<Point3d>(center, viewport.CenterPoint);
                Assert.AreEqual<double>(width, viewport.Width);
                Assert.AreEqual<double>(height, viewport.Height);

                transaction.Commit();
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("CreateViewport"), Times.Once());

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<LayoutEvent>()), Times.Once());
        }

        [TestMethod("CreateViewport_ZeroDimensions_ReturnsValidObjectId")]
        public void CreateViewport_ZeroDimensions_ReturnsValidObjectId()
        {
            // Arrange
            var layoutId = _layoutService.CreateLayout("ViewportTestLayout2");
            var center = new Point3d(50, 50, 0);
            var width = 0.0;
            var height = 0.0;

            // Act
            var result = _layoutService.CreateViewport(layoutId, center, width, height);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod("SetCurrentLayout_ExistingLayout_ReturnsTrue")]
        public void SetCurrentLayout_ExistingLayout_ReturnsTrue()
        {
            // Arrange
            var layoutName = "CurrentLayoutTest";
            _layoutService.CreateLayout(layoutName);

            // Act
            var result = _layoutService.SetCurrentLayout(layoutName);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod("SetCurrentLayout_NonExistentLayout_ReturnsFalse")]
        public void SetCurrentLayout_NonExistentLayout_ReturnsFalse()
        {
            // Arrange
            var layoutName = "NonExistentLayout";

            // Act
            var result = _layoutService.SetCurrentLayout(layoutName);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("GetAllLayouts_ReturnsLayoutCollection")]
        public void GetAllLayouts_ReturnsLayoutCollection()
        {
            // Arrange
            var testLayouts = new[] { "Layout1", "Layout2", "Layout3" };
            foreach (var layoutName in testLayouts)
            {
                _layoutService.CreateLayout(layoutName);
            }

            // Act
            var layouts = _layoutService.GetAllLayouts().ToList();

            // Assert
            Assert.IsNotNull(layouts);
            Assert.IsTrue(layouts.Count >= testLayouts.Length);
            Assert.IsTrue(testLayouts.All(name => layouts.Any(layout => layout.Name == name)));
        }

        [TestMethod("LayoutExists_ExistingLayout_ReturnsTrue")]
        public void LayoutExists_ExistingLayout_ReturnsTrue()
        {
            // Arrange
            var layoutName = "ExistTestLayout";
            _layoutService.CreateLayout(layoutName);

            // Act
            var result = _layoutService.LayoutExists(layoutName);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod("LayoutExists_NonExistentLayout_ReturnsFalse")]
        public void LayoutExists_NonExistentLayout_ReturnsFalse()
        {
            // Act
            var result = _layoutService.LayoutExists("NonExistentLayout");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("GetLayoutInfo_ExistingLayout_ReturnsLayoutInfo")]
        public void GetLayoutInfo_ExistingLayout_ReturnsLayoutInfo()
        {
            // Arrange
            var layoutName = "InfoTestLayout";
            _layoutService.CreateLayout(layoutName);

            // Act
            var layoutInfo = _layoutService.GetLayoutInfo(layoutName);

            // Assert
            Assert.IsNotNull(layoutInfo);
            Assert.AreEqual<string>(layoutName, layoutInfo.Name);
            Assert.IsFalse(layoutInfo.ObjectId.IsNull);
        }

        [TestMethod("GetLayoutInfo_NonExistentLayout_ReturnsNull")]
        public void GetLayoutInfo_NonExistentLayout_ReturnsNull()
        {
            // Act
            var layoutInfo = _layoutService.GetLayoutInfo("NonExistentLayout");

            // Assert
            Assert.IsNull(layoutInfo);
        }

        [TestMethod("RenameLayout_ExistingLayout_ReturnsTrue")]
        public void RenameLayout_ExistingLayout_ReturnsTrue()
        {
            // Arrange
            var oldName = "OldLayoutName";
            var newName = "NewLayoutName";
            _layoutService.CreateLayout(oldName);

            // Act
            var result = _layoutService.RenameLayout(oldName, newName);

            // Assert
            Assert.IsTrue(result);

            // 验证重命名是否成功
            Assert.IsFalse(_layoutService.LayoutExists(oldName));
            Assert.IsTrue(_layoutService.LayoutExists(newName));
        }

        [TestMethod("RenameLayout_NonExistentLayout_ReturnsFalse")]
        public void RenameLayout_NonExistentLayout_ReturnsFalse()
        {
            // Act
            var result = _layoutService.RenameLayout("NonExistentLayout", "NewName");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("RenameLayout_DuplicateName_ReturnsFalse")]
        public void RenameLayout_DuplicateName_ReturnsFalse()
        {
            // Arrange
            var layout1 = "Layout1";
            var layout2 = "Layout2";
            _layoutService.CreateLayout(layout1);
            _layoutService.CreateLayout(layout2);

            // Act
            var result = _layoutService.RenameLayout(layout1, layout2); // 尝试重命名为已存在的布局名

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("CopyLayout_ExistingLayout_ReturnsValidObjectId")]
        public void CopyLayout_ExistingLayout_ReturnsValidObjectId()
        {
            // Arrange
            var sourceLayoutName = "SourceLayout";
            var newLayoutName = "CopiedLayout";
            _layoutService.CreateLayout(sourceLayoutName);

            // Act
            var result = _layoutService.CopyLayout(sourceLayoutName, newLayoutName);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);

            // 验证复制是否成功
            Assert.IsTrue(_layoutService.LayoutExists(sourceLayoutName));
            Assert.IsTrue(_layoutService.LayoutExists(newLayoutName));
        }

        [TestMethod("CopyLayout_NonExistentLayout_ReturnsNull")]
        public void CopyLayout_NonExistentLayout_ReturnsNull()
        {
            // Act
            var result = _layoutService.CopyLayout("NonExistentLayout", "NewLayout");

            // Assert
            Assert.IsTrue(result.IsNull);
        }

        [TestMethod("GetPaperSize_ExistingLayout_ReturnsPaperSize")]
        public void GetPaperSize_ExistingLayout_ReturnsPaperSize()
        {
            // Arrange
            var layoutName = "PaperSizeTestLayout";
            _layoutService.CreateLayout(layoutName);

            // Act
            var paperSize = _layoutService.GetPaperSize(layoutName);

            // Assert
            Assert.IsNotNull(paperSize);
            Assert.IsTrue(paperSize.Width >= 0);
            Assert.IsTrue(paperSize.Height >= 0);
        }

        [TestMethod("SetPaperSize_ExistingLayout_ReturnsTrue")]
        public void SetPaperSize_ExistingLayout_ReturnsTrue()
        {
            // Arrange
            var layoutName = "SetPaperSizeTestLayout";
            _layoutService.CreateLayout(layoutName);
            var newSize = new PaperSize { Width = 420, Height = 297 }; // A3尺寸

            // Act
            var result = _layoutService.SetPaperSize(layoutName, newSize);

            // Assert
            Assert.IsTrue(result);

            // 验证纸张尺寸是否设置成功
            var actualSize = _layoutService.GetPaperSize(layoutName);
            Assert.AreEqual<double>(newSize.Width, actualSize.Width, 1e-6);
            Assert.AreEqual<double>(newSize.Height, actualSize.Height, 1e-6);
        }

        [TestMethod("SetPaperSize_NonExistentLayout_ReturnsFalse")]
        public void SetPaperSize_NonExistentLayout_ReturnsFalse()
        {
            // Arrange
            var newSize = new PaperSize { Width = 210, Height = 297 }; // A4尺寸

            // Act
            var result = _layoutService.SetPaperSize("NonExistentLayout", newSize);

            // Assert
            Assert.IsFalse(result);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _layoutService?.Dispose();
            base.TestCleanup();
        }
    }
}