using OpenCvSharp;

namespace ImageProcessing;

public static class Service {
    internal const double DefaultGaussianBlurRadius = 5;
    internal const double MinGaussianBlurRadius = 0.1;
    internal const double MaxGaussianBlurRadius = 100;

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
}
