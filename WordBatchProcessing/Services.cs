using Csv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared;
using System.Reflection;

namespace WordBatchProcessing;

public static partial class Services {
    private static readonly ILogger Logger = SharedLogging.Logger;

    public static void SearchText(IConfiguration configuration) {
        #region ArgumentNames
        const string PathArgName = "path";
        const string SavePathArgName = "savePath";
        const string SearchArgName = "search";
        const string MatchCaseArgName = "matchCase";
        const string MatchWholeWordArgName = "matchWholeWord";
        const string MatchWildcardsArgName = "matchWildcards";
        #endregion

        #region Argument string value
        var pathArg = configuration[PathArgName];
        var savePathArg = configuration[SavePathArgName];
        var searchArg = configuration[SearchArgName];
        var matchCaseArg = configuration[MatchCaseArgName];
        var matchWholeWordArg = configuration[MatchWholeWordArgName];
        var matchWildcardsArg = configuration[MatchWildcardsArgName];
        #endregion

        #region Check arguments
        // Path is empty
        if (string.IsNullOrEmpty(pathArg)) {
            Logger.LogError($"Argument '{PathArgName}' cannot be empty");
            return;
        }
        // Replacement is empty
        if (string.IsNullOrEmpty(savePathArg)) {
            Logger.LogError($"Argument '{SavePathArgName}' cannot be empty");
            return;
        }
        // Search is empty
        if (string.IsNullOrEmpty(searchArg)) {
            Logger.LogError($"Argument '{SearchArgName}' cannot be empty");
            return;
        }
        // Path not found
        if (!File.Exists(pathArg)) {
            Logger.LogError($"File '{pathArg}' doesn't exist");
            return;
        }
        // Path not found
        if (!File.Exists(savePathArg)) {
            Logger.LogError($"File '{savePathArg}' doesn't exist");
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
        #endregion

        WordService.SearchText(
            pathArg,
            savePathArg,
            searchArg,
            matchCase,
            matchWholeWord,
            useWildcards
        );
    }

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
            Logger.LogError($"Argument '{PathArgName}' cannot be empty");
            return;
        }
        // ReplacementPath is empty
        if (string.IsNullOrEmpty(replacementPathArg)) {
            Logger.LogError($"Argument '{ReplacementPathArgName}' cannot be empty");
            return;
        }
        // Path not found
        if (!File.Exists(pathArg)) {
            Logger.LogError($"File '{pathArg}' doesn't exist");
            return;
        }
        // ReplacementPath not found
        if (!File.Exists(replacementPathArg)) {
            Logger.LogError($"File '{replacementPathArg}' doesn't exist");
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

    /// <summary>
    /// 批量调整图形大小
    /// </summary>
    /// <param name="configuration"></param>
    public static void BatchResizeShapes(IConfiguration configuration) {
        const string PathArgName = "path";
        const string widthArgName = "width";
        const string heightArgName = "height";

        var pathArg = configuration[PathArgName];
        var widthArg = configuration[widthArgName];
        var heightArg = configuration[heightArgName];

        #region Check arguments
        // Path is empty
        if (string.IsNullOrEmpty(pathArg)) {
            Logger.LogError($"Argument '{PathArgName}' cannot be empty");
            return;
        }
        // Not specify any size
        if (string.IsNullOrEmpty(widthArg) && string.IsNullOrEmpty(heightArg)) {
            Logger.LogError($"Size not specified");
            return;
        }
        // Path not found
        if (!File.Exists(pathArg)) {
            Logger.LogError($"File '{pathArg}' doesn't exist");
            return;
        }
        #endregion

        #region Parse arguments
        var widthRatioResult = (widthArg ?? string.Empty).ParseRatioNumber();
        var heightRatioResult = (heightArg ?? string.Empty).ParseRatioNumber();

        // Both specified
        if (widthArg is not null && heightArg is not null) {
            if (!widthRatioResult.HasValue) {
                Logger.LogError($"Invalid argument '{widthArgName}' value '{widthArg}'");
                return;
            }
            if (!heightRatioResult.HasValue) {
                Logger.LogError($"Invalid argument '{heightArgName}' value '{heightArg}'");
                return;
            }
        }
        // Specified width
        else if (widthArg is not null) {
            if (!widthRatioResult.HasValue) {
                Logger.LogError($"Invalid argument '{widthArgName}' value '{widthArg}'");
                return;
            }
        }
        // Specified height
        else {
            if (!heightRatioResult.HasValue) {
                Logger.LogError($"Invalid argument '{heightArgName}' value '{heightArg}'");
                return;
            }
        }
        #endregion

        #region Process
        var (specifiedWidth, widthHasValue, widthHasRatio) = widthRatioResult;
        var (specifiedHeight, heightHasValue, heightHasRatio) = heightRatioResult;
        // Both specified
        if (widthHasValue && heightHasValue) {
            WordService.BatchResizeShapes(pathArg, originalSize => {
                double newWidth, newHeight;
                newWidth = widthHasRatio ? originalSize.Width * specifiedWidth : specifiedWidth;
                newHeight = heightHasRatio ? originalSize.Height * specifiedHeight : specifiedHeight;
                return new((int)newWidth, (int)newHeight);
            });
            return;
        }
        // Only specified width
        if (widthHasValue) {
            WordService.BatchResizeShapes(pathArg, originalSize => {
                double newWidth = 0, newHeight = 0;
                newWidth = widthHasRatio ? originalSize.Width * specifiedWidth : specifiedWidth;
                newHeight = widthHasRatio
                    ? originalSize.Height * specifiedWidth
                    : originalSize.Height * newWidth / originalSize.Width;
                return new((int)newWidth, (int)newHeight);
            });
            return;
        }
        // Only specified height
        WordService.BatchResizeShapes(pathArg, originalSize => {
            double newWidth = 0, newHeight = 0;
            newHeight = heightHasRatio ? originalSize.Height * specifiedHeight : specifiedHeight;
            newWidth = heightHasRatio
                    ? originalSize.Width * specifiedHeight
                    : originalSize.Width * newHeight / originalSize.Height;
            return new((int)newWidth, (int)newHeight);
        });
        #endregion
    }
}
