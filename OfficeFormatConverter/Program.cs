using Microsoft.Extensions.Logging;
using OfficeFormatConverter;
using Shared;

// 输入参数验证
if (!SharedHelper.CheckArgs(args, Resource.Help)) {
    return;
}

var Logger = SharedLogging.Logger;
var Config = SharedHelper.GetConfiguration(args);
var sourcePath = Config["sourcePath"];
var savePath = Config["savePath"];

try {
    if (SharedHelper.ValidateFileArgument(sourcePath, "sourcePath") || SharedHelper.ValidateFileArgument(Path.GetDirectoryName(savePath), "savePath")) {
        return;
    }
    Service.Convert(sourcePath!, savePath!);
} catch (Exception error) {
    Logger.LogError(error, "Program terminated");
} finally {
    SharedLogging.LoggerFactory.Dispose();
    SharedLogging.FileLoggerFactory.Dispose();
}
