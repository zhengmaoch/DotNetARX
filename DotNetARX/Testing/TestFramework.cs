using System.Diagnostics;

namespace DotNetARX.Testing
{
    /// <summary>
    /// 测试方法属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TestMethodAttribute : Attribute
    {
        public string Description { get; set; }
        public int Timeout { get; set; } = 30000; // 30秒默认超时

        public TestMethodAttribute(string description = null)
        {
            Description = description;
        }
    }

    /// <summary>
    /// 测试初始化属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TestInitializeAttribute : Attribute
    { }

    /// <summary>
    /// 测试清理属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TestCleanupAttribute : Attribute
    { }

    /// <summary>
    /// 测试类属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TestClassAttribute : Attribute
    {
        public string Description { get; set; }

        public TestClassAttribute(string description = null)
        {
            Description = description;
        }
    }

    /// <summary>
    /// 测试结果
    /// </summary>
    public enum TestResult
    {
        Passed,
        Failed,
        Skipped,
        Timeout
    }

    /// <summary>
    /// 测试执行结果
    /// </summary>
    public class TestExecutionResult
    {
        public string TestName { get; set; }
        public string Description { get; set; }
        public TestResult Result { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public Exception Exception { get; set; }
        public string ErrorMessage { get; set; }

        public bool IsSuccess => Result == TestResult.Passed;
    }

    /// <summary>
    /// 测试套件结果
    /// </summary>
    public class TestSuiteResult
    {
        public string SuiteName { get; set; }
        public List<TestExecutionResult> TestResults { get; set; }
        public TimeSpan TotalExecutionTime { get; set; }
        public int TotalTests => TestResults.Count;
        public int PassedTests => TestResults.Count(r => r.Result == TestResult.Passed);
        public int FailedTests => TestResults.Count(r => r.Result == TestResult.Failed);
        public int SkippedTests => TestResults.Count(r => r.Result == TestResult.Skipped);
        public double PassRate => TotalTests > 0 ? (double)PassedTests / TotalTests * 100 : 0;

        public TestSuiteResult()
        {
            TestResults = new List<TestExecutionResult>();
        }
    }

    /// <summary>
    /// 断言类
    /// </summary>
    public static class Assert
    {
        /// <summary>
        /// 断言为真
        /// </summary>
        public static void IsTrue(bool condition, string message = null)
        {
            if (!condition)
            {
                throw new AssertionException(message ?? "断言失败：条件不为真");
            }
        }

        /// <summary>
        /// 断言为假
        /// </summary>
        public static void IsFalse(bool condition, string message = null)
        {
            if (condition)
            {
                throw new AssertionException(message ?? "断言失败：条件不为假");
            }
        }

        /// <summary>
        /// 断言相等
        /// </summary>
        public static void AreEqual<T>(T expected, T actual, string message = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new AssertionException(message ?? $"断言失败：期望值 '{expected}'，实际值 '{actual}'");
            }
        }

        /// <summary>
        /// 断言不相等
        /// </summary>
        public static void AreNotEqual<T>(T expected, T actual, string message = null)
        {
            if (EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new AssertionException(message ?? $"断言失败：值不应该相等 '{actual}'");
            }
        }

        /// <summary>
        /// 断言为空
        /// </summary>
        public static void IsNull(object value, string message = null)
        {
            if (value != null)
            {
                throw new AssertionException(message ?? "断言失败：值不为null");
            }
        }

        /// <summary>
        /// 断言不为空
        /// </summary>
        public static void IsNotNull(object value, string message = null)
        {
            if (value == null)
            {
                throw new AssertionException(message ?? "断言失败：值为null");
            }
        }

        /// <summary>
        /// 断言抛出异常
        /// </summary>
        public static T ThrowsException<T>(Action action, string message = null) where T : Exception
        {
            try
            {
                action();
                throw new AssertionException(message ?? $"断言失败：期望抛出异常 {typeof(T).Name}");
            }
            catch (T expectedException)
            {
                return expectedException;
            }
            catch (Exception actualException)
            {
                throw new AssertionException(message ??
                    $"断言失败：期望异常 {typeof(T).Name}，实际异常 {actualException.GetType().Name}");
            }
        }

        /// <summary>
        /// 断言字符串包含
        /// </summary>
        public static void Contains(string expectedSubstring, string actualString, string message = null)
        {
            if (actualString == null || !actualString.Contains(expectedSubstring))
            {
                throw new AssertionException(message ??
                    $"断言失败：字符串 '{actualString}' 不包含 '{expectedSubstring}'");
            }
        }

        /// <summary>
        /// 强制测试失败
        /// </summary>
        public static void Fail(string message = "测试失败")
        {
            throw new AssertionException(message);
        }
    }

    /// <summary>
    /// 断言异常
    /// </summary>
    public class AssertionException : Exception
    {
        public AssertionException(string message) : base(message)
        {
        }

        public AssertionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// 简单的测试运行器
    /// </summary>
    public class TestRunner
    {
        private readonly ILogger _logger;

