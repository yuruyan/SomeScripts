using CommonTools.Utils;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace SimpleScripts;

/// <summary>
/// 将 Iconfont Unicode 格式替换为 IconfontConstants 常量
/// </summary>
public static class ReplaceIconfontConstants {
    private static readonly ILogger Logger = SharedLogging.Logger;

    public static void Run(string searchDir) {
        // 搜索 .xaml, .cs 文件
        var xamlFiles = new DirectoryInfo(searchDir)
            .GetFiles("*?.xaml", SearchOption.AllDirectories)
            .Select(f => f.FullName);
        var csFiles = new DirectoryInfo(searchDir)
            .GetFiles("*?.cs", SearchOption.AllDirectories)
            .Select(f => f.FullName);
        var sourceDict = GetIconfontDict();
        Replace(xamlFiles, sourceDict);
        Replace(csFiles, sourceDict);
        Logger.LogInformation("done");
    }

    /// <summary>
    /// 执行替换
    /// </summary>
    /// <param name="files"></param>
    /// <param name="iconDict"></param>
    private static void Replace(IEnumerable<string> files, IDictionary<string, string> iconDict) {
        // iconfont Unicode 格式
        var regex = new Regex(@"""(&#x[a-z0-9]{4};)""");
        foreach (var file in files) {
            var srcText = File.ReadAllText(file);
            var distText = regex.Replace(srcText, match => {
                var matchValue = match.Groups[1].Value;
                if (iconDict.TryGetValue(matchValue, out var value)) {
                    return $"\"{{x:Static store:IconfontConstants.{value}}}\"";
                } else {
                    Logger.LogWarning($"No icon match for {matchValue}");
                }
                return matchValue;
            });
            // 匹配
            if (srcText != distText) {
                Logger.LogInformation($"Updated {file}");
                File.WriteAllText(file, distText);
            }
        }
    }

    /// <summary>
    /// 解析 IconfontConstants 为 Dictionary
    /// </summary>
    /// <returns></returns>
    private static IDictionary<string, string> GetIconfontDict() {
        return typeof(IconfontConstants)
            .GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
            .ToDictionary(
                f => $"&#x{Convert.ToString(
                    (int)(char.Parse(f.GetValue(null)!.ToString()!)),
                    16
                )};",
                f => f.Name
            );
    }

    /// <summary>
    /// Unicode 常量，将如下字段替换
    /// </summary>
    public static class IconfontConstants {
        public const string OpenFile = "\ue79e";
        public const string SortOrder = "\ue65e";
        public const string Female = "\ueb3c";
        public const string Male = "\ueb44";
        public const string SaveAs = "\ue607";
        public const string SearchLeft = "\ue74e";
        public const string All = "\ue644";
    }
}
