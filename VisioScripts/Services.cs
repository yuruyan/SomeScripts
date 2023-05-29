using CommonTools.Utils;
using Microsoft.Extensions.Configuration;
using NLog;

namespace VisioScripts;

public static class Services {
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static void BatchCreatingTextShapes(IConfiguration configuration) {
        #region ArgumentNames
        const string PathArgName = "path";
        const string TextPathArgName = "textPath";
        #endregion

        #region Argument string value
        var pathArg = configuration["PathArgName"];
        var textPathArg = configuration["TextPathArgName"];
        #endregion

        #region Check arguments
        // Path is empty
        if (string.IsNullOrEmpty(pathArg)) {
            Logger.Error($"Argument '{PathArgName}' cannot be empty");
            return;
        }
        // TextPath is empty
        if (string.IsNullOrEmpty(textPathArg)) {
            Logger.Error($"Argument '{TextPathArgName}' cannot be empty");
            return;
        }
        // Path not found
        if (!File.Exists(pathArg)) {
            Logger.Error($"File '{pathArg}' doesn't exist");
            return;
        }
        // Path not found
        if (!File.Exists(textPathArg)) {
            Logger.Error($"File '{textPathArg}' doesn't exist");
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
