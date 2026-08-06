using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;

internal class Program {
    private static int Main(string[] args) {
        string rootPath;

        if (args.Length > 0) {
            rootPath = args[0];
        } else {
            Console.Write("请输入根目录路径: ");
            rootPath = Console.ReadLine()?.Trim() ?? "";
        }

        if (string.IsNullOrWhiteSpace(rootPath)) {
            WriteErrorLine("错误: 未提供目录路径。");
            return 2;
        }

        if (!Directory.Exists(rootPath)) {
            WriteErrorLine($"错误: 目录不存在 - {rootPath}");
            return 2;
        }

        bool showDetails = args.Contains("--details") || args.Contains("-d");

        rootPath = Path.GetFullPath(rootPath);
        Console.WriteLine($"正在扫描: {rootPath}");
        Console.WriteLine();

        int differentCount = 0;
        int totalCount = 0;
        int skippedCount = 0;
        var differentDirs = new List<(string Dir, string Parent, string DirSddl, string ParentSddl)>();

        // 递归遍历所有子目录
        var options = new EnumerationOptions {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
        };
        using var enumerator = Directory.EnumerateDirectories(rootPath, "*", options).GetEnumerator();

        while (true) {
            string dir;

            // MoveNext 期间的异常发生在逐目录 try/catch 之外（如损坏的目录），单独捕获
            try {
                if (!enumerator.MoveNext())
                    break;
                dir = enumerator.Current;
            } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
                WriteWarningLine($"警告: 无法枚举某个子目录，已跳过该分支 - {ex.Message}");
                continue;
            }

            totalCount++;
            string? parentDir = Path.GetDirectoryName(dir);

            if (parentDir == null)
                continue;

            try {
                DirectorySecurity dirSecurity = new DirectoryInfo(dir).GetAccessControl();

                // 检查子目录是否有非继承的权限规则（即手动设置的权限）
                AuthorizationRuleCollection nonInheritedRules = dirSecurity.GetAccessRules(
                    includeExplicit: true,
                    includeInherited: false,
                    targetType: typeof(NTAccount));

                if (nonInheritedRules.Count == 0) {
                    // 子目录没有任何显式设置的权限，完全继承自父目录，跳过
                    skippedCount++;
                    continue;
                }

                string dirSddl = dirSecurity.GetSecurityDescriptorSddlForm(AccessControlSections.All);

                DirectorySecurity parentSecurity = new DirectoryInfo(parentDir).GetAccessControl();
                string parentSddl = parentSecurity.GetSecurityDescriptorSddlForm(AccessControlSections.All);

                if (!string.Equals(dirSddl, parentSddl, StringComparison.OrdinalIgnoreCase)) {
                    differentCount++;
                    differentDirs.Add((dir, parentDir, dirSddl, parentSddl));

                    string relativePath = Path.GetRelativePath(rootPath, dir);
                    string parentRelativePath = Path.GetRelativePath(rootPath, parentDir);

                    if (showDetails) {
                        Console.WriteLine($"不一致: {relativePath}");
                        Console.WriteLine($"  子目录  SDDL: {ResolveSidsToNames(dirSddl)}");
                        Console.WriteLine($"  父目录  SDDL: {ResolveSidsToNames(parentSddl)}");
                        Console.WriteLine();
                    } else {
                        Console.WriteLine($"  {relativePath}  (父目录: {parentRelativePath})");
                    }
                }
            } catch (UnauthorizedAccessException) {
                // 跳过无权限访问的目录
                string relativePath = Path.GetRelativePath(rootPath, dir);
                WriteWarningLine($"警告: 无权限访问 - {relativePath}");
            } catch (Exception ex) {
                string relativePath = Path.GetRelativePath(rootPath, dir);
                WriteWarningLine($"警告: 读取 {relativePath} 的 ACL 时出错 - {ex.Message}");
            }
        }

        // 输出汇总结果
        Console.WriteLine();
        WriteSuccessLine($"扫描完成: 共扫描 {totalCount} 个子目录，{differentCount} 个权限不一致，{skippedCount} 个跳过（完全继承父目录权限）。");
        Console.WriteLine();

        if (differentCount > 0) {
            Console.WriteLine();
            WriteHintLine("提示: 使用 --details 或 -d 参数可查看详细的 SDDL 差异。");

            return 1;
        }

        return 0;
    }

    /// <summary>
    /// 将 SDDL 字符串中的 SID 替换为对应的用户名或组名。
    /// </summary>
    private static string ResolveSidsToNames(string sddl) {
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
    private static void WriteSuccessLine(string message) {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>
    /// 以黄色输出警告信息。
    /// </summary>
    private static void WriteWarningLine(string message) {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Error.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>
    /// 以红色输出错误信息。
    /// </summary>
    private static void WriteErrorLine(string message) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>
    /// 以深灰色输出提示信息。
    /// </summary>
    private static void WriteHintLine(string message) {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}