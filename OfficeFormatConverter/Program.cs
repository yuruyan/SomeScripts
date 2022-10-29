using NLog;
using OfficeFormatConverter;
using Shared;

// 输入参数验证
if (!Helper.CheckArgs(args, Resource.Help)) {
    return;
}

Logger Logger = LogManager.GetCurrentClassLogger();

var Config = Helper.GetConfiguration(args);
string sourcePath = Config["sourcePath"];
string savePath = Config["savePath"];
// 参数验证
if (string.IsNullOrEmpty(sourcePath)) {
    Logger.Error("参数 sourcePath 不能为空");
    return;
}
if (string.IsNullOrEmpty(savePath)) {
    Logger.Error("参数 savePath 不能为空");
    return;
}
// 验证文件/文件夹是否存在
if (!File.Exists(sourcePath)) {
    Logger.Error($"路径 '{sourcePath}' 不存在");
    return;
}
if (Path.GetDirectoryName(savePath) is var saveDir && !Directory.Exists(saveDir)) {
    Logger.Error($"目录 '{saveDir}' 不存在");
    return;
}

try {
    Service.Convert(sourcePath, savePath);
} catch (Exception error) {
    Logger.Error(error);
}
