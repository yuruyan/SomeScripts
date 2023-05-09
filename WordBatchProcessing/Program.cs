﻿using NLog;
using Shared;
using WordBatchProcessing;

Logger Logger = LogManager.GetCurrentClassLogger();

// 输入参数验证
if (!SharedHelper.CheckArgs(args, Resource.Help)) {
    return;
}

try {
    Services.BatchReplace(SharedHelper.GetConfiguration(args));
} catch (Exception error) {
    Logger.Error(error);
}