using Microsoft.Extensions.Configuration;
using Shared.Model;
using System.Text.RegularExpressions;

namespace Shared;

public static partial class SharedHelper {
    /// <summary>
    /// 非负数比例数参数正则，不包括前缀 '+'
    /// </summary>
    private static readonly Regex RatioNonNegativeNumberArgRegex = GetRatioNonNegativeNumberArgRegex();
    /// <summary>
    /// 比例数参数正则
    /// </summary>
    private static readonly Regex RatioNumberArgRegex = GetRatioNumberArgRegex();

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
    public static IConfiguration GetConfiguration(this string[] args) => new ConfigurationBuilder().AddCommandLine(args).Build();

    /// <summary>
    /// 是否启用调试模式
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static bool IsDebugMode(this IConfiguration configuration) {
        return configuration.GetSection("debug").Value?.ToLowerInvariant() == "true";
    }

    /// <summary>
    /// 解析比例参数
    /// </summary>
    /// <param name="arg"></param>
    /// <param name="allowNegative">允许负数</param>
    /// <returns></returns>
    public static RatioNumberResult ParseRatioNumber(this string arg, bool allowNegative = false) {
        var regex = allowNegative ? RatioNumberArgRegex : RatioNonNegativeNumberArgRegex;
        var match = regex.Match(arg);
        // Parse failed
        if (!match.Success) {
            return default;
        }
        return new(
            double.Parse(match.Groups["number"].Value),
            true,
            arg.Contains('*')
        );
    }

#if NET7_0_OR_GREATER
    [GeneratedRegex(@"^(?<number>\d+(?:\.\d+)?)\*?$")]
    private static partial Regex GetRatioNonNegativeNumberArgRegex();

    [GeneratedRegex(@"^-?(?<number>\d+(?:\.\d+)?)\*?$")]
    private static partial Regex GetRatioNumberArgRegex();
#elif NET6_0_OR_GREATER
    private static Regex GetRatioNonNegativeNumberArgRegex() => new(@"^(?<number>\d+(?:\.\d+)?)\*?$");

    private static Regex GetRatioNumberArgRegex() => new(@"^-?(?<number>\d+(?:\.\d+)?)\*?$");
#endif
}
