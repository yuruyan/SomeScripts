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

    internal const FlipMode DefaultFlipMode = FlipMode.Y;

    #region Rotate
    internal const double DefaultRotateAngle = 90;
    internal const double MinRotateAngle = 0;
    internal const double MaxRotateAngle = 360;
    #endregion

    #region ConvertToIcon
    internal const int DefaultConvertToIconSize = 64;
    internal const int MinConvertToIconSize = 8;
    internal const int MaxConvertToIconSize = 256;
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

    /// <summary>
    /// 图像镜像
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="savePath"></param>
    /// <param name="flipMode"></param>
    public static void Flip(string sourcePath, string savePath, FlipMode flipMode = DefaultFlipMode) {
        using var src = new Mat(sourcePath);
        using var dst = new Mat();
        Cv2.Flip(src, dst, flipMode);
        dst.SaveImage(savePath);
    }

    /// <summary>
    /// 旋转图像
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="savePath"></param>
    /// <param name="angle"></param>
    public static void Rotate(string sourcePath, string savePath, double angle = DefaultRotateAngle) {
        if (angle < MinRotateAngle) {
            angle = MinRotateAngle;
        }
        if (angle > MaxRotateAngle) {
            angle = MaxRotateAngle;
        }

        using var src = new Mat(sourcePath);
        using var dst = new Mat();
        Mat mat = Cv2.GetRotationMatrix2D(new(src.Width >> 1, src.Height >> 1), angle, 1);
        Cv2.WarpAffine(src, dst, mat, new(src.Cols, src.Rows));
        dst.SaveImage(savePath);
    }

    /// <summary>
    /// 裁剪图像
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="savePath"></param>
    /// <param name="upperLeft">左上角坐标</param>
    /// <param name="size">裁剪后的图像大小</param>
    public static void Crop(string sourcePath, string savePath, Point upperLeft, Size size) {
        using var src = new Mat(sourcePath);
        using var dst = src.Clone(new(upperLeft, size));
        dst.SaveImage(savePath);
    }

    /// <summary>
    /// 缩放图像
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="savePath"></param>
    /// <param name="getSize">获取目标大小，第1、2个参数分别为原图像的宽高</param>
    public static void Resize(string sourcePath, string savePath, Func<int, int, Size> getSize) {
        using var src = new Mat(sourcePath);
        using var dst = new Mat();
        Cv2.Resize(src, dst, getSize(src.Width, src.Height));
        dst.SaveImage(savePath);
    }

    /// <summary>
    /// 图片转换为 icon
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="savePath"></param>
    /// <param name="size"></param>
    public static void ConvertToIcon(string sourcePath, string savePath, int size = DefaultConvertToIconSize) {
        if (size < MinConvertToIconSize) {
            size = MinConvertToIconSize;
        }
        if (size > MaxConvertToIconSize) {
            size = MaxConvertToIconSize;
        }

        var inputBitmap = new System.Drawing.Bitmap(sourcePath);
        int width = size, height = width * inputBitmap.Height / inputBitmap.Width;
        var newBitmap = new System.Drawing.Bitmap(inputBitmap, new System.Drawing.Size(width, height));
        // save the resized png into a memory stream for future use
        using var memoryStream = new MemoryStream();
        // 先删除文件
        if (File.Exists(savePath)) {
            File.Delete(savePath);
        }
        using var outputStream = File.OpenWrite(savePath);

        newBitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
        var iconWriter = new BinaryWriter(outputStream);
        // 0-1 reserved, 0
        iconWriter.Write((byte)0);
        iconWriter.Write((byte)0);
        // 2-3 image type, 1 = icon, 2 = cursor
        iconWriter.Write((short)1);
        // 4-5 number of images
        iconWriter.Write((short)1);
        // image entry 1
        // 0 image width
        iconWriter.Write((byte)width);
        // 1 image height
        iconWriter.Write((byte)height);
        // 2 number of colors
        iconWriter.Write((byte)0);
        // 3 reserved
        iconWriter.Write((byte)0);
        // 4-5 color planes
        iconWriter.Write((short)0);
        // 6-7 bits per pixel
        iconWriter.Write((short)32);
        // 8-11 size of image data
        iconWriter.Write((int)memoryStream.Length);
        // 12-15 offset of image data
        iconWriter.Write((int)(6 + 16));
        // write image data
        // png data must contain the whole png data file
        iconWriter.Write(memoryStream.ToArray());
        iconWriter.Flush();
    }
}
