namespace SYSi.Models
{
    public partial class GpuInfo : ObservableObject
    {
        [ObservableProperty]
        public string _name = string.Empty;

        [ObservableProperty]
        public string _manufacturer = string.Empty;

        [ObservableProperty]
        public string _vramText = string.Empty;

        [ObservableProperty]
        public string _driverVersion = string.Empty;

        [ObservableProperty]
        public string _driverDate = string.Empty;

        [ObservableProperty]
        public string _videoProcessor = string.Empty;

        [ObservableProperty]
        public string _resolution = string.Empty;

        [ObservableProperty]
        public string _refreshRate = string.Empty;

        [ObservableProperty]
        public string _bitsPerPixel = string.Empty;

        [ObservableProperty]
        public string _videoArchitecture = string.Empty;

        [ObservableProperty]
        public string _videoMemoryType = string.Empty;

        [ObservableProperty]
        public string _pnpDeviceId = string.Empty;

        [ObservableProperty]
        public double _usagePercent = 0;
    }
}
