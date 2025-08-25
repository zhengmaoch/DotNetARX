using System;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNetARX.Interfaces;
using DotNetARX.Performance;
using DotNetARX.Logging;
using DotNetARX.Events;

namespace DotNetARX.Tests.Services
{
    [TestClass("CommandServiceTests")]
    public class CommandServiceTests : TestBase
    {
        private CommandService _commandService;
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

            _commandService = new CommandService(
                _mockEventBus.Object,
                _mockPerformanceMonitor.Object,
                _mockLogger.Object);
        }

        [TestMethod("ExecuteCommandCOM_ValidCommand_ReturnsTrue")]
        public void ExecuteCommandCOM_ValidCommand_ReturnsTrue()
        {
            // Arrange
            var command = "LINE";

            // Act
            var result = _commandService.ExecuteCommandCOM(command);

            // Assert
            Assert.IsTrue(result);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("ExecuteCommandCOM"), Times.Once());

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<EventArgs>()), Times.Once());
        }

        [TestMethod("ExecuteCommandCOM_NullCommand_ReturnsFalse")]
        public void ExecuteCommandCOM_NullCommand_ReturnsFalse()
        {
            // Arrange
            string command = null;

            // Act
            var result = _commandService.ExecuteCommandCOM(command);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("ExecuteCommandCOM_EmptyCommand_ReturnsFalse")]
        public void ExecuteCommandCOM_EmptyCommand_ReturnsFalse()
        {
            // Arrange
            var command = string.Empty;

            // Act
            var result = _commandService.ExecuteCommandCOM(command);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod("ExecuteCommandAsync_ValidCommand_ReturnsTrue")]
        public void ExecuteCommandAsync_ValidCommand_ReturnsTrue()
        {
            // Arrange
            var command = "ZOOM E";

            // Act
            var result = _commandService.ExecuteCommandAsync(command);

            // Assert
            Assert.IsTrue(result);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("ExecuteCommandAsync"), Times.Once());

            // 验证事件发布
            _mockEventBus.Verify(x => x.Publish(It.IsAny<EventArgs>()), Times.Once());
        }

        [TestMethod("ExecuteCommandQueue_ValidCommand_ReturnsTrue")]
        public void ExecuteCommandQueue_ValidCommand_ReturnsTrue()
        {
            // Arrange
            var command = "REGEN";

            // Act
            var result = _commandService.ExecuteCommandQueue(command);

            // Assert
            Assert.IsTrue(result);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("ExecuteCommandQueue"), Times.Once());
        }

        [TestMethod("ExecuteARXCommand_ValidCommand_ReturnsTrue")]
        public void ExecuteARXCommand_ValidCommand_ReturnsTrue()
        {
            // Arrange
            var command = "TESTCMD";
            var args = new string[] { "arg1", "arg2" };

            // Act
            var result = _commandService.ExecuteARXCommand(command, args);

            // Assert
            Assert.IsTrue(result);

            // 验证性能监控被调用
            _mockPerformanceMonitor.Verify(x => x.StartOperation("ExecuteARXCommand"), Times.Once());
        }

        [TestMethod("ExecuteARXCommand_NullArgs_ReturnsTrue")]
        public void ExecuteARXCommand_NullArgs_ReturnsTrue()
        {
            // Arrange
            var command = "TESTCMD";
            string[] args = null;

            // Act
            var result = _commandService.ExecuteARXCommand(command, args);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod("ExecuteARXCommand_EmptyArgs_ReturnsTrue")]
        public void ExecuteARXCommand_EmptyArgs_ReturnsTrue()
        {
            // Arrange
            var command = "TESTCMD";
            var args = new string[] { };

            // Act
            var result = _commandService.ExecuteARXCommand(command, args);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod("ExecuteCommandCOM_InvalidCommand_HandleException")]
        public void ExecuteCommandCOM_InvalidCommand_HandleException()
        {
            // Arrange
            var invalidCommand = "INVALIDCOMMAND12345";

            // Act & Assert - 不应抛出异常
            try
            {
                var result = _commandService.ExecuteCommandCOM(invalidCommand);
                // 即使命令无效，也应返回false而不是抛出异常
                Assert.IsFalse(result);
            }
            catch (Exception)
            {
                Assert.Fail("执行无效命令时不应抛出异常");
            }
        }

        [TestMethod("ExecuteCommandAsync_MultipleCommands_AllExecuted")]
        public void ExecuteCommandAsync_MultipleCommands_AllExecuted()
        {
            // Arrange
            var commands = new[] { "ZOOM E", "REGEN", "REDRAW" };

            // Act
            foreach (var command in commands)
            {
                var result = _commandService.ExecuteCommandAsync(command);
                Assert.IsTrue(result);
            }

            // Assert
            // 验证性能监控被调用了3次
            _mockPerformanceMonitor.Verify(x => x.StartOperation("ExecuteCommandAsync"), Times.Exactly(3));
        }

        [TestCleanup]
        public void Cleanup()
        {
            _commandService?.Dispose();
            base.TestCleanup();
        }
    }
}