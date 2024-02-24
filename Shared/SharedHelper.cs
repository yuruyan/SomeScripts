using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Model;
using SharedHelper;
using System.Reflection;
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

#if NET7_0_OR_GREATER
    [GeneratedRegex(@"^(?<number>\d+(?:\.\d+)?)\*?$")]
    private static partial Regex GetRatioNonNegativeNumberArgRegex();

    [GeneratedRegex(@"^-?(?<number>\d+(?:\.\d+)?)\*?$")]
    private static partial Regex GetRatioNumberArgRegex();
#elif NET6_0_OR_GREATER
    private static Regex GetRatioNonNegativeNumberArgRegex() => new(@"^(?<number>\d+(?:\.\d+)?)\*?$");

    private static Regex GetRatioNumberArgRegex() => new(@"^-?(?<number>\d+(?:\.\d+)?)\*?$");
#endif

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

    /// <summary>
    /// 处理服务
    /// </summary>
    /// <param name="args"><paramref name="args"/>[0] 对应 <paramref name="staticServiceType"/> 方法名称，不区分大小写</param>
    /// <param name="staticServiceType">其中静态方法参数为 <see cref="IConfiguration"/></param>
    /// <exception cref="ArgumentException">Invalid first argument in <paramref name="args"/></exception>
    public static void ProcessService(this string[] args, Type staticServiceType) {
        var targetMethodNameLowerCase = args[0].ToLowerInvariant();
        var targetMethod = staticServiceType
            .GetMethods(BindingFlags.Default | BindingFlags.Static | BindingFlags.NonPublic)
            .Where(info => info.ReturnType == typeof(void))
            .Where(info => {
                var paramInfos = info.GetParameters();
                return paramInfos.Length == 1 && paramInfos[0].ParameterType == typeof(IConfiguration);
            })
            .FirstOrDefault(info => info.Name.ToLowerInvariant() == targetMethodNameLowerCase)
            ?? throw new ArgumentException($"Invalid argument '{args[0]}'");
        targetMethod.Invoke(null, new[] { args.GetConfiguration() });
    }

    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="argumentName">命令行参数名称</param>
    /// <returns></returns>
    public static bool ValidateFileArgument(string filePath, string argumentName) {
        if (string.IsNullOrEmpty(filePath)) {
            SharedLogging.Logger.LogError("Argument '{argumentName}' cannot be empty", argumentName);
            return false;
        }
        if (!File.Exists(filePath)) {
            SharedLogging.Logger.LogError("File \"{path}\" cannot be empty", filePath);
            return false;
        }
        return true;
    }

    /// <summary>
    /// 检查文件夹是否存在
    /// </summary>
    /// <param name="directory">文件夹路径</param>
    /// <param name="argumentName">命令行参数名称</param>
    /// <returns></returns>
    public static bool ValidateDirectoryArgument(string directory, string argumentName) {
        if (string.IsNullOrEmpty(directory)) {
            SharedLogging.Logger.LogError("Argument '{argumentName}' cannot be empty", argumentName);
            return false;
        }
        if (!Directory.Exists(directory)) {
            SharedLogging.Logger.LogError("Directory \"{path}\" cannot be empty", directory);
            return false;
        }
        return true;
    }
}
