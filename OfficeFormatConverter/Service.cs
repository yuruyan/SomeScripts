using CommonTools;
using Microsoft.Extensions.Logging;
using Shared;
using System.Runtime.InteropServices;
using PpMediaTaskStatus = Microsoft.Office.Interop.PowerPoint.PpMediaTaskStatus;
using PpSaveAsFileType = Microsoft.Office.Interop.PowerPoint.PpSaveAsFileType;
using XlFileFormat = Microsoft.Office.Interop.Excel.XlFileFormat;
namespace OfficeFormatConverter;

public static class Service {
    private const string Doc = ".doc";
    private const string Docx = ".docx";
    private const string Txt = ".txt";
    private const string Pdf = ".pdf";
    private const string Html = ".html";
    private const string Ppt = ".ppt";
    private const string Pptx = ".pptx";
    private const string Png = ".png";
    private const string Jpg = ".jpg";
    private const string Mp4 = ".mp4";
    private const string Xls = ".xls";
    private const string Xlsx = ".xlsx";
    private const string Csv = ".csv";

    private static readonly ILogger Logger = SharedLogging.Logger;

    /// <summary>
    /// 选择匹配的转换器并转换
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="savePath"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">没有找到合适的转换器</exception>
    public static void Convert(string sourcePath, string savePath) {
        string sourceFileExtension = Path.GetExtension(sourcePath).ToLowerInvariant();
        string saveFileExtension = Path.GetExtension(savePath).ToLowerInvariant();
        Action<string, string> method = (sourceFileExtension, saveFileExtension) switch {
            (Doc, Docx) => WordConvert,
            (Doc, Doc) => WordConvert,
            (Doc, Txt) => WordConvert,
            (Doc, Pdf) => WordConvert,
            (Doc, Html) => WordConvert,
            (Docx, Doc) => WordConvert,
            (Docx, Docx) => WordConvert,
            (Docx, Txt) => WordConvert,
            (Docx, Pdf) => WordConvert,
            (Docx, Html) => WordConvert,
            (Txt, Doc) => WordConvert,
            (Txt, Docx) => WordConvert,
            (Ppt, Pptx) => PowerPointConvert,
            (Ppt, Mp4) => PowerPointConvert,
            (Ppt, Ppt) => PowerPointConvert,
            (Ppt, Pdf) => PowerPointConvert,
            (Ppt, Png) => PowerPointConvert,
            (Ppt, Jpg) => PowerPointConvert,
            (Pptx, Ppt) => PowerPointConvert,
            (Pptx, Mp4) => PowerPointConvert,
            (Pptx, Pptx) => PowerPointConvert,
            (Pptx, Pdf) => PowerPointConvert,
            (Pptx, Png) => PowerPointConvert,
            (Pptx, Jpg) => PowerPointConvert,
            (Csv, Xlsx) => ExcelConvert,
            //(Csv, Xls) => ExcelConvert,
            (Xls, Xlsx) => ExcelConvert,
            //(Xls, Xls) => ExcelConvert,
            (Xls, Csv) => ExcelConvert,
            (Xls, Pdf) => ExcelConvert,
            (Xls, Txt) => ExcelConvert,
            (Xls, Html) => ExcelConvert,
            //(Xlsx, Xls) => ExcelConvert,
            (Xlsx, Xlsx) => ExcelConvert,
            (Xlsx, Csv) => ExcelConvert,
            (Xlsx, Pdf) => ExcelConvert,
            (Xlsx, Txt) => ExcelConvert,
            (Xlsx, Html) => ExcelConvert,
            _ => throw new ArgumentException("没有匹配的转换模式")
        };
        method.Invoke(sourcePath, savePath);
    }

    /// <summary>
    /// word 格式转换
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="savePath"></param>
    public static void WordConvert(string sourcePath, string savePath) {
        ReplaceSlashWithBackSlash(ref sourcePath, ref savePath);
        if (CopyFileIfExtensionEqual(sourcePath, savePath)) {
            return;
        }

        if (Path.GetExtension(sourcePath).ToLowerInvariant() == ".txt") {
            CreateWordFromText(sourcePath, savePath);
        } else {
            WordFormatConvert(sourcePath, savePath);
        }
    }

