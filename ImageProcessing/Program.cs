using ImageProcessing;
using NLog;
using Shared;

// 输入参数验证
if (!SharedHelper.CheckArgs(args, Resource.Help)) {
    return;
}

Logger Logger = LogManager.GetCurrentClassLogger();
try {
    Proxy.Parse(args[0])(SharedHelper.GetConfiguration(args));
} catch (Exception error) {
    Logger.Error(error);
}