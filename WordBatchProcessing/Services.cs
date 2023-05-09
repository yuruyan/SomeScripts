using Csv;
using Microsoft.Extensions.Configuration;
using NLog;

namespace WordBatchProcessing;

public static class Services {
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// 批量替换
    /// </summary>
    /// <param name="configuration"></param>
    public static void BatchReplace(IConfiguration configuration) {
        const string PathArgName = "path";
        const string ReplacementPathArgName = "replacementPath";
        const string MatchCaseArgName = "matchCase";
        const string MatchWholeWordArgName = "matchWholeWord";
        const string MatchWildcardsArgName = "useWildcards";
        const string ReplaceAllArgName = "replaceAll";

        var pathArg = configuration[PathArgName];
        var replacementPathArg = configuration[ReplacementPathArgName];
        var matchCaseArg = configuration[MatchCaseArgName];
        var matchWholeWordArg = configuration[MatchWholeWordArgName];
        var matchWildcardsArg = configuration[MatchWildcardsArgName];
        var replaceAllArg = configuration[ReplaceAllArgName];

        #region Check arguments
        // Path is empty
        if (string.IsNullOrEmpty(pathArg)) {
            Logger.Error($"Argument '{PathArgName}' cannot be empty");
            return;
        }
        // ReplacementPath is empty
        if (string.IsNullOrEmpty(replacementPathArg)) {
            Logger.Error($"Argument '{ReplacementPathArgName}' cannot be empty");
            return;
        }
        // Path not found
        if (!File.Exists(pathArg)) {
            Logger.Error($"File '{pathArg}' doesn't exist");
            return;
        }
        // ReplacementPath not found
        if (!File.Exists(replacementPathArg)) {
            Logger.Error($"File '{replacementPathArg}' doesn't exist");
            return;
        }
        #endregion

        #region Parse arguments
        if (!bool.TryParse(matchCaseArg, out var matchCase)) {
            matchCase = false;
        }
        if (!bool.TryParse(matchWildcardsArg, out var useWildcards)) {
            useWildcards = false;
        }
        if (!bool.TryParse(matchWholeWordArg, out var matchWholeWord)) {
            matchWholeWord = false;
        }
        if (!bool.TryParse(replaceAllArg, out var replaceAll)) {
            replaceAll = true;
        }
        #endregion

        #region Reading replacement file
        var replacementFileText = File.ReadAllText(replacementPathArg);
        var replacementList = CsvReader
            .ReadFromText(
                replacementFileText,
                new() { HeaderMode = HeaderMode.HeaderAbsent }
            )
            .Select<ICsvLine, KeyValuePair<string, string>>(line => {
                return line.ColumnCount switch {
                    0 => new(string.Empty, string.Empty),
                    1 => new(line[0], string.Empty),
                    _ => new(line[0], line[1])
                };
            })
            .ToList();
        #endregion

        WordService.BatchReplace(
            pathArg,
            replacementList,
            matchCase,
            matchWholeWord,
            useWildcards,
            replaceAll
        );
    }
}
