using DotNetARX.Configuration;

namespace DotNetARX.Tests.Services
{
    [TestClass]
    public class EntityOperationServiceTests : TestBase
    {
        private EntityOperationService _entityOperationService;
        private Mock<ILogger> _mockLogger;
        private Mock<IConfigurationManager> _mockConfig;

        [TestInitialize]
        public void Setup()
        {
            base.TestInitialize();

            _mockLogger = new Mock<ILogger>();
            _mockConfig = new Mock<IConfigurationManager>();

            _entityOperationService = new EntityOperationService(
                _mockLogger.Object,
                _mockConfig.Object);
        }

        private ObjectId CreateTestLine()
        {
            var line = new Line(new Point3d(0, 0, 0), new Point3d(100, 100, 0));
            return AddEntityToModelSpace(line);
        }

        private ObjectId CreateTestCircle()
        {
            var circle = new Circle(new Point3d(50, 50, 0), Vector3d.ZAxis, 25);
            return AddEntityToModelSpace(circle);
        }

        [TestMethod]
        public void MoveEntity_ValidEntity_ReturnsTrue()
        {
            // Arrange
            var entityId = CreateTestLine();
            var fromPoint = new Point3d(0, 0, 0);
            var toPoint = new Point3d(50, 50, 0);

            // Act
            var result = _entityOperationService.MoveEntity(entityId, fromPoint, toPoint);

            // Assert
            Assert.IsTrue(result);

            // 验证实体位置
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var line = transaction.GetObject(entityId, OpenMode.ForRead) as Line;
                Assert.AreEqual(new Point3d(50, 50, 0), line.StartPoint);
                Assert.AreEqual(new Point3d(150, 150, 0), line.EndPoint);
                transaction.Commit();
            }
        }