    /// <summary>
    /// 文件后缀相同则直接拷贝
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="savePath"></param>
    /// <returns></returns>
    private static bool CopyFileIfExtensionEqual(string sourcePath, string savePath) {
        string srcExt = Path.GetExtension(sourcePath).ToLowerInvariant();
        string saveExt = Path.GetExtension(savePath).ToLowerInvariant();
        // 直接复制
        if (srcExt == saveExt) {
            File.Copy(sourcePath, savePath, true);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 替换 '/' 为 '\\'
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="savePath"></param>
    private static void ReplaceSlashWithBackSlash(ref string sourcePath, ref string savePath) {
        sourcePath = sourcePath.Replace('/', '\\');
        savePath = savePath.Replace('/', '\\');
    }

    /// <summary>
    /// 从文本文件创建 word
    /// </summary>
    /// <param name="textPath"></param>
    /// <param name="savePath"></param>
    /// <exception cref="ArgumentException">savePath 不是 word 文件</exception>
    private static void CreateWordFromText(string textPath, string savePath) {
        string ext = Path.GetExtension(savePath).ToLowerInvariant();
        // 格式错误
        if (ext != ".docx" && ext != ".doc") {
            throw new ArgumentException("保存文件格式错误");
        }
        var docFormat = Microsoft.Office.Interop.Word.WdSaveFormat.wdFormatDocument97;
        var docxFormat = Microsoft.Office.Interop.Word.WdSaveFormat.wdFormatXMLDocument;
        var app = new Microsoft.Office.Interop.Word.Application();
        var document = app.Documents.Add();
        try {
            document.Paragraphs.Add().Range.Text = File.ReadAllText(textPath);
            // 另存为
            document.SaveAs(
                savePath,
                ext == ".doc" ? docFormat : docxFormat
            );
        } catch {
            throw;
        } finally {
            document.Close();
            app.Quit();
        }
    }

    /// <summary>
    /// word 转其他格式
    /// </summary>
    /// <param name="wordPath"></param>
    /// <param name="savePath"></param>
    private static void WordFormatConvert(string wordPath, string savePath) {
        string savePathExtension = Path.GetExtension(savePath).ToLowerInvariant();
        var saveFormat = savePathExtension switch {
            Docx => Microsoft.Office.Interop.Word.WdSaveFormat.wdFormatXMLDocument,
            Doc => Microsoft.Office.Interop.Word.WdSaveFormat.wdFormatDocument97,
            Txt => Microsoft.Office.Interop.Word.WdSaveFormat.wdFormatUnicodeText,
            Pdf => Microsoft.Office.Interop.Word.WdSaveFormat.wdFormatPDF,
            Html => Microsoft.Office.Interop.Word.WdSaveFormat.wdFormatHTML,
            _ => throw new ArgumentException("没有匹配的格式转换模式")
        };

        var app = new Microsoft.Office.Interop.Word.Application();
        var document = app.Documents.Open(wordPath);
        // 另存为
        try {
            // 文本
            if (savePathExtension == Txt) {
                document.SaveAs(
                    savePath,
                    saveFormat,
                    Encoding: Microsoft.Office.Core.MsoEncoding.msoEncodingUTF8
                );
            } else {
                document.SaveAs(savePath, saveFormat);
            }
        } catch {
            throw;
        } finally {
            document.Close();
            app.Quit();
        }
    }

    /// <summary>
    /// Excel 转换
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="savePath"></param>
    public static void ExcelConvert(string sourcePath, string savePath) {
        ReplaceSlashWithBackSlash(ref sourcePath, ref savePath);
        if (CopyFileIfExtensionEqual(sourcePath, savePath)) {
            return;
        }
        string savePathExtension = Path.GetExtension(savePath).ToLowerInvariant();
        var saveFormat = XlFileFormat.xlWorkbookDefault;
        // pdf 特例
        if (savePathExtension != Pdf) {
            saveFormat = savePathExtension switch {
                //Xls => XlFileFormat.xlExcel8,
                Xlsx => XlFileFormat.xlWorkbookDefault,
                Csv => XlFileFormat.xlCSV,
                Txt => XlFileFormat.xlUnicodeText,
                Html => XlFileFormat.xlHtml,
                _ => throw new ArgumentException("文件格式类型错误")
            };
        }
        var app = new Microsoft.Office.Interop.Excel.Application();
        var workbooks = app.Workbooks;
        var workbook = workbooks.Open(sourcePath);

        try {
            // 保存为 xls、xlsx、html 格式
            if (savePathExtension == Xls || savePathExtension == Xlsx || savePathExtension == Html) {
                if (savePathExtension == Xls) {
                    workbook.AccuracyVersion = 1;
                }
                workbook.SaveAs2(
                    savePath,
                    saveFormat,
                    ConflictResolution: Microsoft.Office.Interop.Excel.XlSaveConflictResolution.xlLocalSessionChanges
                );
            }
            // 其他格式
            else {
                var worksheets = workbook.Worksheets;
                var saveDir = Path.GetDirectoryName(savePath)!;
                var saveFileName = Path.GetFileNameWithoutExtension(savePath)!;

                // 导出全部 sheet
                for (int i = 1; i <= worksheets.Count; i++) {
                    var sheet = worksheets[i];
                    // pdf 文件
                    if (savePathExtension == Pdf) {
                        sheet.ExportAsFixedFormat(
                            Microsoft.Office.Interop.Excel.XlFixedFormatType.xlTypePDF,
                            Path.Combine(saveDir, $"{saveFileName}-{sheet.Name}{savePathExtension}")
                        );
                    }
                    // 其他文件
                    else {
                        string name = $"{saveFileName}-{sheet.Name}{savePathExtension}";
                        sheet.SaveAs(Path.Combine(saveDir, name), saveFormat);
                        Logger.LogInformation($"导出 {name} 成功");
                    }
                    Marshal.ReleaseComObject(sheet);
                }
                Marshal.ReleaseComObject(worksheets);
            }
        } catch {
            throw;
        } finally {
            workbook.Close(false);
            app.Quit();
            // 加速 Excel 退出
            Marshal.ReleaseComObject(workbook);
            Marshal.ReleaseComObject(workbooks);
            Marshal.ReleaseComObject(app);
        }
    }

    /// <summary>
    /// PowerPoint 转换
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="savePath"></param>
    public static void PowerPointConvert(string sourcePath, string savePath) {
        ReplaceSlashWithBackSlash(ref sourcePath, ref savePath);
        if (CopyFileIfExtensionEqual(sourcePath, savePath)) {
            return;
        }

        string savePathExtension = Path.GetExtension(savePath).ToLowerInvariant();
        var saveFormat = savePathExtension switch {
            Pptx => PpSaveAsFileType.ppSaveAsDefault,
            Ppt => PpSaveAsFileType.ppSaveAsPresentation,
            Mp4 => PpSaveAsFileType.ppSaveAsMP4,
            Pdf => PpSaveAsFileType.ppSaveAsPDF,
            Png => PpSaveAsFileType.ppSaveAsPNG,
            Jpg => PpSaveAsFileType.ppSaveAsJPG,
            _ => throw new ArgumentException("没有匹配的格式转换模式")
        };

        var app = new Microsoft.Office.Interop.PowerPoint.Application();
        var presentation = app.Presentations.Open(
            sourcePath,
            WithWindow: Microsoft.Office.Core.MsoTriState.msoFalse
        );

        try {
            // 转换为视频
            if (savePathExtension == Mp4) {
                presentation.CreateVideo(savePath, VertResolution: 1080, Quality: 100);
                Logger.LogInformation("开始导出视频");
                while (
                    presentation.CreateVideoStatus == PpMediaTaskStatus.ppMediaTaskStatusInProgress
                    || presentation.CreateVideoStatus == PpMediaTaskStatus.ppMediaTaskStatusQueued
                ) {
                    Thread.Sleep(1000);
                }
                // 创建完成
                if (presentation.CreateVideoStatus == PpMediaTaskStatus.ppMediaTaskStatusFailed) {
                    Logger.LogError("导出视频失败");
                } else {
                    Logger.LogError("导出视频完毕");
                }
            }
            // 转换为图片
            else if (savePathExtension == Png || savePathExtension == Jpg) {
                // 图片保存目录
                string saveDir = Path.Combine(
                    Path.GetDirectoryName(savePath)!,
                    Path.GetFileNameWithoutExtension(savePath)!
                );
                Directory.CreateDirectory(saveDir);
                int count = 0;
                int slideDoubleWidth = (int)presentation.SlideMaster.Width << 2;
                int slideDoubleHeight = (int)presentation.SlideMaster.Height << 2;
                string format = savePathExtension == Png ? Png : Jpg;
                // 开始导出
                foreach (Microsoft.Office.Interop.PowerPoint.Slide slide in presentation.Slides) {
                    slide.Export(
                        Path.Combine(saveDir, $"slide-{++count}{format}"),
                        format,
                        slideDoubleWidth,
                        slideDoubleHeight
                    );
                    Logger.LogInformation($"导出第 {count} 张幻灯片成功");
                }
            }
            // 其他格式
            else {
                presentation.SaveCopyAs(savePath, saveFormat);
            }
        } catch {
            throw;
        } finally {
            presentation.Close();
            app.Quit();
        }
    }
}
