using NLog;
using OfficeFormatConverter;
using Shared;

// 输入参数验证
if (!Helper.CheckArgs(args, Resource.Help)) {
    return;
}

Logger Logger = LogManager.GetCurrentClassLogger();

var Config = Helper.GetConfiguration(args);
string sourcePath = Config["sourcePath"];
string savePath = Config["savePath"];
// 参数验证
if (string.IsNullOrEmpty(sourcePath)) {
    Logger.Error("参数 sourcePath 不能为空");
    return;
}
if (string.IsNullOrEmpty(savePath)) {
    Logger.Error("参数 savePath 不能为空");
    return;
}
// 验证文件/文件夹是否存在
if (!File.Exists(sourcePath)) {
    Logger.Error($"路径 '{sourcePath}' 不存在");
    return;
}
if (Path.GetDirectoryName(savePath) is var saveDir && !Directory.Exists(saveDir)) {
    Logger.Error($"目录 '{saveDir}' 不存在");
    return;
}

string sourceFileExtension = Path.GetExtension(sourcePath).ToLowerInvariant();
string saveFileExtension = Path.GetExtension(savePath).ToLowerInvariant();

const string Doc = "doc";
const string Docx = "docx";
const string Txt = "txt";
const string Pdf = "pdf";
const string Html = "html";
const string Ppt = "ppt";
const string Pptx = "pptx";
const string Png = "png";
const string Jpeg = "jpeg";
const string Xls = "xls";
const string Xlsx = "xlsx";
const string Csv = "csv";

Action<string, string> method = (sourceFileExtension, saveFileExtension) switch {
    (Doc, Docx) => Service.WordConvert,
    (Doc, Txt) => Service.WordConvert,
    (Doc, Pdf) => Service.WordConvert,
    (Doc, Html) => Service.WordConvert,
    (Docx, Doc) => Service.WordConvert,
    (Docx, Txt) => Service.WordConvert,
    (Docx, Pdf) => Service.WordConvert,
    (Docx, Html) => Service.WordConvert,
    (Txt, Doc) => Service.WordConvert,
    (Txt, Docx) => Service.WordConvert,
    (Ppt, Pptx) => Service.PowerPointConvert,
    (Ppt, Pdf) => Service.PowerPointConvert,
    (Ppt, Png) => Service.PowerPointConvert,
    (Ppt, Jpeg) => Service.PowerPointConvert,
    (Pptx, Ppt) => Service.PowerPointConvert,
    (Pptx, Pdf) => Service.PowerPointConvert,
    (Pptx, Png) => Service.PowerPointConvert,
    (Pptx, Jpeg) => Service.PowerPointConvert,
    (Xls, Xlsx) => Service.ExcelConvert,
    (Xls, Csv) => Service.ExcelConvert,
    (Xls, Pdf) => Service.ExcelConvert,
    (Xls, Txt) => Service.ExcelConvert,
    (Xls, Html) => Service.ExcelConvert,
    (Xlsx, Xls) => Service.ExcelConvert,
    (Xlsx, Csv) => Service.ExcelConvert,
    (Xlsx, Pdf) => Service.ExcelConvert,
    (Xlsx, Txt) => Service.ExcelConvert,
    (Xlsx, Html) => Service.ExcelConvert,
    _ => throw new ArgumentException("没有匹配的转换模式")
};
