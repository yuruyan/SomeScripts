using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SetIPAddress;

internal sealed class Desired {
    public IPAddress Ip { get; init; } = IPAddress.None;

    public int Prefix { get; init; } = 24;

    public IPAddress? Gateway { get; init; }

    public int? GatewayMetric { get; init; }

    /// <summary>null 表示不修改 DNS；空集合表示清空 DNS。</summary>
    public List<IPAddress>? Dns { get; init; }
}

internal static class Program {
    private const int ExitOk = 0;
    private const int ExitUsage = 1;
    private const int ExitAdapter = 2;
    private const int ExitNotElevated = 3;
    private const int ExitNetsh = 4;
    private const int ExitVerify = 5;
    private const int ExitRefused = 6;
    private const int ExitGatewayUnreachable = 7;
    private const int ExitUnexpected = 9;

    /// <summary>网关连通性检查默认观测窗口（秒）。</summary>
    private const int DefaultGatewayCheckSeconds = 15;

    private static int Main(string[] args) {
        try {
            return Run(args);
        } catch (UsageException ex) {
            ConsoleIo.ErrorLine(ex.Message);
            ConsoleIo.ErrorLine("使用 --help 查看用法。");
            return ExitUsage;
        } catch (AdapterException ex) {
            ConsoleIo.ErrorLine(ex.Message);
            return ExitAdapter;
        } catch (ElevationRequiredException ex) {
            ConsoleIo.ErrorLine(ex.Message);
            return ExitNotElevated;
        } catch (Exception ex) {
            ConsoleIo.ErrorLine("未预期的错误:");
            PrintException(ex);
            return ExitUnexpected;
        }
    }

    private static void PrintException(Exception ex) {
        foreach (string line in ex.ToString().Split('\n')) {
            ConsoleIo.ErrorLine("  " + line.TrimEnd('\r'));
        }
    }

    private static int Run(string[] args) {
        if (args.Length == 0) {
            PrintUsage();
            return ExitUsage;
        }

        switch (args[0].ToLowerInvariant()) {
            case "help":
            case "-h":
            case "--help":
            case "/?":
                PrintUsage();
                return ExitOk;
            case "list":
                return ListCommand(args);
            case "show":
                return ShowCommand(args);
            case "set":
                return SetCommand(args);
            case "dhcp":
                return DhcpCommand(args);
            default:
                throw new UsageException($"未知命令: {args[0]}");
        }
    }

    private static int ListCommand(string[] args) {
        Options options = Options.Parse(args, Array.Empty<string>(), new[] { "--all" });
        List<AdapterSnapshot> adapters = Adapters.Enumerate(options.Has("--all"));
        if (adapters.Count == 0) {
            ConsoleIo.ErrorLine("未找到网卡。");
            return ExitAdapter;
        }

        PrintTable(adapters);
        return ExitOk;
    }

    private static int ShowCommand(string[] args) {
        Options options = Options.Parse(args, SelectorKeys, Array.Empty<string>());
        AdapterSnapshot target = SelectOne(Adapters.Enumerate(false), BuildSelector(options));
        PrintDetail(target);
        return ExitOk;
    }

