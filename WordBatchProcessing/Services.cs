using Microsoft.Office.Interop.Word;
using NLog;
using CommonTools.Model;
using System.Runtime.InteropServices;

namespace WordBatchProcessing;

public static class Services {
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// 批量替换
    /// </summary>
    /// <param name="path">Word 文件路径</param>
    /// <param name="replacementList">替换列表</param>
    /// <param name="matchCase">区分大小写</param>
    /// <param name="matchWholeWord">匹配整个文本</param>
    /// <param name="matchWildcards">使用通配符</param>
    /// <param name="replaceAll">是否替换全部</param>
    [NoException]
    public static void BatchReplace(
        string path,
        IReadOnlyCollection<KeyValuePair<string, string>> replacementList,
        bool matchCase = false,
        bool matchWholeWord = false,
        bool matchWildcards = false,
        bool replaceAll = true
    ) {
        // Empty list
        if (replacementList.Count == 0) {
            return;
        }
        Application app = null!;
        Document? document = null;
        try {
            app = new Application();
            Logger.Debug("Opening document...");
            document = app.Documents.Open(path, ConfirmConversions: true, ReadOnly: false);
            document.Select();
            // Switch to editing view
            document.ActiveWindow.View.ReadingLayout = false;
            if (document is null) {
                throw new Exception("Opening failed");
            }
            Logger.Debug("Executing replacements");
            var selection = app.Selection;
            var find = selection.Find;
            foreach (var (id, number) in replacementList) {
                find.ClearFormatting();
                // Execute replacement
                find.Execute(
                    FindText: id,
                    ReplaceWith: number,
                    MatchCase: matchCase,
                    MatchWholeWord: matchWholeWord,
                    MatchWildcards: matchWildcards,
                    Replace: replaceAll
                );
                Logger.Debug($"Replace '{id}' done");
            }
            document.Save();
            Logger.Debug("Over");
        } catch (Exception error) {
            Logger.Error(error);
        } finally {
            try {
                document?.Close();
                app?.Quit();
            } catch (Exception error) {
                Logger.Error(error);
            }
            // ReleaseComObject
            if (document is not null) {
                Marshal.ReleaseComObject(document);
            }
            Marshal.ReleaseComObject(app);
        }
    }
}
