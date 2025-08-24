using System.Text.Json;

namespace DotNetARX.Configuration
{
    /// <summary>
    /// 增强的配置管理器
    /// 支持热重载、环境特定配置、配置验证等高级功能
    /// </summary>
    public class EnhancedConfigurationManager : IConfigurationManager, IDisposable
    {
        private readonly string _basePath;
        private readonly string _environment;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, object> _settings;
        private readonly ConcurrentDictionary<string, IConfigurationValidator> _validators;
        private readonly List<IConfigurationObserver> _observers;
        private readonly FileSystemWatcher _fileWatcher;
        private readonly Timer _autoSaveTimer;
        private readonly object _lockObject = new object();
        private bool _disposed = false;
        private bool _hotReloadEnabled = true;
        private DateTime _lastModified = DateTime.MinValue;

        public event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;

        /// <summary>
        /// 当前环境名称
        /// </summary>
        public string Environment => _environment;

        /// <summary>
        /// 是否启用热重载
        /// </summary>
        public bool HotReloadEnabled
        {
            get => _hotReloadEnabled;
            set
            {
                _hotReloadEnabled = value;
                _fileWatcher.EnableRaisingEvents = value;
            }
        }

        public EnhancedConfigurationManager(string basePath = null, string environment = null)
        {
            _logger = LogManager.GetLogger(typeof(EnhancedConfigurationManager));
            _basePath = basePath ?? Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                "DotNetARX", "Config");
            _environment = environment ?? GetCurrentEnvironment();

            _settings = new ConcurrentDictionary<string, object>();
            _validators = new ConcurrentDictionary<string, IConfigurationValidator>();
            _observers = new List<IConfigurationObserver>();

            // 确保配置目录存在
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }

            // 设置文件监控
            _fileWatcher = new FileSystemWatcher(_basePath, "*.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = _hotReloadEnabled
            };
            _fileWatcher.Changed += OnConfigFileChanged;

            // 设置自动保存定时器（每5分钟）
            _autoSaveTimer = new Timer(AutoSave, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            // 注册默认验证器
            RegisterDefaultValidators();

            // 加载配置
            Load();

            _logger.Info($"增强配置管理器已初始化 - 环境: {_environment}, 路径: {_basePath}");
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
                // 首先尝试环境特定的配置
                var envKey = $"{_environment}.{key}";
                if (_settings.TryGetValue(envKey, out var envValue))
                {
                    return ConvertValue<T>(envValue, defaultValue);
                }

                // 然后尝试通用配置
                if (_settings.TryGetValue(key, out var value))
                {
                    return ConvertValue<T>(value, defaultValue);
                }

                _logger.Debug($"配置键未找到，使用默认值 - Key: {key}, Default: {defaultValue}");
            }
            catch (Exception ex)
            {
                _logger.Error($"获取配置设置失败 - Key: {key}", ex);
            }

