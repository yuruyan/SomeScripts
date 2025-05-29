using System.Diagnostics;
using System.Text.Json;
using DaemonProcess;

try {
    const int CheckInterval = 3000;
    var infoList = JsonSerializer.Deserialize(
        File.ReadAllText("ServerConfig.json"),
        MyGenerationContext.Default.ListServerInfo
    )!;
    while (true) {
        var processNames = Tools.GetProcessNames().Select(p => p.ToLowerInvariant()).ToHashSet();
        foreach (var info in infoList) {
            if (processNames.Contains(info.Path.ToLowerInvariant())) {
                continue;
            }
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
                        cmdContent += $"set {env.Key}=\"{env.Value}\"\n";
                    }
                    cmdContent += $"start \"{info.Path}\" \"{info.Path}\" {info.Args}";
                    File.WriteAllText(tmpFile, cmdContent);
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = $"/c \"{tmpFile}\"";
                }
                Process.Start(startInfo);
                Console.WriteLine($"{DateTime.Now} Started {info.Path} with args {info.Args}");
                _ = Task.Factory.StartNew((file) => {
                    var tmpFile = (string)file!;
                    Thread.Sleep(CheckInterval);
                    if (File.Exists(tmpFile)) {
                        File.Delete(tmpFile);
                    }
                }, tmpFile);
            } catch (Exception e) {
                Console.Error.WriteLine($"{DateTime.Now} Failed to start {info.Path} with args {info.Args}: {e.Message}");
            }
        }
        Thread.Sleep(CheckInterval);
    }
} catch (Exception e) {
    Console.Error.WriteLine(e);
}
