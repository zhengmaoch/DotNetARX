namespace DotNetARX.Tests.Services
{
    [TestClass("DatabaseServiceTests")]
    public class DatabaseServiceTests : TestBase
    {
        private DatabaseService _databaseService;
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

            _databaseService = new DatabaseService(
                _mockEventBus.Object,
                _mockPerformanceMonitor.Object,
                _mockLogger.Object);
        }

        [TestMethod("AddToModelSpace_ValidEntity_ReturnsValidObjectId")]
        public void AddToModelSpace_ValidEntity_ReturnsValidObjectId()
        {
            // Arrange
            var line = new Line(new Point3d(0, 0, 0), new Point3d(100, 100, 0));

            // Act
            var result = _databaseService.AddToModelSpace(line);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod("AddToModelSpace_NullEntity_ThrowsArgumentNullException")]
        public void AddToModelSpace_NullEntity_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                _databaseService.AddToModelSpace((Entity)null));
        }

        [TestMethod("AddToModelSpace_BatchEntities_ReturnsObjectIdCollection")]
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
            var result = _databaseService.AddToModelSpace(entities);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);

            foreach (ObjectId id in result)
            {
                Assert.IsFalse(id.IsNull);
                Assert.IsTrue(id.IsValid);
            }
        }

        [TestMethod("AddToModelSpace_EmptyEntitiesList_ReturnsEmptyCollection")]
        public void AddToModelSpace_EmptyEntitiesList_ReturnsEmptyCollection()
        {
            // Arrange
            var entities = new List<Entity>();

            // Act
            var result = _databaseService.AddToModelSpace(entities);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod("AddToModelSpace_NullEntitiesList_ThrowsArgumentNullException")]
        public void AddToModelSpace_NullEntitiesList_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                _databaseService.AddToModelSpace((IEnumerable<Entity>)null));
        }

        [TestMethod("AddToPaperSpace_ValidEntity_ReturnsValidObjectId")]
        public void AddToPaperSpace_ValidEntity_ReturnsValidObjectId()
        {
            // Arrange
            var circle = new Circle(new Point3d(50, 50, 0), Vector3d.ZAxis, 25);

            // Act
            var result = _databaseService.AddToPaperSpace(circle);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod("AddToCurrentSpace_ValidEntity_ReturnsValidObjectId")]
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
            var result = _databaseService.AddToCurrentSpace(text);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod("DeleteEntity_ValidEntity_ReturnsTrue")]
        public void DeleteEntity_ValidEntity_ReturnsTrue()
        {
            // Arrange
            var line = new Line(new Point3d(0, 0, 0), new Point3d(100, 100, 0));
            var entityId = AddEntityToModelSpace(line);

            // Act
            var result = _databaseService.DeleteEntity(entityId);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod("DeleteEntity_InvalidEntity_ReturnsFalse")]
        public void DeleteEntity_InvalidEntity_ReturnsFalse()
        {
            // Arrange
            var invalidId = ObjectId.Null;

            // Act
            var result = _databaseService.DeleteEntity(invalidId);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("DeleteEntities_ValidEntities_ReturnsDeleteCount")]
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
            var result = _databaseService.DeleteEntities(entityIds.Cast<ObjectId>().ToList());

            // Assert
            Assert.AreEqual(3, result);
        }

        [TestMethod("PurgeDatabase_EmptyDatabase_ReturnsZeroCount")]
        public void PurgeDatabase_EmptyDatabase_ReturnsZeroCount()
        {
            // Act
            var result = _databaseService.PurgeDatabase();

            // Assert
            Assert.AreEqual(0, result);
        }

        [TestMethod("GetDatabaseInfo_ReturnsValidInfo")]
        public void GetDatabaseInfo_ReturnsValidInfo()
        {
            // Act
            var result = _databaseService.GetDatabaseInfo();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(string.IsNullOrEmpty(result.FileName));
            Assert.IsTrue(result.EntityCount >= 0);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _databaseService?.Dispose();
            base.TestCleanup();
        }
    }
}