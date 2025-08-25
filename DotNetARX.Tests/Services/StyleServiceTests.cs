namespace DotNetARX.Tests.Services
{
    [TestClass]
    public class StyleServiceTests : TestBase
    {
        private StyleService _styleService;
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

            _styleService = new StyleService(
                _mockEventBus.Object,
                _mockPerformanceMonitor.Object,
                _mockLogger.Object);
        }

        [TestMethod]
        public void CreateTextStyle_ValidParameters_ReturnsValidObjectId()
        {
            // Arrange
            var styleName = "TestTextStyle";
            var fontName = "Arial";
            var textSize = 2.5;

            // Act
            var result = _styleService.CreateTextStyle(styleName, fontName, textSize);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);

            // 验证样式是否创建成功
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var textStyleTable = transaction.GetObject(TestDatabase.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
                Assert.IsTrue(textStyleTable.Has(styleName));

                var textStyleRecord = transaction.GetObject(textStyleTable[styleName], OpenMode.ForRead) as TextStyleTableRecord;
                Assert.AreEqual(fontName, textStyleRecord.Font.TypeFace);
                Assert.AreEqual(textSize, textStyleRecord.TextSize, 1e-6);

                transaction.Commit();
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("CreateTextStyle"), Times.Once);

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<StyleEvent>()), Times.Once);
        }

        [TestMethod]
        public void CreateTextStyle_DuplicateName_ReturnsExistingStyle()
        {
            // Arrange
            var styleName = "DuplicateTestStyle";
            var fontName = "Arial";
            var textSize = 2.5;

            // Act - 创建第一个样式
            var result1 = _styleService.CreateTextStyle(styleName, fontName, textSize);

            // Act - 尝试创建同名样式
            var result2 = _styleService.CreateTextStyle(styleName, fontName, textSize);

            // Assert
            Assert.IsFalse(result1.IsNull);
            Assert.IsFalse(result2.IsNull);
            Assert.AreEqual(result1, result2); // 应该返回相同的ObjectId
        }

        [TestMethod]
        public void CreateTextStyle_NullStyleName_ReturnsFalse()
        {
            // Arrange
            string styleName = null;
            var fontName = "Arial";
            var textSize = 2.5;

            // Act
            var result = _styleService.CreateTextStyle(styleName, fontName, textSize);

            // Assert
            Assert.IsTrue(result.IsNull);
        }

        [TestMethod]
        public void CreateTextStyle_EmptyStyleName_ReturnsFalse()
        {
            // Arrange
            var styleName = string.Empty;
            var fontName = "Arial";
            var textSize = 2.5;

            // Act
            var result = _styleService.CreateTextStyle(styleName, fontName, textSize);

            // Assert
            Assert.IsTrue(result.IsNull);
        }

        [TestMethod]
        public void CreateDimStyle_ValidParameters_ReturnsValidObjectId()
        {
            // Arrange
            var styleName = "TestDimStyle";
            var textHeight = 2.5;
            var arrowSize = 1.0;

            // Act
            var result = _styleService.CreateDimStyle(styleName, textHeight, arrowSize);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);

            // 验证标注样式是否创建成功
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var dimStyleTable = transaction.GetObject(TestDatabase.DimStyleTableId, OpenMode.ForRead) as DimStyleTable;
                Assert.IsTrue(dimStyleTable.Has(styleName));

                var dimStyleRecord = transaction.GetObject(dimStyleTable[styleName], OpenMode.ForRead) as DimStyleTableRecord;
                Assert.AreEqual(textHeight, dimStyleRecord.Dimtxt, 1e-6);
                Assert.AreEqual(arrowSize, dimStyleRecord.Dimasz, 1e-6);

                transaction.Commit();
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("CreateDimStyle"), Times.Once);
        }

        [TestMethod]
        public void CreateDimStyle_DuplicateName_ReturnsExistingStyle()
        {
            // Arrange
            var styleName = "DuplicateDimStyle";
            var textHeight = 2.5;
            var arrowSize = 1.0;

            // Act - 创建第一个标注样式
            var result1 = _styleService.CreateDimStyle(styleName, textHeight, arrowSize);

            // Act - 尝试创建同名标注样式
            var result2 = _styleService.CreateDimStyle(styleName, textHeight, arrowSize);

            // Assert
            Assert.IsFalse(result1.IsNull);
            Assert.IsFalse(result2.IsNull);
            Assert.AreEqual(result1, result2);
        }

        [TestMethod]
        public void CreateLineType_ValidParameters_ReturnsValidObjectId()
        {
            // Arrange
            var linetypeName = "TestLineType";
            var pattern = "A,0.5,-0.25,0,-0.25";
            var description = "Test Line Type Description";

            // Act
            var result = _styleService.CreateLineType(linetypeName, pattern, description);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);

            // 验证线型是否创建成功
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var linetypeTable = transaction.GetObject(TestDatabase.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;
                Assert.IsTrue(linetypeTable.Has(linetypeName));

                var linetypeRecord = transaction.GetObject(linetypeTable[linetypeName], OpenMode.ForRead) as LinetypeTableRecord;
                Assert.AreEqual(description, linetypeRecord.Comments);

                transaction.Commit();
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("CreateLineType"), Times.Once);
        }

        [TestMethod]
        public void CreateLineType_SimplePattern_WorksCorrectly()
        {
            // Arrange
            var linetypeName = "SimpleTestLineType";
            var pattern = "A,0.5,-0.25";
            var description = "Simple Test Line Type";

            // Act
            var result = _styleService.CreateLineType(linetypeName, pattern, description);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void CreateLineType_DuplicateName_ReturnsExistingLineType()
        {
            // Arrange
            var linetypeName = "DuplicateLineType";
            var pattern = "A,0.5,-0.25";
            var description = "Duplicate Line Type";

            // Act - 创建第一个线型
            var result1 = _styleService.CreateLineType(linetypeName, pattern, description);

            // Act - 尝试创建同名线型
            var result2 = _styleService.CreateLineType(linetypeName, pattern, description);

            // Assert
            Assert.IsFalse(result1.IsNull);
            Assert.IsFalse(result2.IsNull);
            Assert.AreEqual(result1, result2);
        }

        [TestMethod]
        public void CreateLineType_InvalidPattern_ReturnsFalse()
        {
            // Arrange
            var linetypeName = "InvalidPatternLineType";
            var pattern = "InvalidPattern"; // 无效的线型模式
            var description = "Invalid Pattern Line Type";

            // Act
            var result = _styleService.CreateLineType(linetypeName, pattern, description);

            // Assert
            Assert.IsTrue(result.IsNull);
        }

        [TestMethod]
        public void CreateTextStyle_ZeroTextSize_WorksCorrectly()
        {
            // Arrange
            var styleName = "ZeroSizeTextStyle";
            var fontName = "Arial";
            var textSize = 0.0; // 变高度文字样式

            // Act
            var result = _styleService.CreateTextStyle(styleName, fontName, textSize);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);

            // 验证文字样式
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var textStyleTable = transaction.GetObject(TestDatabase.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
                Assert.IsTrue(textStyleTable.Has(styleName));

                var textStyleRecord = transaction.GetObject(textStyleTable[styleName], OpenMode.ForRead) as TextStyleTableRecord;
                Assert.AreEqual(0.0, textStyleRecord.TextSize, 1e-6);

                transaction.Commit();
            }
        }

        [TestMethod]
        public void CreateDimStyle_ZeroValues_WorksCorrectly()
        {
            // Arrange
            var styleName = "ZeroValuesDimStyle";
            var textHeight = 0.0;
            var arrowSize = 0.0;

            // Act
            var result = _styleService.CreateDimStyle(styleName, textHeight, arrowSize);

            // Assert
            Assert.IsFalse(result.IsNull);
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void CreateTextStyle_DifferentFonts_AllWork()
        {
            // Arrange
            var fonts = new[] { "Arial", "Times New Roman", "Courier New" };

            foreach (var font in fonts)
            {
                var styleName = $"TestStyle_{font.Replace(" ", "")}";

                // Act
                var result = _styleService.CreateTextStyle(styleName, font, 2.5);

                // Assert
                Assert.IsFalse(result.IsNull, $"Font {font} should work");
                Assert.IsTrue(result.IsValid, $"Font {font} should create valid style");
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            _styleService?.Dispose();
            base.TestCleanup();
        }
    }
}