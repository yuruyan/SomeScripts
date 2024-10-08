﻿using CommonTools.Utils;
using DirectoryComparison;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// 输入参数验证
if (!ArgumentUtils.CheckArgs(args, Resource.Help)) {
    return;
}

var Logger = SharedLogging.Logger;
var Config = ArgumentUtils.GetConfiguration(args);

try {
    switch (args[0].ToLowerInvariant()) {
        case "savestructure":
            SaveStructure(Config);
            break;
        case "comparedirectories":
            CompareDirectories(Config);
            break;
        default:
            Logger.LogError("Invalid argument");
            break;
    }
} catch (Exception error) {
    Logger.LogError(error, "Program terminated");
} finally {
    SharedLogging.LoggerFactory.Dispose();
    SharedLogging.FileLoggerFactory.Dispose();
}

void CompareDirectories(IConfiguration config) {
    string? file1 = config["StructFile1"], file2 = config["StructFile2"];
    if (!ArgumentUtils.ValidateFileArgument(file1, "StructFile1")) {
        return;
    }
    if (!ArgumentUtils.ValidateFileArgument(file2, "StructFile2")) {
        return;
    }

    Service.CompareDirectories(file1!, file2!);
}

void SaveStructure(IConfiguration config) {
    string? dir = config["Directory"], file = config["SavePath"];
    if (!ArgumentUtils.ValidateDirectoryArgument(dir, "Directory")) {
        return;
    }
    if (string.IsNullOrEmpty(file)) {
        Logger.LogError("Argument 'SavePath' cannot be empty");
        return;
    }

    Service.SaveStructure(dir!, file!);
}