        public TestRunner(ILogger logger = null)
        {
            _logger = logger ?? LogManager.GetLogger(typeof(TestRunner));
        }

        /// <summary>
        /// 运行指定类型的所有测试
        /// </summary>
        public TestSuiteResult RunTests(Type testClass)
        {
            var result = new TestSuiteResult
            {
                SuiteName = testClass.Name
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 创建测试类实例
                var testInstance = Activator.CreateInstance(testClass);

                // 获取测试方法
                var testMethods = GetTestMethods(testClass);
                var initializeMethod = GetInitializeMethod(testClass);
                var cleanupMethod = GetCleanupMethod(testClass);

                foreach (var method in testMethods)
                {
                    var testResult = RunSingleTest(testInstance, method, initializeMethod, cleanupMethod);
                    result.TestResults.Add(testResult);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"运行测试套件失败: {testClass.Name}", ex);
                result.TestResults.Add(new TestExecutionResult
                {
                    TestName = testClass.Name,
                    Result = TestResult.Failed,
                    Exception = ex,
                    ErrorMessage = ex.Message
                });
            }

            stopwatch.Stop();
            result.TotalExecutionTime = stopwatch.Elapsed;

            LogTestSuiteResult(result);
            return result;
        }

        /// <summary>
        /// 运行所有标记为测试类的类型
        /// </summary>
        public List<TestSuiteResult> RunAllTests(Assembly assembly = null)
        {
            assembly = assembly ?? Assembly.GetCallingAssembly();
            var results = new List<TestSuiteResult>();

            var testClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
                .ToList();

            _logger.Info($"发现 {testClasses.Count} 个测试类");

            foreach (var testClass in testClasses)
            {
                var result = RunTests(testClass);
                results.Add(result);
            }

            LogOverallResults(results);
            return results;
        }

        private TestExecutionResult RunSingleTest(object testInstance, MethodInfo testMethod,
            MethodInfo initializeMethod, MethodInfo cleanupMethod)
        {
            var testAttribute = testMethod.GetCustomAttribute<TestMethodAttribute>();
            var result = new TestExecutionResult
            {
                TestName = testMethod.Name,
                Description = testAttribute?.Description ?? testMethod.Name
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 执行初始化
                initializeMethod?.Invoke(testInstance, null);

                // 执行测试方法
                testMethod.Invoke(testInstance, null);

                result.Result = TestResult.Passed;
                _logger.Debug($"测试通过: {result.TestName}");
            }
            catch (TargetInvocationException ex) when (ex.InnerException is AssertionException)
            {
                result.Result = TestResult.Failed;
                result.Exception = ex.InnerException;
                result.ErrorMessage = ex.InnerException.Message;
                _logger.Warning($"测试失败: {result.TestName} - {result.ErrorMessage}");
            }
            catch (Exception ex)
            {
                result.Result = TestResult.Failed;
                result.Exception = ex;
                result.ErrorMessage = ex.Message;
                _logger.Error($"测试异常: {result.TestName}", ex);
            }
            finally
            {
                try
                {
                    // 执行清理
                    cleanupMethod?.Invoke(testInstance, null);
                }
                catch (Exception ex)
                {
                    _logger.Error($"测试清理失败: {result.TestName}", ex);
                }

                stopwatch.Stop();
                result.ExecutionTime = stopwatch.Elapsed;
            }

            return result;
        }

        private MethodInfo[] GetTestMethods(Type testClass)
        {
            return testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
                .ToArray();
        }

        private MethodInfo GetInitializeMethod(Type testClass)
        {
            return testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.GetCustomAttribute<TestInitializeAttribute>() != null);
        }

        private MethodInfo GetCleanupMethod(Type testClass)
        {
            return testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.GetCustomAttribute<TestCleanupAttribute>() != null);
        }

        private void LogTestSuiteResult(TestSuiteResult result)
        {
            _logger.Info($"测试套件完成: {result.SuiteName}");
            _logger.Info($"  总计: {result.TotalTests}, 通过: {result.PassedTests}, " +
                        $"失败: {result.FailedTests}, 跳过: {result.SkippedTests}");
            _logger.Info($"  通过率: {result.PassRate:F1}%, 执行时间: {result.TotalExecutionTime.TotalMilliseconds}ms");
        }

        private void LogOverallResults(List<TestSuiteResult> results)
        {
            var totalTests = results.Sum(r => r.TotalTests);
            var totalPassed = results.Sum(r => r.PassedTests);
            var totalFailed = results.Sum(r => r.FailedTests);
            var totalTime = TimeSpan.FromTicks(results.Sum(r => r.TotalExecutionTime.Ticks));

            _logger.Info("=== 测试执行总结 ===");
            _logger.Info($"测试套件数: {results.Count}");
            _logger.Info($"总测试数: {totalTests}");
            _logger.Info($"通过: {totalPassed}");
            _logger.Info($"失败: {totalFailed}");
            _logger.Info($"通过率: {(totalTests > 0 ? (double)totalPassed / totalTests * 100 : 0):F1}%");
            _logger.Info($"总执行时间: {totalTime.TotalMilliseconds}ms");
        }
    }
}