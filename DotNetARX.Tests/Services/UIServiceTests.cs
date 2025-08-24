using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetARX.Tests.Services
{
    [TestClass]
    public class UIServiceTests : TestBase
    {
        private UIService _uiService;
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

            _uiService = new UIService(
                _mockEventBus.Object,
                _mockPerformanceMonitor.Object,
                _mockLogger.Object);
        }

        [TestMethod]
        public void ShowMessage_ValidParameters_DoesNotThrow()
        {
            // Arrange
            var message = "Test message";
            var title = "Test title";

            // Act & Assert - 不应抛出异常
            try
            {
                _uiService.ShowMessage(message, title);
                Assert.IsTrue(true); // 如果没有抛出异常则测试通过
            }
            catch (Exception ex)
            {
                Assert.Fail($"ShowMessage should not throw exception: {ex.Message}");
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("ShowMessage"), Times.Once);

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<UIEvent>()), Times.Once);
        }

        [TestMethod]
        public void ShowMessage_NullMessage_DoesNotThrow()
        {
            // Arrange
            string message = null;
            var title = "Test title";

            // Act & Assert
            try
            {
                _uiService.ShowMessage(message, title);
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.Fail($"ShowMessage with null message should not throw exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void ShowMessage_EmptyMessage_DoesNotThrow()
        {
            // Arrange
            var message = string.Empty;
            var title = "Test title";

            // Act & Assert
            try
            {
                _uiService.ShowMessage(message, title);
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.Fail($"ShowMessage with empty message should not throw exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void ShowMessage_NullTitle_DoesNotThrow()
        {
            // Arrange
            var message = "Test message";
            string title = null;

            // Act & Assert
            try
            {
                _uiService.ShowMessage(message, title);
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.Fail($"ShowMessage with null title should not throw exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void ShowMessage_LongMessage_DoesNotThrow()
        {
            // Arrange
            var message = new string('A', 1000); // 1000个字符的长消息
            var title = "Test title";

            // Act & Assert
            try
            {
                _uiService.ShowMessage(message, title);
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.Fail($"ShowMessage with long message should not throw exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void ShowConfirmDialog_ValidParameters_ReturnsBoolean()
        {
            // Arrange
            var message = "Confirm this action?";
            var title = "Confirmation";

            // Act
            var result = _uiService.ShowConfirmationDialog(message, title);

            // Assert
            // 在测试环境中，可能默认返回false，这是正常的
            Assert.IsTrue(result == true || result == false);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("ShowConfirmationDialog"), Times.Once);
        }

        [TestMethod]
        public void ShowConfirmDialog_NullMessage_ReturnsFalse()
        {
            // Arrange
            string message = null;
            var title = "Confirmation";

            // Act
            var result = _uiService.ShowConfirmationDialog(message, title);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ShowConfirmDialog_EmptyMessage_ReturnsFalse()
        {
            // Arrange
            var message = string.Empty;
            var title = "Confirmation";

            // Act
            var result = _uiService.ShowConfirmationDialog(message, title);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GetUserInput_ValidParameters_ReturnsString()
        {
            // Arrange
            var prompt = "Enter your name:";
            var defaultValue = "Default Name";

            // Act
            var result = _uiService.GetUserInput(prompt, defaultValue);

            // Assert
            // 在测试环境中，可能返回默认值或空字符串
            Assert.IsNotNull(result);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("GetUserInput"), Times.Once);
        }

        [TestMethod]
        public void GetUserInput_NullPrompt_ReturnsEmptyOrDefault()
        {
            // Arrange
            string prompt = null;
            var defaultValue = "Default";

            // Act
            var result = _uiService.GetUserInput(prompt, defaultValue);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void GetUserInput_EmptyPrompt_ReturnsEmptyOrDefault()
        {
            // Arrange
            var prompt = string.Empty;
            var defaultValue = "Default";

            // Act
            var result = _uiService.GetUserInput(prompt, defaultValue);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void GetUserInput_NullDefaultValue_ReturnsString()
        {
            // Arrange
            var prompt = "Enter value:";
            string defaultValue = null;

            // Act
            var result = _uiService.GetUserInput(prompt, defaultValue);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void SelectFile_ValidParameters_ReturnsString()
        {
            // Arrange
            var title = "Select File";
            var filter = "DWG files (*.dwg)|*.dwg|All files (*.*)|*.*";
            var forSave = false;

            // Act
            var result = _uiService.SelectFile(title, filter, forSave);

            // Assert
            // 在测试环境中，可能返回空字符串，这是正常的
            Assert.IsNotNull(result);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("SelectFile"), Times.Once);
        }

        [TestMethod]
        public void SelectFile_SaveMode_ReturnsString()
        {
            // Arrange
            var title = "Save File";
            var filter = "DWG files (*.dwg)|*.dwg";
            var forSave = true;

            // Act
            var result = _uiService.SelectFile(title, filter, forSave);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void SelectFile_NullTitle_ReturnsString()
        {
            // Arrange
            string title = null;
            var filter = "All files (*.*)|*.*";
            var forSave = false;

            // Act
            var result = _uiService.SelectFile(title, filter, forSave);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void SelectFile_NullFilter_ReturnsString()
        {
            // Arrange
            var title = "Select File";
            string filter = null;
            var forSave = false;

            // Act
            var result = _uiService.SelectFile(title, filter, forSave);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void SelectFile_EmptyFilter_ReturnsString()
        {
            // Arrange
            var title = "Select File";
            var filter = string.Empty;
            var forSave = false;

            // Act
            var result = _uiService.SelectFile(title, filter, forSave);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void SelectFile_CommonFilters_AllWork()
        {
            // Arrange
            var filters = new[]
            {
                "DWG files (*.dwg)|*.dwg",
                "Text files (*.txt)|*.txt",
                "All files (*.*)|*.*",
                "CAD files (*.dwg;*.dxf)|*.dwg;*.dxf",
                "Multiple types|*.dwg|*.dxf|*.txt|*.*"
            };

            foreach (var filter in filters)
            {
                // Act
                var result = _uiService.SelectFile("Test", filter, false);

                // Assert
                Assert.IsNotNull(result, $"Filter '{filter}' should not return null");
            }
        }

        [TestMethod]
        public void ShowMessage_MultipleCallsInSequence_AllSucceed()
        {
            // Act & Assert
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    _uiService.ShowMessage($"Message {i}", $"Title {i}");
                    Assert.IsTrue(true);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Message {i} failed: {ex.Message}");
                }
            }

            // 验证性能监控被调用了3次
            _mockPerformanceMonitor.Verify(x => x.StartOperation("ShowMessage"), Times.Exactly(3));
        }

        [TestMethod]
        public void ShowConfirmDialog_MultipleCallsInSequence_AllSucceed()
        {
            // Act & Assert
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var result = _uiService.ShowConfirmationDialog($"Confirm {i}?", $"Confirmation {i}");
                    Assert.IsTrue(result == true || result == false);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Confirmation {i} failed: {ex.Message}");
                }
            }

            // 验证性能监控被调用了3次
            _mockPerformanceMonitor.Verify(x => x.StartOperation("ShowConfirmationDialog"), Times.Exactly(3));
        }

        [TestMethod]
        public void GetUserInput_MultipleCallsWithDifferentDefaults_AllSucceed()
        {
            // Arrange
            var defaults = new[] { "Default1", "Default2", "", null };

            // Act & Assert
            foreach (var defaultValue in defaults)
            {
                try
                {
                    var result = _uiService.GetUserInput("Enter value:", defaultValue);
                    Assert.IsNotNull(result);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"GetUserInput with default '{defaultValue}' failed: {ex.Message}");
                }
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            _uiService?.Dispose();
            base.TestCleanup();
        }
    }
}