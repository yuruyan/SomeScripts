using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace DaemonProcess;

public static class Tools {
    public const int DefaultCheckingInterval = 3000;

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
    public int CheckingInterval { get; set; } = Tools.DefaultCheckingInterval;

    private int Code;
    public int GetCode() {
        if (Code != 0) {
            return Code;
        }
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(ToString()));
        Code = BitConverter.ToInt32(bytes, 0);
        return Code;
    }
}

public record ServerConfig {
    public int CheckingInterval { get; set; } = Tools.DefaultCheckingInterval;
    public List<ServerInfo> Apps { get; set; } = [];
}

[JsonSerializable(typeof(List<ServerInfo>))]
[JsonSerializable(typeof(ServerConfig))]
public partial class MyGenerationContext : JsonSerializerContext { }
