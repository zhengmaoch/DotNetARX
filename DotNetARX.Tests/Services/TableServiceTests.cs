namespace DotNetARX.Tests.Services
{
    [TestClass("TableServiceTests")]
    public class TableServiceTests : TestBase
    {
        private TableService _tableService;
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

            _tableService = new TableService(
                _mockEventBus.Object,
                _mockPerformanceMonitor.Object,
                _mockLogger.Object);
        }

        private ObjectId CreateTestTable()
        {
            var position = new Point3d(0, 0, 0);
            var rows = 3;
            var columns = 3;
            var rowHeight = 10.0;
            var columnWidth = 50.0;
            return _tableService.CreateTable(position, rows, columns, rowHeight, columnWidth);
        }

        [TestMethod("CreateTable_ValidParameters_ReturnsValidObjectId")]
        public void CreateTable_ValidParameters_ReturnsValidObjectId()
        {
            // Arrange
            var position = new Point3d(0, 0, 0);
            var rows = 3;
            var columns = 4;
            var rowHeight = 10.0;
            var columnWidth = 50.0;

            // Act
            var result = _tableService.CreateTable(position, rows, columns, rowHeight, columnWidth);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);

            // 验证表格属性
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(result, OpenMode.ForRead) as Table;
                Assert.IsNotNull(table);
                Assert.AreEqual(rows, table.Rows);
                Assert.AreEqual(columns, table.Columns);
                Assert.AreEqual(position, table.Position);

                transaction.Commit();
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("CreateTable"), Times.Once());

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<TableEvent>()), Times.Once());
        }

        [TestMethod("CreateTable_MinimumSize_WorksCorrectly")]
        public void CreateTable_MinimumSize_WorksCorrectly()
        {
            // Arrange
            var position = new Point3d(10, 10, 0);
            var rows = 1;
            var columns = 1;
            var rowHeight = 5.0;
            var columnWidth = 20.0;

            // Act
            var result = _tableService.CreateTable(position, rows, columns, rowHeight, columnWidth);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod("CreateTable_LargeTable_WorksCorrectly")]
        public void CreateTable_LargeTable_WorksCorrectly()
        {
            // Arrange
            var position = new Point3d(100, 100, 0);
            var rows = 10;
            var columns = 8;
            var rowHeight = 15.0;
            var columnWidth = 75.0;

            // Act
            var result = _tableService.CreateTable(position, rows, columns, rowHeight, columnWidth);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod("CreateTable_ZeroRows_ReturnsFalse")]
        public void CreateTable_ZeroRows_ReturnsFalse()
        {
            // Arrange
            var position = new Point3d(0, 0, 0);
            var rows = 0;
            var columns = 3;
            var rowHeight = 10.0;
            var columnWidth = 50.0;

            // Act
            var result = _tableService.CreateTable(position, rows, columns, rowHeight, columnWidth);

            // Assert
            Assert.IsTrue(result.IsNull);
        }

        [TestMethod("CreateTable_ZeroColumns_ReturnsFalse")]
        public void CreateTable_ZeroColumns_ReturnsFalse()
        {
            // Arrange
            var position = new Point3d(0, 0, 0);
            var rows = 3;
            var columns = 0;
            var rowHeight = 10.0;
            var columnWidth = 50.0;

            // Act
            var result = _tableService.CreateTable(position, rows, columns, rowHeight, columnWidth);

            // Assert
            Assert.IsTrue(result.IsNull);
        }

        [TestMethod("SetCellText_ValidCell_ReturnsTrue")]
        public void SetCellText_ValidCell_ReturnsTrue()
        {
            // Arrange
            var tableId = CreateTestTable();
            var row = 0;
            var column = 0;
            var text = "Test Cell Text";

            // Act
            var result = _tableService.SetCellText(tableId, row, column, text);

            // Assert
            Assert.IsTrue(result);

            // 验证文本设置成功
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(tableId, OpenMode.ForRead) as Table;
                var cellText = table.Cells[row, column].TextString;
                Assert.AreEqual(text, cellText);

                transaction.Commit();
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("SetCellText"), Times.Once());
        }

        [TestMethod("SetCellText_AllCells_WorksCorrectly")]
        public void SetCellText_AllCells_WorksCorrectly()
        {
            // Arrange
            var tableId = CreateTestTable();
            var testTexts = new[,]
            {
                { "A1", "B1", "C1" },
                { "A2", "B2", "C2" },
                { "A3", "B3", "C3" }
            };

            // Act & Assert
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    var result = _tableService.SetCellText(tableId, row, col, testTexts[row, col]);
                    Assert.IsTrue(result, $"Setting text for cell [{row},{col}] failed");
                }
            }

            // 验证所有文本设置成功
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(tableId, OpenMode.ForRead) as Table;
                for (int row = 0; row < 3; row++)
                {
                    for (int col = 0; col < 3; col++)
                    {
                        var cellText = table.Cells[row, col].TextString;
                        Assert.AreEqual(testTexts[row, col], cellText);
                    }
                }

                transaction.Commit();
            }
        }

        [TestMethod("SetCellText_InvalidCell_ReturnsFalse")]
        public void SetCellText_InvalidCell_ReturnsFalse()
        {
            // Arrange
            var tableId = CreateTestTable();
            var row = 10; // 超出范围的行
            var column = 0;
            var text = "Test Cell Text";

            // Act
            var result = _tableService.SetCellText(tableId, row, column, text);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("SetCellText_NullText_ReturnsTrue")]
        public void SetCellText_NullText_ReturnsTrue()
        {
            // Arrange
            var tableId = CreateTestTable();
            var row = 0;
            var column = 0;
            string text = null;

            // Act
            var result = _tableService.SetCellText(tableId, row, column, text);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod("GetCellText_ValidCell_ReturnsText")]
        public void GetCellText_ValidCell_ReturnsText()
        {
            // Arrange
            var tableId = CreateTestTable();
            var row = 1;
            var column = 1;
            var expectedText = "Sample Text";
            _tableService.SetCellText(tableId, row, column, expectedText);

            // Act
            var actualText = _tableService.GetCellText(tableId, row, column);

            // Assert
            Assert.AreEqual(expectedText, actualText);
        }

        [TestMethod("GetCellText_InvalidCell_ReturnsEmptyString")]
        public void GetCellText_InvalidCell_ReturnsEmptyString()
        {
            // Arrange
            var tableId = CreateTestTable();
            var row = 10; // 超出范围的行
            var column = 0;

            // Act
            var text = _tableService.GetCellText(tableId, row, column);

            // Assert
            Assert.AreEqual(string.Empty, text);
        }

        [TestMethod("SetColumnWidth_ValidColumn_ReturnsTrue")]
        public void SetColumnWidth_ValidColumn_ReturnsTrue()
        {
            // Arrange
            var tableId = CreateTestTable();
            var column = 0;
            var width = 75.0;

            // Act
            var result = _tableService.SetColumnWidth(tableId, column, width);

            // Assert
            Assert.IsTrue(result);

            // 验证列宽设置成功
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(tableId, OpenMode.ForRead) as Table;
                Assert.AreEqual(width, table.Columns[column].Width, 1e-6);

                transaction.Commit();
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("SetColumnWidth"), Times.Once());
        }

        [TestMethod("SetColumnWidth_InvalidColumn_ReturnsFalse")]
        public void SetColumnWidth_InvalidColumn_ReturnsFalse()
        {
            // Arrange
            var tableId = CreateTestTable();
            var column = 10; // 超出范围的列
            var width = 75.0;

            // Act
            var result = _tableService.SetColumnWidth(tableId, column, width);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("SetRowHeight_ValidRow_ReturnsTrue")]
        public void SetRowHeight_ValidRow_ReturnsTrue()
        {
            // Arrange
            var tableId = CreateTestTable();
            var row = 1;
            var height = 15.0;

            // Act
            var result = _tableService.SetRowHeight(tableId, row, height);

            // Assert
            Assert.IsTrue(result);

            // 验证行高设置成功
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(tableId, OpenMode.ForRead) as Table;
                Assert.AreEqual(height, table.Rows[row].Height);

                transaction.Commit();
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("SetRowHeight"), Times.Once());
        }

        [TestMethod("SetRowHeight_InvalidRow_ReturnsFalse")]
        public void SetRowHeight_InvalidRow_ReturnsFalse()
        {
            // Arrange
            var tableId = CreateTestTable();
            var row = 10; // 超出范围的行
            var height = 15.0;

            // Act
            var result = _tableService.SetRowHeight(tableId, row, height);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("InsertRow_ValidPosition_ReturnsTrue")]
        public void InsertRow_ValidPosition_ReturnsTrue()
        {
            // Arrange
            var tableId = CreateTestTable(); // 创建3行3列的表格
            var position = 1; // 在第2行前插入

            // Act
            var result = _tableService.InsertRow(tableId, position);

            // Assert
            Assert.IsTrue(result);

            // 验证行数增加
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(tableId, OpenMode.ForRead) as Table;
                Assert.AreEqual(4, table.Rows); // 原来3行，插入1行后应为4行

                transaction.Commit();
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("InsertRow"), Times.Once());
        }

        [TestMethod("InsertColumn_ValidPosition_ReturnsTrue")]
        public void InsertColumn_ValidPosition_ReturnsTrue()
        {
            // Arrange
            var tableId = CreateTestTable(); // 创建3行3列的表格
            var position = 1; // 在第2列前插入

            // Act
            var result = _tableService.InsertColumn(tableId, position);

            // Assert
            Assert.IsTrue(result);

            // 验证列数增加
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(tableId, OpenMode.ForRead) as Table;
                Assert.AreEqual(4, table.Columns); // 原来3列，插入1列后应为4列

                transaction.Commit();
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("InsertColumn"), Times.Once());
        }

        [TestMethod("DeleteRow_ValidPosition_ReturnsTrue")]
        public void DeleteRow_ValidPosition_ReturnsTrue()
        {
            // Arrange
            var tableId = CreateTestTable(); // 创建3行3列的表格
            var position = 1; // 删除第2行

            // Act
            var result = _tableService.DeleteRow(tableId, position);

            // Assert
            Assert.IsTrue(result);

            // 验证行数减少
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(tableId, OpenMode.ForRead) as Table;
                Assert.AreEqual(2, table.Rows); // 原来3行，删除1行后应为2行

                transaction.Commit();
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DeleteRow"), Times.Once());
        }

        [TestMethod("DeleteColumn_ValidPosition_ReturnsTrue")]
        public void DeleteColumn_ValidPosition_ReturnsTrue()
        {
            // Arrange
            var tableId = CreateTestTable(); // 创建3行3列的表格
            var position = 1; // 删除第2列

            // Act
            var result = _tableService.DeleteColumn(tableId, position);

            // Assert
            Assert.IsTrue(result);

            // 验证列数减少
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(tableId, OpenMode.ForRead) as Table;
                Assert.AreEqual(2, table.Columns); // 原来3列，删除1列后应为2列

                transaction.Commit();
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("DeleteColumn"), Times.Once());
        }

        [TestMethod("GetTableInfo_ValidTable_ReturnsTableInfo")]
        public void GetTableInfo_ValidTable_ReturnsTableInfo()
        {
            // Arrange
            var tableId = CreateTestTable();
            var expectedRows = 3;
            var expectedColumns = 3;

            // Act
            var tableInfo = _tableService.GetTableInfo(tableId);

            // Assert
            Assert.IsNotNull(tableInfo);
            Assert.AreEqual(expectedRows, tableInfo.Rows);
            Assert.AreEqual(expectedColumns, tableInfo.Columns);
            Assert.IsFalse(tableInfo.ObjectId.IsNull);
        }

        [TestMethod("GetTableInfo_InvalidTable_ReturnsNull")]
        public void GetTableInfo_InvalidTable_ReturnsNull()
        {
            // Arrange
            var invalidId = ObjectId.Null;

            // Act
            var tableInfo = _tableService.GetTableInfo(invalidId);

            // Assert
            Assert.IsNull(tableInfo);
        }

        [TestMethod("SetTableTitle_ValidTable_ReturnsTrue")]
        public void SetTableTitle_ValidTable_ReturnsTrue()
        {
            // Arrange
            var tableId = CreateTestTable();
            var title = "Test Table Title";

            // Act
            var result = _tableService.SetTableTitle(tableId, title);

            // Assert
            Assert.IsTrue(result);

            // 验证标题设置成功
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(tableId, OpenMode.ForRead) as Table;
                Assert.AreEqual(title, table.Title);

                transaction.Commit();
            }
        }

        [TestMethod("SetTableTitle_NullTitle_ReturnsTrue")]
        public void SetTableTitle_NullTitle_ReturnsTrue()
        {
            // Arrange
            var tableId = CreateTestTable();
            string title = null;

            // Act
            var result = _tableService.SetTableTitle(tableId, title);

            // Assert
            Assert.IsTrue(result);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _tableService?.Dispose();
            base.TestCleanup();
        }
    }
}