using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Diagnostics;

namespace DotNetARX.Tests
{
    /// <summary>
    /// 测试基类，提供共享的测试基础设施
    /// </summary>
    public class TestBase
    {
        protected Database TestDatabase { get; private set; }
        protected Transaction TestTransaction { get; private set; }

        /// <summary>
        /// 测试初始化
        /// </summary>
        public virtual void TestInitialize()
        {
            // 创建测试数据库
            TestDatabase = new Database(true, true);
            TestTransaction = TestDatabase.TransactionManager.StartTransaction();
        }

        /// <summary>
        /// 测试清理
        /// </summary>
        public virtual void TestCleanup()
        {
            TestTransaction?.Dispose();
            TestDatabase?.Dispose();
        }

        /// <summary>
        /// 向模型空间添加实体
        /// </summary>
        protected ObjectId AddEntityToModelSpace(Entity entity)
        {
            using (var blockTable = TestTransaction.GetObject(TestDatabase.BlockTableId, OpenMode.ForRead) as BlockTable)
            using (var blockTableRecord = TestTransaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord)
            {
                blockTableRecord.AppendEntity(entity);
                TestTransaction.AddNewlyCreatedDBObject(entity, true);
                return entity.ObjectId;
            }
        }

        /// <summary>
        /// 创建测试用的模型空间块表记录
        /// </summary>
        protected BlockTableRecord GetModelSpace()
        {
            using (var blockTable = TestTransaction.GetObject(TestDatabase.BlockTableId, OpenMode.ForRead) as BlockTable)
            {
                return TestTransaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
            }
        }
    }
}