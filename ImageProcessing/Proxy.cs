using Microsoft.Extensions.Configuration;
using NLog;

namespace ImageProcessing;

internal static class Proxy {
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static Action<IConfiguration> Parse(string name) => name.ToLowerInvariant() switch {
        "gaussianblur" => GaussianBlur,
        _ => throw new ArgumentException($"参数 {name} 错误"),
    };

    private static void GaussianBlur(IConfiguration config) {
        string sourcePath = config["sourcePath"];
        string savePath = config["savePath"];
        string radiusArg = config["radius"];
        var savePathDirectory = Path.GetDirectoryName(savePath);

        // 验证 sourcePath
        if (!File.Exists(sourcePath)) {
            throw new FileNotFoundException($"文件 '{sourcePath}' 找不到");
        }
        // 验证 sourcePath
        if (!Directory.Exists(savePathDirectory)) {
            throw new DirectoryNotFoundException($"目录 '{savePathDirectory}' 不存在");
        }
        // 解析 radiusArg
        if (!double.TryParse(radiusArg, out var radius)) {
            radius = Service.DefaultGaussianBlurRadius;
        }
        Service.GaussianBlur(sourcePath, savePath, radius);
    }

}
