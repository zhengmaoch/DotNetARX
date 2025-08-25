using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Moq;
using System.Collections.Generic;

namespace DotNetARX.Tests.Services
{
    [TestClass("AutoCADContextTests")]
    public class AutoCADContextTests : TestBase
    {
        [TestInitialize]
        public void Setup()
        {
            base.TestInitialize();
        }

        [TestCleanup]
        public void Cleanup()
        {
            base.TestCleanup();
        }

        [TestMethod("ExecuteBatch_VoidAction_ExecuteWithoutException")]
        public void ExecuteBatch_VoidAction_ExecuteWithoutException()
        {
            // Arrange
            var entityIds = new List<ObjectId>();
            var line = new Line(new Point3d(0, 0, 0), new Point3d(100, 100, 0));
            var circle = new Circle(new Point3d(50, 50, 0), Vector3d.ZAxis, 25);

            // Act
            AutoCADContext.ExecuteBatch(context =>
            {
                using (var blockTableRecord = context.Transaction.GetObject(
                    context.Database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                {
                    // 添加直线
                    blockTableRecord.AppendEntity(line);
                    context.Transaction.AddNewlyCreatedDBObject(line, true);
                    entityIds.Add(line.ObjectId);

                    // 添加圆
                    blockTableRecord.AppendEntity(circle);
                    context.Transaction.AddNewlyCreatedDBObject(circle, true);
                    entityIds.Add(circle.ObjectId);
                }
            });

            // Assert
            Assert.AreEqual(2, entityIds.Count);
            Assert.IsFalse(entityIds[0].IsNull);
            Assert.IsFalse(entityIds[1].IsNull);
            Assert.AreNotEqual(entityIds[0], entityIds[1]);
        }

        [TestMethod("ExecuteBatch_WithReturnValue_ReturnsExpectedValue")]
        public void ExecuteBatch_WithReturnValue_ReturnsExpectedValue()
        {
            // Arrange
            var expectedCount = 3;

            // Act
            var actualIds = AutoCADContext.ExecuteBatch(context =>
            {
                var ids = new List<ObjectId>();
                using (var blockTableRecord = context.Transaction.GetObject(
                    context.Database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                {
                    // 创建多个实体
                    for (int i = 0; i < expectedCount; i++)
                    {
                        var line = new Line(new Point3d(i * 10, 0, 0), new Point3d(i * 10 + 5, 5, 0));
                        blockTableRecord.AppendEntity(line);
                        context.Transaction.AddNewlyCreatedDBObject(line, true);
                        ids.Add(line.ObjectId);
                    }
                }

                return ids;
            });

            // Assert
            Assert.IsNotNull(actualIds);
            Assert.AreEqual(expectedCount, actualIds.Count);
            foreach (var id in actualIds)
            {
                Assert.IsFalse(id.IsNull);
            }
        }

        [TestMethod("ExecuteBatch_ReturnsObjectIdCollection_ReturnsCorrectCollection")]
        public void ExecuteBatch_ReturnsObjectIdCollection_ReturnsCorrectCollection()
        {
            // Arrange
            // Act
            var actualIds = AutoCADContext.ExecuteBatch(context =>
            {
                var ids = new List<ObjectId>();
                using (var blockTableRecord = context.Transaction.GetObject(
                    context.Database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                {
                    // 创建实体并收集ObjectId
                    var line = new Line(new Point3d(0, 0, 0), new Point3d(10, 10, 0));
                    var circle = new Circle(new Point3d(5, 5, 0), Vector3d.ZAxis, 3);

                    blockTableRecord.AppendEntity(line);
                    context.Transaction.AddNewlyCreatedDBObject(line, true);
                    ids.Add(line.ObjectId);

                    blockTableRecord.AppendEntity(circle);
                    context.Transaction.AddNewlyCreatedDBObject(circle, true);
                    ids.Add(circle.ObjectId);
                }

                return ids;
            });

            // Assert
            Assert.IsNotNull(actualIds);
            Assert.AreEqual(2, actualIds.Count);
            Assert.IsFalse(actualIds[0].IsNull);
            Assert.IsFalse(actualIds[1].IsNull);
            Assert.AreNotEqual(actualIds[0], actualIds[1]);
        }

        [TestMethod("ExecuteSafely_WithReturnValue_ReturnsExpectedValue")]
        public void ExecuteSafely_WithReturnValue_ReturnsExpectedValue()
        {
            // Arrange
            var expectedValue = 42;

            // Act
            var actualValue = AutoCADContext.ExecuteSafely(() => expectedValue);

            // Assert
            Assert.AreEqual<int>(expectedValue, actualValue);
        }

        [TestMethod("GetObject_ValidObjectId_ReturnsCorrectObject")]
        public void GetObject_ValidObjectId_ReturnsCorrectObject()
        {
            // Arrange
            ObjectId lineId = ObjectId.Null;
            
            // First create an entity and get its ID
            AutoCADContext.ExecuteBatch(context =>
            {
                using (var blockTableRecord = context.Transaction.GetObject(
                    context.Database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                {
                    var line = new Line(new Point3d(0, 0, 0), new Point3d(100, 100, 0));
                    blockTableRecord.AppendEntity(line);
                    context.Transaction.AddNewlyCreatedDBObject(line, true);
                    lineId = line.ObjectId;
                }
            });

            // Act
            Line retrievedLine = null;
            AutoCADContext.ExecuteBatch(context =>
            {
                retrievedLine = context.GetObject<Line>(lineId, OpenMode.ForRead);
            });

            // Assert
            Assert.IsNotNull(retrievedLine);
            Assert.IsFalse(retrievedLine.ObjectId.IsNull);
            Assert.AreEqual<Point3d>(new Point3d(0, 0, 0), retrievedLine.StartPoint);
            Assert.AreEqual<Point3d>(new Point3d(100, 100, 0), retrievedLine.EndPoint);
        }

        [TestMethod("GetObject_NullObjectId_ReturnsNull")]
        public void GetObject_NullObjectId_ReturnsNull()
        {
            // Arrange
            var nullId = ObjectId.Null;

            // Act
            Line result = null;
            AutoCADContext.ExecuteBatch(context =>
            {
                result = context.GetObject<Line>(nullId, OpenMode.ForRead);
            });

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod("GetObject_ErasedObjectId_ReturnsNull")]
        public void GetObject_ErasedObjectId_ReturnsNull()
        {
            // Arrange
            ObjectId entityId = ObjectId.Null;
            
            // Create and immediately erase an entity
            AutoCADContext.ExecuteBatch(context =>
            {
                using (var blockTableRecord = context.Transaction.GetObject(
                    context.Database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                {
                    var line = new Line(new Point3d(0, 0, 0), new Point3d(100, 100, 0));
                    blockTableRecord.AppendEntity(line);
                    context.Transaction.AddNewlyCreatedDBObject(line, true);
                    entityId = line.ObjectId;
                    line.Erase(); // 立即删除实体
                }
            });

            // Act
            Line result = null;
            AutoCADContext.ExecuteBatch(context =>
            {
                result = context.GetObject<Line>(entityId, OpenMode.ForRead);
            });

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod("Commit_ValidTransaction_CommitsSuccessfully")]
        public void Commit_ValidTransaction_CommitsSuccessfully()
        {
            // Arrange & Act
            ObjectId lineId = ObjectId.Null;
            AutoCADContext.ExecuteBatch(context =>
            {
                using (var blockTableRecord = context.Transaction.GetObject(
                    context.Database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                {
                    var line = new Line(new Point3d(0, 0, 0), new Point3d(100, 100, 0));
                    blockTableRecord.AppendEntity(line);
                    context.Transaction.AddNewlyCreatedDBObject(line, true);
                    lineId = line.ObjectId;
                }
                // Context will be committed automatically when using ExecuteBatch
            });

            // Assert - 验证对象在事务提交后仍然存在
            Line retrievedLine = null;
            AutoCADContext.ExecuteBatch(context =>
            {
                retrievedLine = context.GetObject<Line>(lineId, OpenMode.ForRead);
            });
            
            Assert.IsNotNull(retrievedLine);
        }

        [TestMethod("Abort_ValidTransaction_AbortsSuccessfully")]
        public void Abort_ValidTransaction_AbortsSuccessfully()
        {
            // Arrange & Act
            ObjectId lineId = ObjectId.Null;
            try
            {
                AutoCADContext.ExecuteBatch(context =>
                {
                    using (var blockTableRecord = context.Transaction.GetObject(
                        context.Database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                    {
                        var line = new Line(new Point3d(0, 0, 0), new Point3d(100, 100, 0));
                        blockTableRecord.AppendEntity(line);
                        context.Transaction.AddNewlyCreatedDBObject(line, true);
                        lineId = line.ObjectId;
                    }
                    // 抛出异常以触发回滚
                    throw new InvalidOperationException("测试异常");
                });
            }
            catch (InvalidOperationException)
            {
                // 预期的异常
            }

            // Assert - 验证对象在事务中止后不存在
            Line result = null;
            AutoCADContext.ExecuteBatch(context =>
            {
                result = context.GetObject<Line>(lineId, OpenMode.ForRead);
            });
            
            Assert.IsNull(result);
        }
    }
}