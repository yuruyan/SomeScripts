# HtmlToMarkdown

将 HTML 文件转换为 Markdown 的命令行工具。基于 [ReverseMarkdown](https://github.com/mysticmind/reversemarkdown-net) 库。

## 用法

```
HtmlToMarkdown <inputFile> <outputFile>
```

- `inputFile` — 输入的 HTML 文件路径
- `outputFile` — 输出的 Markdown 文件路径

### 示例

```
HtmlToMarkdown input.html output.md
```

## 退出码

- `0` — 成功
- `1` — 参数不足或发生错误（文件不存在、无权限等）

## 构建

```bash
dotnet build -c Release
```

发布为单文件原生 AOT 二进制：

```bash
dotnet publish -r win-x64 -c Release
```

## 依赖

- .NET 10.0
- ReverseMarkdown 5.4.0
