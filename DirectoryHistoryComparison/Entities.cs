using System.Text.Json.Serialization;

namespace DirectoryHistoryComparison;

internal class CommonItem {
    public string Name { get; set; } = string.Empty;

    public CommonItem() { }

    public CommonItem(string name) {
        Name = name;
    }
}

internal class FileItem : CommonItem {
    public FileItem() { }

    public FileItem(string name) : base(name) { }
}

internal class DirectoryItem : CommonItem {
    public List<FileItem> Files { get; set; } = [];
    public List<DirectoryItem> Directories { get; set; } = [];

    public DirectoryItem() { }

    public DirectoryItem(string name) : base(name) { }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<int, string>))]
[JsonSerializable(typeof(CommonItem))]
[JsonSerializable(typeof(FileItem))]
[JsonSerializable(typeof(DirectoryItem))]
internal partial class SourceGenerationContext : JsonSerializerContext { }