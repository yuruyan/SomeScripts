using CommonTools.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared;

namespace VisioScripts;

public static class Services {
    private static readonly ILogger Logger = SharedLogging.Logger;

    /// <summary>
    /// 批量创建文本形状
    /// </summary>
    /// <param name="configuration"></param>
    public static void BatchCreatingTextShapes(IConfiguration configuration) {
        #region ArgumentNames
        const string PathArgName = "path";
        const string TextPathArgName = "textPath";
        #endregion

        #region Argument string value
        var pathArg = configuration[PathArgName];
        var textPathArg = configuration[TextPathArgName];
        #endregion

        #region Check arguments
        // Path is empty
        if (string.IsNullOrEmpty(pathArg)) {
            Logger.LogError($"Argument '{PathArgName}' cannot be empty");
            return;
        }
        // TextPath is empty
        if (string.IsNullOrEmpty(textPathArg)) {
            Logger.LogError($"Argument '{TextPathArgName}' cannot be empty");
            return;
        }
        // Path not found
        if (!File.Exists(pathArg)) {
            Logger.LogError($"File '{pathArg}' doesn't exist");
            return;
        }
        // Path not found
        if (!File.Exists(textPathArg)) {
            Logger.LogError($"File '{textPathArg}' doesn't exist");
            return;
        }
        #endregion

        VisioServices.BatchCreatingTextShapes(
            pathArg,
            File
                .ReadAllText(textPathArg)
                .ReplaceLineFeedWithLinuxStyle()
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
        );
    }
}