    private static int SetCommand(string[] args) {
        Options options = Options.Parse(
            args,
            SelectorKeys.Concat(new[] { "--ip", "--mask", "--gw", "--dns", "--gw-metric", "--gw-check" }).ToArray(),
            new[] { "--force" });

        Selector selector = BuildSelector(options);
        string ipText = options.Get("--ip") ?? throw new UsageException("缺少 --ip 参数。");
        (IPAddress ip, int? prefixFromIp) = ParseAddressWithPrefix(ipText);
        int? prefixFromMask = options.Get("--mask") is string maskText ? ParsePrefixOrMask(maskText) : null;
        string? gatewayText = options.Get("--gw");
        bool keepGateway = gatewayText is null;
        IPAddress? requestedGateway = keepGateway || gatewayText!.Equals("none", StringComparison.OrdinalIgnoreCase)
            ? null
            : ParseIpv4(gatewayText);
        int? gatewayMetric = ParseOptionalMetric(options.Get("--gw-metric"));
        List<IPAddress>? dns = ParseOptionalDns(options.Get("--dns"));
        int gatewayCheckSeconds = ParseGatewayCheckSeconds(options.Get("--gw-check"));
        bool force = options.Has("--force");

        EnsureElevated();

        AdapterSnapshot target = SelectOne(Adapters.Enumerate(false), selector);
        int prefix;
        if (prefixFromIp is not null) {
            prefix = prefixFromIp.Value;
        } else if (prefixFromMask is not null) {
            prefix = prefixFromMask.Value;
        } else if (target.Primary is not null && target.Primary.Prefix >= 0) {
            prefix = target.Primary.Prefix;
        } else {
            prefix = 24;
            ConsoleIo.WriteLine("提示: 未提供掩码且网卡当前无 IPv4 地址，按 /24 处理。");
        }

        // 未提供 --gw 时保持原网关；--gw none 表示显式删除。
        IPAddress? gateway = keepGateway
            ? (target.Gateways.Count > 0 ? target.Gateways[0] : null)
            : requestedGateway;

        var desired = new Desired {
            Ip = ip,
            Prefix = prefix,
            Gateway = gateway,
            GatewayMetric = gatewayMetric,
            Dns = dns,
        };

        string gatewayDescription = keepGateway
            ? (gateway is null ? "，网关 无（原配置无网关）" : $"，网关 {gateway}（保持原值）")
            : (gateway is null ? "，网关 移除" : $"，网关 {gateway}");

        ConsoleIo.WriteLine($"目标网卡: {target.Name} | {target.Description} | {(string.IsNullOrEmpty(target.Mac) ? "无 MAC" : target.Mac)}");
        ConsoleIo.WriteLine(
            $"目标配置: {ip}/{prefix}{gatewayDescription}" +
            (dns is null ? string.Empty : $"，DNS {(dns.Count == 0 ? "清空" : string.Join(", ", dns))}"));

        if (!target.DhcpEnabled && MatchesStatic(target, desired)) {
            ConsoleIo.WriteLine("当前配置已是目标配置，无需更改。");
            return ExitOk;
        }

        if (target.Gateways.Count > 0 && !force) {
            ConsoleIo.ErrorLine($"拒绝执行: 网卡 {target.Name} 当前持有网关 {string.Join(", ", target.Gateways)}，更改后可能断开远程连接。");
            ConsoleIo.ErrorLine("确认风险后请加 --force 重试。");
            return ExitRefused;
        }

        int exitCode = Netsh.SetStatic(target.Name, desired.Ip, desired.Prefix, desired.Gateway, desired.GatewayMetric);
        if (exitCode != 0) {
            ConsoleIo.ErrorLine($"netsh 设置地址失败（退出码 {exitCode}）。");
            Rollback(target);
            return ExitNetsh;
        }

        RemoveUnexpectedAddresses(target.Id, new[] { desired.Ip });

        if (desired.Dns is not null) {
            exitCode = Netsh.SetDns(target.Name, desired.Dns);
            if (exitCode != 0) {
                ConsoleIo.ErrorLine($"netsh 设置 DNS 失败（退出码 {exitCode}）。");
                Rollback(target);
                return ExitNetsh;
            }
        }

        bool? verified = TryWaitForStatic(target.Id, desired, TimeSpan.FromSeconds(10));
        if (verified == false) {
            ConsoleIo.ErrorLine("回读校验失败: 配置未按预期生效。");
            Rollback(target);
            return ExitVerify;
        }

        if (verified is null) {
            return ExitVerify;
        }

        bool? gatewayReachable = ProbeGateway(target.Id, gateway, gatewayCheckSeconds);
        if (gatewayReachable == false) {
            ConsoleIo.ErrorLine($"网关 {gateway} 持续不可达，回滚到更改前的配置。");
            Rollback(target);
            return ExitGatewayUnreachable;
        }

        ConsoleIo.WriteLine("更改成功，回读结果:");
        AdapterSnapshot? updated = Adapters.Enumerate(false).FirstOrDefault(a => a.Id == target.Id);
        if (updated is not null) {
            PrintDetail(updated);
            if (updated.DhcpEnabled) {
                ConsoleIo.WriteLine("提示: 系统仍把该网卡的 DHCP 标志报告为“是”（媒体断开的网卡会把静态地址存为“备用配置”），");
                ConsoleIo.WriteLine("      地址已按目标生效；联机后如仍显示 DHCP 启用，请在网卡属性中确认。");
            }
        }

        return ExitOk;
    }

