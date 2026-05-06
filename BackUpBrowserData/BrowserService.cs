using CommonTools.Utils;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace BackUpBrowserData;

public static class BrowserService {
    private static readonly ILogger Logger = SharedLogging.Logger;
    private static readonly string EdgeDefaultFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Microsoft\\Edge\\User Data\\Default"
    );
    private static readonly string ChromeDefaultFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Google\\Chrome\\User Data\\Default"
    );

    public static void BackUpHistory(string browserDataPath, string savePath) {
        // Collections Directory
        var collectionsFile = Path.Combine(browserDataPath, "Collections/collectionsSQLite");
        var saveDirectory = Path.GetDirectoryName(savePath)!;
        // 复制文件到临时目录
        var tempDir = CommonUtils.GetUniqueRandomFileName(saveDirectory);
        if (tempDir == null) {
            Logger.LogError("Create temp directory failed");
            return;
        }
        tempDir = Path.Combine(saveDirectory, tempDir);
        Directory.CreateDirectory(tempDir);
        // Copy files to temp directory
        foreach (var path in Directory.GetFiles(browserDataPath)) {
            var filename = Path.GetFileName(path);
            try {
                File.Copy(path, Path.Combine(tempDir, filename));
                Logger.LogInformation($"{filename} Copyed");
            } catch {
                Logger.LogError($"Copy file {filename} failed");
            }
        }
        // Copy collections file
        if (File.Exists(collectionsFile)) {
            File.Copy(collectionsFile, Path.Combine(tempDir, "collectionsSQLite"));
            Logger.LogInformation("Copy collectionsSQLite done");
        }
        // Delete the original file
        File.Delete(savePath);
        Logger.LogInformation("压缩中...");
        // 开始压缩
        ZipFile.CreateFromDirectory(tempDir, savePath); // 简易
        // 删除临时文件夹
        Directory.Delete(tempDir, true);
        Logger.LogInformation("压缩完毕");
    }

    public static void BackUpHistory(BrowserType browser, string savePath) {
        // default 目录
        string defaultFolder = browser switch {
            BrowserType.Edge => EdgeDefaultFolder,
            BrowserType.Chrome => ChromeDefaultFolder,
            _ => throw new ArgumentException($"Error {nameof(browser)} argument"),
        };
        BackUpHistory(defaultFolder, savePath);
    }
}
