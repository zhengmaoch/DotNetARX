namespace DotNetARX.Examples
{
    /// <summary>
    /// DotNetARX 统一API使用示例
    /// 展示易用性与高性能的完美结合 - 零配置，开箱即用
    /// </summary>
    public static class UnifiedApiExamples
    {
        /// <summary>
        /// 基础绘图示例 - 超简洁API
        /// </summary>
        public static void BasicDrawingExample()
        {
            // ✨ 无需初始化，无需配置，直接使用！

            // 绘制基本图形 - 每个调用都是零开销的高性能实现
            var lineId = Line(Point3d.Origin, new Point3d(100, 100, 0));
            var circleId = Circle(new Point3d(50, 50, 0), 25);
            var arcId = Arc(new Point3d(75, 75, 0), 15, 0, Math.PI);
            var textId = Text("DotNetARX", new Point3d(10, 110, 0), 5);

            // 创建图层并设置为当前 - 智能缓存，自动去重
            CreateLayer("图形层", 1);
            SetCurrentLayer("图形层");

            Console.WriteLine($"绘制完成！创建了 {4} 个图形对象");
        }

        /// <summary>
        /// 实体操作示例 - 智能优化
        /// </summary>
        public static void EntityOperationExample()
        {
            // 创建测试实体
            var lineId = Line(Point3d.Origin, new Point3d(100, 0, 0));
            var circleId = Circle(new Point3d(50, 50, 0), 25);

            // 实体变换 - 自动参数验证，智能退出条件
            Move(lineId, Point3d.Origin, new Point3d(50, 50, 0));
            var copyId = Copy(circleId, new Point3d(50, 50, 0), new Point3d(100, 100, 0));
            Rotate(copyId, new Point3d(100, 100, 0), Math.PI / 4);
            Scale(copyId, new Point3d(100, 100, 0), 1.5);

            Console.WriteLine("实体操作完成！所有操作都是高性能的");
        }

        /// <summary>
        /// 批量操作示例 - 智能批处理优化
        /// </summary>
        public static void BatchOperationExample()
        {
            // 创建大量实体进行批量操作
            var entities = new List<Entity>();
            for (int i = 0; i < 1000; i++)
            {
                entities.Add(new Line(
                    new Point3d(i * 10, 0, 0),
                    new Point3d(i * 10, 100, 0)));
            }

            // 批量绘制 - 系统自动优化为批处理
            var ids = Draw(entities);
            Console.WriteLine($"批量绘制 {ids.Count} 个实体");

            // 批量移动 - 智能分组优化，减少矩阵计算
            var moveOperations = ids.Cast<ObjectId>().Select((id, index) =>
                (id, new Point3d(index * 10, 0, 0), new Point3d(index * 10, 200, 0))).ToList();

            Move(moveOperations);
            Console.WriteLine($"批量移动 {moveOperations.Count} 个实体 - 自动批处理优化");
        }

        /// <summary>
        /// 高级查询示例 - 高性能LINQ
        /// </summary>
        public static void AdvancedQueryExample()
        {
            // 按类型查询 - 内置性能优化
            var allLines = SelectByType<Line>();
            var allCircles = SelectByType<Circle>();

            // 智能过滤查询 - 支持复杂条件
            var longLines = Select<Line>(line => line.Length > 50);
            var largeCircles = Select<Circle>(circle => circle.Radius > 20);

            Console.WriteLine($"查询结果：直线 {allLines.Count} 个，圆 {allCircles.Count} 个");
            Console.WriteLine($"长直线 {longLines.Count} 个，大圆 {largeCircles.Count} 个");
        }

        /// <summary>
        /// 图层管理示例 - 智能缓存
        /// </summary>
        public static void LayerManagementExample()
        {
            // 创建多个图层 - 自动缓存，避免重复创建
            var layerConfigs = new[]
            {
                ("建筑", (short)1),  // 红色
                ("结构", (short)2),  // 黄色
                ("设备", (short)3),  // 绿色
                ("电气", (short)4),  // 青色
            };

            foreach (var (name, color) in layerConfigs)
            {
                var layerId = CreateLayer(name, color);
                Console.WriteLine($"图层 '{name}' 创建成功，ID: {layerId}");
            }

            // 检查图层存在性 - 利用缓存，快速响应
            foreach (var (name, _) in layerConfigs)
            {
                var exists = LayerExists(name);
                Console.WriteLine($"图层 '{name}' 存在: {exists}");
            }

            // 设置当前图层
            SetCurrentLayer("建筑");
            Console.WriteLine("当前图层设置为：建筑");
        }

        /// <summary>
        /// 性能监控示例 - 内置分析
        /// </summary>
        public static void PerformanceMonitoringExample()
        {
            // 执行一些操作
            BasicDrawingExample();
            EntityOperationExample();

            // 查看性能报告 - 零配置的性能分析
            var report = GetPerformanceReport();
            Console.WriteLine("=== 性能监控报告 ===");
            Console.WriteLine(report);

            // 重置统计（可选）
            ResetPerformanceMetrics();
            Console.WriteLine("性能统计已重置");
        }

        /// <summary>
        /// 复合操作示例 - 展示API的强大组合能力
        /// </summary>
        public static void ComplexOperationExample()
        {
            Console.WriteLine("开始复合操作示例...");

            // 1. 创建专用图层
            CreateLayer("复合操作", 5);
            SetCurrentLayer("复合操作");

            // 2. 创建基础图形
            var baseRect = new List<Entity>
            {
                new Line(Point3d.Origin, new Point3d(100, 0, 0)),
                new Line(new Point3d(100, 0, 0), new Point3d(100, 100, 0)),
                new Line(new Point3d(100, 100, 0), new Point3d(0, 100, 0)),
                new Line(new Point3d(0, 100, 0), Point3d.Origin)
            };

            var rectIds = Draw(baseRect);
            Console.WriteLine($"创建基础矩形，{rectIds.Count} 个线段");

            // 3. 批量复制和变换
            var copies = new List<(ObjectId, Point3d, Point3d)>();
            for (int i = 1; i <= 5; i++)
            {
                foreach (ObjectId id in rectIds)
                {
                    var copyId = Copy(id, Point3d.Origin, new Point3d(i * 120, 0, 0));
                    copies.Add((copyId, new Point3d(i * 120, 0, 0), new Point3d(i * 120, i * 20, 0)));
                }
            }

            // 4. 批量移动所有副本
            Move(copies);
            Console.WriteLine($"批量复制并移动了 {copies.Count} 个对象");

            // 5. 添加标注文本
            for (int i = 1; i <= 5; i++)
            {
                Text($"副本 {i}", new Point3d(i * 120 + 10, i * 20 + 110, 0), 8);
            }

            // 6. 查看最终性能报告
            var finalReport = GetPerformanceReport();
            Console.WriteLine("\n=== 复合操作性能报告 ===");
            Console.WriteLine(finalReport);
        }

        /// <summary>
        /// 运行所有示例
        /// </summary>
        public static void RunAllExamples()
        {
            Console.WriteLine("🚀 DotNetARX 统一API示例演示");
            Console.WriteLine("易用性与高性能的完美结合 - 零配置，开箱即用！");
            Console.WriteLine(new string('=', 60));

            try
            {
                Console.WriteLine("\n1. 基础绘图示例");
                BasicDrawingExample();

                Console.WriteLine("\n2. 实体操作示例");
                EntityOperationExample();

                Console.WriteLine("\n3. 批量操作示例");
                BatchOperationExample();

                Console.WriteLine("\n4. 高级查询示例");
                AdvancedQueryExample();

                Console.WriteLine("\n5. 图层管理示例");
                LayerManagementExample();

                Console.WriteLine("\n6. 性能监控示例");
                PerformanceMonitoringExample();

                Console.WriteLine("\n7. 复合操作示例");
                ComplexOperationExample();

                Console.WriteLine(new string('=', 60));
                Console.WriteLine("✅ 所有示例运行完成！");
                Console.WriteLine("📊 查看上面的性能报告了解系统自动优化效果");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 示例执行失败: {ex.Message}");
                Console.WriteLine("🔍 这可能是因为没有活动的AutoCAD文档");
            }
        }
    }
}