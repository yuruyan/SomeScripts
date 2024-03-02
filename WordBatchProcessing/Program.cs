using Microsoft.Extensions.Logging;
using Shared;
using System.Reflection;
using WordBatchProcessing;

var Logger = SharedLogging.Logger;

// 输入参数验证
if (!SharedHelper.CheckArgs(args, Resource.Help)) {
    return;
}

try {
    _ = typeof(Services).GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
    args.ProcessService(typeof(Services));
} catch (Exception error) {
    Logger.LogError(error, "Program terminated");
} finally {
    SharedLogging.LoggerFactory.Dispose();
    SharedLogging.FileLoggerFactory.Dispose();
}