        [TestMethod]
        public void MoveEntity_InvalidEntity_ReturnsFalse()
        {
            // Arrange
            var invalidId = ObjectId.Null;
            var fromPoint = new Point3d(0, 0, 0);
            var toPoint = new Point3d(50, 50, 0);

            // Act
            var result = _entityOperationService.MoveEntity(invalidId, fromPoint, toPoint);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CopyEntity_ValidEntity_ReturnsValidObjectId()
        {
            // Arrange
            var entityId = CreateTestLine();
            var fromPoint = new Point3d(0, 0, 0);
            var toPoint = new Point3d(100, 0, 0);

            // Act
            var result = _entityOperationService.CopyEntity(entityId, fromPoint, toPoint);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);
            Assert.AreNotEqual(entityId, result);

            // 验证复制的实体
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var originalLine = transaction.GetObject(entityId, OpenMode.ForRead) as Line;
                var copiedLine = transaction.GetObject(result, OpenMode.ForRead) as Line;

                Assert.AreEqual(new Point3d(100, 0, 0), copiedLine.StartPoint);
                Assert.AreEqual(new Point3d(200, 100, 0), copiedLine.EndPoint);

                transaction.Commit();
            }
        }

        [TestMethod]
        public void RotateEntity_ValidEntity_ReturnsTrue()
        {
            // Arrange
            var entityId = CreateTestLine();
            var basePoint = new Point3d(0, 0, 0);
            var angle = Math.PI / 2; // 90度

            // Act
            var result = _entityOperationService.RotateEntity(entityId, basePoint, angle);

            // Assert
            Assert.IsTrue(result);

            // 验证实体旋转
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var line = transaction.GetObject(entityId, OpenMode.ForRead) as Line;
                // 旋转90度后，(100,100,0)应该变成(-100,100,0)
                var expectedEndPoint = new Point3d(-100, 100, 0);
                Assert.IsTrue(line.EndPoint.IsEqualTo(expectedEndPoint, new Tolerance(1e-6, 1e-6)));

                transaction.Commit();
            }
        }

        [TestMethod]
        public void ScaleEntity_ValidEntity_ReturnsTrue()
        {
            // Arrange
            var entityId = CreateTestCircle();
            var basePoint = new Point3d(50, 50, 0);
            var scaleFactor = 2.0;

            // Act
            var result = _entityOperationService.ScaleEntity(entityId, basePoint, scaleFactor);

            // Assert
            Assert.IsTrue(result);

            // 验证实体缩放
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var circle = transaction.GetObject(entityId, OpenMode.ForRead) as Circle;
                Assert.AreEqual(50.0, circle.Radius, 1e-6); // 原半径25 * 2 = 50

                transaction.Commit();
            }
        }

        [TestMethod]
        public void ScaleEntity_ZeroScaleFactor_ReturnsFalse()
        {
            // Arrange
            var entityId = CreateTestCircle();
            var basePoint = new Point3d(50, 50, 0);
            var scaleFactor = 0.0;

            // Act
            var result = _entityOperationService.ScaleEntity(entityId, basePoint, scaleFactor);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void OffsetEntity_ValidEntity_ReturnsObjectIdCollection()
        {
            // Arrange
            var entityId = CreateTestCircle();
            var distance = 10.0;

            // Act
            var result = _entityOperationService.OffsetEntity(entityId, distance);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            // 验证偏移的实体
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var originalCircle = transaction.GetObject(entityId, OpenMode.ForRead) as Circle;
                var offsetCircle = transaction.GetObject(result[0], OpenMode.ForRead) as Circle;

                Assert.AreEqual(35.0, offsetCircle.Radius, 1e-6); // 原半径25 + 偏移10 = 35
                Assert.AreEqual(originalCircle.Center, offsetCircle.Center);

                transaction.Commit();
            }
        }

        [TestMethod]
        public void MirrorEntity_ValidEntity_ReturnsValidObjectId()
        {
            // Arrange
            var entityId = CreateTestLine();
            var mirrorPt1 = new Point3d(50, 0, 0);
            var mirrorPt2 = new Point3d(50, 100, 0);
            var eraseSource = false;

            // Act
            var result = _entityOperationService.MirrorEntity(entityId, mirrorPt1, mirrorPt2, eraseSource);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);

            // 验证原实体仍存在（因为eraseSource=false）
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var originalLine = transaction.GetObject(entityId, OpenMode.ForRead) as Line;
                Assert.IsNotNull(originalLine);
                Assert.IsFalse(originalLine.IsErased);

                var mirroredLine = transaction.GetObject(result, OpenMode.ForRead) as Line;
                Assert.IsNotNull(mirroredLine);

                // 镜像后的点应该关于x=50的直线对称
                Assert.AreEqual(new Point3d(100, 0, 0), mirroredLine.StartPoint);
                Assert.AreEqual(new Point3d(0, 100, 0), mirroredLine.EndPoint);

                transaction.Commit();
            }
        }

        [TestMethod]
        public void MirrorEntity_EraseSource_OriginalEntityErased()
        {
            // Arrange
            var entityId = CreateTestLine();
            var mirrorPt1 = new Point3d(50, 0, 0);
            var mirrorPt2 = new Point3d(50, 100, 0);
            var eraseSource = true;

            // Act
            var result = _entityOperationService.MirrorEntity(entityId, mirrorPt1, mirrorPt2, eraseSource);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);

            // 验证原实体被删除
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var originalLine = transaction.GetObject(entityId, OpenMode.ForRead, true) as Line;
                Assert.IsTrue(originalLine.IsErased);

                transaction.Commit();
            }
        }

        [TestMethod]
        public void ValidateEntity_ValidEntity_ReturnsTrue()
        {
            // Arrange
            var entityId = CreateTestLine();

            // Act
            var result = _entityOperationService.ValidateEntity(entityId);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ValidateEntity_InvalidEntity_ReturnsFalse()
        {
            // Arrange
            var invalidId = ObjectId.Null;

            // Act
            var result = _entityOperationService.ValidateEntity(invalidId);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateEntity_ErasedEntity_ReturnsFalse()
        {
            // Arrange
            var entityId = CreateTestLine();

            // 删除实体
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var entity = transaction.GetObject(entityId, OpenMode.ForWrite) as Entity;
                entity.Erase();
                transaction.Commit();
            }

            // Act
            var result = _entityOperationService.ValidateEntity(entityId);

            // Assert
            Assert.IsFalse(result);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _entityOperationService?.Dispose();
            base.TestCleanup();
        }
    }
}