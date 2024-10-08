﻿using BackUpBrowserData;
using CommonTools.Utils;
using Microsoft.Extensions.Logging;

var Logger = SharedLogging.Logger;
// 输入参数验证
if (!ArgumentUtils.CheckArgs(args, Resource.Help)) {
    return;
}

try {
    Service.BackUpHistory(args.GetConfiguration());
} catch (Exception error) {
    Logger.LogError(error, "Program terminated");
} finally {
    SharedLogging.Dispose();
    if (args.ContainsWaitArgument()) {
        ArgumentUtils.WaitUserInputToExit();
    }
}