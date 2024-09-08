using CommonTools.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System.Text.RegularExpressions;

namespace ImageProcessing;

internal static class Proxy {
    private static readonly ILogger Logger = SharedLogging.Logger;

    /// <summary>
    /// 检查 sourcePath 和 savePath 是否合法
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="savePath"></param>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="DirectoryNotFoundException"></exception>
    private static void CheckSourcePathAndSavePath(string? sourcePath, string? savePath) {
        // 验证 sourcePath
        if (!File.Exists(sourcePath)) {
            throw new FileNotFoundException($"文件 '{sourcePath}' 找不到");
        }
    }

    /// <summary>
    /// 解析 Size
    /// </summary>
    /// <param name="widthArg"></param>
    /// <param name="heightArg"></param>
    /// <param name="widthRequired">宽度是否必需</param>
    /// <param name="heightRequired">高度是否必需</param>
    /// <param name="widthOrHeightRequired">width 或 height 必需，此时 <paramref name="heightArg"/> 和 <paramref name="widthArg"/> 失效</param>
    /// <param name="widthArgName">命令行 width 参数名称</param>
    /// <param name="heightArgName">命令行 height 参数名称</param>
    /// <returns><paramref name="widthArg"/> 为空返回 0，<paramref name="heightArg"/> 为空返回 0</returns>
    /// <exception cref="ArgumentException">required 不满足条件时或解析出错或小于 0 抛出异常</exception>
    private static System.Drawing.Size GetSize(
        string? widthArg,
        string? heightArg,
        bool widthRequired = false,
        bool heightRequired = false,
        bool widthOrHeightRequired = false,
        string widthArgName = "width",
        string heightArgName = "height"
    ) {
        // null 判断
        if (widthOrHeightRequired) {
            if (widthArg == null && heightArg == null) {
                throw new ArgumentException($"参数 {widthArgName} 和 {heightArgName} 不能同时为空");
            }
        } else {
            if (widthRequired && widthArg == null) {
                throw new ArgumentException($"参数 {widthArgName} 不能为空");
            }
            if (heightRequired && heightArg == null) {
                throw new ArgumentException($"参数 {heightArgName} 不能为空");
            }
        }
        widthArg ??= "0";
        heightArg ??= "0";
        // 解析
        if (!int.TryParse(widthArg, out var width) || width < 0) {
            throw new ArgumentException($"参数 {widthArgName} 无效");
        }
        if (!int.TryParse(heightArg, out var height) || height < 0) {
            throw new ArgumentException($"参数 {heightArgName} 无效");
        }
        return new System.Drawing.Size(width, height);
    }

    /// <summary>
    /// 高斯模糊
    /// </summary>
    /// <param name="config"></param>
    public static void GaussianBlur(IConfiguration config) {
        var sourcePath = config["sourcePath"];
        var savePath = config["savePath"];
        var radiusArg = config["radius"];

        CheckSourcePathAndSavePath(sourcePath, savePath);
        // 解析 radiusArg
        if (!double.TryParse(radiusArg, out var radius)) {
            radius = Service.DefaultGaussianBlurRadius;
        }
        Service.GaussianBlur(sourcePath!, savePath!, radius);
    }

    private static readonly Regex PutTextPointArgRegex = new(@" *\( *(\d+(?:\.\d+)?) *, *(\d+(?:\.\d+)?) *\) *");

    /// <summary>
    /// 图片添加文字
    /// </summary>
    /// <param name="config"></param>
    public static void PutText(IConfiguration config) {
        var sourcePath = config["sourcePath"];
        var savePath = config["savePath"];
        string text = config["text"] ?? string.Empty;
        string pointArg = config["point"] ?? $"(0,0)";
        string fontSizeArg = config["fontSize"] ?? Service.DefaultPutTextFontSize.ToString();
        string color = config["color"] ?? Service.DefaultPutTextFontColor;
        string fontStyleArg = config["fontStyle"] ?? Service.DefaultPutTextFontStyle.ToString();
        string fontFamily = config["fontFamily"] ?? Service.DefaultPutTextFontFamily;

        CheckSourcePathAndSavePath(sourcePath, savePath);
        // 检验 pointArg
        if (PutTextPointArgRegex.Match(pointArg) is var pointArgMatch && !pointArgMatch.Success) {
            throw new Exception("point 参数格式错误");
        }
        var point = new Point(
            double.Parse(pointArgMatch.Groups[1].Value),
            double.Parse(pointArgMatch.Groups[2].Value)
        );
        // 解析 fontSizeArg
        double fontSize = double.Parse(fontSizeArg);
        // 解析 fontStyleArg
        if (!Enum.TryParse(fontStyleArg, out System.Drawing.FontStyle fontStyle)) {
            fontStyle = Service.DefaultPutTextFontStyle;
        }
        Service.PutText(sourcePath!, savePath!, text, point, fontStyle, fontFamily, fontSize, color);
    }

    /// <summary>
    /// 图像镜像
    /// </summary>
    /// <param name="config"></param>
    public static void Flip(IConfiguration config) {
        var sourcePath = config["sourcePath"];
        var savePath = config["savePath"];
        var modeArg = (config["mode"] ?? Service.DefaultFlipMode.ToString()).ToUpperInvariant();

        CheckSourcePathAndSavePath(sourcePath, savePath);
        // 验证 modeArg 
        if (!Enum.TryParse(modeArg, out FlipMode flipMode)) {
            throw new ArgumentException("参数 mode 无效");
        }
        Service.Flip(sourcePath!, savePath!, flipMode);
    }

