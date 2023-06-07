using CommonTools.Utils;
using NLog;
using System.IO.Compression;

namespace BackUpBrowserData;

public static class BrowserService {
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly string EdgeDefaultFolder = $"C:\\Users\\{Environment.UserName}\\AppData\\Local\\Microsoft\\Edge\\User Data\\Default";
    private static readonly string ChromeDefaultFolder = $"C:\\Users\\{Environment.UserName}\\AppData\\Local\\Google\\Chrome\\User Data\\Default";

    public static void BackUpHistory(BrowserType browser, string savePath) {
        string DefaultFolder = EdgeDefaultFolder;
        // default 目录
        string defaultFolder = browser switch {
            BrowserType.Edge => EdgeDefaultFolder,
            BrowserType.Chrome => ChromeDefaultFolder,
            _ => throw new ArgumentException($"Error {nameof(browser)} argument"),
        };
        // Collections Directory
        var collectionsFile = Path.Combine(defaultFolder, "Collections/collectionsSQLite");
        var saveDirectory = Path.GetDirectoryName(savePath)!;
        // 复制文件到临时目录
        var tempDir = CommonUtils.GetUniqueRandomFileName(saveDirectory);
        if (tempDir == null) {
            Logger.Error("Create temp directory failed");
            return;
        }
        tempDir = Path.Combine(saveDirectory, tempDir);
        Directory.CreateDirectory(tempDir);
        // 判断文件夹创建是否成功
        if (!Directory.Exists(tempDir)) {
            Logger.Error($"创建文件夹 '{tempDir}' 失败");
            return;
        }
        // Copy files to temp directory
        foreach (var path in Directory.GetFiles(defaultFolder)) {
            var filename = Path.GetFileName(path);
            File.Copy(path, Path.Combine(tempDir, filename));
            Logger.Debug($"{filename} Copyed");
        }
        // Copy collections file
        if (File.Exists(collectionsFile)) {
            File.Copy(collectionsFile, Path.Combine(tempDir, "collectionsSQLite"));
            Logger.Debug("Copy collectionsSQLite done");
        }
        // Delete the original file
        File.Delete(savePath);
        Logger.Debug("压缩中...");
        // 开始压缩
        ZipFile.CreateFromDirectory(tempDir, savePath); // 简易
        // 删除临时文件夹
        Directory.Delete(tempDir, true);
        Logger.Debug("压缩完毕");
    }
}
