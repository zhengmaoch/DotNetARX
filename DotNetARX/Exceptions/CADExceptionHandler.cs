namespace DotNetARX.Exceptions
{
    /// <summary>
    /// 统一的CAD异常处理器
    /// </summary>
    public static class CADExceptionHandler
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(CADExceptionHandler));

        /// <summary>
        /// 执行操作并处理异常
        /// </summary>
        public static T ExecuteWithExceptionHandling<T>(
            Func<T> operation,
            T defaultValue = default,
            [CallerMemberName] string operationName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                return operation();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception acadEx)
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                Logger.Error($"AutoCAD异常在{operationName}: {acadEx.ErrorStatus} - {acadEx.Message}", acadEx);

                // 根据错误类型决定是否显示用户提示
                if (ShouldShowUserAlert(acadEx.ErrorStatus))
                {
                    Autodesk.AutoCAD.ApplicationServices.Core.Application.ShowAlertDialog($"{operationName}失败: {GetUserFriendlyMessage(acadEx.ErrorStatus)}");
                }

                return defaultValue;
            }
            catch (DotNetARXException dotNetARXEx)
            {
                Logger.Error($"DotNetARX异常在{dotNetARXEx.Operation}: {dotNetARXEx.Message}", dotNetARXEx);
                Autodesk.AutoCAD.ApplicationServices.Core.Application.ShowAlertDialog($"{dotNetARXEx.Operation}失败: {dotNetARXEx.Message}");
                return defaultValue;
            }
            catch (Exception ex)
            {
                Logger.Error($"系统异常在{operationName}: {ex.Message}", ex);
                Autodesk.AutoCAD.ApplicationServices.Core.Application.ShowAlertDialog($"{operationName}发生未预期错误，请查看日志获取详细信息");
                return defaultValue;
            }
        }

        /// <summary>
        /// 执行操作并处理异常（无返回值版本）
        /// </summary>
        public static bool ExecuteWithExceptionHandling(
            Action operation,
            [CallerMemberName] string operationName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            return ExecuteWithExceptionHandling(() =>
            {
                operation();
                return true;
            }, false, operationName, filePath, lineNumber);
        }

        /// <summary>
        /// 判断是否应该向用户显示警告
        /// </summary>
        private static bool ShouldShowUserAlert(ErrorStatus errorStatus)
        {
            // 某些错误状态不需要向用户显示（如取消操作等）
            switch (errorStatus)
            {
                case ErrorStatus.UserBreak:
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>
        /// 获取用户友好的错误消息
        /// </summary>
        private static string GetUserFriendlyMessage(ErrorStatus errorStatus)
        {
            switch (errorStatus)
            {
                case ErrorStatus.InvalidInput:
                    return "输入参数无效";

                case ErrorStatus.OutOfRange:
                    return "参数超出有效范围";

                case ErrorStatus.InvalidObjectId:
                    return "对象ID无效";

                case ErrorStatus.NullObjectPointer:
                    return "对象指针为空";

                case ErrorStatus.NotOpenForWrite:
                    return "对象未以写入模式打开";

                case ErrorStatus.NotOpenForRead:
                    return "对象未以读取模式打开";

                case ErrorStatus.NotInDatabase:
                    return "对象不在数据库中";

                case ErrorStatus.NoActiveTransactions:
                    return "没有活动事务";

                case ErrorStatus.WrongDatabase:
                    return "数据库不匹配";

                case ErrorStatus.InvalidLayer:
                    return "图层无效";

                case ErrorStatus.InvalidBlockName:
                    return "块名称无效";

                case ErrorStatus.DuplicateRecordName:
                    return "记录名称重复";

                case ErrorStatus.UserBreak:
                    return "用户中断操作";

                default:
                    return $"操作失败 (错误代码: {errorStatus})";
            }
        }

        /// <summary>
        /// 创建并抛出CAD操作异常
        /// </summary>
        public static void ThrowCADException(string operation, string message, ErrorStatus? errorStatus = null)
        {
            Logger.Error($"抛出CAD异常 - {operation}: {message}");
            throw new DotNetARXException(operation, message);
        }

        /// <summary>
        /// 创建并抛出实体操作异常
        /// </summary>
        public static void ThrowEntityException(string operation, ObjectId entityId, string message)
        {
            Logger.Error($"抛出实体异常 - {operation}: {message}, EntityId: {entityId}");
            throw new DotNetARXException(operation + entityId, message);
        }
    }
}