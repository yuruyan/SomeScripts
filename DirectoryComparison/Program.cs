using DirectoryComparison;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared;

// 输入参数验证
if (!SharedHelper.CheckArgs(args, Resource.Help)) {
    return;
}

var Logger = SharedLogging.Logger;
var Config = SharedHelper.GetConfiguration(args);

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
    Environment.Exit(-1);
}

void CompareDirectories(IConfiguration config) {
    string? file1 = config["StructFile1"], file2 = config["StructFile2"];
    if (!SharedHelper.ValidateFileArgument(file1, "StructFile1")) {
        return;
    }
    if (!SharedHelper.ValidateFileArgument(file2, "StructFile2")) {
        return;
    }

    Service.CompareDirectories(file1!, file2!);
}

void SaveStructure(IConfiguration config) {
    string? dir = config["Directory"], file = config["SavePath"];
    if (!SharedHelper.ValidateDirectoryArgument(dir, "Directory")) {
        return;
    }
    if (string.IsNullOrEmpty(file)) {
        Logger.LogError("Argument 'SavePath' cannot be empty");
        return;
    }

    Service.SaveStructure(dir!, file!);
}
