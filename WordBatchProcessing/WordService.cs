using Microsoft.Office.Interop.Word;
using NLog;
using CommonTools.Model;
using System.Runtime.InteropServices;
using CommonTools.Utils;
using System.Drawing;
using Microsoft.Office.Core;

namespace WordBatchProcessing;

public static class WordService {
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="path">Word 文件路径</param>
    /// <param name="action">(app, document, ComObjects)，在 <paramref name="action"/> 中将 ComObject 添加到集合</param>
    private static void WrapExecution(string path, Action<Application, Document, List<object>> action) {
        Application app = null!;
        Document? document = null;
        var comObjects = new List<object>();
        try {
            Logger.Debug("Starting application");
            app = new Application();
            Logger.Debug("Opening document...");
            document = app.Documents.Open(path, ConfirmConversions: true, ReadOnly: false);
            if (document is null) {
                throw new Exception("Opening document failed");
            }
            document.Select();
            // Process
            action(app, document, comObjects);
            Logger.Debug("Over");
        } catch (Exception error) {
            Logger.Error(error);
        } finally {
            try {
                document?.Close();
                app.Quit();
            } catch (Exception error) {
                Logger.Error(error);
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
    /// 批量调整图形大小
    /// </summary>
    /// <param name="path">Word 文件路径</param>
    /// <param name="getSize">根据原图像大小返回新图像大小</param>
    public static void BatchResizeShapes(string path, Func<Size, Size> getSize) {
        WrapExecution(path, (_, document, comObjects) => {
            var activeWindow = document.ActiveWindow;
            var activeWindowView = activeWindow.View;
            var shapes = document.InlineShapes;

            comObjects.Add(activeWindow);
            comObjects.Add(activeWindowView);
            comObjects.Add(shapes);

            // Switch to editing view
            activeWindowView.ReadingLayout = false;
            Logger.Debug("Executing resizing");
            // Starts from 1
            for (int i = 1, total = shapes.Count; i <= total; i++) {
                var shape = shapes[i];
                comObjects.Add(shape);
                var originalLockAspectRatio = shape.LockAspectRatio;
                // Unlock AspectRatio
                shape.LockAspectRatio = MsoTriState.msoFalse;
                var newSize = getSize(new((int)shape.Width, (int)shape.Height));
                shape.Height = newSize.Height;
                shape.Width = newSize.Width;
                // Restore AspectRatio
                shape.LockAspectRatio = originalLockAspectRatio;
                Logger.Debug($"Updated image {i}");
            }
            document.Save();
        });
    }

    /// <summary>
    /// 批量替换
    /// </summary>
    /// <param name="path">Word 文件路径</param>
    /// <param name="replacementList">替换列表</param>
    /// <param name="matchCase">区分大小写</param>
    /// <param name="matchWholeWord">匹配整个文本</param>
    /// <param name="useWildcards">使用通配符</param>
    /// <param name="replaceAll">是否替换全部</param>
    [NoException]
    public static void BatchReplace(
        string path,
        IReadOnlyCollection<KeyValuePair<string, string>> replacementList,
        bool matchCase = false,
        bool matchWholeWord = false,
        bool useWildcards = false,
        bool replaceAll = true
    ) {
        // Empty list
        if (replacementList.Count == 0) {
            return;
        }
        WrapExecution(path, (app, document, comObjects) => {
            var activeWindow = document.ActiveWindow;
            var activeWindowView = activeWindow.View;
            var range = document.Content;
            var find = range.Find;
            var replacement = find.Replacement;

            comObjects.Add(activeWindow);
            comObjects.Add(activeWindowView);
            comObjects.Add(range);
            comObjects.Add(find);
            comObjects.Add(replacement);

            // Switch to editing view
            activeWindowView.ReadingLayout = false;
            // Set find properties
            if (!useWildcards) {
                find.MatchCase = matchCase;
                find.MatchWholeWord = matchWholeWord;
            }
            find.Forward = true;
            find.MatchWildcards = useWildcards;
            find.Wrap = WdFindWrap.wdFindContinue;
            var replaceMode = replaceAll ? WdReplace.wdReplaceAll : WdReplace.wdReplaceOne;

            Logger.Debug("Executing replacements");
            foreach (var (key, value) in replacementList) {
                find.ClearFormatting();
                replacement.ClearFormatting();
                // Execute replacement
                find.Execute(
                    FindText: key,
                    ReplaceWith: value,
                    Replace: replaceMode
                );
                Logger.Debug($"Replace '{key}' with '{value}' done");
            }
            document.Save();
        });
    }
}
