using System.Diagnostics;
using System.Text.Json;
using DaemonProcess;

try {
    var infoList = JsonSerializer.Deserialize(
        File.ReadAllText("ServerConfig.json"),
        MyGenerationContext.Default.ListServerInfo
    )!;
    while (true) {
        foreach (var info in infoList) {
            if (PortChecker.IsPortInUse(info.Port)) {
                continue;
            }
            try {
                Process.Start(new ProcessStartInfo {
                    FileName = info.Path,
                    WorkingDirectory = Path.GetDirectoryName(info.Path),
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
