using DotNetARX.Configuration;

namespace DotNetARX.Tests.Services
{
    [TestClass("EntityServiceTests")]
    public class EntityServiceTests : TestBase
    {
        private EntityService _entityService;
        private Mock<ILogger> _mockLogger;
        private Mock<IConfigurationManager> _mockConfig;

        [TestInitialize]
        public void Setup()
        {
            base.TestInitialize();

            _mockLogger = new Mock<ILogger>();
            _mockConfig = new Mock<IConfigurationManager>();

            _entityService = new EntityService(
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

        [TestMethod("MoveEntity_ValidEntity_ReturnsTrue")]
        public void MoveEntity_ValidEntity_ReturnsTrue()
        {
            // Arrange
            var entityId = CreateTestLine();
            var fromPoint = new Point3d(0, 0, 0);
            var toPoint = new Point3d(50, 50, 0);

            // Act
            var result = _entityService.MoveEntity(entityId, fromPoint, toPoint);

            // Assert
            Assert.IsTrue(result);

            // 验证实体位置
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var line = transaction.GetObject(entityId, OpenMode.ForRead) as Line;
                Assert.AreEqual<Point3d>(new Point3d(50, 50, 0), line.StartPoint);
                Assert.AreEqual<Point3d>(new Point3d(150, 150, 0), line.EndPoint);
                transaction.Commit();
            }
        }

        [TestMethod("MoveEntity_InvalidEntity_ReturnsFalse")]
        public void MoveEntity_InvalidEntity_ReturnsFalse()
        {
            // Arrange
            var invalidId = ObjectId.Null;
            var fromPoint = new Point3d(0, 0, 0);
            var toPoint = new Point3d(50, 50, 0);

            // Act
            var result = _entityService.MoveEntity(invalidId, fromPoint, toPoint);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("CopyEntity_ValidEntity_ReturnsValidObjectId")]
        public void CopyEntity_ValidEntity_ReturnsValidObjectId()
        {
            // Arrange
            var entityId = CreateTestLine();
            var fromPoint = new Point3d(0, 0, 0);
            var toPoint = new Point3d(100, 0, 0);

            // Act
            var result = _entityService.CopyEntity(entityId, fromPoint, toPoint);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);
            Assert.AreNotEqual(entityId, result);

            // 验证复制的实体
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var originalLine = transaction.GetObject(entityId, OpenMode.ForRead) as Line;
                var copiedLine = transaction.GetObject(result, OpenMode.ForRead) as Line;

                Assert.AreEqual<Point3d>(new Point3d(100, 0, 0), copiedLine.StartPoint);
                Assert.AreEqual<Point3d>(new Point3d(200, 100, 0), copiedLine.EndPoint);

                transaction.Commit();
            }
        }

        [TestMethod("RotateEntity_ValidEntity_ReturnsTrue")]
        public void RotateEntity_ValidEntity_ReturnsTrue()
        {
            // Arrange
            var entityId = CreateTestLine();
            var basePoint = new Point3d(0, 0, 0);
            var angle = Math.PI / 2; // 90度

            // Act
            var result = _entityService.RotateEntity(entityId, basePoint, angle);

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

        [TestMethod("ScaleEntity_ValidEntity_ReturnsTrue")]
        public void ScaleEntity_ValidEntity_ReturnsTrue()
        {
            // Arrange
            var entityId = CreateTestCircle();
            var basePoint = new Point3d(50, 50, 0);
            var scaleFactor = 2.0;

            // Act
            var result = _entityService.ScaleEntity(entityId, basePoint, scaleFactor);

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

        [TestMethod("ScaleEntity_ZeroScaleFactor_ReturnsFalse")]
        public void ScaleEntity_ZeroScaleFactor_ReturnsFalse()
        {
            // Arrange
            var entityId = CreateTestCircle();
            var basePoint = new Point3d(50, 50, 0);
            var scaleFactor = 0.0;

            // Act
            var result = _entityService.ScaleEntity(entityId, basePoint, scaleFactor);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("OffsetEntity_ValidEntity_ReturnsObjectIdCollection")]
        public void OffsetEntity_ValidEntity_ReturnsObjectIdCollection()
        {
            // Arrange
            var entityId = CreateTestCircle();
            var distance = 10.0;

            // Act
            var result = _entityService.OffsetEntity(entityId, distance);

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

        [TestMethod("MirrorEntity_ValidEntity_ReturnsValidObjectId")]
        public void MirrorEntity_ValidEntity_ReturnsValidObjectId()
        {
            // Arrange
            var entityId = CreateTestLine();
            var mirrorPoint1 = new Point3d(0, 50, 0);
            var mirrorPoint2 = new Point3d(100, 50, 0);

            // Act
            var result = _entityService.MirrorEntity(entityId, mirrorPoint1, mirrorPoint2, true);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);
            Assert.AreNotEqual(entityId, result);

            // 验证镜像的实体
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var originalLine = transaction.GetObject(entityId, OpenMode.ForRead) as Line;
                var mirroredLine = transaction.GetObject(result, OpenMode.ForRead) as Line;

                // 原线段从(0,0,0)到(100,100,0)，镜像后应该从(0,100,0)到(100,0,0)
                Assert.IsTrue(mirroredLine.StartPoint.IsEqualTo(new Point3d(0, 100, 0), new Tolerance(1e-6, 1e-6)));
                Assert.IsTrue(mirroredLine.EndPoint.IsEqualTo(new Point3d(100, 0, 0), new Tolerance(1e-6, 1e-6)));

                transaction.Commit();
            }
        }

        [TestMethod("ArrayEntity_RectangularArray_ReturnsObjectIdCollection")]
        public void ArrayEntity_RectangularArray_ReturnsObjectIdCollection()
        {
            // Arrange
            var entityId = CreateTestCircle();
            var rows = 3;
            var columns = 2;
            var rowOffset = 50.0;
            var columnOffset = 30.0;

            // Act
            var result = _entityService.ArrayEntity(entityId, rows, columns, rowOffset, columnOffset);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(6, result.Count); // 3行×2列=6个实体

            // 验证阵列的实体位置
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var originalCircle = transaction.GetObject(entityId, OpenMode.ForRead) as Circle;
                var firstCopiedCircle = transaction.GetObject(result[0], OpenMode.ForRead) as Circle;

                // 第一个复制的圆应该在(50,50,0)位置（第一行第一列）
                Assert.IsTrue(firstCopiedCircle.Center.IsEqualTo(new Point3d(50, 50, 0), new Tolerance(1e-6, 1e-6)));

                transaction.Commit();
            }
        }

