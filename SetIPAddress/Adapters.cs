using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SetIPAddress;

internal sealed class Ipv4Entry {
    public IPAddress Address { get; set; } = IPAddress.None;

    public int Prefix { get; set; } = -1;

    public override string ToString() => Prefix >= 0 ? $"{Address}/{Prefix}" : Address.ToString();
}

internal sealed class AdapterSnapshot {
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Mac { get; set; } = string.Empty;

    public int Index { get; set; } = -1;

    public OperationalStatus Status { get; set; }

    public bool Up => Status == OperationalStatus.Up;

    public long Speed { get; set; }

    public NetworkInterfaceType Type { get; set; }

    public bool DhcpEnabled { get; set; }

    public List<Ipv4Entry> V4 { get; } = new();

    public List<IPAddress> Gateways { get; } = new();

    public List<IPAddress> Dns { get; } = new();

    public Ipv4Entry? Primary => V4.Count > 0 ? V4[0] : null;
}

/// <summary>网卡定位条件，多个条件之间为“与”关系。</summary>
internal sealed class Selector {
    public string? Name { get; init; }

    public string? Description { get; init; }

    public string? Mac { get; init; }

    public int? Index { get; init; }

    public bool IsEmpty => Name is null && Description is null && Mac is null && Index is null;

    public bool Matches(AdapterSnapshot adapter) {
        if (Name is not null && !string.Equals(adapter.Name, Name, StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        if (Description is not null && adapter.Description.IndexOf(Description, StringComparison.OrdinalIgnoreCase) < 0) {
            return false;
        }

        if (Mac is not null && !string.Equals(NormalizeMac(adapter.Mac), NormalizeMac(Mac), StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        if (Index is not null && adapter.Index != Index.Value) {
            return false;
        }

        return true;
    }

    private static string NormalizeMac(string mac) =>
        mac.Replace(":", string.Empty).Replace("-", string.Empty).Replace(".", string.Empty).Replace(" ", string.Empty);
}

internal static class Adapters {
    /// <summary>枚举网卡。<paramref name="includeSpecial"/> 为 true 时包含回环与隧道网卡。</summary>
    public static List<AdapterSnapshot> Enumerate(bool includeSpecial) {
        var result = new List<AdapterSnapshot>();

        foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces()) {
            if (!includeSpecial &&
                (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                 nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel)) {
                continue;
            }

            var adapter = new AdapterSnapshot {
                Id = nic.Id,
                Name = nic.Name,
                Description = nic.Description,
                Mac = FormatMac(nic.GetPhysicalAddress()),
                Status = nic.OperationalStatus,
                Speed = nic.Speed,
                Type = nic.NetworkInterfaceType,
            };

            try {
                ReadProperties(nic, adapter);
            } catch (NetworkInformationException) {
                // 网卡属性不可读（例如已禁用），保留基础信息用于展示
            }

            result.Add(adapter);
        }

        return result;
    }

    private static void ReadProperties(NetworkInterface nic, AdapterSnapshot adapter) {
        IPInterfaceProperties properties = nic.GetIPProperties();

        IPv4InterfaceProperties? v4 = null;
        try {
            v4 = properties.GetIPv4Properties();
        } catch (NetworkInformationException) {
            // 没有 IPv4 绑定
        } catch (PlatformNotSupportedException) {
        }

        adapter.Index = v4?.Index ?? -1;
        adapter.DhcpEnabled = v4?.IsDhcpEnabled ?? false;

        foreach (UnicastIPAddressInformation unicast in properties.UnicastAddresses) {
            if (unicast.Address.AddressFamily != AddressFamily.InterNetwork) {
                continue;
            }

            int prefix = -1;
            try {
                prefix = unicast.PrefixLength;
            } catch (NotImplementedException) {
            }

            if (prefix < 0) {
                prefix = MaskToPrefix(unicast.IPv4Mask);
            }

            adapter.V4.Add(new Ipv4Entry { Address = unicast.Address, Prefix = prefix });
        }

        foreach (GatewayIPAddressInformation gateway in properties.GatewayAddresses) {
            if (gateway.Address.AddressFamily == AddressFamily.InterNetwork) {
                adapter.Gateways.Add(gateway.Address);
            }
        }

        foreach (IPAddress dns in properties.DnsAddresses) {
            if (dns.AddressFamily == AddressFamily.InterNetwork) {
                adapter.Dns.Add(dns);
            }
        }
    }

    public static int MaskToPrefix(IPAddress? mask) {
        if (mask is null || mask.AddressFamily != AddressFamily.InterNetwork) {
            return -1;
        }

        byte[] bytes = mask.GetAddressBytes();
        int prefix = 0;
        bool sawZero = false;

        foreach (byte b in bytes) {
            for (int bit = 7; bit >= 0; bit--) {
                if ((b & (1 << bit)) != 0) {
                    if (sawZero) {
                        return -1; // 非连续掩码
                    }

                    prefix++;
                } else {
                    sawZero = true;
                }
            }
        }

        return prefix;
    }

    public static IPAddress PrefixToMask(int prefix) {
        if (prefix < 0 || prefix > 32) {
            throw new ArgumentOutOfRangeException(nameof(prefix));
        }

        uint value = prefix == 0 ? 0u : uint.MaxValue << (32 - prefix);
        return new IPAddress(new[]
        {
            (byte)(value >> 24),
            (byte)(value >> 16),
            (byte)(value >> 8),
            (byte)value,
        });
    }

    private static string FormatMac(PhysicalAddress address) {
        byte[] bytes = address.GetAddressBytes();
        if (bytes.Length == 0) {
            return string.Empty;
        }

        return string.Join("-", bytes.Select(b => b.ToString("X2")));
    }
}
