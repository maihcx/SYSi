namespace SYSi.Models
{
    public partial class NetworkAdapterInfo : ObservableObject
    {
        [ObservableProperty]
        public string _name = string.Empty;

        [ObservableProperty]
        public string _macAddress = string.Empty;

        [ObservableProperty]
        public string _ipAddress = string.Empty;

        [ObservableProperty]
        public string _ipv6Address = string.Empty;

        [ObservableProperty]
        public string _subnetMask = string.Empty;

        [ObservableProperty]
        public string _gateway = string.Empty;

        [ObservableProperty]
        public string _dnsServers = string.Empty;

        [ObservableProperty]
        public string _linkSpeed = string.Empty;

        [ObservableProperty]
        public bool _isConnected = false;

        [ObservableProperty]
        public string _adapterType = string.Empty;
    }
}
