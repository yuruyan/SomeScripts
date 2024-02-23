using System.Text.Json;

namespace DirectoryHistoryComparison;

internal class Main {
    /// <summary>
    /// 保存文件夹结构到文件
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="outfile"></param>
    public void SaveStructure(string directory, string outfile) {
        var dir = GetDirectoryStructure(directory);
        dir.Name = directory;
        var json = JsonSerializer.Serialize(dir, SourceGenerationContext.Default.DirectoryItem);
        File.WriteAllText(outfile, json);
    }

    /// <summary>
    /// 比对两个文件夹结构
    /// </summary>
    /// <param name="dir1DataPath"></param>
    /// <param name="dir2DataPath"></param>
    public void CompareDirectories(string dir1DataPath, string dir2DataPath) {
        var dir1 = JsonSerializer.Deserialize(File.ReadAllText(dir1DataPath), SourceGenerationContext.Default.DirectoryItem)!;
        var dir2 = JsonSerializer.Deserialize(File.ReadAllText(dir2DataPath), SourceGenerationContext.Default.DirectoryItem)!;
        CompareDirectories(dir1, dir2, Path.GetDirectoryName(dir1.Name)!);
    }

    /// <summary>
    /// 比较文件夹结构
    /// </summary>
    /// <param name="dir1"></param>
    /// <param name="dir2"></param>
    /// <param name="currentRoot">当前父目录</param>
    private void CompareDirectories(DirectoryItem dir1, DirectoryItem dir2, string currentRoot) {
        // todo: 不区分大小写
        // 文件夹名称不一致
        if (dir1.Name != dir2.Name) {
            Console.WriteLine(Path.Combine(currentRoot, dir2.Name));
            return;
        }
        // 比对文件
        foreach (var file in dir2.Files) {
            if (!dir1.Files.Contains(file)) {
                Console.WriteLine(Path.Combine(currentRoot, file.Name));
            }
        }
        // 比对文件夹
        var subDir1 = dir1.Directories.Select(item => item.Name).ToList();
        var currentSubDirRoot = Path.Combine(currentRoot, dir2.Name);
        foreach (var dir in dir2.Directories) {
            // 新目录
            if (!subDir1.Contains(dir.Name)) {
                Console.WriteLine(Path.Combine(currentSubDirRoot, dir.Name));
                continue;
            }
            CompareDirectories(dir1.Directories.First(item => item.Name == dir.Name), dir, dir2.Name);
        }
    }

    /// <summary>
    /// 获取文件夹结构
    /// </summary>
    /// <param name="directory"></param>
    /// <returns></returns>
    private DirectoryItem GetDirectoryStructure(string directory) {
        return new DirectoryItem(Path.GetFileName(directory)) {
            Files = Directory.GetFiles(directory).Select(item => new FileItem(Path.GetFileName(item))).ToList(),
            Directories = Directory.GetDirectories(directory).Select(GetDirectoryStructure).ToList()
        };
    }
}
