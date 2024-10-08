﻿using CommonTools.Utils;
using Microsoft.Extensions.Logging;
using OfficeFormatConverter;

// 输入参数验证
if (!ArgumentUtils.CheckArgs(args, Resource.Help)) {
    return;
}

var Logger = SharedLogging.Logger;
var Config = ArgumentUtils.GetConfiguration(args);
var sourcePath = Config["sourcePath"];
var savePath = Config["savePath"];

try {
    if (string.IsNullOrEmpty(savePath)) {
        Logger.LogError("Save path cannot be empty");
        return;
    }
    if (ArgumentUtils.ValidateFileArgument(sourcePath, "sourcePath")) {
        return;
    }
    Service.Convert(sourcePath!, savePath!);
} catch (Exception error) {
    Logger.LogError(error, "Program terminated");
} finally {
    SharedLogging.LoggerFactory.Dispose();
    SharedLogging.FileLoggerFactory.Dispose();
}
