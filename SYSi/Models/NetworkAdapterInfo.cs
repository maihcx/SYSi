using System;
using System.Collections.Generic;
using System.Text;

namespace SYSi.Models
{
    public class NetworkAdapterInfo
    {
        public string Name { get; set; } = string.Empty;
        public string MacAddress { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string Ipv6Address { get; set; } = string.Empty;
        public string SubnetMask { get; set; } = string.Empty;
        public string Gateway { get; set; } = string.Empty;
        public string DnsServers { get; set; } = string.Empty;
        public string LinkSpeed { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public string AdapterType { get; set; } = string.Empty;
    }
}
