using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SYSi.Services.HardwareService;

public sealed partial class HardwareService
{
    public List<NetworkAdapterInfo> GetNetworkInfo()
    {
        var adapters = new List<NetworkAdapterInfo>();

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ShouldExclude(nic))
            {
                continue;
            }
            
            adapters.Add(BuildAdapterInfo(nic));
        }

        return adapters;
    }

    private static bool IsPhysicalAdapter(NetworkInterface nic)
    {
        string regKey = $@"SYSTEM\CurrentControlSet\Control\Network\{{4D36E972-E325-11CE-BFC1-08002BE10318}}\{nic.Id}\Connection";
        using var rk = Registry.LocalMachine.OpenSubKey(regKey, false);

        if (rk == null)
        {
            return false;
        }

        string pnpId = rk.GetValue("PnpInstanceID", "")?.ToString() ?? "";

        if (pnpId.StartsWith("PCI", StringComparison.OrdinalIgnoreCase) ||
            pnpId.StartsWith("USB", StringComparison.OrdinalIgnoreCase) ||
            pnpId.StartsWith("BTH", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (pnpId.StartsWith("ROOT\\", StringComparison.OrdinalIgnoreCase) ||
            pnpId.StartsWith("SWD\\MSRRAS\\", StringComparison.OrdinalIgnoreCase) ||
            pnpId.Contains("vwifimp_wfd", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrEmpty(pnpId))
        {
            return nic.Name.StartsWith("vEthernet", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool ShouldExclude(NetworkInterface nic)
    {
        return nic.NetworkInterfaceType is NetworkInterfaceType.Loopback
                                     or NetworkInterfaceType.Tunnel
            ? true
            : !IsPhysicalAdapter(nic);
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
