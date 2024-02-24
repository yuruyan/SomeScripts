﻿using Microsoft.Extensions.Logging;
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
// 参数验证
if (string.IsNullOrEmpty(sourcePath)) {
    Logger.LogError("参数 sourcePath 不能为空");
    return;
}
if (string.IsNullOrEmpty(savePath)) {
    Logger.LogError("参数 savePath 不能为空");
    return;
}
// 验证文件/文件夹是否存在
if (!File.Exists(sourcePath)) {
    Logger.LogError($"路径 '{sourcePath}' 不存在");
    return;
}
if (Path.GetDirectoryName(savePath) is var saveDir && !Directory.Exists(saveDir)) {
    Logger.LogError($"目录 '{saveDir}' 不存在");
    return;
}

try {
    Service.Convert(sourcePath, savePath);
} catch (Exception error) {
    Logger.LogError(error, "Program terminated");
    Environment.Exit(-1);
}
