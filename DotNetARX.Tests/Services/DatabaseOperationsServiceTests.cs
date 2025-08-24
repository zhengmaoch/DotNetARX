using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetARX.Tests.Services
{
    [TestClass]
    public class DatabaseOperationsServiceTests : TestBase
    {
        private DatabaseOperationsService _databaseOperationsService;
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

            _databaseOperationsService = new DatabaseOperationsService(
                _mockEventBus.Object,
                _mockPerformanceMonitor.Object,
                _mockLogger.Object);
        }

        [TestMethod]
        public void AddToModelSpace_ValidEntity_ReturnsValidObjectId()
        {
            // Arrange
            var line = new Line(new Point3d(0, 0, 0), new Point3d(100, 100, 0));

            // Act
            var result = _databaseOperationsService.AddToModelSpace(line);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);

            // 验证实体被添加到数据库
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var entity = transaction.GetObject(result, OpenMode.ForRead) as Line;
                Assert.IsNotNull(entity);
                Assert.AreEqual(new Point3d(0, 0, 0), entity.StartPoint);
                Assert.AreEqual(new Point3d(100, 100, 0), entity.EndPoint);
                transaction.Commit();
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("AddToModelSpace"), Times.Once);

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<EntityEvent>()), Times.Once);

            // 验证日志记录
            _mockLogger.Verify(x => x.Info(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void AddToModelSpace_NullEntity_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                _databaseOperationsService.AddToModelSpace(null));
        }

        [TestMethod]
        public void AddToModelSpace_BatchEntities_ReturnsObjectIdCollection()
        {
            // Arrange
            var entities = new List<Entity>
            {
                new Line(new Point3d(0, 0, 0), new Point3d(10, 10, 0)),
                new Circle(new Point3d(20, 20, 0), Vector3d.ZAxis, 5),
                new DBText { TextString = "Test", Position = new Point3d(30, 30, 0), Height = 1.0 }
            };

            // Act
            var result = _databaseOperationsService.AddToModelSpace(entities);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);

            foreach (ObjectId id in result)
            {
                Assert.IsFalse(id.IsNull);
                Assert.IsTrue(id.IsValid);
            }

            // 验证所有实体都被添加
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var line = transaction.GetObject(result[0], OpenMode.ForRead) as Line;
                var circle = transaction.GetObject(result[1], OpenMode.ForRead) as Circle;
                var text = transaction.GetObject(result[2], OpenMode.ForRead) as DBText;

                Assert.IsNotNull(line);
                Assert.IsNotNull(circle);
                Assert.IsNotNull(text);
                Assert.AreEqual("Test", text.TextString);

                transaction.Commit();
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("AddToModelSpace_Batch"), Times.Once);
        }

        [TestMethod]
        public void AddToModelSpace_EmptyEntitiesList_ReturnsEmptyCollection()
        {
            // Arrange
            var entities = new List<Entity>();

            // Act
            var result = _databaseOperationsService.AddToModelSpace(entities);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void AddToModelSpace_NullEntitiesList_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                _databaseOperationsService.AddToModelSpace((IEnumerable<Entity>)null));
        }

        [TestMethod]
        public void AddToPaperSpace_ValidEntity_ReturnsValidObjectId()
        {
            // Arrange
            var circle = new Circle(new Point3d(50, 50, 0), Vector3d.ZAxis, 25);

            // Act
            var result = _databaseOperationsService.AddToPaperSpace(circle);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);

            // 验证实体被添加到图纸空间
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var entity = transaction.GetObject(result, OpenMode.ForRead) as Circle;
                Assert.IsNotNull(entity);
                Assert.AreEqual(new Point3d(50, 50, 0), entity.Center);
                Assert.AreEqual(25.0, entity.Radius);
                transaction.Commit();
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("AddToPaperSpace"), Times.Once);
        }

        [TestMethod]
        public void AddToCurrentSpace_ValidEntity_ReturnsValidObjectId()
        {
            // Arrange
            var text = new DBText
            {
                TextString = "Current Space Test",
                Position = new Point3d(0, 0, 0),
                Height = 2.0
            };

            // Act
            var result = _databaseOperationsService.AddToCurrentSpace(text);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);

            // 验证实体被添加到当前空间
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var entity = transaction.GetObject(result, OpenMode.ForRead) as DBText;
                Assert.IsNotNull(entity);
                Assert.AreEqual("Current Space Test", entity.TextString);
                Assert.AreEqual(2.0, entity.Height);
                transaction.Commit();
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("AddToCurrentSpace"), Times.Once);
        }

        [TestMethod]
        public void DeleteEntity_ValidEntity_ReturnsTrue()
        {
            // Arrange
            var line = new Line(new Point3d(0, 0, 0), new Point3d(50, 50, 0));
            var entityId = _databaseOperationsService.AddToModelSpace(line);

            // Act
            var result = _databaseOperationsService.DeleteEntity(entityId);

            // Assert
            Assert.IsTrue(result);

            // 验证实体被删除
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var entity = transaction.GetObject(entityId, OpenMode.ForRead, true) as Entity;
                Assert.IsTrue(entity.IsErased);
                transaction.Commit();
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DeleteEntity"), Times.Once);
        }

        [TestMethod]
        public void DeleteEntity_InvalidEntity_ReturnsFalse()
        {
            // Arrange
            var invalidId = ObjectId.Null;

            // Act
            var result = _databaseOperationsService.DeleteEntity(invalidId);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void DeleteEntities_ValidEntities_ReturnsDeletedCount()
        {
            // Arrange
            var entities = new List<Entity>
            {
                new Line(new Point3d(0, 0, 0), new Point3d(10, 10, 0)),
                new Circle(new Point3d(20, 20, 0), Vector3d.ZAxis, 5),
                new DBText { TextString = "Delete Test", Position = new Point3d(30, 30, 0), Height = 1.0 }
            };

            var entityIds = _databaseOperationsService.AddToModelSpace(entities);

            // Act
            var result = _databaseOperationsService.DeleteEntities(entityIds.Cast<ObjectId>());

            // Assert
            Assert.AreEqual(3, result);

            // 验证所有实体都被删除
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in entityIds)
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, true) as Entity;
                    Assert.IsTrue(entity.IsErased);
                }
                transaction.Commit();
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DeleteEntities_Batch"), Times.Once);
        }

        [TestMethod]
        public void DeleteEntities_EmptyList_ReturnsZero()
        {
            // Arrange
            var emptyList = new List<ObjectId>();

            // Act
            var result = _databaseOperationsService.DeleteEntities(emptyList);

            // Assert
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void DeleteEntities_NullList_ReturnsZero()
        {
            // Act
            var result = _databaseOperationsService.DeleteEntities(null);

            // Assert
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void GetDatabaseInfo_ReturnsValidInfo()
        {
            // Arrange
            // 添加一些测试实体以增加实体计数
            var line = new Line(new Point3d(0, 0, 0), new Point3d(100, 100, 0));
            _databaseOperationsService.AddToModelSpace(line);

            // Act
            var result = _databaseOperationsService.GetDatabaseInfo();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.FileName);
            Assert.IsNotNull(result.Version);
            Assert.IsTrue(result.CreationTime > DateTime.MinValue);
            Assert.IsTrue(result.ModificationTime > DateTime.MinValue);
            Assert.IsTrue(result.EntityCount >= 1); // 至少有我们添加的线
            Assert.IsTrue(result.LayerCount >= 1); // 至少有默认图层"0"
            Assert.IsTrue(result.BlockCount >= 2); // 至少有*Model_Space和*Paper_Space
            Assert.IsNotNull(result.CurrentLayer);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("GetDatabaseInfo"), Times.Once);
        }

        [TestMethod]
        public void AddToModelSpace_BatchWithNullEntities_SkipsNullEntities()
        {
            // Arrange
            var entities = new List<Entity>
            {
                new Line(new Point3d(0, 0, 0), new Point3d(10, 10, 0)),
                null, // 空实体
                new Circle(new Point3d(20, 20, 0), Vector3d.ZAxis, 5),
                null  // 另一个空实体
            };

            // Act
            var result = _databaseOperationsService.AddToModelSpace(entities);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count); // 只有两个有效实体被添加

            // 验证有效实体都被添加
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var line = transaction.GetObject(result[0], OpenMode.ForRead) as Line;
                var circle = transaction.GetObject(result[1], OpenMode.ForRead) as Circle;

                Assert.IsNotNull(line);
                Assert.IsNotNull(circle);

                transaction.Commit();
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            _databaseOperationsService?.Dispose();
            base.TestCleanup();
        }
    }
}