    private static int DhcpCommand(string[] args) {
        Options options = Options.Parse(args, SelectorKeys.Concat(new[] { "--gw-check" }).ToArray(), new[] { "--force" });
        Selector selector = BuildSelector(options);
        int gatewayCheckSeconds = ParseGatewayCheckSeconds(options.Get("--gw-check"));
        bool force = options.Has("--force");

        EnsureElevated();

        AdapterSnapshot target = SelectOne(Adapters.Enumerate(false), selector);
        ConsoleIo.WriteLine($"目标网卡: {target.Name} | {target.Description}");

        if (target.DhcpEnabled) {
            ConsoleIo.WriteLine("网卡已处于 DHCP 模式，无需更改。");
            return ExitOk;
        }

        if (target.Gateways.Count > 0 && !force) {
            ConsoleIo.ErrorLine($"拒绝执行: 网卡 {target.Name} 当前持有网关 {string.Join(", ", target.Gateways)}，更改后可能断开远程连接。");
            ConsoleIo.ErrorLine("确认风险后请加 --force 重试。");
            return ExitRefused;
        }

        int exitCode = Netsh.SetDhcp(target.Name);
        if (exitCode == 0) {
            exitCode = Netsh.SetDnsDhcp(target.Name);
        }

        if (exitCode != 0) {
            ConsoleIo.ErrorLine($"netsh 切换 DHCP 失败（退出码 {exitCode}）。");
            Rollback(target);
            return ExitNetsh;
        }

        bool? dhcpVerified = TryWaitForDhcp(target.Id, TimeSpan.FromSeconds(15));
        if (dhcpVerified == false) {
            ConsoleIo.ErrorLine("回读校验失败: 网卡未切到 DHCP。");
            Rollback(target);
            return ExitVerify;
        }

        if (dhcpVerified is null) {
            return ExitVerify;
        }

        AdapterSnapshot? afterDhcp = Adapters.Enumerate(false).FirstOrDefault(a => a.Id == target.Id);
        IPAddress? dhcpGateway = afterDhcp is not null && afterDhcp.Gateways.Count > 0 ? afterDhcp.Gateways[0] : null;
        bool? gatewayReachable = ProbeGateway(target.Id, dhcpGateway, gatewayCheckSeconds);
        if (gatewayReachable == false) {
            ConsoleIo.ErrorLine($"网关 {dhcpGateway} 持续不可达，回滚到更改前的配置。");
            Rollback(target);
            return ExitGatewayUnreachable;
        }

        ConsoleIo.WriteLine("已切换为 DHCP，回读结果:");
        AdapterSnapshot? updated = Adapters.Enumerate(false).FirstOrDefault(a => a.Id == target.Id);
        if (updated is not null) {
            PrintDetail(updated);
        }

        return ExitOk;
    }

    private static readonly string[] SelectorKeys = { "--name", "--desc", "--mac", "--index" };

    private static Selector BuildSelector(Options options) {
        int? index = null;
        if (options.Get("--index") is string indexText) {
            if (!int.TryParse(indexText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) || parsed < 0) {
                throw new UsageException($"--index 需要非负整数: {indexText}");
            }

            index = parsed;
        }

        return new Selector {
            Name = options.Get("--name"),
            Description = options.Get("--desc"),
            Mac = options.Get("--mac"),
            Index = index,
        };
    }

