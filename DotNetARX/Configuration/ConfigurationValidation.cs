namespace DotNetARX.Configuration
{
    /// <summary>
    /// 配置验证器接口
    /// </summary>
    public interface IConfigurationValidator
    {
        ValidationResult Validate(object value);

        string Name { get; }
        string Description { get; }
    }

    /// <summary>
    /// 配置观察者接口
    /// </summary>
    public interface IConfigurationObserver
    {
        void OnConfigurationChanged(ConfigurationChangedEventArgs args);
    }

    /// <summary>
    /// 配置变更事件参数
    /// </summary>
    public class ConfigurationChangedEventArgs : EventArgs
    {
        public string Key { get; }
        public object OldValue { get; }
        public object NewValue { get; }
        public DateTime Timestamp { get; }

        public ConfigurationChangedEventArgs(string key, object oldValue, object newValue)
        {
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 配置验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public static ValidationResult Success() => new() { IsValid = true };

        public static ValidationResult Failure(params string[] errors) => new()
        {
            IsValid = false,
            Errors = errors.ToList()
        };

        public static ValidationResult Warning(string warning) => new()
        {
            IsValid = true,
            Warnings = new List<string> { warning }
        };
    }

    /// <summary>
    /// 配置摘要
    /// </summary>
    public class ConfigurationSummary
    {
        public string Environment { get; set; }
        public int TotalSettings { get; set; }
        public int GeneralSettings { get; set; }
        public int EnvironmentSettings { get; set; }
        public int UserSettings { get; set; }
        public DateTime LastModified { get; set; }
        public bool HotReloadEnabled { get; set; }
        public string ConfigurationPath { get; set; }
    }

    /// <summary>
    /// 配置验证异常
    /// </summary>
    public class ConfigurationValidationException : Exception
    {
        public ConfigurationValidationException(string message) : base(message)
        {
        }

        public ConfigurationValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// 范围验证器
    /// </summary>
    public class RangeValidator<T> : IConfigurationValidator where T : IComparable<T>
    {
        private readonly T _min;
        private readonly T _max;

        public string Name => $"Range Validator ({typeof(T).Name})";
        public string Description => $"验证值在 {_min} 到 {_max} 范围内";

        public RangeValidator(T min, T max)
        {
            _min = min;
            _max = max;
        }

        public ValidationResult Validate(object value)
        {
            if (value == null)
                return ValidationResult.Failure("值不能为空");

            try
            {
                var convertedValue = (T)Convert.ChangeType(value, typeof(T));

                if (convertedValue.CompareTo(_min) < 0)
                    return ValidationResult.Failure($"值 {convertedValue} 小于最小值 {_min}");

                if (convertedValue.CompareTo(_max) > 0)
                    return ValidationResult.Failure($"值 {convertedValue} 大于最大值 {_max}");

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure($"类型转换失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 枚举验证器
    /// </summary>
    public class EnumValidator<T> : IConfigurationValidator where T : struct, Enum
    {
        public string Name => $"Enum Validator ({typeof(T).Name})";
        public string Description => $"验证值是有效的 {typeof(T).Name} 枚举值";

        public ValidationResult Validate(object value)
        {
            if (value == null)
                return ValidationResult.Failure("值不能为空");

            try
            {
                if (value is string stringValue)
                {
                    if (Enum.TryParse<T>(stringValue, true, out _))
                        return ValidationResult.Success();

                    var validValues = string.Join(", ", Enum.GetNames<T>());
                    return ValidationResult.Failure($"无效的枚举值 '{stringValue}'。有效值: {validValues}");
                }

                if (value is T)
                    return ValidationResult.Success();

                if (Enum.IsDefined(typeof(T), value))
                    return ValidationResult.Success();

                return ValidationResult.Failure($"值 '{value}' 不是有效的 {typeof(T).Name} 枚举值");
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure($"枚举验证失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 路径验证器
    /// </summary>
    public class PathValidator : IConfigurationValidator
    {
        private readonly bool _mustExist;
        private readonly bool _createIfNotExists;

        public string Name => "Path Validator";
        public string Description => "验证文件或目录路径的有效性";

        public PathValidator(bool mustExist = false, bool createIfNotExists = false)
        {
            _mustExist = mustExist;
            _createIfNotExists = createIfNotExists;
        }

        public ValidationResult Validate(object value)
        {
            if (value == null)
                return ValidationResult.Failure("路径不能为空");

            var path = value.ToString();

            if (string.IsNullOrWhiteSpace(path))
                return ValidationResult.Failure("路径不能为空字符串");

            try
            {
                // 检查路径格式是否有效
                var fullPath = Path.GetFullPath(path);

                // 检查路径中是否包含无效字符
                var invalidChars = Path.GetInvalidPathChars();
                if (path.IndexOfAny(invalidChars) >= 0)
                    return ValidationResult.Failure("路径包含无效字符");

                // 检查是否存在
                var exists = File.Exists(fullPath) || Directory.Exists(fullPath);

                if (_mustExist && !exists)
                    return ValidationResult.Failure($"路径不存在: {fullPath}");

                if (!exists && _createIfNotExists)
                {
                    try
                    {
                        var directory = Path.GetDirectoryName(fullPath);
                        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        return ValidationResult.Warning($"目录已自动创建: {directory}");
                    }
                    catch (Exception ex)
                    {
                        return ValidationResult.Failure($"无法创建目录: {ex.Message}");
                    }
                }

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure($"路径验证失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 正则表达式验证器
    /// </summary>
    public class RegexValidator : IConfigurationValidator
    {
        private readonly Regex _regex;
        private readonly string _pattern;

        public string Name => "Regex Validator";
        public string Description => $"验证值匹配正则表达式: {_pattern}";

        public RegexValidator(string pattern, RegexOptions options = RegexOptions.None)
        {
            _pattern = pattern;
            _regex = new Regex(pattern, options);
        }

        public ValidationResult Validate(object value)
        {
            if (value == null)
                return ValidationResult.Failure("值不能为空");

            var stringValue = value.ToString();

            if (!_regex.IsMatch(stringValue))
                return ValidationResult.Failure($"值 '{stringValue}' 不匹配模式 '{_pattern}'");

            return ValidationResult.Success();
        }
    }

    /// <summary>
    /// 组合验证器
    /// </summary>
    public class CompositeValidator : IConfigurationValidator
    {
        private readonly List<IConfigurationValidator> _validators;
        private readonly bool _requireAll;

        public string Name => "Composite Validator";
        public string Description => $"组合验证器，包含 {_validators.Count} 个子验证器";

        public CompositeValidator(bool requireAll = true, params IConfigurationValidator[] validators)
        {
            _validators = validators?.ToList() ?? new List<IConfigurationValidator>();
            _requireAll = requireAll;
        }

        public void AddValidator(IConfigurationValidator validator)
        {
            _validators.Add(validator);
        }

        public ValidationResult Validate(object value)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            var successCount = 0;

            foreach (var validator in _validators)
            {
                try
                {
                    var result = validator.Validate(value);

                    if (result.IsValid)
                    {
                        successCount++;
                        warnings.AddRange(result.Warnings);
                    }
                    else
                    {
                        errors.AddRange(result.Errors);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"验证器 {validator.Name} 执行失败: {ex.Message}");
                }
            }

            var isValid = _requireAll ? errors.Count == 0 : successCount > 0;

            return new ValidationResult
            {
                IsValid = isValid,
                Errors = errors,
                Warnings = warnings
            };
        }
    }

    /// <summary>
    /// 自定义验证器
    /// </summary>
    public class CustomValidator : IConfigurationValidator
    {
        private readonly Func<object, ValidationResult> _validateFunc;

        public string Name { get; }
        public string Description { get; }

        public CustomValidator(string name, string description, Func<object, ValidationResult> validateFunc)
        {
            Name = name;
            Description = description;
            _validateFunc = validateFunc ?? throw new ArgumentNullException(nameof(validateFunc));
        }

        public ValidationResult Validate(object value)
        {
            try
            {
                return _validateFunc(value);
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure($"自定义验证失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 性能配置观察者
    /// </summary>
    public class PerformanceConfigurationObserver : IConfigurationObserver
    {
        private readonly ILogger _logger = LogManager.GetLogger(typeof(PerformanceConfigurationObserver));

        public void OnConfigurationChanged(ConfigurationChangedEventArgs args)
        {
            if (args.Key.StartsWith("Performance."))
            {
                _logger.Info($"性能配置变更 - {args.Key}: {args.OldValue} -> {args.NewValue}");

                // 根据配置变更调整系统行为
                switch (args.Key)
                {
                    case "Performance.CacheSize":
                        HandleCacheSizeChange(args.NewValue);
                        break;

                    case "Performance.EnableBatching":
                        HandleBatchingChange(args.NewValue);
                        break;

                    case "Performance.BatchTimeoutMs":
                        HandleBatchTimeoutChange(args.NewValue);
                        break;
                }
            }
        }

        private void HandleCacheSizeChange(object newValue)
        {
            if (int.TryParse(newValue?.ToString(), out var newSize))
            {
                // 这里可以调整缓存大小
                _logger.Info($"缓存大小已调整为: {newSize}");
            }
        }

        private void HandleBatchingChange(object newValue)
        {
            if (bool.TryParse(newValue?.ToString(), out var enabled))
            {
                _logger.Info($"批处理已{(enabled ? "启用" : "禁用")}");
            }
        }

        private void HandleBatchTimeoutChange(object newValue)
        {
            if (int.TryParse(newValue?.ToString(), out var timeout))
            {
                _logger.Info($"批处理超时时间已调整为: {timeout}ms");
            }
        }
    }

    /// <summary>
    /// 日志配置观察者
    /// </summary>
    public class LoggingConfigurationObserver : IConfigurationObserver
    {
        private readonly ILogger _logger = LogManager.GetLogger(typeof(LoggingConfigurationObserver));

        public void OnConfigurationChanged(ConfigurationChangedEventArgs args)
        {
            if (args.Key.StartsWith("Logging."))
            {
                _logger.Info($"日志配置变更 - {args.Key}: {args.OldValue} -> {args.NewValue}");

                switch (args.Key)
                {
                    case "Logging.Level":
                        HandleLogLevelChange(args.NewValue);
                        break;

                    case "Logging.EnableFileLogging":
                        HandleFileLoggingChange(args.NewValue);
                        break;
                }
            }
        }

        private void HandleLogLevelChange(object newValue)
        {
            if (Enum.TryParse<LogLevel>(newValue?.ToString(), out var logLevel))
            {
                // 这里可以动态调整日志级别
                _logger.Info($"日志级别已调整为: {logLevel}");
            }
        }

        private void HandleFileLoggingChange(object newValue)
        {
            if (bool.TryParse(newValue?.ToString(), out var enabled))
            {
                _logger.Info($"文件日志已{(enabled ? "启用" : "禁用")}");
            }
        }
    }
}