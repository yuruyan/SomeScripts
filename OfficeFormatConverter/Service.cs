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
    private const string Jpeg = ".jpeg";
    private const string Xls = ".xls";
    private const string Xlsx = ".xlsx";
    private const string Csv = ".csv";

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
            (Doc, Txt) => WordConvert,
            (Doc, Pdf) => WordConvert,
            (Doc, Html) => WordConvert,
            (Docx, Doc) => WordConvert,
            (Docx, Txt) => WordConvert,
            (Docx, Pdf) => WordConvert,
            (Docx, Html) => WordConvert,
            (Txt, Doc) => WordConvert,
            (Txt, Docx) => WordConvert,
            (Ppt, Pptx) => PowerPointConvert,
            (Ppt, Pdf) => PowerPointConvert,
            (Ppt, Png) => PowerPointConvert,
            (Ppt, Jpeg) => PowerPointConvert,
            (Pptx, Ppt) => PowerPointConvert,
            (Pptx, Pdf) => PowerPointConvert,
            (Pptx, Png) => PowerPointConvert,
            (Pptx, Jpeg) => PowerPointConvert,
            (Xls, Xlsx) => ExcelConvert,
            (Xls, Csv) => ExcelConvert,
            (Xls, Pdf) => ExcelConvert,
            (Xls, Txt) => ExcelConvert,
            (Xls, Html) => ExcelConvert,
            (Xlsx, Xls) => ExcelConvert,
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
        if (Path.GetExtension(sourcePath).ToLowerInvariant() == ".txt") {
            CreateWordFromText(sourcePath, savePath);
        } else {
            WordFormatConvert(sourcePath, savePath);
        }
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
        document.Paragraphs.Add().Range.Text = File.ReadAllText(textPath);
        // 另存为
        document.SaveAs(
            savePath,
            ext == ".doc" ? docFormat : docxFormat
        );
        document.Close();
        app.Quit();
    }

    /// <summary>
    /// word 转其他格式
    /// </summary>
    /// <param name="wordPath"></param>
    /// <param name="savePath"></param>
    private static void WordFormatConvert(string wordPath, string savePath) {
        var saveFormat = Path.GetExtension(savePath).ToLowerInvariant() switch {
            Docx => Microsoft.Office.Interop.Word.WdSaveFormat.wdFormatXMLDocument,
            Doc => Microsoft.Office.Interop.Word.WdSaveFormat.wdFormatDocument97,
            Txt => Microsoft.Office.Interop.Word.WdSaveFormat.wdFormatText,
            Pdf => Microsoft.Office.Interop.Word.WdSaveFormat.wdFormatPDF,
            Html => Microsoft.Office.Interop.Word.WdSaveFormat.wdFormatHTML,
            _ => throw new ArgumentException("没有匹配的格式转换模式")
        };
        var app = new Microsoft.Office.Interop.Word.Application();
        var document = app.Documents.Open(wordPath);
        // 另存为
        document.SaveAs(savePath, saveFormat);
        document.Close();
        app.Quit();
    }

    public static void ExcelConvert(string sourcePath, string savePath) {

    }

    public static void PowerPointConvert(string sourcePath, string savePath) {

    }
}
