using ApplicationProperty;
using Microsoft.Extensions.Logging;
using Shared;
using System.Diagnostics;

// 输入参数验证
if (!SharedHelper.CheckArgs(args, Resource.Help)) {
    return;
}

var Logger = SharedLogging.Logger;
var Config = SharedHelper.GetConfiguration(args);
var filepath = Config["filepath"];
if (!File.Exists(filepath)) {
    Logger.LogError($"文件 '{nameof(filepath)}' 不存在！");
    return;
}
// 获取文件属性
try {
    Console.WriteLine(FileVersionInfo.GetVersionInfo(filepath));
} catch (Exception error) {
    Logger.LogError(error, "Program terminated");
    Environment.Exit(-1);
}
