using OpenCvSharp;
using Svg;
using System.Drawing;
using System.Drawing.Imaging;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;

namespace ImageProcessing;

public static class Service {
    #region ImageFormat
    internal const string ImageFormatBmp = ".bmp";
    internal const string ImageFormatGif = ".gif";
    internal const string ImageFormatIcon = ".icon";
    internal const string ImageFormatJpeg = ".jpeg";
    internal const string ImageFormatJpg = ".jpg";
    internal const string ImageFormatPng = ".png";
    #endregion

    #region GaussianBlur
    internal const double DefaultGaussianBlurRadius = 5;
    internal const double MinGaussianBlurRadius = 0.1;
    internal const double MaxGaussianBlurRadius = 100;
    #endregion

    #region PutText
    internal const double DefaultPutTextFontSize = 24;
    internal const double MinPutTextFontSize = 1;
    internal const double MaxPutTextFontSize = 1024;
    internal const string DefaultPutTextFontColor = "#000000";
    internal const FontStyle DefaultPutTextFontStyle = FontStyle.Regular;
    internal const string DefaultPutTextFontFamily = "等线";
    #endregion

    internal const FlipMode DefaultFlipMode = FlipMode.Y;

    #region Rotate
    internal const double DefaultRotateAngle = 90;
    internal const double MinRotateAngle = 0;
    internal const double MaxRotateAngle = 360;
    #endregion

    #region ConvertToIcon
    internal const int DefaultConvertToIconSize = 128;
    //internal const int MinConvertToIconSize = 8;
    internal const int MaxConvertToIconSize = 256;
    #endregion