    /// <summary>
    /// 图像旋转
    /// </summary>
    /// <param name="config"></param>
    public static void Rotate(IConfiguration config) {
        var sourcePath = config["sourcePath"];
        var savePath = config["savePath"];
        var angleArg = config["angle"] ?? Service.DefaultRotateAngle.ToString();

        CheckSourcePathAndSavePath(sourcePath, savePath);
        // 验证 angleArg
        if (!double.TryParse(angleArg, out var angle)) {
            throw new FormatException("参数 angle 无效");
        }
        Service.Rotate(sourcePath!, savePath!, angle);
    }

    /// <summary>
    /// 图像裁剪
    /// </summary>
    /// <param name="config"></param>
    public static void Crop(IConfiguration config) {
        var sourcePath = config["sourcePath"];
        var savePath = config["savePath"];
        var xArg = config["x"];
        var yArg = config["y"];
        var widthArg = config["width"];
        var heightArg = config["height"];

        CheckSourcePathAndSavePath(sourcePath, savePath);
        // 参数验证
        var position = GetSize(xArg, yArg, widthArgName: "x", heightArgName: "y");
        var size = GetSize(widthArg, heightArg, true, true);
        Service.Crop(
            sourcePath!,
            savePath!,
            new(position.Width, position.Height),
            new(size.Width, size.Height)
        );
    }

    private static readonly Regex ResizeSizeArgRegex = new(@"^(\d+(?:\.\d+)?)\*?$");

    /// <summary>
    /// 图像缩放
    /// </summary>
    /// <param name="config"></param>
    public static void Resize(IConfiguration config) {
        var sourcePath = config["sourcePath"];
        var savePath = config["savePath"];
        var widthArg = config["width"]?.Trim();
        var heightArg = config["height"]?.Trim();

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
            Service.Resize(sourcePath!, savePath!, (w, h) => {
                double width = (double)(isWidthRatio ? w * widthVal : widthVal);
                double height = (double)(isHeightRatio ? h * heightVal : heightVal);
                return new(width, height);
            });
            return;
        }
        // 只指定 width
        if (widthVal != null) {
            Service.Resize(sourcePath!, savePath!, (w, h) => {
                double width = (double)(isWidthRatio ? w * widthVal : widthVal);
                double height = width * h / w;
                return new(width, height);
            });
            return;
        }
        // 只指定 height
        // 为了消除 heightVal waring
        if (heightVal != null) {
            Service.Resize(sourcePath!, savePath!, (w, h) => {
                double height = (double)(isHeightRatio ? h * heightVal : heightVal);
                double width = w * height / h;
                return new(width, height);
            });
        }
    }

    /// <summary>
    /// 转换图片为 icon
    /// </summary>
    /// <param name="config"></param>
    public static void ConvertToIcon(IConfiguration config) {
        var sourcePath = config["sourcePath"];
        var savePath = config["savePath"];
        var widthArg = config["width"];
        var heightArg = config["height"];

        var size = GetSize(widthArg, heightArg);
        CheckSourcePathAndSavePath(sourcePath, savePath);
        if (size.Width == 0 && size.Height == 0) {
            size.Width = Service.DefaultConvertToIconSize;
        }
        Service.ConvertToIcon(sourcePath!, savePath!, size.Width, size.Height);
    }

    /// <summary>
    /// 灰度处理
    /// </summary>
    /// <param name="config"></param>
    public static void GrayScale(IConfiguration config) {
        var sourcePath = config["sourcePath"];
        var savePath = config["savePath"];

        CheckSourcePathAndSavePath(sourcePath, savePath);
        Service.GrayScale(sourcePath!, savePath!);
    }

    /// <summary>
    /// 不透明度处理
    /// </summary>
    /// <param name="config"></param>
    public static void Transparentize(IConfiguration config) {
        var sourcePath = config["sourcePath"];
        var savePath = config["savePath"];
        var opacityArg = config["opacity"] ?? Service.DefaultTransparentizeOpacity.ToString();

        if (!float.TryParse(opacityArg, out var opacity)) {
            Logger.LogError($"参数 {nameof(opacity)} 无效");
            return;
        }
        CheckSourcePathAndSavePath(sourcePath, savePath);
        Service.Transparentize(sourcePath!, savePath!, opacity);
    }

    /// <summary>
    /// 图片反色
    /// </summary>
    /// <param name="config"></param>
    public static void InvertColor(IConfiguration config) {
        var sourcePath = config["sourcePath"];
        var savePath = config["savePath"];

        CheckSourcePathAndSavePath(sourcePath, savePath);
        Service.InvertColor(sourcePath!, savePath!);
    }

    /// <summary>
    /// Svg 转图片
    /// </summary>
    /// <param name="config"></param>
    public static void SvgConvert(IConfiguration config) {
        var sourcePath = config["sourcePath"];
        var savePath = config["savePath"];
        var widthArg = config["width"];
        var heightArg = config["height"];

        var size = GetSize(widthArg, heightArg);
        CheckSourcePathAndSavePath(sourcePath, savePath);
        Service.SvgConvert(sourcePath!, savePath!, size.Width, size.Height);
    }
}
