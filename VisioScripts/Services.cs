using CommonTools.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
        if (ArgumentUtils.ValidateFileArgument(pathArg, PathArgName)) {
            return;
        }
        if (ArgumentUtils.ValidateFileArgument(textPathArg, TextPathArgName)) {
            return;
        }
        #endregion

        VisioServices.BatchCreatingTextShapes(
            pathArg!,
            File.ReadAllText(textPathArg!)
                .ReplaceLineFeedWithLinuxStyle()
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
        );
    }
}
