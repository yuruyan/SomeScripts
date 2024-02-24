using CommonTools.Utils;
using System.Text.Json;

namespace DirectoryComparison;

internal static class Service {
    /// <summary>
    /// 保存文件夹结构到文件
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="savePath"></param>
    public static void SaveStructure(string directory, string savePath) {
        var dir = GetDirectoryStructure(directory);
        dir.Name = directory;
        var json = JsonSerializer.Serialize(dir, SourceGenerationContext.Default.DirectoryItem);
        File.WriteAllText(savePath, json);
    }

    /// <summary>
    /// 比对两个文件夹结构
    /// </summary>
    /// <param name="dir1DataPath"></param>
    /// <param name="dir2DataPath"></param>
    public static void CompareDirectories(string dir1DataPath, string dir2DataPath) {
        var dir1 = JsonSerializer.Deserialize(File.ReadAllText(dir1DataPath), SourceGenerationContext.Default.DirectoryItem)!;
        var dir2 = JsonSerializer.Deserialize(File.ReadAllText(dir2DataPath), SourceGenerationContext.Default.DirectoryItem)!;
        CompareDirectories(dir1, dir2, Path.GetDirectoryName(dir1.Name)!, Path.GetDirectoryName(dir2.Name)!);
    }

    /// <summary>
    /// 比较文件夹结构
    /// </summary>
    /// <param name="dir1"></param>
    /// <param name="dir2"></param>
    /// <param name="dir1Root">dir1 父目录</param>
    /// <param name="dir2Root">dir2 父目录</param>
    private static void CompareDirectories(DirectoryItem dir1, DirectoryItem dir2, string dir1Root, string dir2Root) {
        var subDir1Root = Path.Combine(dir1Root, dir1.Name);
        var subDir2Root = Path.Combine(dir2Root, dir2.Name);
        // 文件夹名称不一致
        if (!dir1.Name.Equals(dir2.Name, StringComparison.InvariantCultureIgnoreCase)) {
            Console.WriteLine($"Deleted Directory: {subDir1Root}");
            Console.WriteLine($"New Directory: {subDir2Root}");
            return;
        }

        #region 比对文件
        var dir1LowercaseFiles = dir1.Files.Select(item => item.Name.ToLowerInvariant()).ToHashSet();
        var dir2LowercaseFiles = dir2.Files.Select(item => item.Name.ToLowerInvariant()).ToHashSet();
        // 删除的文件
        dir1.Files
            .Where(item => !dir2LowercaseFiles.Contains(item.Name.ToLowerInvariant()))
            .ForEach(item => Console.WriteLine("Deleted File: " + Path.Combine(subDir1Root, item.Name)));
        // 新增的文件
        dir2.Files
            .Where(item => !dir1LowercaseFiles.Contains(item.Name.ToLowerInvariant()))
            .ForEach(item => Console.WriteLine("New File: " + Path.Combine(subDir2Root, item.Name)));
        #endregion

        #region 比对文件夹
        var subDir1LowercaseDirs = dir1.Directories.Select(item => item.Name.ToLowerInvariant()).ToHashSet();
        var subDir2LowercaseDirs = dir2.Directories.Select(item => item.Name.ToLowerInvariant()).ToHashSet();
        // 删除的文件夹
        dir1.Directories
            .Where(item => !subDir2LowercaseDirs.Contains(item.Name.ToLowerInvariant()))
            .ForEach(item => Console.WriteLine("Deleted Directory: " + Path.Combine(subDir1Root, item.Name)));
        // 新增的文件夹
        dir2.Directories
            .Where(item => !subDir1LowercaseDirs.Contains(item.Name.ToLowerInvariant()))
            .ForEach(item => Console.WriteLine("New Directory: " + Path.Combine(subDir2Root, item.Name)));
        // 递归比对相同文件夹
        foreach (var dir in dir2.Directories) {
            if (!subDir1LowercaseDirs.Contains(dir.Name.ToLowerInvariant())) {
                continue;
            }
            CompareDirectories(
                dir1.Directories.First(item => item.Name.Equals(dir.Name, StringComparison.InvariantCultureIgnoreCase)),
                dir,
                subDir1Root,
                subDir2Root
            );
        }
        #endregion
    }

    /// <summary>
    /// 获取文件夹结构
    /// </summary>
    /// <param name="directory"></param>
    /// <returns></returns>
    private static DirectoryItem GetDirectoryStructure(string directory) {
        return new DirectoryItem(Path.GetFileName(directory)) {
            Files = Directory.GetFiles(directory).Select(item => new FileItem(Path.GetFileName(item))).ToList(),
            Directories = Directory.GetDirectories(directory).Select(GetDirectoryStructure).ToList()
        };
    }
}
