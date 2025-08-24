using System.Text.Json;

namespace DotNetARX.Configuration
{
    /// <summary>
    /// 配置管理器接口
    /// </summary>
    public interface IConfigurationManager
    {
        T GetSetting<T>(string key, T defaultValue = default);

        void SetSetting<T>(string key, T value);

        bool HasSetting(string key);

        void RemoveSetting(string key);

        void Save();

        void Load();

        void Reset();
    }

    /// <summary>
    /// 配置管理器实现
    /// </summary>
    public class ConfigurationManager : IConfigurationManager, IDisposable
    {
        private readonly string _configFilePath;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, object> _settings;
        private readonly object _lockObject = new object();
        private bool _disposed = false;

        public ConfigurationManager(string configFilePath = null)
        {
            _logger = LogManager.GetLogger(typeof(ConfigurationManager));

            _configFilePath = configFilePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DotNetARX",
                "Config",
                "settings.json");

            _settings = new ConcurrentDictionary<string, object>();

            // 确保配置目录存在
            var configDirectory = Path.GetDirectoryName(_configFilePath);
            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }

            Load();
        }

        /// <summary>
        /// 获取配置设置
        /// </summary>
        public T GetSetting<T>(string key, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(key))
            {
                _logger.Warning("配置键为空或null");
                return defaultValue;
            }

            try
            {
                if (_settings.TryGetValue(key, out var value))
                {
                    if (value is JsonElement jsonElement)
                    {
                        return DeserializeJsonElement(jsonElement, defaultValue);
                    }

                    if (value is T directValue)
                    {
                        return directValue;
                    }

                    // 尝试转换类型
                    return (T)Convert.ChangeType(value, typeof(T));
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"获取配置设置失败 - Key: {key}", ex);
            }

            return defaultValue;
        }

        /// <summary>
        /// 设置配置值
        /// </summary>
        public void SetSetting<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
            {
                _logger.Warning("配置键为空或null，忽略设置");
                return;
            }

            try
            {
                _settings.AddOrUpdate(key, value, (k, v) => value);
                _logger.Debug($"设置配置成功 - Key: {key}, Value: {value}");
            }
            catch (Exception ex)
            {
                _logger.Error($"设置配置失败 - Key: {key}", ex);
                throw;
            }
        }

        /// <summary>
        /// 检查是否存在配置
        /// </summary>
        public bool HasSetting(string key)
        {
            return !string.IsNullOrEmpty(key) && _settings.ContainsKey(key);
        }

        /// <summary>
        /// 移除配置
        /// </summary>
        public void RemoveSetting(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            try
            {
                if (_settings.TryRemove(key, out var removedValue))
                {
                    _logger.Debug($"移除配置成功 - Key: {key}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"移除配置失败 - Key: {key}", ex);
            }
        }

        /// <summary>
        /// 保存配置到文件
        /// </summary>
        public void Save()
        {
            lock (_lockObject)
            {
                try
                {
                    var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });

                    File.WriteAllText(_configFilePath, json);
                    _logger.Debug($"配置保存成功 - 路径: {_configFilePath}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"保存配置失败 - 路径: {_configFilePath}", ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// 从文件加载配置
        /// </summary>
        public void Load()
        {
            lock (_lockObject)
            {
                try
                {
                    if (File.Exists(_configFilePath))
                    {
                        var json = File.ReadAllText(_configFilePath);
                        var loadedSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                        _settings.Clear();

                        if (loadedSettings != null)
                        {
                            foreach (var kvp in loadedSettings)
                            {
                                _settings.TryAdd(kvp.Key, kvp.Value);
                            }
                        }

                        _logger.Debug($"配置加载成功 - 路径: {_configFilePath}, 项目数: {_settings.Count}");
                    }
                    else
                    {
                        // 如果配置文件不存在，使用默认配置
                        LoadDefaultSettings();
                        _logger.Info($"配置文件不存在，已加载默认配置 - 路径: {_configFilePath}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"加载配置失败 - 路径: {_configFilePath}", ex);
                    LoadDefaultSettings();
                }
            }
        }

        /// <summary>
        /// 重置为默认配置
        /// </summary>
        public void Reset()
        {
            lock (_lockObject)
            {
                _settings.Clear();
                LoadDefaultSettings();
                Save();
                _logger.Info("配置已重置为默认值");
            }
        }

        /// <summary>
        /// 加载默认配置
        /// </summary>
        private void LoadDefaultSettings()
        {
            // 默认配置设置
            SetSetting(ConfigurationKeys.DefaultLayerName, "0");
            SetSetting(ConfigurationKeys.DefaultTextStyle, "Standard");
            SetSetting(ConfigurationKeys.DefaultTextHeight, 2.5);
            SetSetting(ConfigurationKeys.DefaultBatchSize, 1000);
            SetSetting(ConfigurationKeys.EnableLogging, true);
            SetSetting(ConfigurationKeys.LogLevel, "Info");
            SetSetting(ConfigurationKeys.AutoSaveInterval, 300); // 5分钟
            SetSetting(ConfigurationKeys.MaxUndoLevels, 50);
        }

        /// <summary>
        /// 反序列化JSON元素
        /// </summary>
        private T DeserializeJsonElement<T>(JsonElement jsonElement, T defaultValue)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
            }
            catch
            {
                return defaultValue;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    Save(); // 释放前保存配置
                }
                catch (Exception ex)
                {
                    _logger.Error("释放配置管理器时保存失败", ex);
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }

    /// <summary>
    /// 全局配置管理器实例
    /// </summary>
    public static class GlobalConfiguration
    {
        private static readonly Lazy<ConfigurationManager> _instance =
            new Lazy<ConfigurationManager>(() => new ConfigurationManager());

        public static IConfigurationManager Instance => _instance.Value;

        // 便捷访问方法
        public static T GetSetting<T>(string key, T defaultValue = default)
        {
            return Instance.GetSetting(key, defaultValue);
        }

        public static void SetSetting<T>(string key, T value)
        {
            Instance.SetSetting(key, value);
        }

        public static void Save()
        {
            Instance.Save();
        }
    }
}