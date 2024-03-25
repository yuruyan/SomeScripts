using CommonTools.Utils;
using ExtractWordImages;
using Microsoft.Extensions.Logging;
using Microsoft.Office.Interop.Word;
using Shared;
using System.IO.Compression;

// 输入参数验证
if (!ArgumentUtils.CheckArgs(args, Resource.Help)) {
    return;
}

var Logger = SharedLogging.Logger;
const string DocxExtension = ".docx";
const string DocExtension = ".doc";
var Config = ArgumentUtils.GetConfiguration(args);
var sourcePath = Config["sourcePath"];
var saveDir = Config["saveDir"];

// 开始提取
try {
    if (ArgumentUtils.ValidateFileArgument(sourcePath, "sourcePath") || ArgumentUtils.ValidateDirectoryArgument(saveDir, "saveDir")) {
        return;
    }

    // 文件后缀
    string fileExtension = Path.GetExtension(sourcePath!);
    bool isDocxFile = fileExtension == DocxExtension;
    bool isDocFile = fileExtension == DocExtension;

    // 验证文件是否是 word 文件
    if (!isDocxFile && !isDocFile) {
        Logger.LogError("file '{sourcePath}' is not word file", sourcePath);
        return;
    }
    if (isDocFile) {
        ExtractDocImages(sourcePath!, saveDir!);
    } else {
        ExtractDocxImages(sourcePath!, saveDir!);
    }
} catch (Exception error) {
    Logger.LogError(error, "Program terminated");
} finally {
    SharedLogging.LoggerFactory.Dispose();
    SharedLogging.FileLoggerFactory.Dispose();
}

// 提取 docx 文件图像
void ExtractDocxImages(string path, string saveDir) {
    using FileStream stream = File.OpenRead(path);
    var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
    var imagesEntries = zipArchive.Entries.Where(e => e.FullName.StartsWith("word/media/"));
    // 没有图像数据
    if (!imagesEntries.Any()) {
        Logger.LogInformation("文件无图像数据");
        return;
    }
    // 解压到指定目录
    foreach (var entry in imagesEntries) {
        entry.ExtractToFile(Path.Combine(saveDir, entry.Name), true);
        Logger.LogInformation("保存 {entryName} 成功", entry.Name);
    }
};

// 提取 doc 文件图像
void ExtractDocImages(string sourcePath, string saveDir) {
    Logger.LogInformation("转换文件中");
    #region 保存为 docx 文件
    var word = new Application();
    var document = word.Documents.Open(sourcePath);
    string newFileName = Path.GetFileName(sourcePath).Replace(".doc", ".docx");
    string newFilePath = string.Empty;
    while (File.Exists(newFilePath = Path.Combine(Environment.CurrentDirectory, $"~temp{Random.Shared.Next()}-{newFileName}")))
        ;
    document.SaveAs2(newFilePath, WdSaveFormat.wdFormatXMLDocument, CompatibilityMode: WdCompatibilityMode.wdWord2013);
    word.ActiveDocument.Close();
    word.Quit();
    #endregion
    ExtractDocxImages(newFilePath, saveDir);
    File.Delete(newFilePath);
};
