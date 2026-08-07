using System.Security.AccessControl;
using System.Security.Principal;
using static ConsoleHelper;

internal class Program {
    private static int Main(string[] args) {
        try {
            string? rootPath = ResolveRootPath(args);
            if (rootPath == null)
                return 2;

            bool showDetails = args.Contains("--details") || args.Contains("-d");

            rootPath = Path.GetFullPath(rootPath);
            Console.WriteLine($"正在扫描: {rootPath}");
            Console.WriteLine();

            var (differentCount, totalCount, skippedCount) = ScanDirectories(rootPath, showDetails);

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
        } catch (Exception ex) {
            // 全局兜底：任何未捕获的异常在此转为错误信息，避免程序崩溃退出
            WriteErrorLine($"错误: 发生未处理的异常 - {ex.Message}");
            WriteErrorLine(ex.StackTrace ?? "无堆栈信息");
            return 2;
        }
    }

    /// <summary>
    /// 从命令行参数或交互输入获取并校验根目录路径。
    /// 返回 null 表示校验失败（错误信息已输出），调用方应直接返回错误码。
    /// </summary>
    private static string? ResolveRootPath(string[] args) {
        string rootPath;

        if (args.Length > 0) {
            rootPath = args[0];
        } else {
            Console.Write("请输入根目录路径: ");
            rootPath = Console.ReadLine()?.Trim() ?? "";
        }

        if (string.IsNullOrWhiteSpace(rootPath)) {
            WriteErrorLine("错误: 未提供目录路径。");
            return null;
        }

        if (!Directory.Exists(rootPath)) {
            WriteErrorLine($"错误: 目录不存在 - {rootPath}");
            return null;
        }

        return rootPath;
    }

    /// <summary>
    /// 递归扫描 rootPath 下所有子目录，输出不一致的目录，并返回统计结果。
    /// </summary>
    private static (int DifferentCount, int TotalCount, int SkippedCount) ScanDirectories(string rootPath, bool showDetails) {
        int differentCount = 0;
        int totalCount = 0;
        int skippedCount = 0;

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

        return (differentCount, totalCount, skippedCount);
    }
}
