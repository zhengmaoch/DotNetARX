namespace DotNetARX.Tests
{
    /// <summary>
    /// Tools类的单元测试
    /// </summary>
    [TestClass("ToolsTests")]
    public class ToolsTests
    {
        [TestInitialize]
        public void Initialize()
        {
            // 测试初始化逻辑
        }

        [TestCleanup]
        public void Cleanup()
        {
            // 测试清理逻辑
        }

        [TestMethod("测试数字字符串验证")]
        public void TestIsNumeric()
        {
            // 有效数字
            Assert.IsTrue("123".IsNumeric(), "整数应该被识别为数字");
            Assert.IsTrue("123.45".IsNumeric(), "小数应该被识别为数字");
            Assert.IsTrue("-123".IsNumeric(), "负整数应该被识别为数字");
            Assert.IsTrue("+123.45".IsNumeric(), "带正号的小数应该被识别为数字");
            Assert.IsTrue("0".IsNumeric(), "零应该被识别为数字");
            Assert.IsTrue("0.0".IsNumeric(), "零点零应该被识别为数字");

            // 无效数字
            Assert.IsFalse("abc".IsNumeric(), "字母不应该被识别为数字");
            Assert.IsFalse("12a3".IsNumeric(), "包含字母的字符串不应该被识别为数字");
            Assert.IsFalse("".IsNumeric(), "空字符串不应该被识别为数字");
            Assert.IsFalse(" ".IsNumeric(), "空白字符串不应该被识别为数字");
            Assert.IsFalse("12.34.56".IsNumeric(), "多个小数点的字符串不应该被识别为数字");
        }

        [TestMethod("测试整数字符串验证")]
        public void TestIsInt()
        {
            // 有效整数
            Assert.IsTrue("123".IsInt(), "正整数应该被识别为整数");
            Assert.IsTrue("-123".IsInt(), "负整数应该被识别为整数");
            Assert.IsTrue("0".IsInt(), "零应该被识别为整数");
            Assert.IsTrue("+123".IsInt(), "带正号的整数应该被识别为整数");

            // 无效整数
            Assert.IsFalse("123.45".IsInt(), "小数不应该被识别为整数");
            Assert.IsFalse("abc".IsInt(), "字母不应该被识别为整数");
            Assert.IsFalse("12a3".IsInt(), "包含字母的字符串不应该被识别为整数");
            Assert.IsFalse("".IsInt(), "空字符串不应该被识别为整数");
            Assert.IsFalse(" ".IsInt(), "空白字符串不应该被识别为整数");
        }

        [TestMethod("测试安全的数字转换")]
        public void TestToDoubleOrDefault()
        {
            // 有效转换
            Assert.AreEqual(123.0, "123".ToDoubleOrDefault(), "整数字符串转换");
            Assert.AreEqual(123.45, "123.45".ToDoubleOrDefault(), "小数字符串转换");
            Assert.AreEqual(-123.45, "-123.45".ToDoubleOrDefault(), "负数字符串转换");

            // 无效转换应返回默认值
            Assert.AreEqual(0.0, "abc".ToDoubleOrDefault(), "无效字符串应返回默认值0");
            Assert.AreEqual(999.0, "abc".ToDoubleOrDefault(999.0), "无效字符串应返回指定默认值");
        }

        [TestMethod("测试安全的整数转换")]
        public void TestToIntOrDefault()
        {
            // 有效转换
            Assert.AreEqual(123, "123".ToIntOrDefault(), "整数字符串转换");
            Assert.AreEqual(-123, "-123".ToIntOrDefault(), "负整数字符串转换");

            // 无效转换应返回默认值
            Assert.AreEqual(0, "abc".ToIntOrDefault(), "无效字符串应返回默认值0");
            Assert.AreEqual(999, "abc".ToIntOrDefault(999), "无效字符串应返回指定默认值");
        }

        [TestMethod("测试空白字符串检查")]
        public void TestIsNullOrWhiteSpace()
        {
            // 应该返回true的情况
            Assert.IsTrue(((string)null).IsNullOrWhiteSpace(), "null应该被识别为空白");
            Assert.IsTrue("".IsNullOrWhiteSpace(), "空字符串应该被识别为空白");
            Assert.IsTrue("   ".IsNullOrWhiteSpace(), "只包含空格的字符串应该被识别为空白");
            Assert.IsTrue("\t\n\r".IsNullOrWhiteSpace(), "只包含空白字符的字符串应该被识别为空白");

            // 应该返回false的情况
            Assert.IsFalse("abc".IsNullOrWhiteSpace(), "包含内容的字符串不应该被识别为空白");
            Assert.IsFalse(" abc ".IsNullOrWhiteSpace(), "包含内容但有前后空格的字符串不应该被识别为空白");
        }

        [TestMethod("测试当前路径获取")]
        public void TestGetCurrentPath()
        {
            var path = ToolsImproved.GetCurrentPath();
            Assert.IsNotNull(path, "路径不应该为null");
            Assert.IsFalse(string.IsNullOrWhiteSpace(path), "路径不应该为空");
        }

        [TestMethod("测试异常处理")]
        public void TestExceptionHandling()
        {
            // 测试异常被正确捕获并返回默认值
            var result = ToolsImproved.GetCurrentPath();
            // 由于这个方法有异常处理，即使出错也应该返回空字符串而不是抛出异常
            Assert.IsNotNull(result, "即使出错也应该返回非null值");
        }
    }
}