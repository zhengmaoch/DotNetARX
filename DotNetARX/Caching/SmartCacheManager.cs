namespace DotNetARX.Caching
{
    /// <summary>
    /// 智能缓存管理器
    /// 提供多级缓存策略、LRU算法、内存压力自动调整
    /// </summary>
    public static class SmartCacheManager
    {
        private static readonly ConcurrentDictionary<string, ISmartCache> _caches = new();
        private static readonly Timer _memoryPressureTimer;
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(SmartCacheManager));
        private static long _totalMemoryUsage = 0;
        private static readonly long _maxMemoryUsage = GetMaxMemoryUsage();

        static SmartCacheManager()
        {
            // 每30秒检查一次内存压力
            _memoryPressureTimer = new Timer(CheckMemoryPressure, null, 30000, 30000);
            _logger.Info("智能缓存管理器初始化完成");
        }

        /// <summary>
        /// 获取或创建缓存实例
        /// </summary>
        public static ISmartCache<TKey, TValue> GetCache<TKey, TValue>(
            string cacheName,
            int maxSize = 1000,
            TimeSpan? defaultExpiration = null)
        {
            var cache = _caches.GetOrAdd(cacheName, name =>
                new LRUSmartCache<TKey, TValue>(name, maxSize, defaultExpiration ?? TimeSpan.FromMinutes(30)));

            return (ISmartCache<TKey, TValue>)cache;
        }

        /// <summary>
        /// 获取所有缓存的统计信息
        /// </summary>
        public static CacheManagerStatistics GetStatistics()
        {
            var stats = new CacheManagerStatistics
            {
                TotalCaches = _caches.Count,
                TotalMemoryUsage = _totalMemoryUsage,
                MaxMemoryUsage = _maxMemoryUsage,
                MemoryPressureLevel = GetMemoryPressureLevel()
            };

            foreach (var cache in _caches.Values)
            {
                var cacheStats = cache.GetStatistics();
                stats.TotalItems += cacheStats.ItemCount;
                stats.TotalHits += cacheStats.HitCount;
                stats.TotalMisses += cacheStats.MissCount;
            }

            stats.HitRatio = stats.TotalHits + stats.TotalMisses > 0
                ? (double)stats.TotalHits / (stats.TotalHits + stats.TotalMisses)
                : 0;

            return stats;
        }

        /// <summary>
        /// 清理所有缓存
        /// </summary>
        public static void ClearAll()
        {
            foreach (var cache in _caches.Values)
            {
                cache.Clear();
            }
            _totalMemoryUsage = 0;
            _logger.Info("所有缓存已清理");
        }

        /// <summary>
        /// 移除指定缓存
        /// </summary>
        public static bool RemoveCache(string cacheName)
        {
            if (_caches.TryRemove(cacheName, out var cache))
            {
                cache.Clear();
                _logger.Info($"缓存 {cacheName} 已移除");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检查内存压力并自动调整
        /// </summary>
        private static void CheckMemoryPressure(object state)
        {
            try
            {
                var pressureLevel = GetMemoryPressureLevel();

                if (pressureLevel >= MemoryPressureLevel.High)
                {
                    _logger.Warning($"内存压力较高 ({pressureLevel})，开始自动清理缓存");

                    var itemsToRemove = pressureLevel == MemoryPressureLevel.Critical ? 0.5 : 0.3;

                    foreach (var cache in _caches.Values)
                    {
                        cache.TrimToSize((int)(cache.GetStatistics().ItemCount * (1 - itemsToRemove)));
                    }

                    // 强制垃圾回收
                    if (pressureLevel == MemoryPressureLevel.Critical)
                    {
                        GC.Collect(2, GCCollectionMode.Forced);
                        GC.WaitForPendingFinalizers();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("检查内存压力时发生错误", ex);
            }
        }

        /// <summary>
        /// 获取内存压力级别
        /// </summary>
        private static MemoryPressureLevel GetMemoryPressureLevel()
        {
            var currentMemory = GC.GetTotalMemory(false);
            var workingSet = Environment.WorkingSet;

            var memoryUsageRatio = (double)workingSet / _maxMemoryUsage;

            return memoryUsageRatio switch
            {
                >= 0.9 => MemoryPressureLevel.Critical,
                >= 0.7 => MemoryPressureLevel.High,
                >= 0.5 => MemoryPressureLevel.Medium,
                _ => MemoryPressureLevel.Low
            };
        }

        /// <summary>
        /// 获取最大内存使用量
        /// </summary>
        private static long GetMaxMemoryUsage()
        {
            // 获取系统可用内存的80%作为最大使用量
            var totalMemory = GC.GetTotalMemory(false);
            var physicalMemory = Environment.WorkingSet;

            // 使用较保守的策略
            return Math.Max(totalMemory * 4, physicalMemory * 2);
        }

        /// <summary>
        /// 更新内存使用量统计
        /// </summary>
        internal static void UpdateMemoryUsage(long delta)
        {
            Interlocked.Add(ref _totalMemoryUsage, delta);
        }
    }

    /// <summary>
    /// 智能缓存接口
    /// </summary>
    public interface ISmartCache
    {
        string Name { get; }

        void Clear();

        void TrimToSize(int targetSize);

        CacheStatistics GetStatistics();
    }

    /// <summary>
    /// 泛型智能缓存接口
    /// </summary>
    public interface ISmartCache<TKey, TValue> : ISmartCache
    {
        TValue Get(TKey key);

        TValue GetOrAdd(TKey key, Func<TKey, TValue> factory);

        TValue GetOrAdd(TKey key, Func<TKey, TValue> factory, TimeSpan expiration);

        void Set(TKey key, TValue value);

        void Set(TKey key, TValue value, TimeSpan expiration);

        bool TryGet(TKey key, out TValue value);

        bool Remove(TKey key);

        bool ContainsKey(TKey key);
    }

    /// <summary>
    /// LRU智能缓存实现
    /// </summary>
    public class LRUSmartCache<TKey, TValue> : ISmartCache<TKey, TValue>, IDisposable
    {
        private readonly string _name;
        private readonly int _maxSize;
        private readonly TimeSpan _defaultExpiration;
        private readonly ConcurrentDictionary<TKey, CacheItem<TValue>> _cache;
        private readonly LinkedList<TKey> _accessOrder;
        private readonly object _lockObject = new object();
        private readonly Timer _cleanupTimer;
        private readonly ILogger _logger;

        // 统计信息
        private long _hitCount = 0;

        private long _missCount = 0;
        private long _evictionCount = 0;
        private bool _disposed = false;

        public string Name => _name;

        public LRUSmartCache(string name, int maxSize = 1000, TimeSpan? defaultExpiration = null)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _maxSize = maxSize;
            _defaultExpiration = defaultExpiration ?? TimeSpan.FromMinutes(30);
            _cache = new ConcurrentDictionary<TKey, CacheItem<TValue>>();
            _accessOrder = new LinkedList<TKey>();
            _logger = LogManager.GetLogger(typeof(LRUSmartCache<TKey, TValue>));

            // 每5分钟清理一次过期项
            _cleanupTimer = new Timer(CleanupExpiredItems, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            _logger.Debug($"LRU缓存创建: {name}, 最大大小: {maxSize}, 默认过期时间: {_defaultExpiration}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue Get(TKey key)
        {
            if (TryGet(key, out var value))
            {
                return value;
            }

            Interlocked.Increment(ref _missCount);
            return default(TValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(TKey key, out TValue value)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                if (!item.IsExpired)
                {
                    // 更新访问时间和顺序
                    item.LastAccessed = DateTime.UtcNow;
                    UpdateAccessOrder(key);

                    Interlocked.Increment(ref _hitCount);
                    value = item.Value;
                    return true;
                }
                else
                {
                    // 移除过期项
                    Remove(key);
                }
            }

            Interlocked.Increment(ref _missCount);
            value = default(TValue);
            return false;
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
        {
            return GetOrAdd(key, factory, _defaultExpiration);
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory, TimeSpan expiration)
        {
            if (TryGet(key, out var existingValue))
            {
                return existingValue;
            }

            var newValue = factory(key);
            Set(key, newValue, expiration);
            return newValue;
        }

        public void Set(TKey key, TValue value)
        {
            Set(key, value, _defaultExpiration);
        }

        public void Set(TKey key, TValue value, TimeSpan expiration)
        {
            var now = DateTime.UtcNow;
            var item = new CacheItem<TValue>
            {
                Value = value,
                CreatedAt = now,
                LastAccessed = now,
                ExpiresAt = now.Add(expiration)
            };

            // 估算内存使用
            var memoryDelta = EstimateMemoryUsage(value);

            _cache.AddOrUpdate(key, item, (k, existing) =>
            {
                // 如果是更新，计算内存差异
                var oldMemory = EstimateMemoryUsage(existing.Value);
                SmartCacheManager.UpdateMemoryUsage(memoryDelta - oldMemory);
                return item;
            });

            if (!_cache.ContainsKey(key))
            {
                SmartCacheManager.UpdateMemoryUsage(memoryDelta);
            }

            UpdateAccessOrder(key);
            EnsureCapacity();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(TKey key)
        {
            if (_cache.TryRemove(key, out var item))
            {
                lock (_lockObject)
                {
                    _accessOrder.Remove(key);
                }

                // 更新内存使用统计
                var memoryDelta = EstimateMemoryUsage(item.Value);
                SmartCacheManager.UpdateMemoryUsage(-memoryDelta);

                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(TKey key)
        {
            return _cache.ContainsKey(key) && !_cache[key].IsExpired;
        }

        public void Clear()
        {
            lock (_lockObject)
            {
                var memoryDelta = 0L;
                foreach (var item in _cache.Values)
                {
                    memoryDelta += EstimateMemoryUsage(item.Value);
                }

                _cache.Clear();
                _accessOrder.Clear();

                SmartCacheManager.UpdateMemoryUsage(-memoryDelta);
                _logger.Debug($"缓存 {_name} 已清空");
            }
        }

        public void TrimToSize(int targetSize)
        {
            if (_cache.Count <= targetSize) return;

            var itemsToRemove = _cache.Count - targetSize;
            var keysToRemove = new List<TKey>();

            lock (_lockObject)
            {
                var current = _accessOrder.First;
                while (current != null && keysToRemove.Count < itemsToRemove)
                {
                    keysToRemove.Add(current.Value);
                    current = current.Next;
                }
            }

            foreach (var key in keysToRemove)
            {
                Remove(key);
                Interlocked.Increment(ref _evictionCount);
            }

            _logger.Debug($"缓存 {_name} 裁剪到 {targetSize} 项，移除了 {keysToRemove.Count} 项");
        }

        public CacheStatistics GetStatistics()
        {
            return new CacheStatistics
            {
                Name = _name,
                ItemCount = _cache.Count,
                MaxSize = _maxSize,
                HitCount = _hitCount,
                MissCount = _missCount,
                EvictionCount = _evictionCount,
                HitRatio = _hitCount + _missCount > 0 ? (double)_hitCount / (_hitCount + _missCount) : 0
            };
        }

        /// <summary>
        /// 更新访问顺序（LRU算法核心）
        /// </summary>
        private void UpdateAccessOrder(TKey key)
        {
            lock (_lockObject)
            {
                _accessOrder.Remove(key);
                _accessOrder.AddLast(key);
            }
        }

        /// <summary>
        /// 确保容量不超限
        /// </summary>
        private void EnsureCapacity()
        {
            if (_cache.Count > _maxSize)
            {
                // 移除最少使用的项
                TKey oldestKey = default(TKey);
                lock (_lockObject)
                {
                    if (_accessOrder.Count > 0)
                    {
                        oldestKey = _accessOrder.First.Value;
                    }
                }

                if (!EqualityComparer<TKey>.Default.Equals(oldestKey, default(TKey)))
                {
                    Remove(oldestKey);
                    Interlocked.Increment(ref _evictionCount);
                }
            }
        }

        /// <summary>
        /// 清理过期项
        /// </summary>
        private void CleanupExpiredItems(object state)
        {
            try
            {
                var expiredKeys = new List<TKey>();
                var now = DateTime.UtcNow;

                foreach (var kvp in _cache)
                {
                    if (kvp.Value.IsExpired)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }

                foreach (var key in expiredKeys)
                {
                    Remove(key);
                }

                if (expiredKeys.Count > 0)
                {
                    _logger.Debug($"缓存 {_name} 清理了 {expiredKeys.Count} 个过期项");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"清理过期项时发生错误 (缓存: {_name})", ex);
            }
        }

        /// <summary>
        /// 估算内存使用量
        /// </summary>
        private static long EstimateMemoryUsage(TValue value)
        {
            if (value == null) return 0;

            // 简单的内存估算
            return value switch
            {
                string str => str.Length * 2 + 24, // Unicode字符 + 对象头
                byte[] bytes => bytes.Length + 24,
                _ => 64 // 其他对象的平均估算
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cleanupTimer?.Dispose();
                Clear();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 缓存项
    /// </summary>
    internal class CacheItem<T>
    {
        public T Value { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessed { get; set; }
        public DateTime ExpiresAt { get; set; }

        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }

    /// <summary>
    /// 内存压力级别
    /// </summary>
    public enum MemoryPressureLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// 缓存统计信息
    /// </summary>
    public class CacheStatistics
    {
        public string Name { get; set; }
        public int ItemCount { get; set; }
        public int MaxSize { get; set; }
        public long HitCount { get; set; }
        public long MissCount { get; set; }
        public long EvictionCount { get; set; }
        public double HitRatio { get; set; }
    }

    /// <summary>
    /// 缓存管理器统计信息
    /// </summary>
    public class CacheManagerStatistics
    {
        public int TotalCaches { get; set; }
        public int TotalItems { get; set; }
        public long TotalHits { get; set; }
        public long TotalMisses { get; set; }
        public double HitRatio { get; set; }
        public long TotalMemoryUsage { get; set; }
        public long MaxMemoryUsage { get; set; }
        public MemoryPressureLevel MemoryPressureLevel { get; set; }
    }
}