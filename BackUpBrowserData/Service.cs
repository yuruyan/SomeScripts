using Microsoft.Extensions.Configuration;
using NLog;

namespace BackUpBrowserData;

public static class Service {
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static void BackUpHistory(IConfiguration configuration) {
        const string BrowserArgName = "browser";
        const string SavePathArgName = "savePath";

        string savePathArg = configuration[SavePathArgName];
        string browserArg = configuration[BrowserArgName];
        // 验证参数
        if (string.IsNullOrEmpty(savePathArg)) {
            Logger.Error($"Argument {SavePathArgName} cannot be empty");
            return;
        }
        if (!Enum.TryParse(browserArg, out BrowserType browserType)) {
            Logger.Error($"Invalid argument {BrowserArgName}");
            return;
        }
        // 检查路径
        string saveDirectory = Path.GetDirectoryName(savePathArg)!;
        if (!Directory.Exists(saveDirectory)) {
            Logger.Error($"目录 '{saveDirectory}' 不存在！");
            return;
        }
        BrowserService.BackUpHistory(browserType, savePathArg);
    }
}
