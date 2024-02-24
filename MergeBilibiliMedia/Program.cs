using MergeBilibiliMedia;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using Shared;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

// 输入参数验证
if (!SharedHelper.CheckArgs(args, Resource.Help)) {
    return;
}

var Logger = SharedLogging.Logger;
//var specialCharacters = new string[] { "?", "*", ":", "\"", "<", ">", "\\", "/", "|" };
var SpecialCharactersRegex = new Regex("[?*:\"<>\\/|]");
const int DefaultThreadCount = 4, MaxThreadCount = 16, MinThreadCount = 1;
const bool DefaultIgnoreError = false;

var Config = SharedHelper.GetConfiguration(args);
var rootDir = Config["rootDir"];
var saveDir = Config["saveDir"];
var threadArg = Config["threads"];
var ignoreErrorArg = Config["ignoreError"]?.ToLowerInvariant() ?? DefaultIgnoreError.ToString();

// 解析参数
int threadCount = string.IsNullOrEmpty(threadArg) ? DefaultThreadCount : int.TryParse(threadArg, out int n) ? n : DefaultThreadCount;
bool ignoreError = !string.IsNullOrEmpty(ignoreErrorArg) && ignoreErrorArg switch {
    "true" => true,
    "false" => false,
    _ => DefaultIgnoreError
};

// 验证 threadCount
if (threadCount < MinThreadCount) {
    threadCount = MinThreadCount;
}
if (threadCount > MaxThreadCount) {
    threadCount = MaxThreadCount;
}

// 判断路径是否存在
if (!Directory.Exists(rootDir)) {
    Logger.LogError($"{nameof(rootDir)} 不存在！");
    return;
}
if (!Directory.Exists(saveDir)) {
    Logger.LogError($"{nameof(saveDir)} 不存在！");
    return;
}
// 检查 ffmpeg 是否存在
try {
    Process.Start(new ProcessStartInfo {
        FileName = "ffmpeg",
        CreateNoWindow = true,
    })!.WaitForExit();
} catch {
    Logger.LogError("Starting ffmpeg failed");
    return;
}

// 去除文件不合法字符
var validateVideoName = (string name) => SpecialCharactersRegex.Replace(name, "");
// 线程列表
var tasks = new Task[threadCount];
// 源视频文件夹列表
var videoDirQueue = new ConcurrentQueue<string>(Directory.GetDirectories(rootDir));

// 并行合并
for (int i = 0; i < threadCount; i++) {
    tasks[i] = Task.Run(() => {
        while (true) {
            try {
                if (!videoDirQueue.TryDequeue(out var dir)) {
                    return;
                }
                string entryJson = File.ReadAllText(Path.Combine(dir, "entry.json"));
                // 弹幕文件
                //string danmuXml = File.ReadAllText(Path.Combine(dir, "danmaku.xml"));
                JToken jToken = JToken.Parse(entryJson);
                string folderName = jToken["type_tag"]!.ToString();
                // 视频名称
                string videoName = jToken["page_data"]!["part"]!.ToString();
                // 音频文件
                string audioPath = Path.Combine(dir, folderName, "audio.m4s");
                // 视频文件
                string videoPath = Path.Combine(dir, folderName, "video.m4s");
                // 输出文件
                string outVideoPath = Path.Combine(saveDir, validateVideoName(videoName) + ".mp4");
                Logger.LogInformation($"starting {videoName}");
                Process.Start(new ProcessStartInfo {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"{videoPath}\" -i \"{audioPath}\" -vcodec copy -acodec copy -y \"{outVideoPath}\"",
                    CreateNoWindow = true,
                })!.WaitForExit();
                Logger.LogInformation($"finished {videoName}");
            } catch {
                if (ignoreError) {
                    continue;
                }
                throw;
            }
        }
    });
}

Task.WaitAll(tasks);
Console.WriteLine("合并完成");
