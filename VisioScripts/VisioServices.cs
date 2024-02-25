using CommonTools.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Office.Interop.Visio;
using Shared;
using System.Runtime.InteropServices;

namespace VisioScripts;

public static class VisioServices {
    private static readonly ILogger Logger = SharedLogging.Logger;

    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="path">Visio 文件路径</param>
    /// <param name="action">(app, document, ComObjects)，在 <paramref name="action"/> 中将 ComObject 添加到集合</param>
    private static void WrapExecution(string path, Action<Application, Document, List<object>> action) {
        Application app = null!;
        Document? document = null;
        var comObjects = new List<object>();
        try {
            Logger.LogInformation("Starting application");
            app = new Application();
            Logger.LogInformation("Opening document...");
            document = app.Documents.Open(path);
            if (document is null) {
                throw new Exception("Opening document failed");
            }
            //document.Select();
            // Process
            action(app, document, comObjects);
            Logger.LogInformation("Over");
        } catch (Exception error) {
            Logger.LogError(error, "Error in processing document");
        } finally {
            try {
                document?.Close();
                app.Quit();
                Logger.LogInformation("Application quit");
            } catch (Exception error) {
                Logger.LogError(error, "Error in closing document");
            }
            // 逆序释放引用
            comObjects
                .Where(item => item != null)
                .RemoveDuplicates()
                .Reverse<object>()
                .ForEach(item => Marshal.FinalReleaseComObject(item));
            // ReleaseComObject
            if (document is not null) {
                Marshal.FinalReleaseComObject(document);
            }
            Marshal.FinalReleaseComObject(app);
        }
    }

    /// <summary>
    /// 批量创建文本形状
    /// </summary>
    /// <param name="path"></param>
    /// <param name="textLines"></param>
    public static void BatchCreatingTextShapes(string path, IEnumerable<string> textLines) {
        if (!textLines.Any()) {
            return;
        }

        WrapExecution(path, (_, document, comObjects) => {
            var pages = document.Pages;
            if (pages.OfType<Page>().FirstOrDefault() is not Page page) {
                return;
            }

            comObjects.Add(pages);
            comObjects.Add(page);

            int i = 0;
            // Batch creating shapes
            foreach (var text in textLines) {
                var shape = page.DrawRectangle(i, i, i + 2, i + 1);
                shape.Text = text;
                comObjects.Add(shape);
                i++;
                Logger.LogInformation($"Shape {i} created");
            }
            document.Save();
        });
    }
}
