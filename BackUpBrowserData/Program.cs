using BackUpBrowserData;
using CommonTools.Utils;
using NLog;
using Shared;
using System.IO.Compression;

Logger Logger = LogManager.GetCurrentClassLogger();

// 输入参数验证
if (!SharedHelper.CheckArgs(args, Resource.Help)) {
    return;
}

try {
    Service.BackUpHistory(args.GetConfiguration());
} catch (Exception error) {
    Logger.Error(error);
}