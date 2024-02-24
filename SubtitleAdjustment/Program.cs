using Microsoft.Extensions.Logging;
using Shared;
using SubtitleAdjustment;

// 输入参数验证
if (!SharedHelper.CheckArgs(args, Resource.Help)) {
    return;
}

var Logger = SharedLogging.Logger;
var Config = SharedHelper.GetConfiguration(args);
var sourcePath = Config["sourcePath"];
var savePath = Config["savePath"];
var offsetArg = Config["offset"];

// 参数验证
if (string.IsNullOrEmpty(sourcePath)) {
    Logger.LogError("参数 sourcePath 不能为空");
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

try {
    Service.Adjust(sourcePath, savePath, offset);
} catch (Exception error) {
    Logger.LogError(error, "Program terminated");
    Environment.Exit(-1);
}