    private static AdapterSnapshot SelectOne(List<AdapterSnapshot> adapters, Selector selector) {
        if (selector.IsEmpty) {
            List<AdapterSnapshot> up = adapters.Where(a => a.Up).ToList();
            if (up.Count == 1) {
                return up[0];
            }

            throw new AdapterException(up.Count == 0
                ? "未指定网卡，且没有已连接的网卡。\n" + Describe(adapters)
                : $"未指定网卡，但有 {up.Count} 个已连接网卡，请用 --name/--desc/--mac/--index 指定。\n" + Describe(up));
        }

        List<AdapterSnapshot> matched = adapters.Where(selector.Matches).ToList();
        if (matched.Count == 1) {
            return matched[0];
        }

        if (matched.Count == 0) {
            throw new AdapterException("未找到匹配的网卡。\n" + Describe(adapters));
        }

        throw new AdapterException($"匹配到 {matched.Count} 个网卡，请提供更精确的条件。\n" + Describe(matched));
    }

    private static string Describe(IEnumerable<AdapterSnapshot> adapters) =>
        string.Join(
            Environment.NewLine,
            adapters.Select(a =>
                $"  - {a.Name} | {a.Description} | {(string.IsNullOrEmpty(a.Mac) ? "无 MAC" : a.Mac)} | 索引 {(a.Index >= 0 ? a.Index.ToString(CultureInfo.InvariantCulture) : "-")} | {StatusText(a.Status)}"));

    private static void EnsureElevated() {
        if (!ConsoleIo.IsElevated()) {
            throw new ElevationRequiredException();
        }
    }

    private static bool MatchesStatic(AdapterSnapshot adapter, Desired desired) {
        if (adapter.V4.Count != 1 ||
            !adapter.V4[0].Address.Equals(desired.Ip) ||
            adapter.V4[0].Prefix != desired.Prefix) {
            return false;
        }

        if (desired.Gateway is null) {
            if (adapter.Gateways.Count > 0) {
                return false;
            }
        } else if (!adapter.Gateways.Any(gateway => gateway.Equals(desired.Gateway))) {
            return false;
        }

        if (desired.Dns is not null) {
            if (desired.Dns.Count != adapter.Dns.Count ||
                !desired.Dns.All(server => adapter.Dns.Contains(server))) {
                return false;
            }
        }

        return true;
    }

    private static bool WaitForStatic(string id, Desired desired, TimeSpan timeout) {
        long deadline = Environment.TickCount64 + (long)timeout.TotalMilliseconds;
        while (true) {
            AdapterSnapshot? adapter = Adapters.Enumerate(false).FirstOrDefault(a => a.Id == id);
            if (adapter is not null && MatchesStatic(adapter, desired)) {
                return true;
            }

            if (Environment.TickCount64 >= deadline) {
                return false;
            }

            Thread.Sleep(500);
        }
    }

    private static bool WaitForDhcp(string id, TimeSpan timeout) {
        long deadline = Environment.TickCount64 + (long)timeout.TotalMilliseconds;
        while (true) {
            AdapterSnapshot? adapter = Adapters.Enumerate(false).FirstOrDefault(a => a.Id == id);
            if (adapter is not null && adapter.DhcpEnabled) {
                return true;
            }

            if (Environment.TickCount64 >= deadline) {
                return false;
            }

            Thread.Sleep(500);
        }
    }

    /// <summary>null 表示校验过程本身抛异常：配置已提交，不做回滚，交由人工核对。</summary>
    private static bool? TryWaitForStatic(string id, Desired desired, TimeSpan timeout) {
        try {
            return WaitForStatic(id, desired, timeout);
        } catch (Exception ex) {
            ConsoleIo.ErrorLine("回读校验异常（netsh 已执行，未回滚，请人工核对）:");
            PrintException(ex);
            return null;
        }
    }

