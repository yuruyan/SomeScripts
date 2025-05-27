using System.Net.NetworkInformation;
using System.Text.Json.Serialization;

namespace DaemonProcess;

public static class PortChecker {
    public static bool IsPortInUse(int port) {
        var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
        var tcpConnections = ipGlobalProperties.GetActiveTcpListeners();
        return tcpConnections.Any(t => t.Port == port);
    }
}

public record ServerInfo {
    public string Path { get; set; } = string.Empty;
    public string Args { get; set; } = string.Empty;
    public int Port { get; set; } = 0;
    public bool ShowWindow { get; set; }
}

[JsonSerializable(typeof(List<ServerInfo>))]
public partial class MyGenerationContext : JsonSerializerContext {

}