        [TestMethod("ArrayEntity_ZeroRows_ThrowsArgumentException")]
        public void ArrayEntity_ZeroRows_ThrowsArgumentException()
        {
            // Arrange
            var entityId = CreateTestCircle();
            var rows = 0;
            var columns = 2;
            var rowOffset = 50.0;
            var columnOffset = 30.0;

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _entityService.ArrayEntity(entityId, rows, columns, rowOffset, columnOffset));
        }

        [TestMethod("ArrayEntity_NegativeColumns_ThrowsArgumentException")]
        public void ArrayEntity_NegativeColumns_ThrowsArgumentException()
        {
            // Arrange
            var entityId = CreateTestCircle();
            var rows = 3;
            var columns = -1;
            var rowOffset = 50.0;
            var columnOffset = 30.0;

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _entityService.ArrayEntity(entityId, rows, columns, rowOffset, columnOffset));
        }

        [TestMethod("ChangeEntityProperties_ValidEntity_ReturnsTrue")]
        public void ChangeEntityProperties_ValidEntity_ReturnsTrue()
        {
            // Arrange
            var entityId = CreateTestLine();
            var newProperties = new EntityProperties
            {
                ColorIndex = 1,
                LayerName = "TestLayer",
                LineType = "DASHED",
                LineWeight = LineWeight.LineWeight020
            };

            // Act
            var result = _entityService.ChangeEntityProperties(entityId, newProperties);

            // Assert
            Assert.IsTrue(result);

            // 验证实体属性
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var line = transaction.GetObject(entityId, OpenMode.ForRead) as Line;
                Assert.AreEqual<double>(1, line.Color.ColorIndex);
                Assert.AreEqual<string>("TestLayer", line.Layer);
                // LineType和LineWeight可能需要特殊处理，这里仅验证颜色和图层
                transaction.Commit();
            }
        }

        [TestMethod("ChangeEntityProperties_InvalidEntity_ReturnsFalse")]
        public void ChangeEntityProperties_InvalidEntity_ReturnsFalse()
        {
            // Arrange
            var invalidId = ObjectId.Null;
            var newProperties = new EntityProperties
            {
                ColorIndex = 1,
                LayerName = "TestLayer"
            };

            // Act
            var result = _entityService.ChangeEntityProperties(invalidId, newProperties);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("GetEntityInfo_ValidEntity_ReturnsEntityInfo")]
        public void GetEntityInfo_ValidEntity_ReturnsEntityInfo()
        {
            // Arrange
            var entityId = CreateTestCircle();

            // Act
            var result = _entityService.GetEntityInfo(entityId);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(string.IsNullOrEmpty(result.EntityType));
            Assert.IsFalse(result.ObjectId.IsNull);
            Assert.AreEqual<ObjectId>(entityId, result.ObjectId);
            Assert.IsTrue(result.Handle.Value > 0);
        }

        [TestMethod("GetEntityInfo_InvalidEntity_ReturnsNull")]
        public void GetEntityInfo_InvalidEntity_ReturnsNull()
        {
            // Arrange
            var invalidId = ObjectId.Null;

            // Act
            var result = _entityService.GetEntityInfo(invalidId);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod("EraseEntity_ValidEntity_ReturnsTrue")]
        public void EraseEntity_ValidEntity_ReturnsTrue()
        {
            // Arrange
            var entityId = CreateTestLine();

            // Act
            var result = _entityService.EraseEntity(entityId);

            // Assert
            Assert.IsTrue(result);

            // 验证实体已被删除
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                Assert.IsTrue(entityId.IsErased);
                transaction.Commit();
            }
        }

        [TestMethod("EraseEntity_InvalidEntity_ReturnsFalse")]
        public void EraseEntity_InvalidEntity_ReturnsFalse()
        {
            // Arrange
            var invalidId = ObjectId.Null;

            // Act
            var result = _entityService.EraseEntity(invalidId);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("GetEntityBounds_ValidEntity_ReturnsExtents3d")]
        public void GetEntityBounds_ValidEntity_ReturnsExtents3d()
        {
            // Arrange
            var entityId = CreateTestLine();

            // Act
            var result = _entityService.GetEntityBounds(entityId);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Value.MinPoint.IsNull());
            Assert.IsFalse(result.Value.MaxPoint.IsNull());
        }

        [TestMethod("GetEntityBounds_InvalidEntity_ReturnsNull")]
        public void GetEntityBounds_InvalidEntity_ReturnsNull()
        {
            // Arrange
            var invalidId = ObjectId.Null;

            // Act
            var result = _entityService.GetEntityBounds(invalidId);

            // Assert
            Assert.IsNull(result);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _entityService?.Dispose();
            base.TestCleanup();
        }
    }
}