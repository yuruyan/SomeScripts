using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.Json.Serialization;

namespace DaemonProcess;

public static class Tools {
    public static bool IsPortInUse(int port) {
        var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
        var tcpConnections = ipGlobalProperties.GetActiveTcpListeners();
        return tcpConnections.Any(t => t.Port == port);
    }

    public static List<string> GetProcessNames() {
        var processes = Process.GetProcesses();
        var filepaths = new List<string>();
        foreach (var process in processes) {
            try {
                var filename = process.MainModule?.FileName;
                if (!string.IsNullOrEmpty(filename)) {
                    filepaths.Add(filename);
                }
            } catch { }
        }
        return filepaths;
    }
}

public record ServerInfo {
    public int Port { get; set; }
    public string Path { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public string Args { get; set; } = string.Empty;
    public bool ShowWindow { get; set; }
    public Dictionary<string, string> EnvironmentVariables { get; set; } = [];
}

[JsonSerializable(typeof(List<ServerInfo>))]
public partial class MyGenerationContext : JsonSerializerContext {

}