            return defaultValue;
        }

        /// <summary>
        /// 设置配置值（带验证）
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
                // 验证配置值
                ValidateSetting(key, value);

                var oldValue = _settings.TryGetValue(key, out var existing) ? existing : null;
                _settings.AddOrUpdate(key, value, (k, v) => value);

                // 通知观察者
                NotifyConfigurationChanged(key, oldValue, value);

                _logger.Debug($"设置配置成功 - Key: {key}, Value: {value}");
            }
            catch (Exception ex)
            {
                _logger.Error($"设置配置失败 - Key: {key}", ex);
                throw;
            }
        }

        /// <summary>
        /// 设置环境特定的配置
        /// </summary>
        public void SetEnvironmentSetting<T>(string key, T value, string environment = null)
        {
            var env = environment ?? _environment;
            var envKey = $"{env}.{key}";
            SetSetting(envKey, value);
        }

        /// <summary>
        /// 获取环境特定的配置
        /// </summary>
        public T GetEnvironmentSetting<T>(string key, T defaultValue = default, string environment = null)
        {
            var env = environment ?? _environment;
            var envKey = $"{env}.{key}";
            return GetSetting(envKey, defaultValue);
        }

        /// <summary>
        /// 检查是否存在配置
        /// </summary>
        public bool HasSetting(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            var envKey = $"{_environment}.{key}";
            return _settings.ContainsKey(envKey) || _settings.ContainsKey(key);
        }

        /// <summary>
        /// 移除配置
        /// </summary>
        public void RemoveSetting(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            try
            {
                var envKey = $"{_environment}.{key}";
                var removedEnv = _settings.TryRemove(envKey, out var envValue);
                var removedGeneral = _settings.TryRemove(key, out var generalValue);

                if (removedEnv || removedGeneral)
                {
                    NotifyConfigurationChanged(key, envValue ?? generalValue, null);
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
                    var baseConfigPath = Path.Combine(_basePath, "appsettings.json");
                    var envConfigPath = Path.Combine(_basePath, $"appsettings.{_environment}.json");

                    // 分离通用配置和环境特定配置
                    var generalSettings = new Dictionary<string, object>();
                    var environmentSettings = new Dictionary<string, object>();

                    foreach (var kvp in _settings)
                    {
                        if (kvp.Key.StartsWith($"{_environment}."))
                        {
                            var realKey = kvp.Key.Substring(_environment.Length + 1);
                            environmentSettings[realKey] = kvp.Value;
                        }
                        else if (!kvp.Key.Contains('.'))
                        {
                            generalSettings[kvp.Key] = kvp.Value;
                        }
                    }

                    // 保存通用配置
                    if (generalSettings.Any())
                    {
                        SaveSettingsToFile(generalSettings, baseConfigPath);
                    }

                    // 保存环境特定配置
                    if (environmentSettings.Any())
                    {
                        SaveSettingsToFile(environmentSettings, envConfigPath);
                    }

                    _lastModified = DateTime.UtcNow;
                    _logger.Debug($"配置保存成功 - 环境: {_environment}");
                }
                catch (Exception ex)
                {
                    _logger.Error("保存配置失败", ex);
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
                    _settings.Clear();

                    // 加载基础配置
                    var baseConfigPath = Path.Combine(_basePath, "appsettings.json");
                    LoadSettingsFromFile(baseConfigPath, "");

                    // 加载环境特定配置
                    var envConfigPath = Path.Combine(_basePath, $"appsettings.{_environment}.json");
                    LoadSettingsFromFile(envConfigPath, $"{_environment}.");

                    // 加载开发者个人配置（不提交到版本控制）
                    var userConfigPath = Path.Combine(_basePath, "appsettings.user.json");
                    LoadSettingsFromFile(userConfigPath, "user.");

                    // 应用配置验证
                    ValidateAllSettings();

                    _lastModified = DateTime.UtcNow;
                    _logger.Debug($"配置加载成功 - 项目数: {_settings.Count}, 环境: {_environment}");
                }
                catch (Exception ex)
                {
                    _logger.Error("加载配置失败", ex);
                    LoadDefaultConfiguration();
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
                LoadDefaultConfiguration();
                Save();
                _logger.Info("配置已重置为默认值");
            }
        }

        /// <summary>
        /// 注册配置验证器
        /// </summary>
        public void RegisterValidator(string key, IConfigurationValidator validator)
        {
            _validators.AddOrUpdate(key, validator, (k, v) => validator);
            _logger.Debug($"注册配置验证器 - Key: {key}, Validator: {validator.GetType().Name}");
        }

        /// <summary>
        /// 注册配置观察者
        /// </summary>
        public void RegisterObserver(IConfigurationObserver observer)
        {
            lock (_observers)
            {
                _observers.Add(observer);
            }
            _logger.Debug($"注册配置观察者 - Observer: {observer.GetType().Name}");
        }

        /// <summary>
        /// 移除配置观察者
        /// </summary>
        public void UnregisterObserver(IConfigurationObserver observer)
        {
            lock (_observers)
            {
                _observers.Remove(observer);
            }
        }

        /// <summary>
        /// 获取配置摘要
        /// </summary>
        public ConfigurationSummary GetConfigurationSummary()
        {
            var summary = new ConfigurationSummary
            {
                Environment = _environment,
                TotalSettings = _settings.Count,
                LastModified = _lastModified,
                HotReloadEnabled = _hotReloadEnabled,
                ConfigurationPath = _basePath
            };

            // 按环境分组配置
            var generalSettings = 0;
            var environmentSettings = 0;
            var userSettings = 0;

            foreach (var key in _settings.Keys)
            {
                if (key.StartsWith($"{_environment}."))
                    environmentSettings++;
                else if (key.StartsWith("user."))
                    userSettings++;
                else if (!key.Contains('.'))
                    generalSettings++;
            }

            summary.GeneralSettings = generalSettings;
            summary.EnvironmentSettings = environmentSettings;
            summary.UserSettings = userSettings;

            return summary;
        }

        /// <summary>
        /// 导出配置
        /// </summary>
        public string ExportConfiguration(bool includeEnvironmentSpecific = true, bool includeUserSettings = false)
        {
            var exportSettings = new Dictionary<string, object>();

            foreach (var kvp in _settings)
            {
                var include = true;

                if (!includeEnvironmentSpecific && kvp.Key.StartsWith($"{_environment}."))
                    include = false;

                if (!includeUserSettings && kvp.Key.StartsWith("user."))
                    include = false;

                if (include)
                {
                    exportSettings[kvp.Key] = kvp.Value;
                }
            }

            return JsonSerializer.Serialize(exportSettings, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }

        /// <summary>
        /// 导入配置
        /// </summary>
        public void ImportConfiguration(string jsonConfiguration, bool mergeWithExisting = true)
        {
            try
            {
                var importedSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonConfiguration);

                if (!mergeWithExisting)
                {
                    _settings.Clear();
                }

                foreach (var kvp in importedSettings)
                {
                    SetSetting(kvp.Key, kvp.Value);
                }

                Save();
                _logger.Info($"配置导入成功，导入 {importedSettings.Count} 个设置");
            }
            catch (Exception ex)
            {
                _logger.Error("配置导入失败", ex);
                throw;
            }
        }

        #region 私有方法

        private string GetCurrentEnvironment()
        {
            // 优先级：环境变量 > 命令行参数 > 默认值
            var env = System.Environment.GetEnvironmentVariable("DOTNETARX_ENVIRONMENT")
                     ?? System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                     ?? "Production";

            return env;
        }

        private void RegisterDefaultValidators()
        {
            // 性能相关配置验证器
            RegisterValidator("Performance.CacheSize", new RangeValidator<int>(100, 10000));
            RegisterValidator("Performance.ThreadPoolSize", new RangeValidator<int>(1, System.Environment.ProcessorCount * 4));

            // 日志级别验证器
            RegisterValidator("Logging.Level", new EnumValidator<LogLevel>());

            // 路径验证器
            RegisterValidator("Paths.TempDirectory", new PathValidator());
            RegisterValidator("Paths.LogDirectory", new PathValidator());
        }

        private T ConvertValue<T>(object value, T defaultValue)
        {
            if (value == null) return defaultValue;

            if (value is JsonElement jsonElement)
            {
                return DeserializeJsonElement(jsonElement, defaultValue);
            }

            if (value is T directValue)
            {
                return directValue;
            }

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

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

        private void ValidateSetting(string key, object value)
        {
            if (_validators.TryGetValue(key, out var validator))
            {
                var validationResult = validator.Validate(value);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors);
                    throw new ConfigurationValidationException($"配置验证失败 - Key: {key}, Errors: {errors}");
                }
            }
        }

        private void ValidateAllSettings()
        {
            var validationErrors = new List<string>();

            foreach (var kvp in _settings)
            {
                try
                {
                    ValidateSetting(kvp.Key, kvp.Value);
                }
                catch (ConfigurationValidationException ex)
                {
                    validationErrors.Add(ex.Message);
                }
            }

            if (validationErrors.Any())
            {
                _logger.Warning($"配置验证发现 {validationErrors.Count} 个问题: {string.Join("; ", validationErrors)}");
            }
        }

        private void NotifyConfigurationChanged(string key, object oldValue, object newValue)
        {
            var args = new ConfigurationChangedEventArgs(key, oldValue, newValue);

            // 通知事件订阅者
            ConfigurationChanged?.Invoke(this, args);

            // 通知注册的观察者
            lock (_observers)
            {
                foreach (var observer in _observers)
                {
                    try
                    {
                        observer.OnConfigurationChanged(args);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"通知配置观察者失败 - Observer: {observer.GetType().Name}", ex);
                    }
                }
            }
        }

        private void LoadSettingsFromFile(string filePath, string keyPrefix)
        {
            if (!File.Exists(filePath)) return;

            try
            {
                var json = File.ReadAllText(filePath);
                var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                if (settings != null)
                {
                    foreach (var kvp in settings)
                    {
                        var key = string.IsNullOrEmpty(keyPrefix) ? kvp.Key : $"{keyPrefix}{kvp.Key}";
                        _settings.TryAdd(key, kvp.Value);
                    }
                }

                _logger.Debug($"从文件加载配置 - 路径: {filePath}, 项目数: {settings?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                _logger.Error($"从文件加载配置失败 - 路径: {filePath}", ex);
            }
        }

        private void SaveSettingsToFile(Dictionary<string, object> settings, string filePath)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            File.WriteAllText(filePath, json);
        }

        private void LoadDefaultConfiguration()
        {
            _logger.Info("加载默认配置");

            // 性能配置
            SetSetting("Performance.CacheSize", 1000);
            SetSetting("Performance.CacheExpirationMinutes", 30);
            SetSetting("Performance.EnableBatching", true);
            SetSetting("Performance.BatchTimeoutMs", 10);

            // 日志配置
            SetSetting("Logging.Level", LogLevel.Information);
            SetSetting("Logging.EnableFileLogging", true);
            SetSetting("Logging.MaxFileSizeMB", 10);

            // AutoCAD配置
            SetSetting("AutoCAD.DefaultLineWeight", 0.25);
            SetSetting("AutoCAD.DefaultColor", 256);
            SetSetting("AutoCAD.EnableTransactionLogging", false);

            // 诊断配置
            SetSetting("Diagnostics.EnableAutoCheck", true);
            SetSetting("Diagnostics.CheckIntervalMinutes", 15);
            SetSetting("Diagnostics.MaxReportRetentionDays", 7);
        }

        private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            if (!_hotReloadEnabled) return;

            try
            {
                // 延迟一点以确保文件写入完成
                Task.Delay(100).ContinueWith(_ =>
                {
                    _logger.Info($"检测到配置文件变更，重新加载配置 - 文件: {e.Name}");
                    Load();
                });
            }
            catch (Exception ex)
            {
                _logger.Error("配置文件热重载失败", ex);
            }
        }

        private void AutoSave(object state)
        {
            try
            {
                if (_settings.Any())
                {
                    Save();
                    _logger.Debug("自动保存配置完成");
                }
            }
            catch (Exception ex)
            {
                _logger.Error("自动保存配置失败", ex);
            }
        }

        #endregion 私有方法

        public void Dispose()
        {
            if (!_disposed)
            {
                _fileWatcher?.Dispose();
                _autoSaveTimer?.Dispose();

                try
                {
                    Save();
                }
                catch (Exception ex)
                {
                    _logger.Error("配置管理器释放时保存配置失败", ex);
                }

                _disposed = true;
                _logger.Info("增强配置管理器已释放");
            }
        }
    }
}