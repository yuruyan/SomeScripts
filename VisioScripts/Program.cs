using NLog;
using Shared;
using VisioScripts;

Logger Logger = LogManager.GetCurrentClassLogger();

// 输入参数验证
if (!SharedHelper.CheckArgs(args, Resource.Help)) {
    return;
}

try {
    args.ProcessService(typeof(Services));
} catch (Exception error) {
    Logger.Error(error);
}
