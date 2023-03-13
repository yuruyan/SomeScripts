using BackUpBrowserData;
using CommonTools.Utils;
using NLog;
using Shared;
using System.IO.Compression;

// 输入参数验证
if (!Helper.CheckArgs(args, Resource.Help)) {
    return;
}

string EdgeDefaultFolder = $"C:\\Users\\{Environment.UserName}\\AppData\\Local\\Microsoft\\Edge\\User Data\\Default";
string ChromeDefaultFolder = $"C:\\Users\\{Environment.UserName}\\AppData\\Local\\Google\\Chrome\\User Data\\Default";
string DefaultFolder = EdgeDefaultFolder;
const BrowserType DefaultBrowser = BrowserType.Edge;
const bool DefaultOverrideIfExist = true;
Logger Logger = LogManager.GetCurrentClassLogger();

var Config = Helper.GetConfiguration(args);
string savePath = Config["savePath"];
// 验证参数
if (!bool.TryParse(Config["override"], out bool overrideIfExist)) {
    overrideIfExist = DefaultOverrideIfExist;
}
if (!Enum.TryParse(Config["browser"], out BrowserType browserType)) {
    browserType = DefaultBrowser;
}
if (new FileInfo(savePath).Directory is var dir && (dir is null || !dir.Exists)) {
    Logger.Error($"目录 '{Path.GetDirectoryName(savePath)}' 不存在！");
    return;
}

// default 目录
string defaultFolder = browserType switch {
    BrowserType.Edge => EdgeDefaultFolder,
    BrowserType.Chrome => ChromeDefaultFolder,
    _ => DefaultFolder,
};

// 复制文件到临时目录
string tempDir = string.Empty;
while (Directory.Exists(tempDir = $"{Path.Combine(Path.GetDirectoryName(savePath), RandomUtils.RandomLetter(16))}")) ;
Directory.CreateDirectory(tempDir);
// 判断文件夹创建是否成功
if (!Directory.Exists(tempDir)) {
    Console.WriteLine($"创建文件夹 '{tempDir}' 失败");
}
foreach (var path in Directory.GetFiles(defaultFolder)) {
    File.Copy(path, Path.Combine(tempDir, Path.GetFileName(path)));
}

// 设置 savePath
if (overrideIfExist) {
    File.Delete(savePath);
} else {
    string? dirName = Path.GetDirectoryName(savePath);
    string name = Path.GetFileNameWithoutExtension(savePath);
    string extension = Path.GetExtension(savePath);
    int count = 1;
    while (File.Exists(savePath)) {
        savePath = Path.Combine(dirName, $"{name}（{count++}）{extension}");
    }
}

// 开始压缩
//ZipFile.CreateFromDirectory(tempDir, savePath); // 简易
using var archive = ZipFile.Open(savePath, ZipArchiveMode.Create);
foreach (var path in Directory.GetFiles(tempDir)) {
    string name = Path.GetFileName(path);
    archive.CreateEntryFromFile(path, name);
    Logger.Debug($"正在压缩 '{name}'");
}
// 删除临时文件夹
Directory.Delete(tempDir, true);
Logger.Debug("压缩完毕");
