using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using DotNetARX.DependencyInjection;

namespace DotNetARX.Performance.Benchmarks
{
    /// <summary>
    /// CAD 操作性能基准测试
    /// </summary>
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class CADOperationBenchmarks : DotNetARXBenchmarkBase
    {
        private ObjectId _testLineId;
        private ObjectId _testCircleId;
        private Point3d _sourcePoint;
        private Point3d _targetPoint;

        [GlobalSetup]
        public override void Setup()
        {
            base.Setup();

            // 初始化测试数据
            _sourcePoint = Point3d.Origin;
            _targetPoint = new Point3d(100, 100, 0);

            // 创建测试实体
            try
            {
                _testLineId = CAD.Line(_sourcePoint, _targetPoint);
                _testCircleId = CAD.Circle(new Point3d(50, 50, 0), 25);
            }
            catch (Exception ex)
            {
                _logger.Warning($"基准测试设置失败 (可能没有活动文档): {ex.Message}");
            }
        }

        [Benchmark]
        [BenchmarkCategory("Drawing")]
        public ObjectId CreateLine()
        {
            return CAD.Line(Point3d.Origin, new Point3d(100, 100, 0));
        }

        [Benchmark]
        [BenchmarkCategory("Drawing")]
        public ObjectId CreateCircle()
        {
            return CAD.Circle(new Point3d(50, 50, 0), 25);
        }

        [Benchmark]
        [BenchmarkCategory("Drawing")]
        public ObjectId CreateArc()
        {
            return CAD.Arc(new Point3d(0, 0, 0), 30, 0, Math.PI);
        }

        [Benchmark]
        [BenchmarkCategory("Transform")]
        public bool MoveEntity()
        {
            if (_testLineId.IsNull) return false;
            return CAD.Move(_testLineId, _sourcePoint, _targetPoint);
        }

        [Benchmark]
        [BenchmarkCategory("Transform")]
        public ObjectId CopyEntity()
        {
            if (_testLineId.IsNull) return ObjectId.Null;
            return CAD.Copy(_testLineId, _sourcePoint, _targetPoint);
        }

        [Benchmark]
        [BenchmarkCategory("Transform")]
        public bool RotateEntity()
        {
            if (_testLineId.IsNull) return false;
            return CAD.Rotate(_testLineId, _sourcePoint, Math.PI / 4);
        }

        [Benchmark]
        [BenchmarkCategory("Transform")]
        public bool ScaleEntity()
        {
            if (_testLineId.IsNull) return false;
            return CAD.Scale(_testLineId, _sourcePoint, 1.5);
        }

        [Benchmark]
        [BenchmarkCategory("Query")]
        public List<ObjectId> SelectLinesByType()
        {
            return CAD.SelectByType<Line>();
        }

        [Benchmark]
        [BenchmarkCategory("Query")]
        public List<ObjectId> SelectCirclesByType()
        {
            return CAD.SelectByType<Circle>();
        }

        [Benchmark]
        [BenchmarkCategory("Layer")]
        public ObjectId CreateLayer()
        {
            return CAD.CreateLayer($"TestLayer_{DateTime.Now.Ticks}", 1);
        }

        [Benchmark]
        [BenchmarkCategory("Layer")]
        public bool LayerExists()
        {
            return CAD.LayerExists("0");
        }
    }

