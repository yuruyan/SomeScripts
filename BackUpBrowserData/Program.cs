using BackUpBrowserData;
using CommonTools;
using Microsoft.Extensions.Logging;
using Shared;

var Logger = SharedLogging.Logger;
// 输入参数验证
if (!SharedHelper.CheckArgs(args, Resource.Help)) {
    return;
}

try {
    Service.BackUpHistory(args.GetConfiguration());
} catch (Exception error) {
    Logger.LogError(error, "Program terminated");
} finally {
    SharedLogging.Dispose();
    if (args.ContainsWaitArgument()) {
        SharedHelper.WaitUserInputToExit();
    }
}