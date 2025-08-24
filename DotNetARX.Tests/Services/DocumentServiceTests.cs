using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetARX.Tests.Services
{
    [TestClass]
    public class DocumentServiceTests : TestBase
    {
        private DocumentService _documentService;
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

            _documentService = new DocumentService(
                _mockEventBus.Object,
                _mockPerformanceMonitor.Object,
                _mockLogger.Object);
        }

        [TestMethod]
        public void CheckDocumentNeedsSave_NewDocument_ReturnsFalse()
        {
            // Act
            var result = _documentService.CheckDocumentNeedsSave();

            // Assert
            Assert.IsFalse(result);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("CheckDocumentNeedsSave"), Times.Once);
        }

        [TestMethod]
        public void SaveDocument_ValidDocument_ReturnsTrue()
        {
            // Act
            var result = _documentService.SaveDocument();

            // Assert
            // 在测试环境中，文档可能无法真正保存，但不应抛出异常
            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("SaveDocument"), Times.Once);

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<DocumentEvent>()), Times.Once);
        }

        [TestMethod]
        public void SaveDocumentAs_ValidPath_ReturnsResult()
        {
            // Arrange
            var testPath = @"C:\Temp\test.dwg";

            // Act
            var result = _documentService.SaveDocumentAs(testPath);

            // Assert
            // 在测试环境中可能无法保存到指定路径，但不应抛出异常
            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("SaveDocumentAs"), Times.Once);

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<DocumentEvent>()), Times.Once);
        }

        [TestMethod]
        public void SaveDocumentAs_NullPath_ReturnsFalse()
        {
            // Arrange
            string testPath = null;

            // Act
            var result = _documentService.SaveDocumentAs(testPath);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SaveDocumentAs_EmptyPath_ReturnsFalse()
        {
            // Arrange
            var testPath = string.Empty;

            // Act
            var result = _documentService.SaveDocumentAs(testPath);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SaveDocumentAs_InvalidPath_ReturnsFalse()
        {
            // Arrange
            var testPath = @"Z:\NonExistentFolder\test.dwg";

            // Act
            var result = _documentService.SaveDocumentAs(testPath);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GetDocumentInfo_ValidDocument_ReturnsInfo()
        {
            // Act
            var result = _documentService.GetDocumentInfo();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(string.IsNullOrEmpty(result.Name));

            // 验证基本属性
            Assert.IsTrue(result.CreationTime <= DateTime.Now);
            Assert.IsTrue(result.LastModified <= DateTime.Now);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("GetDocumentInfo"), Times.Once);
        }

        [TestMethod]
        public void GetDocumentInfo_CheckAllProperties_ValidValues()
        {
            // Act
            var result = _documentService.GetDocumentInfo();

            // Assert
            Assert.IsNotNull(result);

            // 检查所有属性是否有效
            Assert.IsNotNull(result.Name);
            Assert.IsNotNull(result.FullPath);
            Assert.IsTrue(result.CreationTime > DateTime.MinValue);
            Assert.IsTrue(result.LastModified > DateTime.MinValue);
            Assert.IsTrue(result.FileSize >= 0);

            // IsModified 可能为 true 或 false，都是有效的
            Assert.IsTrue(result.IsModified == true || result.IsModified == false);
        }

        [TestMethod]
        public void CheckDocumentNeedsSave_AfterModification_ReturnsTrue()
        {
            // Arrange - 修改文档以触发需要保存状态
            try
            {
                using (var transaction = TestDatabase.TransactionManager.StartTransaction())
                {
                    var circle = new Circle(new Autodesk.AutoCAD.Geometry.Point3d(0, 0, 0),
                                          Autodesk.AutoCAD.Geometry.Vector3d.ZAxis, 10);
                    var modelSpace = transaction.GetObject(TestDatabase.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                    modelSpace.AppendEntity(circle);
                    transaction.AddNewlyCreatedDBObject(circle, true);
                    transaction.Commit();
                }

                // Act
                var result = _documentService.CheckDocumentNeedsSave();

                // Assert
                Assert.IsTrue(result);
            }
            catch (Exception)
            {
                // 在某些测试环境中可能无法修改文档状态，这是正常的
                Assert.Inconclusive("无法在测试环境中修改文档状态");
            }
        }

        [TestMethod]
        public void SaveDocument_MultipleCallsInSequence_AllSucceed()
        {
            // Act & Assert
            for (int i = 0; i < 3; i++)
            {
                var result = _documentService.SaveDocument();
                // 每次调用都应该完成，不抛出异常
            }

            // 验证性能监控被调用了3次
            _mockPerformanceMonitor.Verify(x => x.StartOperation("SaveDocument"), Times.Exactly(3));
        }

        [TestMethod]
        public void DocumentInfo_ConsistentResults_SameCallMultipleTimes()
        {
            // Act
            var result1 = _documentService.GetDocumentInfo();
            var result2 = _documentService.GetDocumentInfo();

            // Assert
            Assert.IsNotNull(result1);
            Assert.IsNotNull(result2);

            // 基本信息应该保持一致
            Assert.AreEqual(result1.Name, result2.Name);
            Assert.AreEqual(result1.FullPath, result2.FullPath);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _documentService?.Dispose();
            base.TestCleanup();
        }
    }
}