using ApplicationProperty;
using NLog;
using Shared;
using System.Diagnostics;

// 输入参数验证
if (!SharedHelper.CheckArgs(args, Resource.Help)) {
    return;
}

Logger Logger = LogManager.GetCurrentClassLogger();
var Config = SharedHelper.GetConfiguration(args);
string filepath = Config["filepath"];
if (!File.Exists(filepath)) {
    Logger.Error($"文件 '{nameof(filepath)}' 不存在！");
    return;
}
// 获取文件属性
try {
    Console.WriteLine(FileVersionInfo.GetVersionInfo(filepath));
} catch (Exception error) {
    Logger.Error(error);
    Environment.Exit(-1);
}
