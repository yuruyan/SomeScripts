using CommonTools.Utils;
using Microsoft.Extensions.Logging;
using Shared;
using SubtitleAdjustment;

// 输入参数验证
if (!ArgumentUtils.CheckArgs(args, Resource.Help)) {
    return;
}

var Logger = SharedLogging.Logger;
var Config = ArgumentUtils.GetConfiguration(args);
var sourcePath = Config["sourcePath"];
var savePath = Config["savePath"];
var offsetArg = Config["offset"];

try {
    if (ArgumentUtils.ValidateFileArgument(sourcePath, "sourcePath")) {
        return;
    }
    if (string.IsNullOrEmpty(savePath)) {
        Logger.LogError("参数 savePath 不能为空");
        return;
    }
    // 解析 offset
    if (!int.TryParse(offsetArg, out var offset)) {
        Logger.LogError("解析 offset 失败");
        return;
    }
    Service.Adjust(sourcePath!, savePath, offset);
} catch (Exception error) {
    Logger.LogError(error, "Program terminated");
} finally {
    SharedLogging.LoggerFactory.Dispose();
    SharedLogging.FileLoggerFactory.Dispose();
}