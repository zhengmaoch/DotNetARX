using DotNetARX.Testing;
using DotNetARX.Tests.Services;
using System.Reflection;

namespace DotNetARX.Tests
{
    /// <summary>
    /// 测试运行程序
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("DotNetARX 测试运行程序");
            Console.WriteLine("========================");

            // 创建测试运行器
            var runner = new TestRunner();

            // 运行AutoCADContextTests
            var result = runner.RunTests(typeof(AutoCADContextTests));

            // 输出结果
            Console.WriteLine($"\n测试套件: {result.SuiteName}");
            Console.WriteLine($"总计: {result.TotalTests}, 通过: {result.PassedTests}, 失败: {result.FailedTests}");
            Console.WriteLine($"通过率: {result.PassRate:F1}%");
            Console.WriteLine($"执行时间: {result.TotalExecutionTime.TotalMilliseconds}ms");

            foreach (var testResult in result.TestResults)
            {
                var status = testResult.IsSuccess ? "✓" : "✗";
                Console.WriteLine($"  {status} {testResult.TestName}: {testResult.Result} ({testResult.ExecutionTime.TotalMilliseconds}ms)");
                if (!testResult.IsSuccess && testResult.Exception != null)
                {
                    Console.WriteLine($"    错误: {testResult.Exception.Message}");
                }
            }

            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}