    /// <summary>
    /// 缓存系统性能基准测试
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(launchCount: 1, warmupCount: 3, targetCount: 5)]
    public class CachePerformanceBenchmarks : DotNetARXBenchmarkBase
    {
        private ISmartCache<string, string> _smartCache;
        private ConcurrentDictionary<string, string> _concurrentDict;
        private readonly string[] _keys;
        private readonly string[] _values;

        public CachePerformanceBenchmarks()
        {
            // 准备测试数据
            _keys = Enumerable.Range(0, 1000).Select(i => $"key_{i}").ToArray();
            _values = Enumerable.Range(0, 1000).Select(i => $"value_{i}_{Guid.NewGuid()}").ToArray();
        }

        [GlobalSetup]
        public override void Setup()
        {
            base.Setup();
            _smartCache = SmartCacheManager.GetCache<string, string>("benchmark_cache");
            _concurrentDict = new ConcurrentDictionary<string, string>();
        }

        [Benchmark]
        [BenchmarkCategory("Cache_Write")]
        public void SmartCache_Set()
        {
            for (int i = 0; i < 100; i++)
            {
                _smartCache.Set(_keys[i], _values[i]);
            }
        }

        [Benchmark]
        [BenchmarkCategory("Cache_Write")]
        public void ConcurrentDict_Set()
        {
            for (int i = 0; i < 100; i++)
            {
                _concurrentDict.TryAdd(_keys[i], _values[i]);
            }
        }

        [Benchmark]
        [BenchmarkCategory("Cache_Read")]
        public void SmartCache_Get()
        {
            for (int i = 0; i < 100; i++)
            {
                _smartCache.TryGet(_keys[i % 50], out _);
            }
        }

        [Benchmark]
        [BenchmarkCategory("Cache_Read")]
        public void ConcurrentDict_Get()
        {
            for (int i = 0; i < 100; i++)
            {
                _concurrentDict.TryGetValue(_keys[i % 50], out _);
            }
        }

        [Benchmark]
        [BenchmarkCategory("Cache_Mixed")]
        public void SmartCache_Mixed()
        {
            for (int i = 0; i < 50; i++)
            {
                _smartCache.Set(_keys[i], _values[i]);
            }
            for (int i = 0; i < 50; i++)
            {
                _smartCache.TryGet(_keys[i % 25], out _);
            }
        }

        [Benchmark]
        [BenchmarkCategory("Cache_Mixed")]
        public void ConcurrentDict_Mixed()
        {
            for (int i = 0; i < 50; i++)
            {
                _concurrentDict.TryAdd(_keys[i], _values[i]);
            }
            for (int i = 0; i < 50; i++)
            {
                _concurrentDict.TryGetValue(_keys[i % 25], out _);
            }
        }
    }

    /// <summary>
    /// 服务定位器性能基准测试
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(launchCount: 1, warmupCount: 3, targetCount: 5)]
    public class ServiceLocatorBenchmarks : DotNetARXBenchmarkBase
    {
        [Benchmark]
        [BenchmarkCategory("DI")]
        public object GetService_Logger()
        {
            return SmartServiceLocator.GetService<ILogger>();
        }

        [Benchmark]
        [BenchmarkCategory("DI")]
        public object GetService_ConfigManager()
        {
            return SmartServiceLocator.GetService<IConfigurationManager>();
        }

        [Benchmark]
        [BenchmarkCategory("DI")]
        public bool TryGetService_Logger()
        {
            return SmartServiceLocator.TryGetService<ILogger>(out _);
        }

        [Benchmark]
        [BenchmarkCategory("DI")]
        public object CreateScope()
        {
            using var scope = SmartServiceLocator.CreateScope();
            return scope.GetService<ILogger>();
        }
    }

    /// <summary>
    /// 字符串和集合操作性能基准测试
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(launchCount: 1, warmupCount: 2, targetCount: 3)]
    public class UtilityBenchmarks : DotNetARXBenchmarkBase
    {
        private readonly string[] _testStrings;
        private readonly List<int> _testNumbers;

        public UtilityBenchmarks()
        {
            _testStrings = Enumerable.Range(0, 1000)
                .Select(i => $"TestString_{i}_{Guid.NewGuid()}")
                .ToArray();
            _testNumbers = Enumerable.Range(0, 1000).ToList();
        }

        [Benchmark]
        [BenchmarkCategory("String")]
        public string StringConcatenation()
        {
            var result = "";
            for (int i = 0; i < 100; i++)
            {
                result += _testStrings[i];
            }
            return result;
        }

        [Benchmark]
        [BenchmarkCategory("String")]
        public string StringBuilder_Concatenation()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 100; i++)
            {
                sb.Append(_testStrings[i]);
            }
            return sb.ToString();
        }

        [Benchmark]
        [BenchmarkCategory("Collection")]
        public List<int> ListAddition()
        {
            var list = new List<int>();
            for (int i = 0; i < 1000; i++)
            {
                list.Add(i);
            }
            return list;
        }

        [Benchmark]
        [BenchmarkCategory("Collection")]
        public List<int> ListCapacityAddition()
        {
            var list = new List<int>(1000);
            for (int i = 0; i < 1000; i++)
            {
                list.Add(i);
            }
            return list;
        }

        [Benchmark]
        [BenchmarkCategory("LINQ")]
        public IEnumerable<int> LinqWhere()
        {
            return _testNumbers.Where(x => x % 2 == 0);
        }

        [Benchmark]
        [BenchmarkCategory("LINQ")]
        public List<int> LinqWhereToList()
        {
            return _testNumbers.Where(x => x % 2 == 0).ToList();
        }

        [Benchmark]
        [BenchmarkCategory("LINQ")]
        public int LinqSum()
        {
            return _testNumbers.Sum();
        }
    }
}