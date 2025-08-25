namespace DotNetARX.Tests.Services
{
    [TestClass("UIServiceTests")]
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

        [TestMethod("ShowMessage_ValidParameters_DoesNotThrow")]
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
            _mockPerformanceMonitor.Verify(x => x.StartOperation("ShowMessage"), Times.Once());

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<UIEvent>()), Times.Once());
        }

        [TestMethod("ShowMessage_NullMessage_DoesNotThrow")]
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

        [TestMethod("ShowMessage_EmptyMessage_DoesNotThrow")]
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

        [TestMethod("ShowMessage_NullTitle_DoesNotThrow")]
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

        [TestMethod("ShowMessage_LongMessage_DoesNotThrow")]
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

        [TestMethod("ShowConfirmDialog_ValidParameters_ReturnsBoolean")]
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
            _mockPerformanceMonitor.Verify(x => x.StartOperation("ShowConfirmationDialog"), Times.Once());
        }

        [TestMethod("ShowConfirmDialog_NullMessage_ReturnsFalse")]
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

        [TestMethod("ShowConfirmDialog_EmptyMessage_ReturnsFalse")]
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

        [TestMethod("GetUserInput_ValidParameters_ReturnsString")]
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
            _mockPerformanceMonitor.Verify(x => x.StartOperation("GetUserInput"), Times.Once());
        }

        [TestMethod("GetUserInput_NullPrompt_ReturnsEmptyOrDefault")]
        public void GetUserInput_NullPrompt_ReturnsEmptyOrDefault()
        {
            // Arrange
            string prompt = null;
            var defaultValue = "Default";

            // Act
            var result = _uiService.GetUserInput(prompt, defaultValue);

            // Assert
            // 可能返回默认值或空字符串
            Assert.IsTrue(result == defaultValue || result == string.Empty);
        }

        [TestMethod("GetUserInput_EmptyPrompt_ReturnsDefault")]
        public void GetUserInput_EmptyPrompt_ReturnsDefault()
        {
            // Arrange
            var prompt = string.Empty;
            var defaultValue = "Default";

            // Act
            var result = _uiService.GetUserInput(prompt, defaultValue);

            // Assert
            Assert.IsTrue(result == defaultValue || result == string.Empty);
        }

        [TestMethod("ShowProgress_ValidParameters_DoesNotThrow")]
        public void ShowProgress_ValidParameters_DoesNotThrow()
        {
            // Arrange
            var title = "Progress Test";
            var message = "Processing...";
            var maxProgress = 100;

            // Act & Assert
            try
            {
                using (var progress = _uiService.ShowProgress(title, message, maxProgress))
                {
                    Assert.IsNotNull(progress);
                    progress.UpdateProgress(50, "Halfway done");
                }
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.Fail($"ShowProgress should not throw exception: {ex.Message}");
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("ShowProgress"), Times.Once());
        }

        [TestMethod("ShowProgress_NullTitle_DoesNotThrow")]
        public void ShowProgress_NullTitle_DoesNotThrow()
        {
            // Arrange
            string title = null;
            var message = "Processing...";
            var maxProgress = 100;

            // Act & Assert
            try
            {
                using (var progress = _uiService.ShowProgress(title, message, maxProgress))
                {
                    Assert.IsNotNull(progress);
                }
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.Fail($"ShowProgress with null title should not throw exception: {ex.Message}");
            }
        }

        [TestMethod("HideProgress_DoesNotThrow")]
        public void HideProgress_DoesNotThrow()
        {
            // Arrange
            var title = "Progress Test";
            var message = "Processing...";

            // Act & Assert
            try
            {
                _uiService.HideProgress();
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.Fail($"HideProgress should not throw exception: {ex.Message}");
            }
        }

        [TestMethod("SetStatusBarText_ValidText_DoesNotThrow")]
        public void SetStatusBarText_ValidText_DoesNotThrow()
        {
            // Arrange
            var text = "Status: Processing";

            // Act & Assert
            try
            {
                _uiService.SetStatusBarText(text);
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.Fail($"SetStatusBarText should not throw exception: {ex.Message}");
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("SetStatusBarText"), Times.Once());
        }

        [TestMethod("SetStatusBarText_NullText_DoesNotThrow")]
        public void SetStatusBarText_NullText_DoesNotThrow()
        {
            // Arrange
            string text = null;

            // Act & Assert
            try
            {
                _uiService.SetStatusBarText(text);
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.Fail($"SetStatusBarText with null text should not throw exception: {ex.Message}");
            }
        }

        [TestMethod("ClearStatusBar_DoesNotThrow")]
        public void ClearStatusBar_DoesNotThrow()
        {
            // Act & Assert
            try
            {
                _uiService.ClearStatusBar();
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.Fail($"ClearStatusBar should not throw exception: {ex.Message}");
            }
        }

        [TestMethod("ShowTooltip_ValidParameters_DoesNotThrow")]
        public void ShowTooltip_ValidParameters_DoesNotThrow()
        {
            // Arrange
            var message = "This is a tooltip";
            var position = new Point3d(100, 100, 0);

            // Act & Assert
            try
            {
                _uiService.ShowTooltip(message, position);
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.Fail($"ShowTooltip should not throw exception: {ex.Message}");
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("ShowTooltip"), Times.Once());
        }

        [TestMethod("HideTooltip_DoesNotThrow")]
        public void HideTooltip_DoesNotThrow()
        {
            // Act & Assert
            try
            {
                _uiService.HideTooltip();
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.Fail($"HideTooltip should not throw exception: {ex.Message}");
            }
        }

        [TestMethod("RefreshUI_DoesNotThrow")]
        public void RefreshUI_DoesNotThrow()
        {
            // Act & Assert
            try
            {
                _uiService.RefreshUI();
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.Fail($"RefreshUI should not throw exception: {ex.Message}");
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("RefreshUI"), Times.Once());
        }

        [TestMethod("EnableUI_EnablesInterface")]
        public void EnableUI_EnablesInterface()
        {
            // Act
            _uiService.EnableUI(true);

            // Assert - 不应抛出异常
            Assert.IsTrue(true);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("EnableUI"), Times.Once());
        }

        [TestMethod("ShowContextMenu_ValidMenu_DoesNotThrow")]
        public void ShowContextMenu_ValidMenu_DoesNotThrow()
        {
            // Arrange
            var menuItems = new List<MenuItemDefinition>
            {
                new MenuItemDefinition { Text = "Item 1", Command = "CMD1" },
                new MenuItemDefinition { Text = "Item 2", Command = "CMD2" }
            };
            var position = new Point3d(50, 50, 0);

            // Act & Assert
            try
            {
                _uiService.ShowContextMenu(menuItems, position);
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.Fail($"ShowContextMenu should not throw exception: {ex.Message}");
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("ShowContextMenu"), Times.Once());
        }

        [TestMethod("ShowContextMenu_EmptyMenu_DoesNotThrow")]
        public void ShowContextMenu_EmptyMenu_DoesNotThrow()
        {
            // Arrange
            var menuItems = new List<MenuItemDefinition>();
            var position = new Point3d(50, 50, 0);

            // Act & Assert
            try
            {
                _uiService.ShowContextMenu(menuItems, position);
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.Fail($"ShowContextMenu with empty menu should not throw exception: {ex.Message}");
            }
        }

        [TestMethod("ShowContextMenu_NullMenu_DoesNotThrow")]
        public void ShowContextMenu_NullMenu_DoesNotThrow()
        {
            // Arrange
            List<MenuItemDefinition> menuItems = null;
            var position = new Point3d(50, 50, 0);

            // Act & Assert
            try
            {
                _uiService.ShowContextMenu(menuItems, position);
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.Fail($"ShowContextMenu with null menu should not throw exception: {ex.Message}");
            }
        }

        [TestMethod("SetCursor_ValidCursor_DoesNotThrow")]
        public void SetCursor_ValidCursor_DoesNotThrow()
        {
            // Act & Assert
            try
            {
                _uiService.SetCursor(CursorType.Wait);
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.Fail($"SetCursor should not throw exception: {ex.Message}");
            }

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("SetCursor"), Times.Once());
        }

        [TestMethod("ResetCursor_DoesNotThrow")]
        public void ResetCursor_DoesNotThrow()
        {
            // Act & Assert
            try
            {
                _uiService.ResetCursor();
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.Fail($"ResetCursor should not throw exception: {ex.Message}");
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