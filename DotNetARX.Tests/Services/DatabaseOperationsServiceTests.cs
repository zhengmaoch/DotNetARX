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
        }

        [TestMethod]
        public void AddToModelSpace_NullEntity_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                _databaseOperationsService.AddToModelSpace((Entity)null));
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
        }

        [TestMethod]
        public void DeleteEntity_ValidEntity_ReturnsTrue()
        {
            // Arrange
            var line = new Line(new Point3d(0, 0, 0), new Point3d(100, 100, 0));
            var entityId = AddEntityToModelSpace(line);

            // Act
            var result = _databaseOperationsService.DeleteEntity(entityId);

            // Assert
            Assert.IsTrue(result);
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
        public void DeleteEntities_ValidEntities_ReturnsDeleteCount()
        {
            // Arrange
            var entities = new List<Entity>
            {
                new Line(new Point3d(0, 0, 0), new Point3d(10, 10, 0)),
                new Circle(new Point3d(20, 20, 0), Vector3d.ZAxis, 5),
                new DBText { TextString = "Test", Position = new Point3d(30, 30, 0), Height = 1.0 }
            };

            var entityIds = new ObjectIdCollection();
            foreach (var entity in entities)
            {
                entityIds.Add(AddEntityToModelSpace(entity));
            }

            // Act
            var result = _databaseOperationsService.DeleteEntities(entityIds.Cast<ObjectId>().ToList());

            // Assert
            Assert.AreEqual(3, result);
        }

        [TestMethod]
        public void PurgeDatabase_EmptyDatabase_ReturnsZeroCount()
        {
            // Act
            var result = _databaseOperationsService.PurgeDatabase();

            // Assert
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void GetDatabaseInfo_ReturnsValidInfo()
        {
            // Act
            var result = _databaseOperationsService.GetDatabaseInfo();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.FileName);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _databaseOperationsService?.Dispose();
            base.TestCleanup();
        }
    }
}