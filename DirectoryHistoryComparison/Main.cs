using System.Text.Json;

namespace DirectoryHistoryComparison;

internal class Main {
    /// <summary>
    /// 保存文件夹结构到文件
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="outfile"></param>
    public void SaveStructureToFile(string directory, string outfile) {
        var dir = GetDirectoryStructure(directory);
        dir.Name = directory;
        var json = JsonSerializer.Serialize(dir, SourceGenerationContext.Default.DirectoryItem);
        File.WriteAllText(outfile, json);
    }

    private DirectoryItem GetDirectoryStructure(string directory) {
        return new DirectoryItem(Path.GetFileName(directory)) {
            Files = Directory.GetFiles(directory).Select(item => new FileItem(Path.GetFileName(item))).ToList(),
            Directories = Directory.GetDirectories(directory).Select(GetDirectoryStructure).ToList()
        };
    }
}
