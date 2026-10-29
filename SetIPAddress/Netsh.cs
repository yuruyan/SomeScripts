using System.Diagnostics;
using System.Net;

namespace SetIPAddress;

/// <summary>
/// netsh 调用封装。刻意不重定向子进程输出：
/// 让 netsh 直接继承本进程的控制台句柄，避免 2008 R2 上 OEM 代码页（936）解码问题。
/// 成功与否只认退出码 + 调用方回读校验。
/// </summary>
internal static class Netsh {
    private static readonly string NetshPath = Path.Combine(Environment.SystemDirectory, "netsh.exe");

    public static int SetStatic(string name, IPAddress address, int prefix, IPAddress? gateway, int? gatewayMetric) {
        var args = new List<string>
        {
            "interface",
            "ipv4",
            "set",
            "address",
            $"name={name}",
            "source=static",
            $"address={address}",
            $"mask={Adapters.PrefixToMask(prefix)}",
            $"gateway={(gateway is null ? "none" : gateway.ToString())}",
        };

        if (gateway is not null && gatewayMetric is not null) {
            args.Add($"gwmetric={gatewayMetric.Value}");
        }

        return Run(args);
    }

    public static int AddAddress(string name, IPAddress address, int prefix) => Run(new List<string>
    {
        "interface",
        "ipv4",
        "add",
        "address",
        $"name={name}",
        $"address={address}",
        $"mask={Adapters.PrefixToMask(prefix)}",
    });

    /// <summary>
    /// 删除附加 IPv4 地址。netsh 的 set address 只替换主地址，旧地址会残留，
    /// 因此必须显式删除。部分版本删除带网关的地址时要求 gateway=all，故失败后重试。
    /// </summary>
    public static int DeleteAddress(string name, IPAddress address) {
        int exitCode = Run(new List<string>
        {
            "interface",
            "ipv4",
            "delete",
            "address",
            $"name={name}",
            $"address={address}",
        });

        if (exitCode == 0) {
            return 0;
        }

        return Run(new List<string>
        {
            "interface",
            "ipv4",
            "delete",
            "address",
            $"name={name}",
            $"address={address}",
            "gateway=all",
        });
    }

    public static int SetDhcp(string name) => Run(new List<string>
    {
        "interface",
        "ipv4",
        "set",
        "address",
        $"name={name}",
        "source=dhcp",
    });

    /// <summary>设置静态 DNS。<paramref name="servers"/> 为空表示清空 DNS 服务器。</summary>
    public static int SetDns(string name, IReadOnlyList<IPAddress> servers) {
        if (servers.Count == 0) {
            return Run(new List<string>
            {
                "interface",
                "ipv4",
                "set",
                "dnsservers",
                $"name={name}",
                "source=static",
                "address=none",
            });
        }

        int exitCode = Run(new List<string>
        {
            "interface",
            "ipv4",
            "set",
            "dnsservers",
            $"name={name}",
            "source=static",
            $"address={servers[0]}",
            "register=primary",
        });

        for (int i = 1; i < servers.Count && exitCode == 0; i++) {
            exitCode = Run(new List<string>
            {
                "interface",
                "ipv4",
                "add",
                "dnsservers",
                $"name={name}",
                $"address={servers[i]}",
                $"index={i + 1}",
            });
        }

        return exitCode;
    }

    public static int SetDnsDhcp(string name) => Run(new List<string>
    {
        "interface",
        "ipv4",
        "set",
        "dnsservers",
        $"name={name}",
        "source=dhcp",
    });

    private static int Run(List<string> args) {
        var startInfo = new ProcessStartInfo(NetshPath) {
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (string arg in args) {
            startInfo.ArgumentList.Add(arg);
        }

        ConsoleIo.WriteLine("> netsh " + string.Join(' ', args.Select(QuoteForDisplay)));

        using Process? process = Process.Start(startInfo);
        if (process is null) {
            ConsoleIo.ErrorLine($"无法启动 {NetshPath}");
            return -1;
        }

        process.WaitForExit();
        return process.ExitCode;
    }

    private static string QuoteForDisplay(string arg) =>
        arg.IndexOf(' ') >= 0 ? "\"" + arg + "\"" : arg;
}
