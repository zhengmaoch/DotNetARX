namespace DotNetARX.Tests.Services
{
    [TestClass("StyleServiceTests")]
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

        [TestMethod("CreateTextStyle_ValidParameters_ReturnsValidObjectId")]
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
            _mockPerformanceMonitor.Verify(x => x.StartOperation("CreateTextStyle"), Times.Once());

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<StyleEvent>()), Times.Once());
        }

        [TestMethod("CreateTextStyle_DuplicateName_ReturnsExistingStyle")]
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

        [TestMethod("CreateTextStyle_NullStyleName_ReturnsFalse")]
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

        [TestMethod("CreateTextStyle_EmptyStyleName_ReturnsFalse")]
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

        [TestMethod("CreateDimStyle_ValidParameters_ReturnsValidObjectId")]
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
            _mockPerformanceMonitor.Verify(x => x.StartOperation("CreateDimStyle"), Times.Once());
        }

        [TestMethod("CreateDimStyle_DuplicateName_ReturnsExistingStyle")]
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

        [TestMethod("CreateLineType_ValidParameters_ReturnsValidObjectId")]
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
            _mockPerformanceMonitor.Verify(x => x.StartOperation("CreateLineType"), Times.Once());
        }

        [TestMethod("CreateLineType_DuplicateName_ReturnsExistingStyle")]
        public void CreateLineType_DuplicateName_ReturnsExistingStyle()
        {
            // Arrange
            var linetypeName = "DuplicateLineType";
            var pattern = "A,0.5,-0.25,0,-0.25";
            var description = "Test Line Type Description";

            // Act - 创建第一个线型
            var result1 = _styleService.CreateLineType(linetypeName, pattern, description);

            // Act - 尝试创建同名线型
            var result2 = _styleService.CreateLineType(linetypeName, pattern, description);

            // Assert
            Assert.IsFalse(result1.IsNull);
            Assert.IsFalse(result2.IsNull);
            Assert.AreEqual(result1, result2);
        }

        [TestMethod("GetAllTextStyles_ReturnsStyleCollection")]
        public void GetAllTextStyles_ReturnsStyleCollection()
        {
            // Arrange
            var testStyles = new[] { "Style1", "Style2", "Style3" };
            foreach (var styleName in testStyles)
            {
                _styleService.CreateTextStyle(styleName, "Arial", 2.5);
            }

            // Act
            var styles = _styleService.GetAllTextStyles().ToList();

            // Assert
            Assert.IsNotNull(styles);
            Assert.IsTrue(styles.Count >= testStyles.Length);
            Assert.IsTrue(testStyles.All(name => styles.Any(style => style.Name == name)));
        }

        [TestMethod("GetAllDimStyles_ReturnsStyleCollection")]
        public void GetAllDimStyles_ReturnsStyleCollection()
        {
            // Arrange
            var testStyles = new[] { "DimStyle1", "DimStyle2", "DimStyle3" };
            foreach (var styleName in testStyles)
            {
                _styleService.CreateDimStyle(styleName, 2.5, 1.0);
            }

            // Act
            var styles = _styleService.GetAllDimStyles().ToList();

            // Assert
            Assert.IsNotNull(styles);
            Assert.IsTrue(styles.Count >= testStyles.Length);
            Assert.IsTrue(testStyles.All(name => styles.Any(style => style.Name == name)));
        }

        [TestMethod("GetAllLineTypes_ReturnsStyleCollection")]
        public void GetAllLineTypes_ReturnsStyleCollection()
        {
            // Arrange
            var testTypes = new[] { "LineType1", "LineType2", "LineType3" };
            foreach (var typeName in testTypes)
            {
                _styleService.CreateLineType(typeName, "A,0.5,-0.25,0,-0.25", "Test Description");
            }

            // Act
            var types = _styleService.GetAllLineTypes().ToList();

            // Assert
            Assert.IsNotNull(types);
            Assert.IsTrue(types.Count >= testTypes.Length);
            Assert.IsTrue(testTypes.All(name => types.Any(type => type.Name == name)));
        }

        [TestMethod("TextStyleExists_ExistingStyle_ReturnsTrue")]
        public void TextStyleExists_ExistingStyle_ReturnsTrue()
        {
            // Arrange
            var styleName = "ExistTestStyle";
            _styleService.CreateTextStyle(styleName, "Arial", 2.5);

            // Act
            var result = _styleService.TextStyleExists(styleName);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod("TextStyleExists_NonExistentStyle_ReturnsFalse")]
        public void TextStyleExists_NonExistentStyle_ReturnsFalse()
        {
            // Act
            var result = _styleService.TextStyleExists("NonExistentStyle");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("DimStyleExists_ExistingStyle_ReturnsTrue")]
        public void DimStyleExists_ExistingStyle_ReturnsTrue()
        {
            // Arrange
            var styleName = "ExistDimStyle";
            _styleService.CreateDimStyle(styleName, 2.5, 1.0);

            // Act
            var result = _styleService.DimStyleExists(styleName);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod("DimStyleExists_NonExistentStyle_ReturnsFalse")]
        public void DimStyleExists_NonExistentStyle_ReturnsFalse()
        {
            // Act
            var result = _styleService.DimStyleExists("NonExistentDimStyle");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("LineTypeExists_ExistingType_ReturnsTrue")]
        public void LineTypeExists_ExistingType_ReturnsTrue()
        {
            // Arrange
            var typeName = "ExistLineType";
            _styleService.CreateLineType(typeName, "A,0.5,-0.25,0,-0.25", "Test Description");

            // Act
            var result = _styleService.LineTypeExists(typeName);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod("LineTypeExists_NonExistentType_ReturnsFalse")]
        public void LineTypeExists_NonExistentType_ReturnsFalse()
        {
            // Act
            var result = _styleService.LineTypeExists("NonExistentLineType");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("GetTextStyleInfo_ExistingStyle_ReturnsStyleInfo")]
        public void GetTextStyleInfo_ExistingStyle_ReturnsStyleInfo()
        {
            // Arrange
            var styleName = "InfoTestStyle";
            var fontName = "Times New Roman";
            var textSize = 3.0;
            _styleService.CreateTextStyle(styleName, fontName, textSize);

            // Act
            var styleInfo = _styleService.GetTextStyleInfo(styleName);

            // Assert
            Assert.IsNotNull(styleInfo);
            Assert.AreEqual(styleName, styleInfo.Name);
            Assert.AreEqual(fontName, styleInfo.FontName);
            Assert.AreEqual(textSize, styleInfo.TextSize, 1e-6);
        }

        [TestMethod("GetTextStyleInfo_NonExistentStyle_ReturnsNull")]
        public void GetTextStyleInfo_NonExistentStyle_ReturnsNull()
        {
            // Act
            var styleInfo = _styleService.GetTextStyleInfo("NonExistentStyle");

            // Assert
            Assert.IsNull(styleInfo);
        }

        [TestMethod("GetDimStyleInfo_ExistingStyle_ReturnsStyleInfo")]
        public void GetDimStyleInfo_ExistingStyle_ReturnsStyleInfo()
        {
            // Arrange
            var styleName = "InfoDimStyle";
            var textHeight = 3.5;
            var arrowSize = 1.5;
            _styleService.CreateDimStyle(styleName, textHeight, arrowSize);

            // Act
            var styleInfo = _styleService.GetDimStyleInfo(styleName);

            // Assert
            Assert.IsNotNull(styleInfo);
            Assert.AreEqual(styleName, styleInfo.Name);
            Assert.AreEqual(textHeight, styleInfo.TextHeight, 1e-6);
            Assert.AreEqual(arrowSize, styleInfo.ArrowSize, 1e-6);
        }

        [TestMethod("GetDimStyleInfo_NonExistentStyle_ReturnsNull")]
        public void GetDimStyleInfo_NonExistentStyle_ReturnsNull()
        {
            // Act
            var styleInfo = _styleService.GetDimStyleInfo("NonExistentDimStyle");

            // Assert
            Assert.IsNull(styleInfo);
        }

        [TestMethod("SetCurrentTextStyle_ExistingStyle_ReturnsTrue")]
        public void SetCurrentTextStyle_ExistingStyle_ReturnsTrue()
        {
            // Arrange
            var styleName = "CurrentTestStyle";
            _styleService.CreateTextStyle(styleName, "Arial", 2.5);

            // Act
            var result = _styleService.SetCurrentTextStyle(styleName);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod("SetCurrentTextStyle_NonExistentStyle_ReturnsFalse")]
        public void SetCurrentTextStyle_NonExistentStyle_ReturnsFalse()
        {
            // Act
            var result = _styleService.SetCurrentTextStyle("NonExistentStyle");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("SetCurrentDimStyle_ExistingStyle_ReturnsTrue")]
        public void SetCurrentDimStyle_ExistingStyle_ReturnsTrue()
        {
            // Arrange
            var styleName = "CurrentDimStyle";
            _styleService.CreateDimStyle(styleName, 2.5, 1.0);

            // Act
            var result = _styleService.SetCurrentDimStyle(styleName);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod("SetCurrentDimStyle_NonExistentStyle_ReturnsFalse")]
        public void SetCurrentDimStyle_NonExistentStyle_ReturnsFalse()
        {
            // Act
            var result = _styleService.SetCurrentDimStyle("NonExistentDimStyle");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("DeleteTextStyle_ExistingEmptyStyle_ReturnsTrue")]
        public void DeleteTextStyle_ExistingEmptyStyle_ReturnsTrue()
        {
            // Arrange
            var styleName = "DeleteTestStyle";
            _styleService.CreateTextStyle(styleName, "Arial", 2.5);

            // Act
            var result = _styleService.DeleteTextStyle(styleName);

            // Assert
            Assert.IsTrue(result);

            // 验证样式是否被删除
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var textStyleTable = transaction.GetObject(TestDatabase.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
                Assert.IsFalse(textStyleTable.Has(styleName));

                transaction.Commit();
            }
        }

        [TestMethod("DeleteTextStyle_NonExistentStyle_ReturnsFalse")]
        public void DeleteTextStyle_NonExistentStyle_ReturnsFalse()
        {
            // Act
            var result = _styleService.DeleteTextStyle("NonExistentStyle");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("DeleteDimStyle_ExistingEmptyStyle_ReturnsTrue")]
        public void DeleteDimStyle_ExistingEmptyStyle_ReturnsTrue()
        {
            // Arrange
            var styleName = "DeleteDimStyle";
            _styleService.CreateDimStyle(styleName, 2.5, 1.0);

            // Act
            var result = _styleService.DeleteDimStyle(styleName);

            // Assert
            Assert.IsTrue(result);

            // 验证标注样式是否被删除
            using (var transaction = TestDatabase.TransactionManager.StartTransaction())
            {
                var dimStyleTable = transaction.GetObject(TestDatabase.DimStyleTableId, OpenMode.ForRead) as DimStyleTable;
                Assert.IsFalse(dimStyleTable.Has(styleName));

                transaction.Commit();
            }
        }

        [TestMethod("DeleteDimStyle_NonExistentStyle_ReturnsFalse")]
        public void DeleteDimStyle_NonExistentStyle_ReturnsFalse()
        {
            // Act
            var result = _styleService.DeleteDimStyle("NonExistentDimStyle");

            // Assert
            Assert.IsFalse(result);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _styleService?.Dispose();
            base.TestCleanup();
        }
    }
}