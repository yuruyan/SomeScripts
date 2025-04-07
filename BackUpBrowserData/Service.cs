using CommonTools.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BackUpBrowserData;

public static class Service {
    private static readonly ILogger Logger = SharedLogging.Logger;

    public static void BackUpHistory(IConfiguration configuration) {
        const string BrowserArgName = "browser";
        const string SavePathArgName = "savePath";
        const string BrowserDataPathArgName = "browserDataPath";

        var savePathArg = configuration[SavePathArgName]!;
        var browserArg = configuration[BrowserArgName];
        var browserDataPathArg = configuration[BrowserDataPathArgName];

        // 检查路径
        savePathArg = Path.GetFullPath(savePathArg);
        string saveDirectory = Path.GetDirectoryName(savePathArg)!;
        if (!Directory.Exists(saveDirectory)) {
            Logger.LogError("Directory '{saveDirectory}' doesn't exist", saveDirectory);
            return;
        }
        // 验证参数
        if (string.IsNullOrEmpty(browserArg) && string.IsNullOrEmpty(browserDataPathArg)) {
            Logger.LogError("Argument {browserArg} or {BrowserDataPathArgName} cannot be empty at the same time", BrowserArgName, BrowserDataPathArgName);
            return;
        }
        // Use browserDataPathArg
        if (!string.IsNullOrEmpty(browserDataPathArg)) {
            if (!ArgumentUtils.ValidateDirectoryArgument(browserDataPathArg, BrowserDataPathArgName)) {
                return;
            }
            BrowserService.BackUpHistory(browserDataPathArg, savePathArg);
            return;
        }
        // Use browserArg
        if (!Enum.TryParse(browserArg, true, out BrowserType browserType)) {
            Logger.LogError("Invalid argument {BrowserArgName}", BrowserArgName);
            return;
        }
        BrowserService.BackUpHistory(browserType, savePathArg);
    }
}
