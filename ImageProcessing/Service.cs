using OpenCvSharp;

namespace ImageProcessing;

public static class Service {
    #region GaussianBlur
    internal const double DefaultGaussianBlurRadius = 5;
    internal const double MinGaussianBlurRadius = 0.1;
    internal const double MaxGaussianBlurRadius = 100;
    #endregion

    #region PutText
    internal const double DefaultPutTextFontScale = 1;
    internal const double MinPutTextFontScale = 0.1;
    internal const double MaxPutTextFontScale = 16;
    internal const string DefaultPutTextFontColor = "#000000";
    #endregion

    /// <summary>
    /// 高斯模糊
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="savePath"></param>
    /// <param name="radius"></param>
    public static void GaussianBlur(string sourcePath, string savePath, double radius = DefaultGaussianBlurRadius) {
        if (radius < MinGaussianBlurRadius) {
            radius = MinGaussianBlurRadius;
        }
        if (radius > MaxGaussianBlurRadius) {
            radius = MaxGaussianBlurRadius;
        }

        using var src = new Mat(sourcePath);
        using var dst = new Mat();
        Cv2.GaussianBlur(src, dst, Size.Zero, radius);
        dst.SaveImage(savePath);
    }

    /// <summary>
    /// 图片添加文字
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="savePath"></param>
    /// <param name="text"></param>
    /// <param name="point">文字左上角坐标</param>
    /// <param name="fontScale">字体放大倍数</param>
    /// <param name="color">字体颜色</param>
    /// <exception cref="ArgumentException">text 为空字符串</exception>
    public static void PutText(
        string sourcePath,
        string savePath,
        string text,
        Point point,
        double fontScale = DefaultPutTextFontScale,
        string color = DefaultPutTextFontColor
    ) {
        if (fontScale < MinPutTextFontScale) {
            fontScale = MinPutTextFontScale;
        }
        if (fontScale > MaxPutTextFontScale) {
            fontScale = MaxPutTextFontScale;
        }
        if (string.IsNullOrEmpty(text)) {
            throw new ArgumentException("text 不能为空");
        }

        using var src = new Mat(sourcePath);
        using var dst = src.Clone();
        var c = System.Drawing.ColorTranslator.FromHtml(color);
        Cv2.PutText(
            dst,
            text,
            point,
            HersheyFonts.HersheySimplex,
            fontScale,
            Scalar.FromRgb(c.R, c.G, c.B)
        );
        dst.SaveImage(savePath);
    }
}
