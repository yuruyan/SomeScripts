using System.Globalization;
using System.Text.RegularExpressions;

namespace SubtitleAdjustment;

public interface IAdjustable {
    /// <summary>
    /// 调节
    /// </summary>
    /// <param name="subs">字幕</param>
    /// <param name="offset">时间偏移量(ms)，正数表示将字幕滞后，负数表示将字幕超前</param>
    /// <returns>调节后的字幕</returns>
    public string Adjust(string subText, int offset);
}

/// <summary>
/// src 字幕调节
/// </summary>
public class SrtAdjustment : IAdjustable {
    /// <summary>
    /// 文件后缀，包括 '.'
    /// </summary>
    public static string FileExtension => ".srt";
    private const string TimePattern = "HH:mm:ss,fff";
    private const string FormatTimePattern = "HH:mm:ss,fff";
    private static readonly DateTimeFormatInfo DateTimeFormatInfo = new() {
        ShortTimePattern = FormatTimePattern
    };
    private static readonly Regex SubtitleNumRegex = new(@"^\d+$");
    private static readonly Regex SubtitleLineRegex = new(@"\d+:\d+:\d+,\d+ --> \d+:\d+:\d+,\d+");
    private static readonly Regex SubtitleTimeRegex = new(@"(?<time>\d+:\d+:\d+),(?<milli>\d+)");

    public string Adjust(string subText, int offset) {
        var subs = subText.Split('\n', StringSplitOptions.TrimEntries);
        for (int i = 1; i < subs.Length; i++) {
            if (!SubtitleNumRegex.IsMatch(subs[i - 1]) || !SubtitleLineRegex.IsMatch(subs[i])) {
                continue;
            }
            // 调节
            subs[i] = SubtitleTimeRegex.Replace(subs[i], m =>
                DateTime.Parse(m.Groups["time"].Value, DateTimeFormatInfo)
                    .AddMilliseconds(int.Parse(m.Groups["milli"].Value))
                    .AddMilliseconds(offset)
                    .ToString(TimePattern)
            );
            i += 2;
        }
        return string.Join('\n', subs);
    }
}

/// <summary>
/// ass 字幕调节
/// </summary>
public class AssAdjustment : IAdjustable {
    /// <inheritdoc cref="SrtAdjustment.FileExtension"/>
    public static string FileExtension => ".ass";
    private const string TimePattern = "HH:mm:ss.ff";
    private static readonly DateTimeFormatInfo DateTimeFormatInfo = new() {
        ShortTimePattern = TimePattern
    };
    private static readonly Regex SubtitleLineRegex = new(@"[a-zA-Z]+: ", RegexOptions.IgnoreCase);
    private static readonly Regex SubtitleTimeRegex = new(@"\d+:\d+:\d+\.\d+");

    public string Adjust(string subText, int offset) {
        var subs = subText.Split('\n');
        // 字幕解析失败
        if (GetSubtitleStartIndex(subs) is var subStartIndex && subStartIndex == -1) {
            return subText;
        }
        // 调节
        for (int i = subStartIndex; i < subs.Length; i++) {
            subs[i] = AdjustTime(subs[i], offset);
        }
        return string.Join('\n', subs);
    }

    /// <summary>
    /// 调节时间
    /// </summary>
    /// <param name="subText"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    private string AdjustTime(string subText, int offset) {
        // 判断是否是字幕行
        if (!SubtitleLineRegex.IsMatch(subText)) {
            return subText;
        }
        return SubtitleTimeRegex.Replace(subText, m =>
            DateTime.Parse(m.Value, DateTimeFormatInfo)
                .AddMilliseconds(offset)
                .ToString(TimePattern)
        );
    }

    /// <summary>
    /// 获取字幕开始行索引
    /// </summary>
    /// <param name="subs"></param>
    /// <returns>找不到返回 -1</returns>
    private int GetSubtitleStartIndex(string[] subs) {
        const string flag1 = "[Events]";
        const string flag2 = "Format: ";
        for (int i = 1; i < subs.Length; i++) {
            if (subs[i - 1].StartsWith(flag1) && subs[i].StartsWith(flag2)) {
                return i + 1;
            }
        }
        return -1;
    }
}

/// <summary>
/// ssa 字幕调节
/// </summary>
public class SsaAdjustment : IAdjustable {
    /// <inheritdoc cref="SrtAdjustment.FileExtension"/>
    public static string FileExtension => ".ssa";
    private readonly IAdjustable AssAdjustment = new AssAdjustment();

    public string Adjust(string subText, int offset) => AssAdjustment.Adjust(subText, offset);
}

public static class Service {
    /// <summary>
    /// &lt;字幕后缀(包括 '.')，IAdjustable&gt;
    /// </summary>
    private static readonly IReadOnlyDictionary<string, IAdjustable> SubAdjustmentDict;

    static Service() {
        SubAdjustmentDict = new Dictionary<string, IAdjustable>() {
            {SrtAdjustment.FileExtension, new SrtAdjustment() },
            {SsaAdjustment.FileExtension, new SsaAdjustment() },
            {AssAdjustment.FileExtension, new AssAdjustment() },
        };
    }

    /// <summary>
    /// 字幕调节
    /// </summary>
    /// <param name="sourcePath">源文件路径</param>
    /// <param name="savePath">保存路径</param>
    /// <param name="offset">时间偏移量(ms)，正数表示将字幕滞后，负数表示将字幕超前</param>
    /// <exception cref="ArgumentException">字幕类型不支持</exception>
    public static void Adjust(string sourcePath, string savePath, int offset) {
        var extLower = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (!CheckSubtitleType(extLower)) {
            throw new ArgumentException($"字幕类型 {extLower} 不支持");
        }
        File.WriteAllText(
            savePath,
            SubAdjustmentDict[extLower].Adjust(File.ReadAllText(sourcePath), offset)
        );
    }

    /// <summary>
    /// 检查字幕类型是否合法
    /// </summary>
    /// <param name="extension">字幕后缀，包括 '.'</param>
    /// <returns>合法返回 true，否则返回 false</returns>
    private static bool CheckSubtitleType(string extension)
        => SubAdjustmentDict.ContainsKey(extension.ToLowerInvariant());
}
