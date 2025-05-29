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
                Process.Start(new ProcessStartInfo {
                    FileName = info.Path,
                    WorkingDirectory = string.IsNullOrEmpty(info.WorkingDirectory) ? Path.GetDirectoryName(info.Path) : info.WorkingDirectory,
                    Arguments = info.Args,
                    CreateNoWindow = !info.ShowWindow,
                    UseShellExecute = info.ShowWindow,
                });
                Console.WriteLine($"{DateTime.Now} Started {info.Path} with args {info.Args}");
            } catch (Exception e) {
                Console.Error.WriteLine($"Failed to start {info.Path} with args {info.Args}: {e.Message}");
            }
        }
        Thread.Sleep(3000);
    }
} catch (Exception e) {
    Console.Error.WriteLine(e);
}
