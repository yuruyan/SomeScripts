using Microsoft.Extensions.Logging;
using Shared;
using WordBatchProcessing;

var Logger = SharedLogging.Logger;

// 输入参数验证
if (!SharedHelper.CheckArgs(args, Resource.Help)) {
    return;
}

try {
    Services.Process(args);
} catch (Exception error) {
    Logger.LogError(error, "Program terminated");
} finally {
    SharedLogging.LoggerFactory.Dispose();
    SharedLogging.FileLoggerFactory.Dispose();
}