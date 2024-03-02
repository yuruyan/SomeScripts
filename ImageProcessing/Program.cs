using ImageProcessing;
using Microsoft.Extensions.Logging;
using Shared;

// 输入参数验证
if (!SharedHelper.CheckArgs(args, Resource.Help)) {
    return;
}

var Logger = SharedLogging.Logger;
try {
    Proxy.Parse(args[0])(SharedHelper.GetConfiguration(args));
} catch (Exception error) {
    Logger.LogError(error, "Program terminated");
} finally {
    SharedLogging.LoggerFactory.Dispose();
    SharedLogging.FileLoggerFactory.Dispose();
}