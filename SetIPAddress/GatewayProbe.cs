using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace SetIPAddress;

/// <summary>
/// 网关连通性判定：ICMP 回显成功，或 ARP 能解析出网关 MAC（二层可达），任一成立即视为可达。
/// 很多网关/防火墙屏蔽 ICMP，若只看 ping 会把正确配置误判为不可达并回滚。
/// </summary>
internal static class GatewayProbe
{
    private const int PingTimeoutMilliseconds = 1000;

    public static bool IsReachable(IPAddress gateway)
    {
        if (gateway.AddressFamily != AddressFamily.InterNetwork)
        {
            return true;
        }

        try
        {
            using var ping = new Ping();
            PingReply reply = ping.Send(gateway, PingTimeoutMilliseconds);
            if (reply.Status == IPStatus.Success)
            {
                return true;
            }
        }
        catch (Exception)
        {
            // ICMP 不可用时继续用 ARP 判定
        }

        return TryArp(gateway);
    }

    private static bool TryArp(IPAddress gateway)
    {
        try
        {
            byte[] macAddress = new byte[6];
            uint length = (uint)macAddress.Length;
            uint destination = BitConverter.ToUInt32(gateway.GetAddressBytes(), 0);
            int result = SendARP(destination, 0, macAddress, ref length);
            return result == 0 && length > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern int SendARP(
        uint destinationIp,
        uint sourceIp,
        byte[] physicalAddress,
        ref uint physicalAddressLength);
}
