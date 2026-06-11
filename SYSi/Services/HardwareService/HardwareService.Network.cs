using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SYSi.Services.HardwareService;

public sealed partial class HardwareService
{
    public List<NetworkAdapterInfo> GetNetworkInfo()
    {
        var adapters = new List<NetworkAdapterInfo>();
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }

                if (nic.OperationalStatus    == OperationalStatus.Unknown)
                {
                    continue;
                }

                adapters.Add(BuildAdapterInfo(nic));
            }
        }
        catch { }
        return adapters;
    }

    private static NetworkAdapterInfo BuildAdapterInfo(NetworkInterface nic)
    {
        var props = nic.GetIPProperties();

        var info = new NetworkAdapterInfo
        {
            Name        = nic.Name,
            AdapterType = nic.NetworkInterfaceType.ToString(),
            MacAddress  = FormatMac(nic.GetPhysicalAddress().ToString()),
            IsConnected = nic.OperationalStatus == OperationalStatus.Up,
            LinkSpeed   = nic.Speed > 0 ? FormatNetworkSpeed(nic.Speed) : "N/A",
        };

        foreach (var ua in props.UnicastAddresses)
        {
            if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
            {
                info.IpAddress  = ua.Address.ToString();
                info.SubnetMask = ua.IPv4Mask.ToString();
            }
            else if (ua.Address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                info.Ipv6Address = ua.Address.ToString();
            }
        }

        info.Gateway    = props.GatewayAddresses.FirstOrDefault()?.Address.ToString() ?? string.Empty;
        info.DnsServers = string.Join(", ", props.DnsAddresses.Select(a => a.ToString()));

        return info;
    }
}
