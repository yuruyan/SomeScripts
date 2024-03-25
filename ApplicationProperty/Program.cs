using ApplicationProperty;
using CommonTools.Utils;
using Microsoft.Extensions.Logging;
using Shared;
using System.Diagnostics;

// 输入参数验证
if (!ArgumentUtils.CheckArgs(args, Resource.Help)) {
    return;
}

var Logger = SharedLogging.Logger;
var Config = ArgumentUtils.GetConfiguration(args);
var filepath = Config["filepath"];

// 获取文件属性
try {
    if (!ArgumentUtils.ValidateFileArgument(filepath, "filepath")) {
        return;
    }
    Console.WriteLine(FileVersionInfo.GetVersionInfo(filepath!));
} catch (Exception error) {
    Logger.LogError(error, "Program terminated");
} finally {
    SharedLogging.LoggerFactory.Dispose();
    SharedLogging.FileLoggerFactory.Dispose();
}
