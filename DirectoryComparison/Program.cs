using DirectoryHistoryComparison;
using NLog;
using Shared;

// 输入参数验证
if (!SharedHelper.CheckArgs(args, Resource.Help)) {
    return;
}

Logger Logger = LogManager.GetCurrentClassLogger();
var Config = SharedHelper.GetConfiguration(args);

try {
    switch (args[0].ToLowerInvariant()) {
        case "savestructure":
            SaveStructure(Config["Directory"], Config["SavePath"]);
            break;
        case "comparedirectories":
            CompareDirectories(Config["StructFile1"], Config["StructFile2"]);
            break;
        default:
            Logger.Error("Invalid argument");
            break;
    }
} catch (Exception error) {
    Logger.Error(error);
    Environment.Exit(-1);
}

void CompareDirectories(string path1, string path2) {
    if (string.IsNullOrEmpty(path1)) {
        Logger.Error($"Argument StructFile1 cannot be empty");
        return;
    }
    if (!File.Exists(path1)) {
        Logger.Error($"File '{path1}' doesn't exist");
        return;
    }
    if (string.IsNullOrEmpty(path2)) {
        Logger.Error($"Argument StructFile2 cannot be empty");
        return;
    }
    if (!File.Exists(path2)) {
        Logger.Error($"File '{path2}' doesn't exist");
        return;
    }

    Service.CompareDirectories(path1, path2);
}

void SaveStructure(string directory, string savePath) {
    if (string.IsNullOrEmpty(directory)) {
        Logger.Error($"Argument Directory cannot be empty");
        return;
    }
    if (!Directory.Exists(directory)) {
        Logger.Error($"Directory '{directory}' doesn't exist");
        return;
    }

    Service.SaveStructure(directory, savePath);
}