    private static bool? TryWaitForDhcp(string id, TimeSpan timeout) {
        try {
            return WaitForDhcp(id, timeout);
        } catch (Exception ex) {
            ConsoleIo.ErrorLine("回读校验异常（netsh 已执行，未回滚，请人工核对）:");
            PrintException(ex);
            return null;
        }
    }

    /// <summary>删除网卡上除 <paramref name="keep"/> 之外的 IPv4 地址（netsh set address 不会清理旧地址）。</summary>
    private static void RemoveUnexpectedAddresses(string id, IReadOnlyCollection<IPAddress> keep) {
        AdapterSnapshot? current = Adapters.Enumerate(false).FirstOrDefault(a => a.Id == id);
        if (current is null) {
            return;
        }

        var keepSet = new HashSet<string>(keep.Select(address => address.ToString()), StringComparer.Ordinal);
        foreach (Ipv4Entry entry in current.V4) {
            if (!keepSet.Contains(entry.Address.ToString())) {
                Netsh.DeleteAddress(current.Name, entry.Address);
            }
        }
    }

    private static void Rollback(AdapterSnapshot original) {
        ConsoleIo.WriteLine("正在回滚到更改前的配置…");

        Ipv4Entry? primary = original.Primary;
        if (original.DhcpEnabled || primary is null) {
            Netsh.SetDhcp(original.Name);
            Netsh.SetDnsDhcp(original.Name);
            ConsoleIo.WriteLine("已恢复为 DHCP。");
            return;
        }

        int prefix = primary.Prefix >= 0 ? primary.Prefix : 24;
        IPAddress? gateway = original.Gateways.Count > 0 ? original.Gateways[0] : null;
        int exitCode = Netsh.SetStatic(original.Name, primary.Address, prefix, gateway, null);

        RemoveUnexpectedAddresses(original.Id, original.V4.Select(entry => entry.Address).ToList());

        for (int i = 1; i < original.V4.Count; i++) {
            Ipv4Entry entry = original.V4[i];
            Netsh.AddAddress(original.Name, entry.Address, entry.Prefix >= 0 ? entry.Prefix : 24);
        }

        Netsh.SetDns(original.Name, original.Dns);

        ConsoleIo.WriteLine(exitCode == 0
            ? "已恢复原静态地址（请用 show 复核）。"
            : $"回滚命令返回退出码 {exitCode}，请手工核对配置。");
    }

    private static void PrintTable(List<AdapterSnapshot> adapters) {
        string[] headers = { "索引", "连接名", "状态", "DHCP", "IPv4", "网关", "描述" };
        var rows = adapters.Select(a => new[]
        {
            a.Index >= 0 ? a.Index.ToString(CultureInfo.InvariantCulture) : "-",
            a.Name,
            StatusText(a.Status),
            a.DhcpEnabled ? "是" : "否",
            a.V4.Count == 0 ? "-" : string.Join(", ", a.V4),
            a.Gateways.Count == 0 ? "-" : string.Join(", ", a.Gateways),
            a.Description,
        }).ToList();

        var widths = new int[headers.Length];
        for (int column = 0; column < headers.Length; column++) {
            widths[column] = TextWidth(headers[column]);
            foreach (string[] row in rows) {
                widths[column] = Math.Max(widths[column], TextWidth(row[column]));
            }
        }

        PrintRow(headers, widths);
        PrintRow(widths.Select(w => new string('-', w)).ToArray(), widths);
        foreach (string[] row in rows) {
            PrintRow(row, widths);
        }
    }

    private static void PrintRow(string[] cells, int[] widths) {
        var builder = new System.Text.StringBuilder();
        for (int i = 0; i < cells.Length; i++) {
            if (i > 0) {
                builder.Append("  ");
            }

            builder.Append(cells[i]);
            if (i < cells.Length - 1) {
                builder.Append(' ', widths[i] - TextWidth(cells[i]));
            }
        }

        ConsoleIo.WriteLine(builder.ToString());
    }

