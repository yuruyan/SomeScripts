using System.Text.Json.Serialization;

namespace DirectoryComparison;

internal record CommonItem {
    public string Name { get; set; } = string.Empty;

    public CommonItem() { }

    public CommonItem(string name) {
        Name = name;
    }
}

internal record FileItem : CommonItem {
    public DateTime ModifyTime { get; set; }

    public FileItem() { }

    public FileItem(string name) : base(name) { }

    public FileItem(string name, DateTime modifyTime) : base(name) {
        ModifyTime = modifyTime;
    }
}

internal record DirectoryItem : CommonItem {
    public List<FileItem> Files { get; set; } = [];
    public List<DirectoryItem> Directories { get; set; } = [];

    public DirectoryItem() { }

    public DirectoryItem(string name) : base(name) { }
}

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<int, string>))]
[JsonSerializable(typeof(CommonItem))]
[JsonSerializable(typeof(FileItem))]
[JsonSerializable(typeof(DirectoryItem))]
internal partial class SourceGenerationContext : JsonSerializerContext { }