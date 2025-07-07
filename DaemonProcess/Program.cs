using System.Diagnostics;
using System.Text.Json;
using DaemonProcess;

const string LockFilePath = "LockFile.lock";
const string ConfigFilePath = "ServerConfig.json";

try {
    var file = File.Open(LockFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
    Console.WriteLine("Process started.");
    Start();
} catch {
    Console.Error.WriteLine("Process is already running.");
    Environment.Exit(1);
}

static ServerConfig GetServerConfig() {
    return JsonSerializer.Deserialize(
        File.ReadAllText(ConfigFilePath),
        MyGenerationContext.Default.ServerConfig
    )!;
}

static void Start() {
    var InfoList = new List<ServerInfo>();
    var CheckInterval = TimeSpan.FromMilliseconds(Tools.DefaultCheckingInterval);
    var CheckConfigInterval = TimeSpan.FromMilliseconds(10000);
    var AppStartTimeDict = new Dictionary<int, DateTime>();

    // 定时检查配置文件，如果有更新则重新读取
    Task.Run(() => {
        while (true) {
            Thread.Sleep(CheckConfigInterval);
            try {
                var config = GetServerConfig();
                InfoList = config.Apps;
                CheckInterval = TimeSpan.FromMilliseconds(config.CheckingInterval);
                foreach (var info in InfoList) {
                    if (AppStartTimeDict.ContainsKey(info.GetCode())) {
                        continue;
                    }
                    AppStartTimeDict[info.GetCode()] = DateTime.MinValue;
                }
            } catch { }
        }
    });

    try {
        var config = GetServerConfig();
        InfoList = config.Apps;
        CheckInterval = TimeSpan.FromMilliseconds(config.CheckingInterval);

        foreach (var info in InfoList) {
            AppStartTimeDict[info.GetCode()] = DateTime.MinValue;
        }

        while (true) {
            var processNames = Tools.GetProcessNames().Select(p => p.ToLowerInvariant()).ToHashSet();
            foreach (var info in InfoList) {
                // 检查上次启动时间
                if ((DateTime.Now - AppStartTimeDict[info.GetCode()]).TotalMilliseconds < info.CheckingInterval) {
                    continue;
                }
                // 如果设置了端口
                if (info.Port > 0) {
                    if (Tools.IsPortInUse(info.Port)) {
                        continue;
                    }
                } else {
                    if (processNames.Contains(info.Path.ToLowerInvariant())) {
                        continue;
                    }
                }
                _ = StartAppAsync(info, AppStartTimeDict);
            }
            Thread.Sleep(CheckInterval);
        }
    } catch (Exception e) {
        Console.Error.WriteLine(e);
    }
}

static Task StartAppAsync(ServerInfo serverInfo, Dictionary<int, DateTime> AppStartTimeDict) {
    return Task.Factory.StartNew(state => {
        var info = (ServerInfo)state!;
        try {
            var startInfo = new ProcessStartInfo {
                FileName = info.Path,
                WorkingDirectory = string.IsNullOrEmpty(info.WorkingDirectory) ? Path.GetDirectoryName(info.Path) : info.WorkingDirectory,
                Arguments = info.Args,
                CreateNoWindow = !info.ShowWindow,
                UseShellExecute = info.ShowWindow,
            };
            var tmpFile = Path.GetFullPath(Path.GetRandomFileName() + ".bat");
            if (info.EnvironmentVariables != null && info.EnvironmentVariables.Count > 0) {
                // 不显示cmd瞬间弹框
                startInfo.UseShellExecute = false;
                var cmdContent = "@echo off\n";
                foreach (var env in info.EnvironmentVariables) {
                    cmdContent += $"set {env.Key}={env.Value}\n";
                }
                var hideWindow = info.ShowWindow ? "" : " /B";
                cmdContent += $"start {hideWindow} \"{info.Path}\" \"{info.Path}\" {info.Args}";
                File.WriteAllText(tmpFile, cmdContent);
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = $"/c \"{tmpFile}\"";
            }
            Process.Start(startInfo);
            AppStartTimeDict[info.GetCode()] = DateTime.Now;
            Console.WriteLine($"{DateTime.Now} Started {info.Path} with args {info.Args}");
            _ = Task.Factory.StartNew((file) => {
                var tmpFile = (string)file!;
                Thread.Sleep(info.CheckingInterval);
                if (File.Exists(tmpFile)) {
                    File.Delete(tmpFile);
                }
            }, tmpFile);
        } catch (Exception e) {
            Console.Error.WriteLine($"{DateTime.Now} Failed to start {info.Path} with args {info.Args}: {e.Message}");
        }
    }, serverInfo);
}