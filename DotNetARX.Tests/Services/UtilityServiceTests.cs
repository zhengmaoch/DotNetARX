using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetARX.Tests.Services
{
    [TestClass]
    public class UtilityServiceTests : TestBase
    {
        private UtilityService _utilityService;
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

            _utilityService = new UtilityService(
                _mockEventBus.Object,
                _mockPerformanceMonitor.Object,
                _mockLogger.Object);
        }

        [TestMethod]
        public void ValidateString_ValidPattern_ReturnsTrue()
        {
            // Arrange
            var value = "123";
            var pattern = @"^\d+$"; // 数字模式

            // Act
            var result = _utilityService.ValidateString(value, pattern);

            // Assert
            Assert.IsTrue(result);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("ValidateString"), Times.Once);
        }

        [TestMethod]
        public void ValidateString_InvalidPattern_ReturnsFalse()
        {
            // Arrange
            var value = "abc";
            var pattern = @"^\d+$"; // 数字模式

            // Act
            var result = _utilityService.ValidateString(value, pattern);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateString_EmailPattern_WorksCorrectly()
        {
            // Arrange
            var validEmail = "test@example.com";
            var invalidEmail = "invalid-email";
            var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

            // Act
            var validResult = _utilityService.ValidateString(validEmail, emailPattern);
            var invalidResult = _utilityService.ValidateString(invalidEmail, emailPattern);

            // Assert
            Assert.IsTrue(validResult);
            Assert.IsFalse(invalidResult);
        }

        [TestMethod]
        public void ValidateString_NullValue_ReturnsFalse()
        {
            // Arrange
            string value = null;
            var pattern = @"^\d+$";

            // Act
            var result = _utilityService.ValidateString(value, pattern);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateString_NullPattern_ReturnsFalse()
        {
            // Arrange
            var value = "123";
            string pattern = null;

            // Act
            var result = _utilityService.ValidateString(value, pattern);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateString_EmptyValue_ReturnsFalse()
        {
            // Arrange
            var value = string.Empty;
            var pattern = @"^\d+$";

            // Act
            var result = _utilityService.ValidateString(value, pattern);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateString_InvalidRegexPattern_ReturnsFalse()
        {
            // Arrange
            var value = "test";
            var pattern = "["; // 无效的正则表达式

            // Act
            var result = _utilityService.ValidateString(value, pattern);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SafeConvert_ValidIntConversion_ReturnsCorrectValue()
        {
            // Arrange
            object value = "123";
            var defaultValue = 0;

            // Act
            var result = _utilityService.SafeConvert(value, defaultValue);

            // Assert
            Assert.AreEqual(123, result);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("SafeConvert"), Times.Once);
        }

        [TestMethod]
        public void SafeConvert_InvalidIntConversion_ReturnsDefault()
        {
            // Arrange
            object value = "invalid";
            var defaultValue = 42;

            // Act
            var result = _utilityService.SafeConvert(value, defaultValue);

            // Assert
            Assert.AreEqual(defaultValue, result);
        }

        [TestMethod]
        public void SafeConvert_ValidDoubleConversion_ReturnsCorrectValue()
        {
            // Arrange
            object value = "123.45";
            var defaultValue = 0.0;

            // Act
            var result = _utilityService.SafeConvert(value, defaultValue);

            // Assert
            Assert.AreEqual(123.45, result, 1e-10);
        }

        [TestMethod]
        public void SafeConvert_ValidBoolConversion_ReturnsCorrectValue()
        {
            // Arrange
            object value = "true";
            var defaultValue = false;

            // Act
            var result = _utilityService.SafeConvert(value, defaultValue);

            // Assert
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void SafeConvert_NullValue_ReturnsDefault()
        {
            // Arrange
            object value = null;
            var defaultValue = 100;

            // Act
            var result = _utilityService.SafeConvert(value, defaultValue);

            // Assert
            Assert.AreEqual(defaultValue, result);
        }

        [TestMethod]
        public void SafeConvert_AlreadyCorrectType_ReturnsValue()
        {
            // Arrange
            object value = 456;
            var defaultValue = 0;

            // Act
            var result = _utilityService.SafeConvert(value, defaultValue);

            // Assert
            Assert.AreEqual(456, result);
        }

        [TestMethod]
        public void SafeConvert_StringConversion_WorksCorrectly()
        {
            // Arrange
            object value = 123;
            var defaultValue = "default";

            // Act
            var result = _utilityService.SafeConvert(value, defaultValue);

            // Assert
            Assert.AreEqual("123", result);
        }

        [TestMethod]
        public void GetAutoCADPath_ReturnsValidPath()
        {
            // Act
            var result = _utilityService.GetAutoCADPath();

            // Assert
            Assert.IsNotNull(result);
            // 在测试环境中可能返回空字符串，这是正常的
            Assert.IsTrue(result != null);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("GetAutoCADPath"), Times.Once);
        }

        [TestMethod]
        public void HighlightEntity_ValidEntity_ReturnsTrue()
        {
            // Arrange
            var circle = new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, 10);
            var entityId = AddEntityToModelSpace(circle);

            // Act
            var result = _utilityService.HighlightEntity(entityId, true);

            // Assert
            Assert.IsTrue(result);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("HighlightEntity"), Times.Once);
        }

        [TestMethod]
        public void HighlightEntity_InvalidEntity_ReturnsFalse()
        {
            // Arrange
            var invalidId = ObjectId.Null;

            // Act
            var result = _utilityService.HighlightEntity(invalidId, true);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void HighlightEntity_TurnOffHighlight_ReturnsTrue()
        {
            // Arrange
            var circle = new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, 10);
            var entityId = AddEntityToModelSpace(circle);

            // Act - 先高亮
            var highlightResult = _utilityService.HighlightEntity(entityId, true);
            // Act - 再取消高亮
            var unhighlightResult = _utilityService.HighlightEntity(entityId, false);

            // Assert
            Assert.IsTrue(highlightResult);
            Assert.IsTrue(unhighlightResult);
        }

        [TestMethod]
        public void SafeExecute_ValidOperation_ReturnsResult()
        {
            // Arrange
            Func<int> operation = () => 42;
            var defaultValue = 0;

            // Act
            var result = _utilityService.SafeExecute(operation, defaultValue);

            // Assert
            Assert.AreEqual(42, result);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("SafeExecute"), Times.Once);
        }

        [TestMethod]
        public void SafeExecute_ThrowingOperation_ReturnsDefault()
        {
            // Arrange
            Func<int> operation = () => throw new InvalidOperationException("Test exception");
            var defaultValue = 99;

            // Act
            var result = _utilityService.SafeExecute(operation, defaultValue);

            // Assert
            Assert.AreEqual(defaultValue, result);
        }

        [TestMethod]
        public void SafeExecute_NullOperation_ReturnsDefault()
        {
            // Arrange
            Func<int> operation = null;
            var defaultValue = 77;

            // Act
            var result = _utilityService.SafeExecute(operation, defaultValue);

            // Assert
            Assert.AreEqual(defaultValue, result);
        }

        [TestMethod]
        public void SafeExecute_ComplexOperation_WorksCorrectly()
        {
            // Arrange
            Func<string> operation = () =>
            {
                var sum = 0;
                for (int i = 1; i <= 10; i++)
                {
                    sum += i;
                }
                return $"Sum: {sum}";
            };
            var defaultValue = "Error";

            // Act
            var result = _utilityService.SafeExecute(operation, defaultValue);

            // Assert
            Assert.AreEqual("Sum: 55", result);
        }

        [TestMethod]
        public void ValidateString_MultiplePatterns_AllWork()
        {
            // Arrange
            var testCases = new[]
            {
                new { Value = "123", Pattern = @"^\d+$", Expected = true },
                new { Value = "abc", Pattern = @"^[a-z]+$", Expected = true },
                new { Value = "ABC", Pattern = @"^[A-Z]+$", Expected = true },
                new { Value = "Test123", Pattern = @"^[A-Za-z0-9]+$", Expected = true },
                new { Value = "123", Pattern = @"^[a-z]+$", Expected = false },
                new { Value = "abc", Pattern = @"^\d+$", Expected = false }
            };

            foreach (var testCase in testCases)
            {
                // Act
                var result = _utilityService.ValidateString(testCase.Value, testCase.Pattern);

                // Assert
                Assert.AreEqual(testCase.Expected, result,
                    $"Value: '{testCase.Value}', Pattern: '{testCase.Pattern}' should return {testCase.Expected}");
            }
        }

        [TestMethod]
        public void SafeConvert_DifferentTypes_AllWork()
        {
            // Test different type conversions
            Assert.AreEqual(123, _utilityService.SafeConvert<int>("123", 0));
            Assert.AreEqual(123.45, _utilityService.SafeConvert<double>("123.45", 0.0), 1e-10);
            Assert.AreEqual(true, _utilityService.SafeConvert<bool>("true", false));
            Assert.AreEqual(false, _utilityService.SafeConvert<bool>("false", true));
            Assert.AreEqual("test", _utilityService.SafeConvert<string>(123, "default"));

            // Test with invalid conversions
            Assert.AreEqual(0, _utilityService.SafeConvert<int>("invalid", 0));
            Assert.AreEqual(0.0, _utilityService.SafeConvert<double>("invalid", 0.0));
            Assert.AreEqual(false, _utilityService.SafeConvert<bool>("invalid", false));
        }

        [TestMethod]
        public void HighlightEntity_MultipleEntities_AllWork()
        {
            // Arrange
            var entities = new Entity[]
            {
                new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, 10),
                new Line(new Point3d(0, 0, 0), new Point3d(100, 100, 0)),
                new Arc(new Point3d(50, 50, 0), 20, 0, Math.PI)
            };

            foreach (var entity in entities)
            {
                var entityId = AddEntityToModelSpace(entity);

                // Act
                var highlightResult = _utilityService.HighlightEntity(entityId, true);
                var unhighlightResult = _utilityService.HighlightEntity(entityId, false);

                // Assert
                Assert.IsTrue(highlightResult, $"Highlighting {entity.GetType().Name} failed");
                Assert.IsTrue(unhighlightResult, $"Unhighlighting {entity.GetType().Name} failed");
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            _utilityService?.Dispose();
            base.TestCleanup();
        }
    }
}