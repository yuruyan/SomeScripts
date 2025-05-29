using System.Diagnostics;
using System.Text.Json;
using DaemonProcess;

try {
    var infoList = JsonSerializer.Deserialize(
        File.ReadAllText("ServerConfig.json"),
        MyGenerationContext.Default.ListServerInfo
    )!;
    while (true) {
        var processNames = Tools.GetProcessNames();
        foreach (var info in infoList) {
            if (processNames.Contains(info.Path)) {
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
                if (info.EnvironmentVariables != null && info.EnvironmentVariables.Count > 0) {
                    startInfo.UseShellExecute = false;
                    startInfo.FileName = "cmd.exe";
                    var env = string.Join("&& set ", info.EnvironmentVariables.Select(x => $"{x.Key}={x.Value}"));
                    startInfo.Arguments = $"/c \"set {env}&& start {info.Path} {info.Args}\"";
                }
                Process.Start(startInfo);
                Console.WriteLine($"{DateTime.Now} Started {info.Path} with args {info.Args}");
            } catch (Exception e) {
                Console.Error.WriteLine($"{DateTime.Now} Failed to start {info.Path} with args {info.Args}: {e.Message}");
            }
        }
        Thread.Sleep(3000);
    }
} catch (Exception e) {
    Console.Error.WriteLine(e);
}
