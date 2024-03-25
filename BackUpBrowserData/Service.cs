using CommonTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared;

namespace BackUpBrowserData;

public static class Service {
    private static readonly ILogger Logger = SharedLogging.Logger;

    public static void BackUpHistory(IConfiguration configuration) {
        const string BrowserArgName = "browser";
        const string SavePathArgName = "savePath";

        var savePathArg = configuration[SavePathArgName];
        var browserArg = configuration[BrowserArgName];
        // 验证参数
        if (string.IsNullOrEmpty(savePathArg)) {
            Logger.LogError("Argument {SavePathArgName} cannot be empty", SavePathArgName);
            return;
        }
        if (!Enum.TryParse(browserArg, out BrowserType browserType)) {
            Logger.LogError("Invalid argument {BrowserArgName}", BrowserArgName);
            return;
        }
        // 检查路径
        string saveDirectory = Path.GetDirectoryName(savePathArg)!;
        if (!Directory.Exists(saveDirectory)) {
            Logger.LogError("Directory '{saveDirectory}' doesn't exist", saveDirectory);
            return;
        }
        BrowserService.BackUpHistory(browserType, savePathArg!);
    }
}
