namespace DotNetARX.Tests.Services
{
    [TestClass]
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

        [TestMethod]
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
            _mockPerformanceMonitor.Verify(x => x.StartOperation("CreateTable"), Times.Once);

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<TableEvent>()), Times.Once);
        }

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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
            _mockPerformanceMonitor.Verify(x => x.StartOperation("SetCellText"), Times.Once);
        }

        [TestMethod]
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

        [TestMethod]
        public void SetCellText_InvalidTableId_ReturnsFalse()
        {
            // Arrange
            var invalidTableId = ObjectId.Null;
            var row = 0;
            var column = 0;
            var text = "Test Text";

            // Act
            var result = _tableService.SetCellText(invalidTableId, row, column, text);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SetCellText_InvalidRowIndex_ReturnsFalse()
        {
            // Arrange
            var tableId = CreateTestTable();
            var row = 10; // 超出范围
            var column = 0;
            var text = "Test Text";

            // Act
            var result = _tableService.SetCellText(tableId, row, column, text);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SetCellText_InvalidColumnIndex_ReturnsFalse()
        {
            // Arrange
            var tableId = CreateTestTable();
            var row = 0;
            var column = 10; // 超出范围
            var text = "Test Text";

            // Act
            var result = _tableService.SetCellText(tableId, row, column, text);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GetCellText_ValidCell_ReturnsCorrectText()
        {
            // Arrange
            var tableId = CreateTestTable();
            var row = 1;
            var column = 1;
            var expectedText = "Expected Text";

            // 先设置文本
            _tableService.SetCellText(tableId, row, column, expectedText);

            // Act
            var result = _tableService.GetCellText(tableId, row, column);

            // Assert
            Assert.AreEqual(expectedText, result);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("GetCellText"), Times.Once);
        }

        [TestMethod]
        public void GetCellText_EmptyCell_ReturnsEmptyString()
        {
            // Arrange
            var tableId = CreateTestTable();
            var row = 2;
            var column = 2;

            // Act
            var result = _tableService.GetCellText(tableId, row, column);

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void GetCellText_InvalidTableId_ReturnsNull()
        {
            // Arrange
            var invalidTableId = ObjectId.Null;
            var row = 0;
            var column = 0;

            // Act
            var result = _tableService.GetCellText(invalidTableId, row, column);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void MergeCells_ValidRange_ReturnsTrue()
        {
            // Arrange
            var tableId = CreateTestTable();
            var startRow = 0;
            var startColumn = 0;
            var endRow = 1;
            var endColumn = 1;

            // Act
            var result = _tableService.MergeCells(tableId, startRow, startColumn, endRow, endColumn);

            // Assert
            Assert.IsTrue(result);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("MergeCells"), Times.Once);
        }

        [TestMethod]
        public void MergeCells_SingleCell_ReturnsTrue()
        {
            // Arrange
            var tableId = CreateTestTable();
            var startRow = 1;
            var startColumn = 1;
            var endRow = 1;
            var endColumn = 1;

            // Act
            var result = _tableService.MergeCells(tableId, startRow, startColumn, endRow, endColumn);

            // Assert
            Assert.IsTrue(result); // 合并单个单元格应该成功
        }

        [TestMethod]
        public void MergeCells_InvalidTableId_ReturnsFalse()
        {
            // Arrange
            var invalidTableId = ObjectId.Null;
            var startRow = 0;
            var startColumn = 0;
            var endRow = 1;
            var endColumn = 1;

            // Act
            var result = _tableService.MergeCells(invalidTableId, startRow, startColumn, endRow, endColumn);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MergeCells_InvalidRange_ReturnsFalse()
        {
            // Arrange
            var tableId = CreateTestTable();
            var startRow = 2;
            var startColumn = 2;
            var endRow = 1; // 结束行小于开始行
            var endColumn = 1; // 结束列小于开始列

            // Act
            var result = _tableService.MergeCells(tableId, startRow, startColumn, endRow, endColumn);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MergeCells_OutOfBounds_ReturnsFalse()
        {
            // Arrange
            var tableId = CreateTestTable();
            var startRow = 0;
            var startColumn = 0;
            var endRow = 10; // 超出表格范围
            var endColumn = 10; // 超出表格范围

            // Act
            var result = _tableService.MergeCells(tableId, startRow, startColumn, endRow, endColumn);

            // Assert
            Assert.IsFalse(result);
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

        [TestCleanup]
        public void Cleanup()
        {
            _tableService?.Dispose();
            base.TestCleanup();
        }
    }
}