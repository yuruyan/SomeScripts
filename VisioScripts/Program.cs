using Microsoft.Extensions.Logging;
using Shared;
using VisioScripts;

var Logger = SharedLogging.Logger;

// 输入参数验证
if (!SharedHelper.CheckArgs(args, Resource.Help)) {
    return;
}

try {
    args.ProcessService(typeof(Services));
} catch (Exception error) {
    Logger.LogError(error, "Program terminated");
    Environment.Exit(-1);
}
