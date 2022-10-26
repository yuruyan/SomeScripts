using Microsoft.Extensions.Configuration;
using NLog;

namespace ImageProcessing;

internal static class Proxy {
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static Action<IConfiguration> Parse(string name) => name.ToLowerInvariant() switch {
        "gaussianblur" => GaussianBlur,
        _ => throw new ArgumentException($"参数 {name} 错误"),
    };

    /// <summary>
    /// 检查 sourcePath 和 savePath 是否合法
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="savePath"></param>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="DirectoryNotFoundException"></exception>
    private static void CheckSourcePathAndSavePath(string sourcePath, string savePath) {
        var savePathDirectory = Path.GetDirectoryName(savePath);

        // 验证 sourcePath
        if (!File.Exists(sourcePath)) {
            throw new FileNotFoundException($"文件 '{sourcePath}' 找不到");
        }
        // 验证 savePath
        if (!Directory.Exists(savePathDirectory)) {
            throw new DirectoryNotFoundException($"目录 '{savePathDirectory}' 不存在");
        }
    }

    /// <summary>
    /// 高斯模糊
    /// </summary>
    /// <param name="config"></param>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="DirectoryNotFoundException"></exception>
    private static void GaussianBlur(IConfiguration config) {
        string sourcePath = config["sourcePath"];
        string savePath = config["savePath"];
        string radiusArg = config["radius"];

        CheckSourcePathAndSavePath(sourcePath, savePath);
        // 解析 radiusArg
        if (!double.TryParse(radiusArg, out var radius)) {
            radius = Service.DefaultGaussianBlurRadius;
        }
        Service.GaussianBlur(sourcePath, savePath, radius);
    }

}
