using Microsoft.Extensions.Configuration;
using NLog;
using OpenCvSharp;
using System.Text.RegularExpressions;

namespace ImageProcessing;

internal static class Proxy {
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static Action<IConfiguration> Parse(string name) => name.ToLowerInvariant() switch {
        "gaussianblur" => GaussianBlur,
        "puttext" => PutText,
        "flip" => Flip,
        "rotate" => Rotate,
        "crop" => Crop,
        "resize" => Resize,
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

    private static readonly Regex PointArgRegex = new(@" *\( *(\d+(?:\.\d+)?) *, *(\d+(?:\.\d+)?) *\) *");

    /// <summary>
    /// 图片添加文字
    /// </summary>
    /// <param name="config"></param>
    private static void PutText(IConfiguration config) {
        string sourcePath = config["sourcePath"];
        string savePath = config["savePath"];
        string text = config["text"] ?? string.Empty;
        string pointArg = config["point"] ?? $"(0,0)";
        string fontScaleArg = config["fontScale"] ?? Service.DefaultPutTextFontScale.ToString();
        string color = config["color"] ?? Service.DefaultPutTextFontColor.ToString();

        CheckSourcePathAndSavePath(sourcePath, savePath);
        // 检验 pointArg
        if (PointArgRegex.Match(pointArg) is var pointArgMatch && !pointArgMatch.Success) {
            throw new Exception("point 参数格式错误");
        }
        var point = new Point(
            double.Parse(pointArgMatch.Groups[1].Value),
            double.Parse(pointArgMatch.Groups[2].Value)
        );
        // 解析 fontScaleArg
        double fontScale = double.Parse(fontScaleArg);
        Service.PutText(sourcePath, savePath, text, point, fontScale, color);
    }

    /// <summary>
    /// 图像镜像
    /// </summary>
    /// <param name="config"></param>
    private static void Flip(IConfiguration config) {
        string sourcePath = config["sourcePath"];
        string savePath = config["savePath"];
        string modeArg = (config["mode"] ?? Service.DefaultFlipMode.ToString()).ToUpperInvariant();

        CheckSourcePathAndSavePath(sourcePath, savePath);
        // 验证 modeArg 
        if (!Enum.TryParse(modeArg, out FlipMode flipMode)) {
            throw new ArgumentException("参数 mode 无效");
        }
        Service.Flip(sourcePath, savePath, flipMode);
    }

    /// <summary>
    /// 图像旋转
    /// </summary>
    /// <param name="config"></param>
    private static void Rotate(IConfiguration config) {
        string sourcePath = config["sourcePath"];
        string savePath = config["savePath"];
        string angleArg = config["angle"] ?? Service.DefaultRotateAngle.ToString();

        CheckSourcePathAndSavePath(sourcePath, savePath);
        // 验证 angleArg
        if (!double.TryParse(angleArg, out var angle)) {
            throw new FormatException("参数 angle 无效");
        }
        Service.Rotate(sourcePath, savePath, angle);
    }

    /// <summary>
    /// 图像裁剪
    /// </summary>
    /// <param name="config"></param>
    private static void Crop(IConfiguration config) {
        string sourcePath = config["sourcePath"];
        string savePath = config["savePath"];
        string xArg = config["x"] ?? "0";
        string yArg = config["y"] ?? "0";
        string widthArg = config["width"] ?? throw new ArgumentException("参数 width 不能为空");
        string heightArg = config["height"] ?? throw new ArgumentException("参数 height 不能为空");

        CheckSourcePathAndSavePath(sourcePath, savePath);
        // 参数验证
        if (!int.TryParse(xArg, out var x)) {
            throw new ArgumentException($"参数 {nameof(x)} 无效");
        }
        if (!int.TryParse(yArg, out var y)) {
            throw new ArgumentException($"参数 {nameof(y)} 无效");
        }
        if (!int.TryParse(widthArg, out var width)) {
            throw new ArgumentException($"参数 {nameof(width)} 无效");
        }
        if (!int.TryParse(heightArg, out var height)) {
            throw new ArgumentException($"参数 {nameof(height)} 无效");
        }

        Service.Crop(sourcePath, savePath, new(x, y), new(width, height));
    }

    private static readonly Regex ResizeSizeArgRegex = new(@"^(\d+(?:\.\d+)?)\*?$");

    /// <summary>
    /// 图像缩放
    /// </summary>
    /// <param name="config"></param>
    private static void Resize(IConfiguration config) {
        string sourcePath = config["sourcePath"];
        string savePath = config["savePath"];
        string? widthArg = config["width"]?.Trim();
        string? heightArg = config["height"]?.Trim();

        CheckSourcePathAndSavePath(sourcePath, savePath);
        // 都为空
        if (string.IsNullOrEmpty(widthArg) && string.IsNullOrEmpty(heightArg)) {
            throw new ArgumentException("参数 width 和 height 未指定");
        }

        // 正则匹配
        Match? widthMatch = widthArg == null ? null : ResizeSizeArgRegex.Match(widthArg);
        Match? heightMatch = heightArg == null ? null : ResizeSizeArgRegex.Match(heightArg);
        // 合法性判断
        if (widthMatch != null && !widthMatch.Success) {
            throw new FormatException("参数 width 不合法");
        }
        if (heightMatch != null && !heightMatch.Success) {
            throw new FormatException("参数 height 不合法");
        }

        double? widthVal = widthMatch == null ? null : double.Parse(widthMatch.Groups[1].Value);
        double? heightVal = heightMatch == null ? null : double.Parse(heightMatch.Groups[1].Value);
        bool isWidthRatio = widthArg != null && widthArg.Contains('*');
        bool isHeightRatio = heightArg != null && heightArg.Contains('*');
        // 都指定
        if (widthVal != null && heightVal != null) {
            Service.Resize(sourcePath, savePath, (w, h) => {
                double width = (double)(isWidthRatio ? w * widthVal : widthVal);
                double height = (double)(isHeightRatio ? h * heightVal : heightVal);
                return new(width, height);
            });
            return;
        }
        // 只指定 width
        if (widthVal != null) {
            Service.Resize(sourcePath, savePath, (w, h) => {
                double width = (double)(isWidthRatio ? w * widthVal : widthVal);
                double height = width * h / w;
                return new(width, height);
            });
            return;
        }
        // 只指定 height
        // 为了消除 heightVal waring
        if (heightVal != null) {
            Service.Resize(sourcePath, savePath, (w, h) => {
                double height = (double)(isHeightRatio ? h * heightVal : heightVal);
                double width = w * height / h;
                return new(width, height);
            });
        }
    }
}
