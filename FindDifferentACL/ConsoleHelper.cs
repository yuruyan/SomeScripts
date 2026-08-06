using System.Security.Principal;
using System.Text.RegularExpressions;

/// <summary>
/// 控制台彩色输出与 SDDL 解析等辅助方法。
/// </summary>
internal static class ConsoleHelper {
    /// <summary>
    /// 将 SDDL 字符串中的 SID 替换为对应的用户名或组名。
    /// </summary>
    public static string ResolveSidsToNames(string sddl) {
        // SID 格式: S-1-5-... 或 S-1-0-0 等
        return Regex.Replace(sddl, @"S-\d+-\d+(?:-\d+)+", match => {
            try {
                var sid = new SecurityIdentifier(match.Value);
                var account = (NTAccount)sid.Translate(typeof(NTAccount));
                // 将原始 SID 替换为 "用户名(SID)" 格式，方便对照
                return $"{account.Value}({match.Value})";
            } catch {
                // 如果无法解析（如未知 SID），保留原始值
                return match.Value;
            }
        });
    }

    /// <summary>
    /// 以绿色输出成功信息。
    /// </summary>
    public static void WriteSuccessLine(string message) {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>
    /// 以黄色输出警告信息。
    /// </summary>
    public static void WriteWarningLine(string message) {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Error.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>
    /// 以红色输出错误信息。
    /// </summary>
    public static void WriteErrorLine(string message) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>
    /// 以深灰色输出提示信息。
    /// </summary>
    public static void WriteHintLine(string message) {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