    private static int TextWidth(string text) {
        int width = 0;
        foreach (char ch in text) {
            width += IsWide(ch) ? 2 : 1;
        }

        return width;
    }

    private static bool IsWide(char ch) =>
        (ch >= 0x1100 && ch <= 0x115F) ||
        (ch >= 0x2E80 && ch <= 0xA4CF) ||
        (ch >= 0xAC00 && ch <= 0xD7A3) ||
        (ch >= 0xF900 && ch <= 0xFAFF) ||
        (ch >= 0xFE30 && ch <= 0xFE4F) ||
        (ch >= 0xFF00 && ch <= 0xFF60) ||
        (ch >= 0xFFE0 && ch <= 0xFFE6);

    private static void PrintDetail(AdapterSnapshot adapter) {
        PrintField("连接名", adapter.Name);
        PrintField("描述", adapter.Description);
        PrintField("类型", TypeText(adapter.Type));
        PrintField("MAC", string.IsNullOrEmpty(adapter.Mac) ? "-" : adapter.Mac);
        PrintField("IPv4 索引", adapter.Index >= 0 ? adapter.Index.ToString(CultureInfo.InvariantCulture) : "-");
        PrintField("状态", StatusText(adapter.Status) + SpeedText(adapter.Speed));
        PrintField("DHCP", adapter.DhcpEnabled ? "是" : "否");
        PrintField("IPv4", adapter.V4.Count == 0 ? "-" : string.Join(", ", adapter.V4));
        PrintField("网关", adapter.Gateways.Count == 0 ? "-" : string.Join(", ", adapter.Gateways));
        PrintField("DNS", adapter.Dns.Count == 0 ? "-" : string.Join(", ", adapter.Dns));
    }

    private static void PrintField(string name, string value) => ConsoleIo.WriteLine($"{Pad(name, 10)}: {value}");

    private static string Pad(string text, int width) => text + new string(' ', Math.Max(0, width - TextWidth(text)));

    private static string StatusText(OperationalStatus status) => status switch {
        OperationalStatus.Up => "已连接",
        OperationalStatus.Down => "已断开",
        OperationalStatus.NotPresent => "未插入",
        OperationalStatus.Dormant => "休眠",
        OperationalStatus.LowerLayerDown => "底层断开",
        _ => status.ToString(),
    };

    private static string SpeedText(long bitsPerSecond) {
        if (bitsPerSecond <= 0) {
            return string.Empty;
        }

        if (bitsPerSecond >= 1_000_000_000) {
            return $" ({bitsPerSecond / 1_000_000_000d:0.##} Gbps)";
        }

        if (bitsPerSecond >= 1_000_000) {
            return $" ({bitsPerSecond / 1_000_000d:0.##} Mbps)";
        }

        return $" ({bitsPerSecond / 1_000d:0.##} Kbps)";
    }

    private static string TypeText(NetworkInterfaceType type) => type switch {
        NetworkInterfaceType.Ethernet => "以太网",
        NetworkInterfaceType.Ethernet3Megabit => "以太网",
        NetworkInterfaceType.FastEthernetT => "以太网",
        NetworkInterfaceType.FastEthernetFx => "以太网",
        NetworkInterfaceType.GigabitEthernet => "以太网",
        NetworkInterfaceType.Wireless80211 => "无线",
        NetworkInterfaceType.Loopback => "回环",
        NetworkInterfaceType.Tunnel => "隧道",
        NetworkInterfaceType.Ppp => "PPP",
        _ => type.ToString(),
    };

    private static (IPAddress Address, int? Prefix) ParseAddressWithPrefix(string text) {
        int slash = text.IndexOf('/');
        if (slash < 0) {
            return (ParseIpv4(text), null);
        }

        return (ParseIpv4(text[..slash]), ParsePrefixOrMask(text[(slash + 1)..]));
    }

    private static IPAddress ParseIpv4(string text) {
        if (!IPAddress.TryParse(text.Trim(), out IPAddress? address) || address.AddressFamily != AddressFamily.InterNetwork) {
            throw new UsageException($"不是合法的 IPv4 地址: {text}");
        }

        return address;
    }