    #region Transparentize
    internal const float DefaultTransparentizeOpacity = 0.5f;
    internal const float MinTransparentizeOpacity = 0f;
    internal const float MaxTransparentizeOpacity = 1f;
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
    /// <param name="fontStyle">字体样式</param>
    /// <param name="fontFamily">字体</param>
    /// <param name="fontSize">字体大小</param>
    /// <param name="color">字体颜色</param>
    /// <exception cref="ArgumentException">text 为空字符串</exception>
    public static void PutText(
        string sourcePath,
        string savePath,
        string text,
        Point point,
        FontStyle fontStyle = DefaultPutTextFontStyle,
        string fontFamily = DefaultPutTextFontFamily,
        double fontSize = DefaultPutTextFontSize,
        string color = DefaultPutTextFontColor
    ) {
        if (fontSize < MinPutTextFontSize) {
            fontSize = MinPutTextFontSize;
        }
        if (fontSize > MaxPutTextFontSize) {
            fontSize = MaxPutTextFontSize;
        }
        if (string.IsNullOrEmpty(text)) {
            throw new ArgumentException("text 不能为空");
        }

        // 设置字体、字号、是否加粗
        using var LabelFont = new Font(fontFamily, (float)fontSize, fontStyle);
        // 设置字体颜色
        using var labelColor = new SolidBrush(ColorTranslator.FromHtml(color));
        // 底图
        using var ms = new MemoryStream(File.ReadAllBytes(sourcePath));
        // 底图
        using var imgSource = Image.FromStream(ms);
        // 设置画图对象
        using var graphics = Graphics.FromImage(imgSource);
        // 绘图区域框，x方向开始位置，y方向开始位置，宽和高是矩形的宽和高
        var rt = new Rectangle(point.X, point.Y, imgSource.Width, imgSource.Height);
        // 相对于左上角的x、y坐标
        graphics.DrawString(text, LabelFont, labelColor, point.X, point.Y);
        imgSource.Save(savePath, GetImageFormat(savePath));
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
        using var mat = Cv2.GetRotationMatrix2D(new(src.Width >> 1, src.Height >> 1), angle, 1);
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
    /// <exception cref="ArgumentException">数值越界</exception>
    public static void Crop(string sourcePath, string savePath, Point upperLeft, Size size) {
        var rect = new Rect(upperLeft, size);
        using var src = new Mat(sourcePath);
        if (rect.Left < 0 || rect.Top < 0 || rect.Right > src.Width || rect.Height > src.Height) {
            throw new ArgumentException("数值越界");
        }
        using var dst = src.Clone(rect);
        dst.SaveImage(savePath);
    }

    /// <summary>
    /// 缩放图像
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="savePath"></param>
    /// <param name="getSize">获取目标大小，第1、2个参数分别为原图像的宽高</param>
    public static void Resize(string sourcePath, string savePath, Func<int, int, Size> getSize) {
        using var src = new Mat(sourcePath, ImreadModes.Unchanged);
        using var dst = src.Resize(getSize(src.Width, src.Height));
        dst.SaveImage(savePath);
    }

    /// <summary>
    /// 图片转换为 icon
    /// <paramref name="width"/> 或 <paramref name="height"/> 为 0 则保持纵横比
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="savePath"></param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <exception cref="ArgumentException">宽高都小于等于 0</exception>
    public static void ConvertToIcon(string sourcePath, string savePath, int width, int height) {
        if (width <= 0 && height <= 0) {
            throw new ArgumentException("width、height 无效");
        }
        if (width > MaxConvertToIconSize) {
            width = MaxConvertToIconSize;
        }
        if (height > MaxConvertToIconSize) {
            height = MaxConvertToIconSize;
        }

        using var inputBitmap = new Bitmap(sourcePath);
        // 保持纵横比
        if (width == 0) {
            width = height * inputBitmap.Width / inputBitmap.Height;
        }
        if (height == 0) {
            height = width * inputBitmap.Height / inputBitmap.Width;
        }
        using var newBitmap = new Bitmap(inputBitmap, new System.Drawing.Size(width, height));
        // save the resized png into a memory stream for future use
        using var memoryStream = new MemoryStream();
        using var outputStream = new FileStream(savePath, FileMode.Create, FileAccess.Write);

        newBitmap.Save(memoryStream, ImageFormat.Png);
        using var iconWriter = new BinaryWriter(outputStream);
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
        iconWriter.Write(6 + 16);
        // write image data
        // png data must contain the whole png data file
        iconWriter.Write(memoryStream.ToArray());
        iconWriter.Flush();
    }

    /// <summary>
    /// 灰度处理
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="savePath"></param>
    public static void GrayScale(string sourcePath, string savePath) {
        using var src = new Mat(sourcePath, ImreadModes.Grayscale);
        using var dst = src.Clone();
        dst.SaveImage(savePath);
    }

    /// <summary>
    /// 图片透明
    /// </summary>
    /// <param name="opacity">不透明度</param>
    /// <returns></returns>
    public static void Transparentize(string sourcePath, string savePath, float opacity = DefaultTransparentizeOpacity) {
        if (opacity < MinTransparentizeOpacity) {
            opacity = MinTransparentizeOpacity;
        }
        if (opacity > MaxTransparentizeOpacity) {
            opacity = MaxTransparentizeOpacity;
        }

        using var srcImage = new Bitmap(sourcePath);
        float[][] nArray = {
            new float[] {1, 0, 0, 0, 0},
            new float[] {0, 1, 0, 0, 0},
            new float[] {0, 0, 1, 0, 0},
            new float[] {0, 0, 0, opacity, 0},
            new float[] {0, 0, 0, 0, 1}
        };
        var width = srcImage.Width;
        var height = srcImage.Height;
        var matrix = new ColorMatrix(nArray);
        using var attributes = new ImageAttributes();
        attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
        using var resultImage = new Bitmap(width, height);
        using var g = Graphics.FromImage(resultImage);
        g.DrawImage(
            srcImage,
            new(0, 0, width, height),
            0,
            0,
            width,
            height,
            GraphicsUnit.Pixel,
            attributes
        );
        resultImage.Save(savePath, GetImageFormat(savePath));
    }

    /// <summary>
    /// 反色处理
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="savePath"></param>
    public static void InvertColor(string sourcePath, string savePath) {
        using var src = new Mat(sourcePath);
        using var dst = new Mat();
        Cv2.BitwiseNot(src, dst);
        dst.SaveImage(savePath);
    }

    /// <summary>
    /// 根据文件名获取 ImageFormat
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private static ImageFormat GetImageFormat(string path)
        => Path.GetExtension(path).ToLowerInvariant() switch {
            ImageFormatBmp => ImageFormat.Bmp,
            ImageFormatGif => ImageFormat.Gif,
            ImageFormatIcon => ImageFormat.Icon,
            ImageFormatJpeg => ImageFormat.Jpeg,
            ImageFormatJpg => ImageFormat.Jpeg,
            ImageFormatPng => ImageFormat.Png,
            _ => ImageFormat.Png
        };

    /// <summary>
    /// Svg 转图片
    /// <paramref name="width"/> 或 <paramref name="height"/> 为 0 则保持纵横比
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="savePath"></param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    public static void SvgConvert(string sourcePath, string savePath, int width, int height) {
        var svgDoc = SvgDocument.Open(sourcePath);
        using Bitmap bitmap = svgDoc.Draw(width, height);
        bitmap.Save(savePath, ImageFormat.Png);
    }
}
