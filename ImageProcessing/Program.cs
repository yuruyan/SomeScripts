using CommonTools;
using ImageProcessing;
using Microsoft.Extensions.Logging;
using Shared;
using System.Reflection;

// 输入参数验证
if (!SharedHelper.CheckArgs(args, Resource.Help)) {
    return;
}

var Logger = SharedLogging.Logger;
try {
    _ = typeof(Proxy).GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
    args.ProcessService(typeof(Proxy));
} catch (Exception error) {
    Logger.LogError(error, "Program terminated");
} finally {
    SharedLogging.LoggerFactory.Dispose();
    SharedLogging.FileLoggerFactory.Dispose();
}