    private static int ParsePrefixOrMask(string text) {
        string value = text.Trim();
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int prefix)) {
            if (prefix < 0 || prefix > 32) {
                throw new UsageException($"掩码前缀需要 0-32: {text}");
            }

            return prefix;
        }

        int fromMask = Adapters.MaskToPrefix(ParseIpv4(value));
        if (fromMask < 0) {
            throw new UsageException($"非法的子网掩码（二进制位不连续）: {text}");
        }

        return fromMask;
    }

    private static int? ParseOptionalMetric(string? text) {
        if (text is null) {
            return null;
        }

        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int metric) || metric < 0 || metric > 9999) {
            throw new UsageException($"--gw-metric 需要 0-9999 的整数: {text}");
        }

        return metric;
    }

    private static List<IPAddress>? ParseOptionalDns(string? text) {
        if (text is null) {
            return null;
        }

        if (text.Equals("none", StringComparison.OrdinalIgnoreCase)) {
            return new List<IPAddress>();
        }

        var servers = new List<IPAddress>();
        foreach (string part in text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)) {
            IPAddress server = ParseIpv4(part);
            if (!servers.Contains(server)) {
                servers.Add(server);
            }
        }

        if (servers.Count == 0) {
            throw new UsageException("--dns 取值无效。");
        }

        if (servers.Count > 4) {
            throw new UsageException("--dns 最多支持 4 个地址。");
        }

        return servers;
    }

    private static int ParseGatewayCheckSeconds(string? text) {
        const int maxSeconds = 600;
        if (text is null) {
            return DefaultGatewayCheckSeconds;
        }

        if (text.Equals("off", StringComparison.OrdinalIgnoreCase)) {
            return 0;
        }

        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seconds) || seconds < 0 || seconds > maxSeconds) {
            throw new UsageException($"--gw-check 需要 0-{maxSeconds} 的整数秒，或 off: {text}");
        }

        return seconds;
    }

    /// <summary>返回 null 表示跳过检查（无网关 / 已关闭 / 网卡未连接）；否则表示网关是否可达。</summary>
    private static bool? ProbeGateway(string adapterId, IPAddress? gateway, int seconds) {
        if (gateway is null || seconds <= 0) {
            return null;
        }

        AdapterSnapshot? live = Adapters.Enumerate(false).FirstOrDefault(a => a.Id == adapterId);
        if (live is null || !live.Up) {
            ConsoleIo.WriteLine("网卡当前未连接，跳过网关连通性检查。");
            return null;
        }

        ConsoleIo.WriteLine($"检查网关 {gateway} 连通性：最长 {seconds} 秒（ICMP 或 ARP 任一可达即通过）…");
        bool reachable = WaitForGateway(gateway, TimeSpan.FromSeconds(seconds));
        ConsoleIo.WriteLine(reachable ? "网关可达。" : $"网关 {gateway} 连续 {seconds} 秒不可达。");
        return reachable;
    }

    private static bool WaitForGateway(IPAddress gateway, TimeSpan timeout) {
        long deadline = Environment.TickCount64 + (long)timeout.TotalMilliseconds;
        while (true) {
            if (GatewayProbe.IsReachable(gateway)) {
                return true;
            }

            if (Environment.TickCount64 >= deadline) {
                return false;
            }

            Thread.Sleep(1000);
        }
    }

    private static void PrintUsage() {
        string[] lines =
        {
            "SetIPAddress - 更改指定网卡的 IPv4 配置（兼容 Windows Server 2008 R2）",
            string.Empty,
            "用法:",
            "  SetIPAddress list [--all]",
            "  SetIPAddress show  <选择器>",
            "  SetIPAddress set   <选择器> --ip <IPv4[/前缀]> [--mask <掩码|前缀>] [--gw <IPv4|none>]",
            "                            [--dns <IPv4[,IPv4...]|none>] [--gw-metric <0-9999>]",
            "                            [--gw-check <秒|off>] [--force]",
            "  SetIPAddress dhcp  <选择器> [--gw-check <秒|off>] [--force]",
            string.Empty,
            "选择器（可组合，多个条件为“与”）：",
            "  --name  <连接名>   例如 \"本地连接\"",
            "  --desc  <关键字>   网卡描述的子串",
            "  --mac   <MAC>      AA-BB-CC-DD-EE-FF 或 AA:BB:CC:DD:EE:FF",
            "  --index <ifIndex>  IPv4 接口索引（见 list 的“索引”列）",
            string.Empty,
            "说明:",
            "  * 不指定选择器时，仅当恰好有一个已连接网卡才会自动选中。",
            "  * 未提供掩码时沿用网卡当前前缀；当前无 IPv4 则按 /24。",
            "  * 未提供 --gw 表示保持网卡当前网关不变；--gw none 表示删除网关。",
            "  * 保持网关时会回填原网关（netsh 省略 gateway 参数会清空网关）；多网关只保留第一个，原跃点数不保留（可显式 --gw-metric）。",
            "  * 未提供 --dns 表示不修改 DNS；--dns none 表示清空。",
            "  * 目标网卡持有网关时必须加 --force，避免误断远程连接。",
            "  * 更改后按网卡回读校验；校验失败会按快照回滚。",
            "  * 更改后探测网关：默认观测 15 秒，期间 ICMP 或 ARP 任一次可达即通过；",
            "    持续不可达则回滚（退出码 7）。--gw-check off 关闭，--gw-check <秒> 调整窗口。",
            "  * 媒体断开的网卡会以“备用配置”保存静态地址，此时 DHCP 标志可能仍显示“是”。",
            string.Empty,
            "退出码: 0 成功 / 1 参数错误 / 2 网卡定位失败 / 3 权限不足 /",
            "        4 netsh 执行失败 / 5 校验失败（已回滚）/ 6 风险拒绝执行 /",
            "        7 网关不可达（已回滚）/ 9 未预期错误",
        };

        foreach (string line in lines) {
            ConsoleIo.WriteLine(line);
        }
    }

    private sealed class Options {
        private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _flags = new(StringComparer.OrdinalIgnoreCase);

        public static Options Parse(string[] args, string[] valueKeys, string[] flagKeys) {
            var options = new Options();

            for (int i = 1; i < args.Length; i++) {
                string token = args[i];
                if (!token.StartsWith("--", StringComparison.Ordinal)) {
                    throw new UsageException($"无法识别的参数: {token}");
                }

                int separator = token.IndexOf('=');
                if (separator > 0) {
                    string key = token[..separator];
                    if (!valueKeys.Any(k => k.Equals(key, StringComparison.OrdinalIgnoreCase))) {
                        throw new UsageException($"参数 {key} 不接受取值或不存在。");
                    }

                    options._values[key] = token[(separator + 1)..];
                    continue;
                }

                if (flagKeys.Any(k => k.Equals(token, StringComparison.OrdinalIgnoreCase))) {
                    options._flags.Add(token);
                    continue;
                }

                if (!valueKeys.Any(k => k.Equals(token, StringComparison.OrdinalIgnoreCase))) {
                    throw new UsageException($"未知参数: {token}");
                }

                if (i + 1 >= args.Length) {
                    throw new UsageException($"参数 {token} 缺少取值。");
                }

                options._values[token] = args[++i];
            }

            return options;
        }

        public bool Has(string flag) => _flags.Contains(flag);

        public string? Get(string key) => _values.TryGetValue(key, out string? value) ? value : null;
    }
}

internal sealed class UsageException : Exception {
    public UsageException(string message)
        : base(message) {
    }
}

internal sealed class AdapterException : Exception {
    public AdapterException(string message)
        : base(message) {
    }
}

internal sealed class ElevationRequiredException : Exception {
    public ElevationRequiredException()
        : base("需要管理员权限：请以“管理员身份运行”本程序。") {
    }
}
