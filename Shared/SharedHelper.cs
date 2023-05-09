using Microsoft.Extensions.Configuration;

namespace Shared;

public static class SharedHelper {
    /// <summary>
    /// 检查参数是否有参数
    /// </summary>
    /// <param name="args"></param>
    /// <param name="helpMessage">参数为 0 时的提示消息</param>
    /// <returns></returns>
    public static bool CheckArgs(string[] args, string helpMessage) {
        if (args.Length == 0) {
            Console.WriteLine(helpMessage);
            return false;
        }
        return true;
    }

    /// <summary>
    /// 获取 Configuration
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static IConfiguration GetConfiguration(string[] args) => new ConfigurationBuilder().AddCommandLine(args).Build();
}